using System.ComponentModel.DataAnnotations;
using BCSStudyHub.Application.Common;
using BCSStudyHub.Domain.Entities;

namespace BCSStudyHub.Application.Library;

/// <summary>Who is acting and which part of the library they are managing.</summary>
public enum LibraryManagementScope
{
    /// <summary>The caller manages their own private resources.</summary>
    Personal,

    /// <summary>The caller holds library.manage and manages shared resources.</summary>
    Shared,
}

public sealed record LibraryActor(Guid UserId, LibraryManagementScope Scope);

public sealed record LibraryBookInput
{
    [StringLength(200)]
    public string? Publisher { get; init; }

    [StringLength(80)]
    public string? Edition { get; init; }

    [Range(1800, 2200)]
    public int? PublicationYear { get; init; }

    [StringLength(20)]
    public string? Isbn { get; init; }

    [Range(1, 20000)]
    public int? PageCount { get; init; }

    [StringLength(60)]
    public string? Language { get; init; }
}

public sealed record LibraryResourceInput : IValidatableObject
{
    [Required]
    public string Kind { get; init; } = string.Empty;

    [Required]
    public string Category { get; init; } = nameof(LibraryResourceCategory.General);

    [Required, StringLength(300, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; init; }

    [StringLength(500)]
    public string? Authors { get; init; }

    [StringLength(200)]
    public string? SourceName { get; init; }

    [Required]
    public string SourceType { get; init; } = nameof(LibrarySourceType.Unspecified);

    [StringLength(200)]
    public string? Website { get; init; }

    [StringLength(1024)]
    public string? Url { get; init; }

    public DateOnly? PublicationDate { get; init; }

    [Required]
    public string RightsBasis { get; init; } = nameof(LibraryRightsBasis.NotApplicable);

    [StringLength(1000)]
    public string? RightsNote { get; init; }

    [StringLength(100000)]
    public string? NoteContent { get; init; }

    public LibraryBookInput? Book { get; init; }

    [MaxLength(20)]
    public IReadOnlyList<Guid> SubjectIds { get; init; } = [];

    [MaxLength(50)]
    public IReadOnlyList<Guid> SyllabusItemIds { get; init; } = [];

    [MaxLength(15)]
    public IReadOnlyList<string> Tags { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var kindValid = TryParse<LibraryResourceKind>(Kind, out var kind);
        if (!kindValid)
        {
            yield return new ValidationResult("The resource kind is not supported.", [nameof(Kind)]);
        }

        if (!TryParse<LibraryResourceCategory>(Category, out _))
        {
            yield return new ValidationResult("The resource category is not supported.", [nameof(Category)]);
        }

        if (!TryParse<LibrarySourceType>(SourceType, out _))
        {
            yield return new ValidationResult("The source type is not supported.", [nameof(SourceType)]);
        }

        if (!TryParse<LibraryRightsBasis>(RightsBasis, out _))
        {
            yield return new ValidationResult("The rights basis is not supported.", [nameof(RightsBasis)]);
        }

        if (!string.IsNullOrWhiteSpace(Url) && !LibraryRules.IsSafeWebUrl(Url))
        {
            yield return new ValidationResult("The URL must be an absolute http or https address without credentials.", [nameof(Url)]);
        }

        if (kindValid && kind == LibraryResourceKind.ExternalLink && string.IsNullOrWhiteSpace(Url))
        {
            yield return new ValidationResult("External resources require a URL.", [nameof(Url)]);
        }

        if (kindValid && kind == LibraryResourceKind.Note && string.IsNullOrWhiteSpace(NoteContent))
        {
            yield return new ValidationResult("Study notes require note content.", [nameof(NoteContent)]);
        }

        if (kindValid && kind != LibraryResourceKind.Book && Book is not null)
        {
            yield return new ValidationResult("Book details are only accepted for book resources.", [nameof(Book)]);
        }

        if (Book is not null && !string.IsNullOrWhiteSpace(Book.Isbn) && !LibraryRules.IsValidIsbn(Book.Isbn))
        {
            yield return new ValidationResult("The ISBN must be a valid ISBN-10 or ISBN-13.", [nameof(Book)]);
        }

        if (SubjectIds is null || SubjectIds.Distinct().Count() != SubjectIds.Count || SubjectIds.Contains(Guid.Empty))
        {
            yield return new ValidationResult("Subjects must be unique.", [nameof(SubjectIds)]);
        }

        if (SyllabusItemIds is null || SyllabusItemIds.Distinct().Count() != SyllabusItemIds.Count || SyllabusItemIds.Contains(Guid.Empty))
        {
            yield return new ValidationResult("Topics must be unique.", [nameof(SyllabusItemIds)]);
        }

        if (!LibraryRules.TryNormalizeTags(Tags, out _))
        {
            yield return new ValidationResult($"Tags must be non-empty, unique, and at most {LibraryRules.MaxTagLength} characters.", [nameof(Tags)]);
        }
    }

    internal static bool TryParse<T>(string? value, out T parsed) where T : struct, Enum =>
        Enum.TryParse(value, true, out parsed) && Enum.IsDefined(parsed) && !int.TryParse(value, out _);
}

public sealed record LibraryChapterInput : IValidatableObject
{
    [Range(1, 1000)]
    public int ChapterNumber { get; init; }

