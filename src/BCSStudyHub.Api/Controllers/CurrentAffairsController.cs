using System.Security.Claims;
using BCSStudyHub.Application.Authorization;
using BCSStudyHub.Application.CurrentAffairs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BCSStudyHub.Api.Controllers;

/// <summary>Published current affairs. Browsing is anonymous; signed-in users also get their bookmark/note/revision flags.</summary>
[ApiController]
[Route("api/v1/current-affairs")]
[AllowAnonymous]
public sealed class CurrentAffairsController(ICurrentAffairsService service) : ControllerBase
{
    [HttpGet("entries")]
    public async Task<IActionResult> GetEntries(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? tag,
        [FromQuery] Guid? subjectId,
        [FromQuery] Guid? syllabusItemId,
        [FromQuery] bool bookmarkedOnly = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "recent",
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetEntriesAsync(
            new CurrentAffairsQuery(search, category, from, to, tag, subjectId, syllabusItemId, bookmarkedOnly, pageNumber, pageSize, sortBy),
            CurrentAffairsHttp.TryGetUserId(User),
            cancellationToken));

    [HttpGet("entries/{entryId:guid}")]
    public async Task<IActionResult> GetEntry(Guid entryId, CancellationToken cancellationToken)
    {
        var entry = await service.GetEntryAsync(entryId, CurrentAffairsHttp.TryGetUserId(User), cancellationToken);
        return entry is null ? NotFound() : Ok(entry);
    }

    /// <summary>Daily, weekly (ISO week), monthly, or yearly collection containing <paramref name="date"/> (default: today in Bangladesh).</summary>
    [HttpGet("digest")]
    public async Task<IActionResult> GetDigest(
        [FromQuery] string period = "Day",
        [FromQuery] DateOnly? date = null,
        [FromQuery] string? category = null,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetDigestAsync(period, date, category, CurrentAffairsHttp.TryGetUserId(User), cancellationToken));

    [HttpGet("archive")]
    public async Task<IActionResult> GetArchive(CancellationToken cancellationToken) =>
        Ok(await service.GetArchiveAsync(cancellationToken));

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken) =>
        Ok(await service.GetCategoriesAsync(cancellationToken));

    [HttpGet("tags")]
    public async Task<IActionResult> GetTags(CancellationToken cancellationToken) =>
        Ok(await service.GetTagsAsync(cancellationToken));
}

[ApiController]
[Route("api/v1/me/current-affairs")]
[Authorize]
public sealed class CurrentAffairsPersonalController(ICurrentAffairsService service) : ControllerBase
{
    private Guid UserId => CurrentAffairsHttp.GetUserId(User);

