namespace BCSStudyHub.Domain.Entities;

public enum SyllabusItemKind
{
    Topic,
    Subtopic,
}

public enum SyllabusVerificationStatus
{
    Unverified,
    Verified,
    NeedsReview,
}

public sealed class SyllabusItem
{
    public Guid Id { get; set; }
    public Guid SyllabusSubjectId { get; set; }
    public SyllabusSubject SyllabusSubject { get; set; } = null!;
    public Guid? PaperId { get; set; }
    public Paper? Paper { get; set; }
    public Guid? ParentId { get; set; }
    public SyllabusItem? Parent { get; set; }
    public ICollection<SyllabusItem> Children { get; set; } = [];
    public SyllabusItemKind Kind { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public SyllabusVerificationStatus VerificationStatus { get; set; } = SyllabusVerificationStatus.Unverified;
    public ICollection<SyllabusItemSource> SourceReferences { get; set; } = [];
    public ICollection<TopicProgress> ProgressRecords { get; set; } = [];
}
