using System.ComponentModel.DataAnnotations;
using BCSStudyHub.Application.Common;
using BCSStudyHub.Domain.Entities;

namespace BCSStudyHub.Application.Admin;

public sealed class AdminException(int statusCode, string title, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
}

// ---------- Site settings ----------

public sealed record SiteSettingsInput : IValidatableObject
{
    [Required, StringLength(80, MinimumLength = 1)]
    public string SiteName { get; init; } = "BCS Study Hub";

    [StringLength(200)]
    public string? Tagline { get; init; }

    public bool AnnouncementEnabled { get; init; }

    [StringLength(500)]
    public string? AnnouncementText { get; init; }

    [Required]
    public string AnnouncementTone { get; init; } = nameof(Domain.Entities.AnnouncementTone.Info);

    public DateTimeOffset? AnnouncementStartsAt { get; init; }
    public DateTimeOffset? AnnouncementEndsAt { get; init; }

    [StringLength(254), EmailAddress]
    public string? SupportEmail { get; init; }

    public bool RegistrationOpen { get; init; } = true;

    [StringLength(500)]
    public string? FooterNotice { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enum.TryParse<AnnouncementTone>(AnnouncementTone, true, out var tone) || !Enum.IsDefined(tone) || int.TryParse(AnnouncementTone, out _))
        {
            yield return new ValidationResult("The announcement tone must be Info, Warning, or Critical.", [nameof(AnnouncementTone)]);
        }

        if (AnnouncementEnabled && string.IsNullOrWhiteSpace(AnnouncementText))
        {
            yield return new ValidationResult("An enabled announcement needs text.", [nameof(AnnouncementText)]);
        }

        if (AnnouncementStartsAt is DateTimeOffset start && AnnouncementEndsAt is DateTimeOffset end && end <= start)
        {
            yield return new ValidationResult("The announcement must end after it starts.", [nameof(AnnouncementEndsAt)]);
        }
    }
}

public sealed record SiteSettingsDto(
    string SiteName,
    string? Tagline,
    bool AnnouncementEnabled,
    string? AnnouncementText,
    string AnnouncementTone,
    DateTimeOffset? AnnouncementStartsAt,
    DateTimeOffset? AnnouncementEndsAt,
    string? SupportEmail,
    bool RegistrationOpen,
    string? FooterNotice,
    DateTimeOffset? UpdatedAt);

/// <summary>What anonymous visitors may see: the announcement only while it is enabled and inside its window.</summary>
public sealed record PublicSiteSettingsDto(
    string SiteName,
    string? Tagline,
    string? Announcement,
    string AnnouncementTone,
    string? SupportEmail,
    bool RegistrationOpen,
    string? FooterNotice);

public interface ISiteSettingsService
{
    Task<PublicSiteSettingsDto> GetPublicAsync(CancellationToken cancellationToken);
    Task<SiteSettingsDto> GetAsync(CancellationToken cancellationToken);
    Task<SiteSettingsDto> UpdateAsync(SiteSettingsInput input, Guid actorId, CancellationToken cancellationToken);
    Task<bool> IsRegistrationOpenAsync(CancellationToken cancellationToken);
}

// ---------- Audit ----------

public sealed record AuditQuery(
    string? EntityType,
    string? EntityId,
    Guid? ActorId,
    string? Action,
    DateOnly? From,
    DateOnly? To,
    int PageNumber,
    int PageSize);

public sealed record AuditEntryDto(
    long Id,
    DateTimeOffset OccurredAt,
    string Action,
    string? EntityType,
    string? EntityId,
    Guid? ActorId,
    string? ActorName,
    string? ActorEmail,
    string? CorrelationId);

public interface IAuditQueryService
{
    Task<PagedResponse<AuditEntryDto>> GetAsync(AuditQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetEntityTypesAsync(CancellationToken cancellationToken);
}

// ---------- Overview ----------

public sealed record ModuleOverviewDto(
    string Module,
    string Label,
    string RequiredPermission,
    string ManagePath,
    int Draft,
    int Published,
    int Archived,
    int Attention,
    string? AttentionLabel);

public sealed record AdminOverviewDto(
    IReadOnlyList<ModuleOverviewDto> Modules,
    int Users,
    int NewUsersLast30Days,
    IReadOnlyList<AuditEntryDto> RecentActivity);

public interface IAdminOverviewService
{
    /// <summary>Modules are limited to those the caller's permissions cover; recent activity requires audit.read.</summary>
    Task<AdminOverviewDto> GetAsync(IReadOnlySet<string> permissions, CancellationToken cancellationToken);
}

// ---------- Tags ----------

public enum TagModule
{
    Mcq,
    Written,
    Library,
    CurrentAffairs,
    OfficialSources,
}

public sealed record TagUsageDto(string Tag, IReadOnlyDictionary<string, int> Modules, int Total);

public sealed record TagRenameInput
{
    public string? Module { get; init; }

