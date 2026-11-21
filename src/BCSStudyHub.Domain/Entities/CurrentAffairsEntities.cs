namespace BCSStudyHub.Domain.Entities;

public enum CurrentAffairsCategory
{
    Bangladesh,
    InternationalAffairs,
    Economy,
    Government,
    Politics,
    Constitution,
    Diplomacy,
    InternationalOrganizations,
    Science,
    Technology,
    Environment,
    ClimateChange,
    Awards,
    Sports,
    Appointments,
    GovernmentPolicies,
    NationalProjects,
    Reports,
    ImportantEvents,
}

public enum CurrentAffairsStatus
{
    Draft,
    Published,
    Archived,
}

public enum CurrentAffairsSourceStatus
{
    Unverified,
    Verified,
}

public enum CurrentAffairsQuestionType
{
    Mcq,
    Written,
    Viva,
}

public enum CurrentAffairsReferenceKind
{
    Pdf,
    Image,
    WebPage,
}

public enum CurrentAffairsRevisionOutcome
{
    Again,
    Hard,
    Good,
    Easy,
}

public sealed class CurrentAffairsEntry
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? Content { get; set; }

    /// <summary>The date the event happened or the decision took effect; drives daily/weekly/monthly digests.</summary>
    public DateOnly EventDate { get; set; }

    /// <summary>The date the cited source published the information.</summary>
    public DateOnly? SourcePublishedOn { get; set; }

    public CurrentAffairsCategory Category { get; set; }
    public CurrentAffairsStatus Status { get; set; } = CurrentAffairsStatus.Draft;
    public string SourceName { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public Guid? OfficialSourceId { get; set; }
    public OfficialSource? OfficialSource { get; set; }
    public CurrentAffairsSourceStatus SourceStatus { get; set; } = CurrentAffairsSourceStatus.Unverified;

    /// <summary>Editorial notes visible to learners, for example exam relevance or caveats.</summary>
    public string? Notes { get; set; }

    public Guid CreatedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public ICollection<CurrentAffairsEntryTag> Tags { get; set; } = [];
    public ICollection<CurrentAffairsEntrySubject> Subjects { get; set; } = [];
    public ICollection<CurrentAffairsEntryTopic> Topics { get; set; } = [];
    public ICollection<CurrentAffairsEntryQuestion> Questions { get; set; } = [];
    public ICollection<CurrentAffairsEntryReference> References { get; set; } = [];
    public ICollection<CurrentAffairsEntryResource> LibraryResources { get; set; } = [];
}

public sealed class CurrentAffairsEntryTag
{
    public Guid EntryId { get; set; }
    public CurrentAffairsEntry Entry { get; set; } = null!;
    public string Tag { get; set; } = string.Empty;
}

public sealed class CurrentAffairsEntrySubject
{
    public Guid EntryId { get; set; }
    public CurrentAffairsEntry Entry { get; set; } = null!;
    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
}

public sealed class CurrentAffairsEntryTopic
{
    public Guid EntryId { get; set; }
    public CurrentAffairsEntry Entry { get; set; } = null!;
    public Guid SyllabusItemId { get; set; }
    public SyllabusItem SyllabusItem { get; set; } = null!;
}

/// <summary>A link to an MCQ, Written, or Viva question. Integrity is validated by the service across the three tables.</summary>
public sealed class CurrentAffairsEntryQuestion
{
    public Guid EntryId { get; set; }
    public CurrentAffairsEntry Entry { get; set; } = null!;
    public CurrentAffairsQuestionType QuestionType { get; set; }
    public Guid QuestionId { get; set; }
}

/// <summary>An external PDF, image, or web page reference, typically hosted by the official publisher.</summary>
public sealed class CurrentAffairsEntryReference
{
    public Guid Id { get; set; }
    public Guid EntryId { get; set; }
    public CurrentAffairsEntry Entry { get; set; } = null!;
    public CurrentAffairsReferenceKind Kind { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

/// <summary>A link to a shared digital-library resource (an uploaded PDF or image with recorded rights).</summary>
public sealed class CurrentAffairsEntryResource
{
    public Guid EntryId { get; set; }
    public CurrentAffairsEntry Entry { get; set; } = null!;
    public Guid ResourceId { get; set; }
    public LibraryResource Resource { get; set; } = null!;
}

public sealed class CurrentAffairsBookmark
{
    public Guid UserId { get; set; }
    public Guid EntryId { get; set; }
    public CurrentAffairsEntry Entry { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CurrentAffairsStudyNote
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid EntryId { get; set; }
    public CurrentAffairsEntry Entry { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Per-user spaced revision state for an entry.</summary>
public sealed class CurrentAffairsRevision
{
    public Guid UserId { get; set; }
    public Guid EntryId { get; set; }
    public CurrentAffairsEntry Entry { get; set; } = null!;
    public int ReviewCount { get; set; }

    /// <summary>Position on the user's revision interval ladder (see PlannerSettings); 0 means never reviewed.</summary>
    public int Step { get; set; }
    public int IntervalDays { get; set; }
    public DateOnly NextReviewOn { get; set; }
    public DateTimeOffset? LastReviewedAt { get; set; }
    public CurrentAffairsRevisionOutcome? LastOutcome { get; set; }
    public DateTimeOffset AddedAt { get; set; }
}

public enum OfficialSourceCategory
{
    ConstitutionalBody,
    GovernmentPortal,
    Ministry,
    Department,
    CentralBank,
    Statistics,
    Parliament,
    LegalAndLegislative,
    Judiciary,
    OfficialNews,
    InternationalOrganization,
    ReportsAndPublications,
}

public enum OfficialSourceVerificationStatus
{
    Unverified,
    Verified,
    NeedsReview,
    Unreachable,
}

public enum OfficialSourceStatus
{
    Draft,
    Published,
    Archived,
}

/// <summary>A curated, manually verified official website in the resource directory.</summary>
public sealed class OfficialSource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LocalizedName { get; set; }
    public OfficialSourceCategory Category { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ParentOrganization { get; set; }
    public bool IsInternational { get; set; }

    /// <summary>What a learner can find there, and any copyright or terms-of-use caveats.</summary>
    public string? UsageNotes { get; set; }

    public OfficialSourceVerificationStatus VerificationStatus { get; set; } = OfficialSourceVerificationStatus.Unverified;
    public DateOnly? LastVerifiedOn { get; set; }
    public Guid? VerifiedById { get; set; }
    public string? VerificationNote { get; set; }
    public OfficialSourceStatus Status { get; set; } = OfficialSourceStatus.Draft;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<OfficialSourceTag> Tags { get; set; } = [];
}

public sealed class OfficialSourceTag
{
    public Guid SourceId { get; set; }
    public OfficialSource Source { get; set; } = null!;
    public string Tag { get; set; } = string.Empty;
}
