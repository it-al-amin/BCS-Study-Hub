using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BCSStudyHub.Api.Authorization;
using BCSStudyHub.Application.Admin;
using BCSStudyHub.Application.Authorization;
using BCSStudyHub.Application.CurrentAffairs;
using BCSStudyHub.Application.Library;
using BCSStudyHub.Application.Mcq;
using BCSStudyHub.Application.Viva;
using BCSStudyHub.Application.Written;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BCSStudyHub.Api.Controllers;

internal static class AdminHttp
{
    public static Guid UserId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new AdminException(StatusCodes.Status401Unauthorized, "Authentication required", "A valid user identity is required.");

    public static IReadOnlySet<string> Permissions(ClaimsPrincipal user) =>
        user.FindAll(PermissionPolicies.ClaimType).Select(claim => claim.Value).ToHashSet(StringComparer.Ordinal);
}

/// <summary>Public, non-sensitive site configuration (name, active announcement, registration state).</summary>
[ApiController]
[Route("api/v1/site-settings")]
[AllowAnonymous]
public sealed class SiteSettingsController(ISiteSettingsService settings) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) => Ok(await settings.GetPublicAsync(cancellationToken));
}

[ApiController]
[Route("api/v1/admin/settings")]
[Authorize(Policy = PermissionCodes.SettingsManage)]
public sealed class AdminSettingsController(ISiteSettingsService settings) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) => Ok(await settings.GetAsync(cancellationToken));

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] SiteSettingsInput input, CancellationToken cancellationToken) =>
        Ok(await settings.UpdateAsync(input, AdminHttp.UserId(User), cancellationToken));
}

[ApiController]
[Route("api/v1/admin/audit")]
[Authorize(Policy = PermissionCodes.AuditRead)]
public sealed class AdminAuditController(IAuditQueryService audit) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? entityType,
        [FromQuery] string? entityId,
        [FromQuery] Guid? actorId,
        [FromQuery] string? action,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await audit.GetAsync(new AuditQuery(entityType, entityId, actorId, action, from, to, pageNumber, pageSize), cancellationToken));

    [HttpGet("entity-types")]
    public async Task<IActionResult> GetEntityTypes(CancellationToken cancellationToken) => Ok(await audit.GetEntityTypesAsync(cancellationToken));
}

/// <summary>
/// Cross-module console endpoints. The class policy admits content editors; each operation then narrows to the
/// modules the caller's own permission claims cover, so an MCQ editor cannot touch library tags or publish Viva items.
/// </summary>
[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = PermissionCodes.ContentManage)]
public sealed class AdminConsoleController(
    IAdminOverviewService overview,
    ITagAdminService tags,
    IMcqService mcq,
    IWrittenService written,
    IVivaService viva,
    ILibraryService library,
    ICurrentAffairsService currentAffairs,
    IOfficialSourceService officialSources,
    ApplicationDbContext dbContext) : ControllerBase
{
    public static IReadOnlyDictionary<string, string> BulkModulePermissions { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["McqQuestions"] = PermissionCodes.McqManage,
        ["McqExamSets"] = PermissionCodes.McqManage,
        ["WrittenQuestions"] = PermissionCodes.WrittenManage,
        ["VivaQuestions"] = PermissionCodes.VivaManage,
        ["LibraryResources"] = PermissionCodes.LibraryManage,
        ["CurrentAffairs"] = PermissionCodes.CurrentAffairsManage,
        ["OfficialSources"] = PermissionCodes.CurrentAffairsManage,
    };

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken) =>
        Ok(await overview.GetAsync(AdminHttp.Permissions(User), cancellationToken));

    [HttpGet("tags")]
    public async Task<IActionResult> GetTags([FromQuery] string? module, [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(await tags.GetAsync(TagModules.Allowed(AdminHttp.Permissions(User), module), search, cancellationToken));

    [HttpPost("tags/rename")]
    public async Task<IActionResult> RenameTag([FromBody] TagRenameInput input, CancellationToken cancellationToken) =>
        Ok(await tags.RenameAsync(TagModules.Allowed(AdminHttp.Permissions(User), input.Module), input, AdminHttp.UserId(User), cancellationToken));

    [HttpPost("tags/delete")]
    public async Task<IActionResult> DeleteTag([FromBody] TagDeleteInput input, CancellationToken cancellationToken) =>
        Ok(await tags.DeleteAsync(TagModules.Allowed(AdminHttp.Permissions(User), input.Module), input, AdminHttp.UserId(User), cancellationToken));

    /// <summary>
    /// Publishes or archives up to 100 items through each module's own service, so every item gets the same
    /// validation (verified sources, rights basis, etc.) as a single action. Items are independent: one failure does
    /// not roll back the others, and each result is reported.
    /// </summary>
    [HttpPost("bulk/status")]
    public async Task<IActionResult> BulkStatus([FromBody] BulkStatusInput input, CancellationToken cancellationToken)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), results, validateAllProperties: true))
        {
            throw new AdminException(StatusCodes.Status400BadRequest, "Invalid request", results[0].ErrorMessage ?? "The request is not valid.");
        }

        if (!BulkModulePermissions.TryGetValue(input.Module, out var permission))
        {
            throw new AdminException(StatusCodes.Status400BadRequest, "Invalid request", "The bulk module is not supported.");
        }

        if (!AdminHttp.Permissions(User).Contains(permission))
        {
            throw new AdminException(StatusCodes.Status403Forbidden, "Not permitted", "You do not manage content in this module.");
        }

        var status = input.Action.Equals("publish", StringComparison.OrdinalIgnoreCase) ? "Published" : "Archived";
        var actor = AdminHttp.UserId(User);
        var items = new List<BulkItemResultDto>();
        foreach (var id in input.Ids)
        {
            try
            {
                var found = input.Module.ToLowerInvariant() switch
                {
                    "mcqquestions" => await mcq.SetQuestionStatusAsync(id, status, actor, cancellationToken) is not null,
                    "mcqexamsets" => await mcq.SetExamSetStatusAsync(id, status, actor, cancellationToken) is not null,
                    "writtenquestions" => await written.SetQuestionStatusAsync(id, status, actor, cancellationToken) is not null,
                    "vivaquestions" => await viva.SetQuestionStatusAsync(id, status, actor, cancellationToken) is not null,
                    "libraryresources" => await library.SetStatusAsync(id, status, new LibraryActor(actor, LibraryManagementScope.Shared), cancellationToken) is not null,
                    "currentaffairs" => await currentAffairs.SetStatusAsync(id, status, actor, cancellationToken) is not null,
                    "officialsources" => await officialSources.SetStatusAsync(id, status, actor, cancellationToken) is not null,
                    _ => false,
                };
                items.Add(new BulkItemResultDto(id, found, found ? null : "Not found."));
            }
            catch (Exception exception) when (exception is McqException or WrittenException or VivaException or LibraryException or CurrentAffairsException)
            {
                items.Add(new BulkItemResultDto(id, false, exception.Message));
            }
            finally
            {
                // A failed item may leave modified entities tracked; clearing keeps them out of the next item's save.
                dbContext.ChangeTracker.Clear();
            }
        }

        return Ok(new BulkResultDto(input.Module, status, items.Count(item => item.Succeeded), items.Count(item => !item.Succeeded), items));
    }
}