    [Required, StringLength(60, MinimumLength = 1)]
    public string From { get; init; } = string.Empty;

    [Required, StringLength(60, MinimumLength = 1)]
    public string To { get; init; } = string.Empty;
}

public sealed record TagDeleteInput
{
    public string? Module { get; init; }

    [Required, StringLength(60, MinimumLength = 1)]
    public string Tag { get; init; } = string.Empty;
}

public sealed record TagChangeResultDto(string Tag, int Updated, int Merged, int Removed);

public interface ITagAdminService
{
    Task<IReadOnlyList<TagUsageDto>> GetAsync(IReadOnlySet<TagModule> modules, string? search, CancellationToken cancellationToken);
    Task<TagChangeResultDto> RenameAsync(IReadOnlySet<TagModule> modules, TagRenameInput input, Guid actorId, CancellationToken cancellationToken);
    Task<TagChangeResultDto> DeleteAsync(IReadOnlySet<TagModule> modules, TagDeleteInput input, Guid actorId, CancellationToken cancellationToken);
}

public static class TagModules
{
    /// <summary>The permission that governs each module's tags; callers only touch modules they may manage.</summary>
    public static IReadOnlyDictionary<TagModule, string> Permissions { get; } = new Dictionary<TagModule, string>
    {
        [TagModule.Mcq] = Authorization.PermissionCodes.McqManage,
        [TagModule.Written] = Authorization.PermissionCodes.WrittenManage,
        [TagModule.Library] = Authorization.PermissionCodes.LibraryManage,
        [TagModule.CurrentAffairs] = Authorization.PermissionCodes.CurrentAffairsManage,
        [TagModule.OfficialSources] = Authorization.PermissionCodes.CurrentAffairsManage,
    };

    public static IReadOnlySet<TagModule> Allowed(IEnumerable<string> permissions, string? requested)
    {
        var granted = permissions.ToHashSet(StringComparer.Ordinal);
        var allowed = Permissions.Where(pair => granted.Contains(pair.Value)).Select(pair => pair.Key).ToHashSet();
        if (string.IsNullOrWhiteSpace(requested))
        {
            return allowed;
        }

        if (!Enum.TryParse<TagModule>(requested, true, out var module) || !Enum.IsDefined(module) || int.TryParse(requested, out _))
        {
            throw new AdminException(400, "Invalid request", "The tag module is not supported.");
        }

        return allowed.Contains(module)
            ? new HashSet<TagModule> { module }
            : throw new AdminException(403, "Not permitted", "You do not manage content in this module.");
    }
}

// ---------- Bulk ----------

public sealed record BulkStatusInput : IValidatableObject
{
    /// <summary>McqQuestions, McqExamSets, WrittenQuestions, VivaQuestions, LibraryResources, CurrentAffairs, OfficialSources.</summary>
    [Required]
    public string Module { get; init; } = string.Empty;

    /// <summary>Publish or Archive.</summary>
    [Required]
    public string Action { get; init; } = string.Empty;

    [MinLength(1), MaxLength(BulkLimits.MaxItems)]
    public IReadOnlyList<Guid> Ids { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Action is not ("Publish" or "Archive" or "publish" or "archive"))
        {
            yield return new ValidationResult("The bulk action must be Publish or Archive.", [nameof(Action)]);
        }

        if (Ids is null || Ids.Count == 0 || Ids.Distinct().Count() != Ids.Count || Ids.Contains(Guid.Empty))
        {
            yield return new ValidationResult($"Provide between 1 and {BulkLimits.MaxItems} unique IDs.", [nameof(Ids)]);
        }
    }
}

public static class BulkLimits
{
    public const int MaxItems = 100;
}

public sealed record BulkItemResultDto(Guid Id, bool Succeeded, string? Error);

public sealed record BulkResultDto(string Module, string Action, int Succeeded, int Failed, IReadOnlyList<BulkItemResultDto> Items);
