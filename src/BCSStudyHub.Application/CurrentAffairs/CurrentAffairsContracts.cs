using System.ComponentModel.DataAnnotations;
using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.Library;
using BCSStudyHub.Domain.Entities;

namespace BCSStudyHub.Application.CurrentAffairs;

internal static class EnumInput
{
    public static bool TryParse<T>(string? value, out T parsed) where T : struct, Enum =>
        Enum.TryParse(value, true, out parsed) && Enum.IsDefined(parsed) && !int.TryParse(value, out _);
}

public sealed record CurrentAffairsQuestionLinkInput
{
    [Required]
    public string Type { get; init; } = string.Empty;

    [Required]
    public Guid QuestionId { get; init; }
}

public sealed record CurrentAffairsReferenceInput
{
    [Required]
    public string Kind { get; init; } = nameof(CurrentAffairsReferenceKind.WebPage);

    [Required, StringLength(300, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [Required, StringLength(1024, MinimumLength = 8)]
    public string Url { get; init; } = string.Empty;
}

public sealed record CurrentAffairsEntryInput : IValidatableObject
{
    [Required, StringLength(300, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [Required, StringLength(2000, MinimumLength = 1)]
    public string Summary { get; init; } = string.Empty;

    [StringLength(100000)]
    public string? Content { get; init; }

    public DateOnly EventDate { get; init; }

    public DateOnly? SourcePublishedOn { get; init; }

    [Required]
    public string Category { get; init; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 1)]
    public string SourceName { get; init; } = string.Empty;

    [Required, StringLength(1024, MinimumLength = 8)]
    public string SourceUrl { get; init; } = string.Empty;

    public Guid? OfficialSourceId { get; init; }

    [Required]
    public string SourceStatus { get; init; } = nameof(CurrentAffairsSourceStatus.Unverified);

    [StringLength(4000)]
    public string? Notes { get; init; }

    [MaxLength(15)]
    public IReadOnlyList<string> Tags { get; init; } = [];

    [MaxLength(20)]
    public IReadOnlyList<Guid> SubjectIds { get; init; } = [];

    [MaxLength(50)]
    public IReadOnlyList<Guid> SyllabusItemIds { get; init; } = [];

    [MaxLength(30)]
    public IReadOnlyList<CurrentAffairsQuestionLinkInput> Questions { get; init; } = [];

    [MaxLength(20)]
    public IReadOnlyList<CurrentAffairsReferenceInput> References { get; init; } = [];

    [MaxLength(20)]
    public IReadOnlyList<Guid> LibraryResourceIds { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EventDate.Year is < 1900 or > 2200)
        {
            yield return new ValidationResult("The event date is required and must be between 1900 and 2200.", [nameof(EventDate)]);
        }

        if (SourcePublishedOn is { Year: < 1900 or > 2200 })
        {
            yield return new ValidationResult("The source publication date must be between 1900 and 2200.", [nameof(SourcePublishedOn)]);
        }

        if (!EnumInput.TryParse<CurrentAffairsCategory>(Category, out _))
        {
            yield return new ValidationResult("The current-affairs category is not supported.", [nameof(Category)]);
        }

        if (!EnumInput.TryParse<CurrentAffairsSourceStatus>(SourceStatus, out _))
        {
            yield return new ValidationResult("The source status is not supported.", [nameof(SourceStatus)]);
        }

        if (!LibraryRules.IsSafeWebUrl(SourceUrl))
        {
            yield return new ValidationResult("The source URL must be an absolute http or https address without credentials.", [nameof(SourceUrl)]);
        }

        if (!LibraryRules.TryNormalizeTags(Tags, out _))
        {
            yield return new ValidationResult($"Tags must be non-empty, unique, and at most {LibraryRules.MaxTagLength} characters.", [nameof(Tags)]);
        }

        if (HasDuplicates(SubjectIds) || HasDuplicates(SyllabusItemIds) || HasDuplicates(LibraryResourceIds))
        {
            yield return new ValidationResult("Subjects, topics, and library resources must be unique and non-empty.", [nameof(SubjectIds)]);
        }

        if (Questions is null ||
            Questions.Any(question => question is null || question.QuestionId == Guid.Empty ||
                                      !EnumInput.TryParse<CurrentAffairsQuestionType>(question.Type, out _)) ||
            Questions.Select(question => (question.Type.ToLowerInvariant(), question.QuestionId)).Distinct().Count() != Questions.Count)
        {
            yield return new ValidationResult("Each related question needs a supported type (Mcq, Written, Viva) and must be unique.", [nameof(Questions)]);
        }

        if (References is null ||
            References.Any(reference => reference is null ||
                                        !EnumInput.TryParse<CurrentAffairsReferenceKind>(reference.Kind, out _) ||
                                        !LibraryRules.IsSafeWebUrl(reference.Url)))
        {
            yield return new ValidationResult("Each reference needs a supported kind (Pdf, Image, WebPage) and an http or https URL.", [nameof(References)]);
        }
    }

    private static bool HasDuplicates(IReadOnlyList<Guid>? ids) =>
        ids is null || ids.Contains(Guid.Empty) || ids.Distinct().Count() != ids.Count;
}

public sealed record CurrentAffairsQuery(
    string? Search,
    string? Category,
    DateOnly? From,
    DateOnly? To,
    string? Tag,
    Guid? SubjectId,
    Guid? SyllabusItemId,
    bool BookmarkedOnly,
    int PageNumber,
    int PageSize,
    string SortBy);

public sealed record CurrentAffairsNoteInput
{
    [Required, StringLength(20000, MinimumLength = 1)]
    public string Content { get; init; } = string.Empty;
}

public sealed record CurrentAffairsReviewInput
{
    [Required]
    public string Outcome { get; init; } = string.Empty;
}

public sealed record CurrentAffairsSubjectRefDto(Guid Id, string Code, string Name);

public sealed record CurrentAffairsTopicRefDto(Guid Id, string Title, string SubjectName, string StageCode, string VersionLabel);

public sealed record CurrentAffairsSummaryDto(
    Guid Id,
    string Title,
    string Summary,
    DateOnly EventDate,
    DateOnly? SourcePublishedOn,
    string Category,
    string Status,
    string SourceName,
    string SourceUrl,
    string SourceStatus,
    IReadOnlyList<string> Tags,
    IReadOnlyList<CurrentAffairsSubjectRefDto> Subjects,
    IReadOnlyList<CurrentAffairsTopicRefDto> Topics,
    bool IsBookmarked,
    bool InRevision,
    bool HasNote,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt);

public sealed record CurrentAffairsQuestionRefDto(string Type, Guid Id, string Prompt, string Status, bool IsAvailable);

public sealed record CurrentAffairsReferenceDto(Guid Id, string Kind, string Title, string Url);

public sealed record CurrentAffairsResourceRefDto(Guid Id, string Title, string Kind, string? PrimaryFormat, bool IsAvailable);

public sealed record OfficialSourceRefDto(Guid Id, string Name, string Url, string VerificationStatus, DateOnly? LastVerifiedOn);

public sealed record CurrentAffairsNoteDto(Guid EntryId, string EntryTitle, DateOnly EventDate, string Content, DateTimeOffset UpdatedAt);

public sealed record CurrentAffairsRevisionDto(
    Guid EntryId,
    string EntryTitle,
    string Category,
    DateOnly EventDate,
    int ReviewCount,
    int IntervalDays,
    DateOnly NextReviewOn,
    DateTimeOffset? LastReviewedAt,
    string? LastOutcome,
    bool IsDue);

public sealed record CurrentAffairsBookmarkDto(Guid EntryId, string Title, string Category, DateOnly EventDate, DateTimeOffset CreatedAt);

public sealed record CurrentAffairsDetailDto(
    CurrentAffairsSummaryDto Entry,
    string? Content,
    string? Notes,
    OfficialSourceRefDto? OfficialSource,
    IReadOnlyList<CurrentAffairsQuestionRefDto> Questions,
    IReadOnlyList<CurrentAffairsReferenceDto> References,
    IReadOnlyList<CurrentAffairsResourceRefDto> LibraryResources,
    CurrentAffairsNoteDto? MyNote,
    CurrentAffairsRevisionDto? MyRevision,
    DateTimeOffset CreatedAt);

public sealed record CurrentAffairsDigestGroupDto(string Category, IReadOnlyList<CurrentAffairsSummaryDto> Entries);

public sealed record CurrentAffairsDigestDto(
    string Period,
    string Label,
    DateOnly From,
    DateOnly To,
    DateOnly PreviousAnchor,
    DateOnly NextAnchor,
    int Total,
    IReadOnlyList<CurrentAffairsDigestGroupDto> Groups);

public sealed record CurrentAffairsArchiveMonthDto(int Year, int Month, int Count);

public sealed record CurrentAffairsCategoryCountDto(string Category, int Count);

public sealed record CurrentAffairsQuestionSearchDto(string Type, Guid Id, string Prompt, string Status);

public sealed record CurrentAffairsResourceSearchDto(Guid Id, string Title, string Kind, string? PrimaryFormat);

public interface ICurrentAffairsService
{
    Task<PagedResponse<CurrentAffairsSummaryDto>> GetEntriesAsync(CurrentAffairsQuery query, Guid? userId, CancellationToken cancellationToken);
    Task<CurrentAffairsDetailDto?> GetEntryAsync(Guid entryId, Guid? userId, CancellationToken cancellationToken);
    Task<CurrentAffairsDigestDto> GetDigestAsync(string period, DateOnly? anchor, string? category, Guid? userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CurrentAffairsArchiveMonthDto>> GetArchiveAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<CurrentAffairsCategoryCountDto>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetTagsAsync(CancellationToken cancellationToken);

    Task<PagedResponse<CurrentAffairsBookmarkDto>> GetBookmarksAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<bool> AddBookmarkAsync(Guid userId, Guid entryId, CancellationToken cancellationToken);
    Task<bool> RemoveBookmarkAsync(Guid userId, Guid entryId, CancellationToken cancellationToken);
    Task<PagedResponse<CurrentAffairsNoteDto>> GetNotesAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<CurrentAffairsNoteDto?> SaveNoteAsync(Guid userId, Guid entryId, CurrentAffairsNoteInput input, CancellationToken cancellationToken);
    Task<bool> DeleteNoteAsync(Guid userId, Guid entryId, CancellationToken cancellationToken);
    Task<PagedResponse<CurrentAffairsRevisionDto>> GetRevisionsAsync(Guid userId, bool dueOnly, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<CurrentAffairsRevisionDto?> AddRevisionAsync(Guid userId, Guid entryId, CancellationToken cancellationToken);
    Task<CurrentAffairsRevisionDto?> ReviewAsync(Guid userId, Guid entryId, CurrentAffairsReviewInput input, CancellationToken cancellationToken);
    Task<bool> RemoveRevisionAsync(Guid userId, Guid entryId, CancellationToken cancellationToken);

    Task<PagedResponse<CurrentAffairsSummaryDto>> GetAdminEntriesAsync(string? search, string? status, string? category, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<CurrentAffairsDetailDto?> GetAdminEntryAsync(Guid entryId, CancellationToken cancellationToken);
    Task<CurrentAffairsDetailDto> CreateEntryAsync(CurrentAffairsEntryInput input, Guid actorId, CancellationToken cancellationToken);
    Task<CurrentAffairsDetailDto?> UpdateEntryAsync(Guid entryId, CurrentAffairsEntryInput input, Guid actorId, CancellationToken cancellationToken);
    Task<CurrentAffairsDetailDto?> SetStatusAsync(Guid entryId, string status, Guid actorId, CancellationToken cancellationToken);
    Task<bool> DeleteEntryAsync(Guid entryId, Guid actorId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CurrentAffairsQuestionSearchDto>> SearchQuestionsAsync(string? type, string search, CancellationToken cancellationToken);
    Task<IReadOnlyList<CurrentAffairsResourceSearchDto>> SearchLibraryResourcesAsync(string? search, CancellationToken cancellationToken);
}

public sealed record OfficialSourceInput : IValidatableObject
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [StringLength(200)]
    public string? LocalizedName { get; init; }

    [Required]
    public string Category { get; init; } = string.Empty;

    [Required, StringLength(512, MinimumLength = 8)]
    public string Url { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; init; }

    [StringLength(200)]
    public string? ParentOrganization { get; init; }

    public bool IsInternational { get; init; }

    [StringLength(2000)]
    public string? UsageNotes { get; init; }

    [Range(0, 10000)]
    public int SortOrder { get; init; }

    [MaxLength(10)]
    public IReadOnlyList<string> Tags { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!EnumInput.TryParse<OfficialSourceCategory>(Category, out _))
        {
            yield return new ValidationResult("The directory category is not supported.", [nameof(Category)]);
        }

        if (!LibraryRules.IsSafeWebUrl(Url))
        {
            yield return new ValidationResult("The website must be an absolute http or https URL without credentials.", [nameof(Url)]);
        }

        if (!LibraryRules.TryNormalizeTags(Tags, out _))
        {
            yield return new ValidationResult("Tags must be non-empty and unique.", [nameof(Tags)]);
        }
    }
}

public sealed record OfficialSourceVerificationInput : IValidatableObject
{
    [Required]
    public string Status { get; init; } = string.Empty;

    [Required, StringLength(1000, MinimumLength = 3)]
    public string Note { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!EnumInput.TryParse<OfficialSourceVerificationStatus>(Status, out var status) ||
            status == OfficialSourceVerificationStatus.Unverified)
        {
            yield return new ValidationResult("Record the outcome as Verified, NeedsReview, or Unreachable.", [nameof(Status)]);
        }
    }
}

public sealed record OfficialSourceDto(
    Guid Id,
    string Name,
    string? LocalizedName,
    string Category,
    string Url,
    bool IsHttps,
    string? Description,
    string? ParentOrganization,
    bool IsInternational,
    string? UsageNotes,
    string VerificationStatus,
    DateOnly? LastVerifiedOn,
    string? VerificationNote,
    string Status,
    int SortOrder,
    IReadOnlyList<string> Tags,
    DateTimeOffset UpdatedAt);

public interface IOfficialSourceService
{
    Task<IReadOnlyList<OfficialSourceDto>> GetPublishedAsync(string? search, string? category, CancellationToken cancellationToken);
    Task<OfficialSourceDto?> GetPublishedAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResponse<OfficialSourceDto>> GetAdminSourcesAsync(string? search, string? status, string? verificationStatus, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<OfficialSourceDto?> GetAdminSourceAsync(Guid id, CancellationToken cancellationToken);
    Task<OfficialSourceDto> CreateAsync(OfficialSourceInput input, Guid actorId, CancellationToken cancellationToken);
    Task<OfficialSourceDto?> UpdateAsync(Guid id, OfficialSourceInput input, Guid actorId, CancellationToken cancellationToken);
    Task<OfficialSourceDto?> RecordVerificationAsync(Guid id, OfficialSourceVerificationInput input, Guid actorId, CancellationToken cancellationToken);
    Task<OfficialSourceDto?> SetStatusAsync(Guid id, string status, Guid actorId, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken);
}