    [HttpGet("bookmarks")]
    public async Task<IActionResult> GetBookmarks([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        Ok(await service.GetBookmarksAsync(UserId, pageNumber, pageSize, cancellationToken));

    [HttpPost("entries/{entryId:guid}/bookmark")]
    public async Task<IActionResult> AddBookmark(Guid entryId, CancellationToken cancellationToken) =>
        await service.AddBookmarkAsync(UserId, entryId, cancellationToken) ? NoContent() : NotFound();

    [HttpDelete("entries/{entryId:guid}/bookmark")]
    public async Task<IActionResult> RemoveBookmark(Guid entryId, CancellationToken cancellationToken) =>
        await service.RemoveBookmarkAsync(UserId, entryId, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("notes")]
    public async Task<IActionResult> GetNotes([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        Ok(await service.GetNotesAsync(UserId, pageNumber, pageSize, cancellationToken));

    [HttpPut("entries/{entryId:guid}/note")]
    public async Task<IActionResult> SaveNote(Guid entryId, [FromBody] CurrentAffairsNoteInput input, CancellationToken cancellationToken)
    {
        var note = await service.SaveNoteAsync(UserId, entryId, input, cancellationToken);
        return note is null ? NotFound() : Ok(note);
    }

    [HttpDelete("entries/{entryId:guid}/note")]
    public async Task<IActionResult> DeleteNote(Guid entryId, CancellationToken cancellationToken) =>
        await service.DeleteNoteAsync(UserId, entryId, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("revision")]
    public async Task<IActionResult> GetRevisions(
        [FromQuery] bool dueOnly = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetRevisionsAsync(UserId, dueOnly, pageNumber, pageSize, cancellationToken));

    [HttpPost("entries/{entryId:guid}/revision")]
    public async Task<IActionResult> AddRevision(Guid entryId, CancellationToken cancellationToken)
    {
        var revision = await service.AddRevisionAsync(UserId, entryId, cancellationToken);
        return revision is null ? NotFound() : Ok(revision);
    }

    [HttpPost("entries/{entryId:guid}/revision/review")]
    public async Task<IActionResult> Review(Guid entryId, [FromBody] CurrentAffairsReviewInput input, CancellationToken cancellationToken)
    {
        var revision = await service.ReviewAsync(UserId, entryId, input, cancellationToken);
        return revision is null ? NotFound() : Ok(revision);
    }

    [HttpDelete("entries/{entryId:guid}/revision")]
    public async Task<IActionResult> RemoveRevision(Guid entryId, CancellationToken cancellationToken) =>
        await service.RemoveRevisionAsync(UserId, entryId, cancellationToken) ? NoContent() : NotFound();
}

[ApiController]
[Route("api/v1/admin/current-affairs")]
[Authorize(Policy = PermissionCodes.CurrentAffairsManage)]
public sealed class AdminCurrentAffairsController(ICurrentAffairsService service) : ControllerBase
{
    private Guid UserId => CurrentAffairsHttp.GetUserId(User);

    [HttpGet("entries")]
    public async Task<IActionResult> GetEntries(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? category,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetAdminEntriesAsync(search, status, category, pageNumber, pageSize, cancellationToken));

    [HttpGet("entries/{entryId:guid}")]
    public async Task<IActionResult> GetEntry(Guid entryId, CancellationToken cancellationToken)
    {
        var entry = await service.GetAdminEntryAsync(entryId, cancellationToken);
        return entry is null ? NotFound() : Ok(entry);
    }

    [HttpPost("entries")]
    public async Task<IActionResult> CreateEntry([FromBody] CurrentAffairsEntryInput input, CancellationToken cancellationToken)
    {
        var entry = await service.CreateEntryAsync(input, UserId, cancellationToken);
        return CreatedAtAction(nameof(GetEntry), new { entryId = entry.Entry.Id }, entry);
    }

    [HttpPut("entries/{entryId:guid}")]
    public async Task<IActionResult> UpdateEntry(Guid entryId, [FromBody] CurrentAffairsEntryInput input, CancellationToken cancellationToken)
    {
        var entry = await service.UpdateEntryAsync(entryId, input, UserId, cancellationToken);
        return entry is null ? NotFound() : Ok(entry);
    }

    [HttpPost("entries/{entryId:guid}/publish")]
    public Task<IActionResult> Publish(Guid entryId, CancellationToken cancellationToken) => SetStatus(entryId, "Published", cancellationToken);

    [HttpPost("entries/{entryId:guid}/archive")]
    public Task<IActionResult> Archive(Guid entryId, CancellationToken cancellationToken) => SetStatus(entryId, "Archived", cancellationToken);

    [HttpDelete("entries/{entryId:guid}")]
    public async Task<IActionResult> DeleteEntry(Guid entryId, CancellationToken cancellationToken) =>
        await service.DeleteEntryAsync(entryId, UserId, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("question-search")]
    public async Task<IActionResult> SearchQuestions([FromQuery] string? type, [FromQuery] string search = "", CancellationToken cancellationToken = default) =>
        Ok(await service.SearchQuestionsAsync(type, search, cancellationToken));

    [HttpGet("resource-search")]
    public async Task<IActionResult> SearchResources([FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(await service.SearchLibraryResourcesAsync(search, cancellationToken));

    private async Task<IActionResult> SetStatus(Guid entryId, string status, CancellationToken cancellationToken)
    {
        var entry = await service.SetStatusAsync(entryId, status, UserId, cancellationToken);
        return entry is null ? NotFound() : Ok(entry);
    }
}

[ApiController]
[Route("api/v1/official-sources")]
[AllowAnonymous]
public sealed class OfficialSourcesController(IOfficialSourceService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSources([FromQuery] string? search, [FromQuery] string? category, CancellationToken cancellationToken) =>
        Ok(await service.GetPublishedAsync(search, category, cancellationToken));

    [HttpGet("{sourceId:guid}")]
    public async Task<IActionResult> GetSource(Guid sourceId, CancellationToken cancellationToken)
    {
        var source = await service.GetPublishedAsync(sourceId, cancellationToken);
        return source is null ? NotFound() : Ok(source);
    }
}

[ApiController]
[Route("api/v1/admin/official-sources")]
[Authorize(Policy = PermissionCodes.CurrentAffairsManage)]
public sealed class AdminOfficialSourcesController(IOfficialSourceService service) : ControllerBase
{
    private Guid UserId => CurrentAffairsHttp.GetUserId(User);

    [HttpGet]
    public async Task<IActionResult> GetSources(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? verificationStatus,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetAdminSourcesAsync(search, status, verificationStatus, pageNumber, pageSize, cancellationToken));

    [HttpGet("{sourceId:guid}")]
    public async Task<IActionResult> GetSource(Guid sourceId, CancellationToken cancellationToken)
    {
        var source = await service.GetAdminSourceAsync(sourceId, cancellationToken);
        return source is null ? NotFound() : Ok(source);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSource([FromBody] OfficialSourceInput input, CancellationToken cancellationToken)
    {
        var source = await service.CreateAsync(input, UserId, cancellationToken);
        return CreatedAtAction(nameof(GetSource), new { sourceId = source.Id }, source);
    }

    [HttpPut("{sourceId:guid}")]
    public async Task<IActionResult> UpdateSource(Guid sourceId, [FromBody] OfficialSourceInput input, CancellationToken cancellationToken)
    {
        var source = await service.UpdateAsync(sourceId, input, UserId, cancellationToken);
        return source is null ? NotFound() : Ok(source);
    }

    /// <summary>Records the outcome of a manual, in-browser check of the URL. The server never fetches the site.</summary>
    [HttpPost("{sourceId:guid}/verification")]
    public async Task<IActionResult> RecordVerification(Guid sourceId, [FromBody] OfficialSourceVerificationInput input, CancellationToken cancellationToken)
    {
        var source = await service.RecordVerificationAsync(sourceId, input, UserId, cancellationToken);
        return source is null ? NotFound() : Ok(source);
    }

    [HttpPost("{sourceId:guid}/publish")]
    public Task<IActionResult> Publish(Guid sourceId, CancellationToken cancellationToken) => SetStatus(sourceId, "Published", cancellationToken);

    [HttpPost("{sourceId:guid}/archive")]
    public Task<IActionResult> Archive(Guid sourceId, CancellationToken cancellationToken) => SetStatus(sourceId, "Archived", cancellationToken);

    [HttpDelete("{sourceId:guid}")]
    public async Task<IActionResult> DeleteSource(Guid sourceId, CancellationToken cancellationToken) =>
        await service.DeleteAsync(sourceId, UserId, cancellationToken) ? NoContent() : NotFound();

    private async Task<IActionResult> SetStatus(Guid sourceId, string status, CancellationToken cancellationToken)
    {
        var source = await service.SetStatusAsync(sourceId, status, UserId, cancellationToken);
        return source is null ? NotFound() : Ok(source);
    }
}

internal static class CurrentAffairsHttp
{
    public static Guid? TryGetUserId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;

    public static Guid GetUserId(ClaimsPrincipal user) =>
        TryGetUserId(user) ?? throw new CurrentAffairsException(
            StatusCodes.Status401Unauthorized,
            "Authentication required",
            "A valid user identity is required.");
}
