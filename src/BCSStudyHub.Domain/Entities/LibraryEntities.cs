namespace BCSStudyHub.Domain.Entities;

/// <summary>The structural kind of a library resource.</summary>
public enum LibraryResourceKind
{
    Book,
    File,
    ExternalLink,
    Note,
}

/// <summary>The content category used for browsing and filtering.</summary>
public enum LibraryResourceCategory
{
    General,
    Textbook,
    StudyNote,
    GovernmentPublication,
    Report,
    NewspaperArticle,
    Reference,
    ChapterMaterial,
    Other,
}

/// <summary>Who can see a resource. Private resources are visible only to their owner.</summary>
public enum LibraryVisibility
{
    Private,
    Shared,
}

public enum LibraryResourceStatus
{
    Draft,
    Published,
    Archived,
}

/// <summary>
/// Descriptive provenance of a source. This is not a reliability score; it lets learners
/// prioritise official and academic material without the platform rating sources subjectively.
/// </summary>
public enum LibrarySourceType
{
    Unspecified,
    OfficialGovernment,
    Academic,
    NewsMedia,
    ReferenceWork,
    Commercial,
    Personal,
    Other,
}

/// <summary>
/// The legal basis on which an uploaded file is held. Only bases that permit redistribution
/// may be attached to a shared resource.
/// </summary>
public enum LibraryRightsBasis
{
    NotApplicable,
    PersonalCopy,
    OwnWork,
    PublicDomain,
    GovernmentPublication,
    OpenLicense,
    PermissionGranted,
}

public enum LibraryFileRole
{
    Primary,
    Cover,
}

public enum LibraryFileFormat
{
    Pdf,
    Image,
    Document,
    Text,
}

public enum LibraryReadingStatus
{
    NotStarted,
    Reading,
    OnHold,
    Completed,
}

public sealed class LibraryResource
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public LibraryResourceKind Kind { get; set; }
    public LibraryResourceCategory Category { get; set; }
    public LibraryVisibility Visibility { get; set; }
    public LibraryResourceStatus Status { get; set; } = LibraryResourceStatus.Draft;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Authors { get; set; }
    public string? SourceName { get; set; }
    public LibrarySourceType SourceType { get; set; }
    public string? Website { get; set; }
    public string? Url { get; set; }
    public DateOnly? PublicationDate { get; set; }
    public LibraryRightsBasis RightsBasis { get; set; }
    public string? RightsNote { get; set; }
    public string? NoteContent { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public LibraryBookDetails? Book { get; set; }
    public ICollection<LibraryFile> Files { get; set; } = [];
    public ICollection<LibraryBookChapter> Chapters { get; set; } = [];
    public ICollection<LibraryResourceSubject> Subjects { get; set; } = [];
    public ICollection<LibraryResourceTopic> Topics { get; set; } = [];
    public ICollection<LibraryResourceTag> Tags { get; set; } = [];
}

public sealed class LibraryBookDetails
{
    public Guid ResourceId { get; set; }
    public LibraryResource Resource { get; set; } = null!;
    public string? Publisher { get; set; }
    public string? Edition { get; set; }
    public int? PublicationYear { get; set; }
    public string? Isbn { get; set; }
    public int? PageCount { get; set; }
    public string? Language { get; set; }
}

public sealed class LibraryFile
{
    public Guid Id { get; set; }
    public Guid ResourceId { get; set; }
    public LibraryResource Resource { get; set; } = null!;
    public LibraryFileRole Role { get; set; }
    public LibraryFileFormat Format { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public string StorageProvider { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public Guid UploadedById { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}

public sealed class LibraryBookChapter
{
    public Guid Id { get; set; }
    public Guid ResourceId { get; set; }
    public LibraryResource Resource { get; set; } = null!;
    public int ChapterNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public int? StartPage { get; set; }
    public int? EndPage { get; set; }
    public ICollection<LibraryChapterTopic> Topics { get; set; } = [];
    public ICollection<LibraryChapterResource> LinkedResources { get; set; } = [];
}

public sealed class LibraryChapterTopic
{
    public Guid ChapterId { get; set; }
    public LibraryBookChapter Chapter { get; set; } = null!;
    public Guid SyllabusItemId { get; set; }
    public SyllabusItem SyllabusItem { get; set; } = null!;
}

public sealed class LibraryChapterResource
{
    public Guid ChapterId { get; set; }
    public LibraryBookChapter Chapter { get; set; } = null!;
    public Guid ResourceId { get; set; }
    public LibraryResource Resource { get; set; } = null!;
}

public sealed class LibraryResourceSubject
{
    public Guid ResourceId { get; set; }
    public LibraryResource Resource { get; set; } = null!;
    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
}

public sealed class LibraryResourceTopic
{
    public Guid ResourceId { get; set; }
    public LibraryResource Resource { get; set; } = null!;
    public Guid SyllabusItemId { get; set; }
    public SyllabusItem SyllabusItem { get; set; } = null!;
}

public sealed class LibraryResourceTag
{
    public Guid ResourceId { get; set; }
    public LibraryResource Resource { get; set; } = null!;
    public string Tag { get; set; } = string.Empty;
}

public sealed class LibraryCollection
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<LibraryCollectionItem> Items { get; set; } = [];
}

public sealed class LibraryCollectionItem
{
    public Guid CollectionId { get; set; }
    public LibraryCollection Collection { get; set; } = null!;
    public Guid ResourceId { get; set; }
    public LibraryResource Resource { get; set; } = null!;
    public int SortOrder { get; set; }
    public DateTimeOffset AddedAt { get; set; }
}

public sealed class LibraryBookmark
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ResourceId { get; set; }
    public LibraryResource Resource { get; set; } = null!;
    public Guid? ChapterId { get; set; }
    public LibraryBookChapter? Chapter { get; set; }
    public int? Page { get; set; }
    public string? Label { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class LibraryReadingProgress
{
    public Guid UserId { get; set; }
    public Guid ResourceId { get; set; }
    public LibraryResource Resource { get; set; } = null!;
    public LibraryReadingStatus Status { get; set; }
    public Guid? CurrentChapterId { get; set; }
    public LibraryBookChapter? CurrentChapter { get; set; }
    public int? CurrentPage { get; set; }
    public int? TotalPages { get; set; }
    public int PercentComplete { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class LibraryPersonalNote
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ResourceId { get; set; }
    public LibraryResource Resource { get; set; } = null!;
    public Guid? ChapterId { get; set; }
    public LibraryBookChapter? Chapter { get; set; }
    public int? Page { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
