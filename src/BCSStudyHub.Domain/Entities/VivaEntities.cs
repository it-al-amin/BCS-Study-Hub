namespace BCSStudyHub.Domain.Entities;

public enum VivaQuestionStatus
{
    Draft,
    Published,
    Archived,
}

public enum VivaReferenceType
{
    QuestionSource,
    SuggestedAnswer,
}

public enum VivaReferenceVerificationStatus
{
    Unverified,
    Verified,
    NeedsReview,
}

public enum VivaSessionStatus
{
    InProgress,
    Completed,
}

public sealed class VivaCategory
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<VivaQuestion> Questions { get; set; } = [];
}

public sealed class VivaQuestion
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public VivaCategory Category { get; set; } = null!;
    public string? Topic { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public VivaQuestionStatus Status { get; set; } = VivaQuestionStatus.Draft;
    public int? BcsExamYear { get; set; }
    public int? ExamNumber { get; set; }
    public string? ExamReference { get; set; }
    public Guid CreatedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<VivaQuestionReference> References { get; set; } = [];
}

public sealed class VivaQuestionReference
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public VivaQuestion Question { get; set; } = null!;
    public VivaReferenceType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Citation { get; set; }
    public VivaReferenceVerificationStatus VerificationStatus { get; set; }
}

public sealed class VivaBookmark
{
    public Guid UserId { get; set; }
    public Guid QuestionId { get; set; }
    public VivaQuestion Question { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class VivaQuestionHistory
{
    public Guid UserId { get; set; }
    public Guid QuestionId { get; set; }
    public VivaQuestion Question { get; set; } = null!;
    public DateTimeOffset LastViewedAt { get; set; }
    public int ViewCount { get; set; }
}

public sealed class VivaAnswerNote
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid QuestionId { get; set; }
    public VivaQuestion Question { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<VivaAnswerNoteRevision> Revisions { get; set; } = [];
}

public sealed class VivaAnswerNoteRevision
{
    public Guid Id { get; set; }
    public Guid AnswerNoteId { get; set; }
    public VivaAnswerNote AnswerNote { get; set; } = null!;
    public int RevisionNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class VivaMockSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public VivaSessionStatus Status { get; set; } = VivaSessionStatus.InProgress;
    public int? CommunicationRating { get; set; }
    public int? SubjectKnowledgeRating { get; set; }
    public int? ClarityRating { get; set; }
    public int? ConfidenceRating { get; set; }
    public string? FeedbackNotes { get; set; }
    public string? ImprovementAreas { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public ICollection<VivaMockSessionQuestion> Questions { get; set; } = [];
}

public sealed class VivaMockSessionQuestion
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public VivaMockSession Session { get; set; } = null!;
    public Guid QuestionId { get; set; }
    public VivaQuestion Question { get; set; } = null!;
    public int Sequence { get; set; }
    public string PromptSnapshot { get; set; } = string.Empty;
    public string CategorySnapshot { get; set; } = string.Empty;
    public string? TopicSnapshot { get; set; }
    public string AnswerNotes { get; set; } = string.Empty;
    public bool NeedsRevision { get; set; }
}

public sealed class VivaProfilePreparation
{
    public Guid UserId { get; set; }
    public string SelfIntroduction { get; set; } = string.Empty;
    public string AcademicBackground { get; set; } = string.Empty;
    public string UniversityAndSubject { get; set; } = string.Empty;
    public string HomeDistrict { get; set; } = string.Empty;
    public string ProfessionalExperience { get; set; } = string.Empty;
    public string CadrePreferences { get; set; } = string.Empty;
    public string InterestsAndHobbies { get; set; } = string.Empty;
    public string StrengthsAndDevelopmentAreas { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