    [Required, StringLength(300, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [StringLength(4000)]
    public string? Summary { get; init; }

    [Range(1, 20000)]
    public int? StartPage { get; init; }

    [Range(1, 20000)]
    public int? EndPage { get; init; }

    [MaxLength(30)]
    public IReadOnlyList<Guid> SyllabusItemIds { get; init; } = [];

    [MaxLength(30)]
    public IReadOnlyList<Guid> LinkedResourceIds { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartPage is int start && EndPage is int end && end < start)
        {
            yield return new ValidationResult("The chapter end page cannot be before its start page.", [nameof(EndPage)]);
        }

        if (SyllabusItemIds is null || SyllabusItemIds.Distinct().Count() != SyllabusItemIds.Count)
        {
            yield return new ValidationResult("Chapter topics must be unique.", [nameof(SyllabusItemIds)]);
        }

        if (LinkedResourceIds is null || LinkedResourceIds.Distinct().Count() != LinkedResourceIds.Count)
        {
            yield return new ValidationResult("Chapter resources must be unique.", [nameof(LinkedResourceIds)]);
        }
    }
}

public sealed record LibraryProgressInput : IValidatableObject
{
    [Required]
    public string Status { get; init; } = nameof(LibraryReadingStatus.Reading);

    public Guid? CurrentChapterId { get; init; }

    [Range(0, 20000)]
    public int? CurrentPage { get; init; }

    [Range(1, 20000)]
    public int? TotalPages { get; init; }

    [Range(0, 100)]
    public int? PercentComplete { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!LibraryResourceInput.TryParse<LibraryReadingStatus>(Status, out _))
        {
            yield return new ValidationResult("The reading status is not supported.", [nameof(Status)]);
        }

        if (CurrentPage is int current && TotalPages is int total && current > total)
        {
            yield return new ValidationResult("The current page cannot exceed the total page count.", [nameof(CurrentPage)]);
        }
    }

    /// <summary>Explicit percentage wins; otherwise it is derived from pages, and completion forces 100.</summary>
    public int ResolvePercent(LibraryReadingStatus status)
    {
        if (status == LibraryReadingStatus.Completed)
        {
            return 100;
        }

        if (PercentComplete is int percent)
        {
            return percent;
        }

        if (CurrentPage is int current && TotalPages is int total && total > 0)
        {
            return (int)Math.Clamp(Math.Round(current * 100d / total), 0, 100);
        }

        return 0;
    }
}

public sealed record LibraryBookmarkInput
{
    [Required]
    public Guid ResourceId { get; init; }

    public Guid? ChapterId { get; init; }

    [Range(1, 20000)]
    public int? Page { get; init; }

    [StringLength(200)]
    public string? Label { get; init; }
}

public sealed record LibraryNoteInput
{
    public Guid? ChapterId { get; init; }

    [Range(1, 20000)]
    public int? Page { get; init; }

    [Required, StringLength(20000, MinimumLength = 1)]
    public string Content { get; init; } = string.Empty;
}

public sealed record LibraryCollectionInput
{
    [Required, StringLength(150, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; init; }
}

public sealed record LibraryCollectionItemInput
{
    [Required]
    public Guid ResourceId { get; init; }
}

public sealed record LibraryResourceQuery(
    string? Search,
    string? Kind,
    string? Category,
    string? Format,
    Guid? SubjectId,
    Guid? SyllabusItemId,
    string? Tag,
    string Scope,
    bool BookmarkedOnly,
    int PageNumber,
    int PageSize,
    string SortBy);

public sealed record LibraryAdminResourceQuery(
    string? Search,
    string? Status,
    string? Kind,
    int PageNumber,
    int PageSize);

/// <summary>An upload handed to the service. The stream must stay open for the duration of the call.</summary>
public sealed record LibraryUpload(Stream Content, string FileName, long Length);

/// <summary>An open file ready to stream. The caller disposes <see cref="Content"/>.</summary>
public sealed record LibraryFileContent(
    Stream Content,
    string ContentType,
    string FileName,
    long SizeBytes,
    bool CanPreviewInline);

public sealed record LibrarySubjectRefDto(Guid Id, string Code, string Name);

public sealed record LibraryTopicRefDto(
    Guid Id,
    string Title,
    string Kind,
    Guid SubjectId,
    string SubjectName,
    Guid SyllabusVersionId,
    string VersionLabel,
    string StageCode);

public sealed record LibraryResourceRefDto(Guid Id, string Title, string Kind);

public sealed record LibraryBookDto(
    string? Publisher,
    string? Edition,
    int? PublicationYear,
    string? Isbn,
    int? PageCount,
    string? Language);

public sealed record LibraryFileDto(
    Guid Id,
    string Role,
    string Format,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Sha256,
    DateTimeOffset UploadedAt,
    bool CanPreviewInline);

public sealed record LibraryProgressDto(
    Guid ResourceId,
    string ResourceTitle,
    string ResourceKind,
    string Status,
    Guid? CurrentChapterId,
    string? CurrentChapterTitle,
    int? CurrentPage,
    int? TotalPages,
    int PercentComplete,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset UpdatedAt);

public sealed record LibraryResourceSummaryDto(
    Guid Id,
    string Kind,
    string Category,
    string Visibility,
    string Status,
    string Title,
    string? Description,
    string? Authors,
    string? SourceName,
    string SourceType,
    string? Url,
    DateOnly? PublicationDate,
    LibraryBookDto? Book,
    string? PrimaryFormat,
    Guid? PrimaryFileId,
    Guid? CoverFileId,
    int ChapterCount,
    IReadOnlyList<LibrarySubjectRefDto> Subjects,
    IReadOnlyList<LibraryTopicRefDto> Topics,
    IReadOnlyList<string> Tags,
    bool IsOwner,
    bool IsBookmarked,
    LibraryProgressDto? Progress,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record LibraryChapterDto(
    Guid Id,
    int ChapterNumber,
    string Title,
    string? Summary,
    int? StartPage,
    int? EndPage,
    IReadOnlyList<LibraryTopicRefDto> Topics,
    IReadOnlyList<LibraryResourceRefDto> LinkedResources);

public sealed record LibraryBookmarkDto(
    Guid Id,
    Guid ResourceId,
    string ResourceTitle,
    string ResourceKind,
    Guid? ChapterId,
    string? ChapterTitle,
    int? Page,
    string? Label,
    DateTimeOffset CreatedAt);

public sealed record LibraryNoteDto(
    Guid Id,
    Guid ResourceId,
    string ResourceTitle,
    Guid? ChapterId,
    string? ChapterTitle,
    int? Page,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record LibraryResourceDetailDto(
    LibraryResourceSummaryDto Resource,
    string? Website,
    string RightsBasis,
    string? RightsNote,
    string? NoteContent,
    DateTimeOffset? PublishedAt,
    bool CanEdit,
    IReadOnlyList<LibraryFileDto> Files,
    IReadOnlyList<LibraryChapterDto> Chapters,
    IReadOnlyList<LibraryBookmarkDto> Bookmarks,
    IReadOnlyList<LibraryNoteDto> Notes,
    IReadOnlyList<LibraryResourceRefDto> UsedInChapters);

public sealed record LibraryCollectionDto(
    Guid Id,
    string Name,
    string? Description,
    int ItemCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record LibraryCollectionDetailDto(
    LibraryCollectionDto Collection,
    IReadOnlyList<LibraryResourceSummaryDto> Items);

public sealed record LibraryStorageUsageDto(
    long UsedBytes,
    long QuotaBytes,
    long MaxFileSizeBytes,
    IReadOnlyCollection<string> AllowedExtensions);

public sealed record LibraryKindCountDto(string Kind, int Count);

public sealed record LibraryDashboardDto(
    int VisibleResources,
    int SharedResources,
    int PrivateResources,
    IReadOnlyList<LibraryKindCountDto> ByKind,
    int BookmarkCount,
    int CollectionCount,
    int ReadingCount,
    int CompletedCount,
    LibraryStorageUsageDto Storage,
    IReadOnlyList<LibraryProgressDto> ContinueReading,
    IReadOnlyList<LibraryResourceSummaryDto> RecentlyAdded,
    IReadOnlyList<LibraryBookmarkDto> RecentBookmarks);

public interface ILibraryService
{
    Task<LibraryDashboardDto> GetDashboardAsync(Guid userId, CancellationToken cancellationToken);
    Task<PagedResponse<LibraryResourceSummaryDto>> GetResourcesAsync(LibraryResourceQuery query, Guid userId, CancellationToken cancellationToken);
    Task<LibraryResourceDetailDto?> GetResourceAsync(Guid resourceId, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetTagsAsync(Guid userId, CancellationToken cancellationToken);

    Task<PagedResponse<LibraryResourceSummaryDto>> GetAdminResourcesAsync(LibraryAdminResourceQuery query, Guid actorId, CancellationToken cancellationToken);
    Task<LibraryResourceDetailDto?> GetManagedResourceAsync(Guid resourceId, LibraryActor actor, CancellationToken cancellationToken);
    Task<LibraryResourceDetailDto> CreateResourceAsync(LibraryResourceInput input, LibraryActor actor, CancellationToken cancellationToken);
    Task<LibraryResourceDetailDto?> UpdateResourceAsync(Guid resourceId, LibraryResourceInput input, LibraryActor actor, CancellationToken cancellationToken);
    Task<LibraryResourceDetailDto?> SetStatusAsync(Guid resourceId, string status, LibraryActor actor, CancellationToken cancellationToken);
    Task<bool> DeleteResourceAsync(Guid resourceId, LibraryActor actor, CancellationToken cancellationToken);

    Task<LibraryFileDto?> UploadFileAsync(Guid resourceId, string role, LibraryUpload upload, LibraryActor actor, CancellationToken cancellationToken);
    Task<bool> DeleteFileAsync(Guid resourceId, Guid fileId, LibraryActor actor, CancellationToken cancellationToken);
    Task<LibraryFileContent?> OpenFileAsync(Guid resourceId, Guid fileId, Guid userId, bool asManager, CancellationToken cancellationToken);

    Task<LibraryChapterDto?> AddChapterAsync(Guid resourceId, LibraryChapterInput input, LibraryActor actor, CancellationToken cancellationToken);
    Task<LibraryChapterDto?> UpdateChapterAsync(Guid resourceId, Guid chapterId, LibraryChapterInput input, LibraryActor actor, CancellationToken cancellationToken);
    Task<bool> DeleteChapterAsync(Guid resourceId, Guid chapterId, LibraryActor actor, CancellationToken cancellationToken);

    Task<PagedResponse<LibraryBookmarkDto>> GetBookmarksAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<LibraryBookmarkDto> AddBookmarkAsync(Guid userId, LibraryBookmarkInput input, CancellationToken cancellationToken);
    Task<bool> DeleteBookmarkAsync(Guid userId, Guid bookmarkId, CancellationToken cancellationToken);

    Task<PagedResponse<LibraryProgressDto>> GetProgressListAsync(Guid userId, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<LibraryProgressDto?> UpdateProgressAsync(Guid userId, Guid resourceId, LibraryProgressInput input, CancellationToken cancellationToken);
    Task<bool> ClearProgressAsync(Guid userId, Guid resourceId, CancellationToken cancellationToken);

    Task<PagedResponse<LibraryNoteDto>> GetNotesAsync(Guid userId, Guid? resourceId, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<LibraryNoteDto?> CreateNoteAsync(Guid userId, Guid resourceId, LibraryNoteInput input, CancellationToken cancellationToken);
    Task<LibraryNoteDto?> UpdateNoteAsync(Guid userId, Guid noteId, LibraryNoteInput input, CancellationToken cancellationToken);
    Task<bool> DeleteNoteAsync(Guid userId, Guid noteId, CancellationToken cancellationToken);

    Task<IReadOnlyList<LibraryCollectionDto>> GetCollectionsAsync(Guid userId, CancellationToken cancellationToken);
    Task<LibraryCollectionDetailDto?> GetCollectionAsync(Guid userId, Guid collectionId, CancellationToken cancellationToken);
    Task<LibraryCollectionDto> CreateCollectionAsync(Guid userId, LibraryCollectionInput input, CancellationToken cancellationToken);
    Task<LibraryCollectionDto?> UpdateCollectionAsync(Guid userId, Guid collectionId, LibraryCollectionInput input, CancellationToken cancellationToken);
    Task<bool> DeleteCollectionAsync(Guid userId, Guid collectionId, CancellationToken cancellationToken);
    Task<bool> AddCollectionItemAsync(Guid userId, Guid collectionId, Guid resourceId, CancellationToken cancellationToken);
    Task<bool> RemoveCollectionItemAsync(Guid userId, Guid collectionId, Guid resourceId, CancellationToken cancellationToken);

    Task<LibraryStorageUsageDto> GetStorageUsageAsync(Guid userId, CancellationToken cancellationToken);
}
