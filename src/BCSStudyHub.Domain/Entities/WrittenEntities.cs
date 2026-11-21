namespace BCSStudyHub.Domain.Entities;

public enum WrittenQuestionType
{
    Descriptive,
    Analytical,
    Essay,
    Composition,
    Translation,
}

public enum WrittenQuestionStatus
{
    Draft,
    Published,
    Archived,
}

public enum WrittenReferenceStatus
{
    Unverified,
    Verified,
    NeedsReview,
}

public enum WrittenModelAnswerStatus
{
    Draft,
    Published,
    Archived,
}

public enum WrittenAnswerStatus
{
    Draft,
    Submitted,
}

public enum WrittenAnswerRevisionKind
{
    DraftSave,
    Submission,
    RevisionStarted,
}

public sealed class WrittenQuestion
{
    public Guid Id { get; set; }
    public Guid SyllabusItemId { get; set; }
    public SyllabusItem SyllabusItem { get; set; } = null!;
    public string Prompt { get; set; } = string.Empty;
    public WrittenQuestionType Type { get; set; }
    public WrittenQuestionStatus Status { get; set; } = WrittenQuestionStatus.Draft;
    public int? BcsExamYear { get; set; }
    public int? ExamNumber { get; set; }
    public string? ExamReference { get; set; }
    public decimal? Marks { get; set; }
    public string? AnswerGuidance { get; set; }
    public Guid CreatedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<WrittenQuestionSourceReference> Sources { get; set; } = [];
    public ICollection<WrittenQuestionResource> Resources { get; set; } = [];
    public ICollection<WrittenQuestionTag> Tags { get; set; } = [];
    public WrittenModelAnswer? ModelAnswer { get; set; }
}

public sealed class WrittenQuestionSourceReference
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public WrittenQuestion Question { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Citation { get; set; }
    public DateOnly? PublishedOn { get; set; }
    public WrittenReferenceStatus VerificationStatus { get; set; }
}

public sealed class WrittenQuestionResource
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public WrittenQuestion Question { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public sealed class WrittenQuestionTag
{
    public Guid QuestionId { get; set; }
    public WrittenQuestion Question { get; set; } = null!;
    public string Tag { get; set; } = string.Empty;
}

public sealed class WrittenModelAnswer
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public WrittenQuestion Question { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public WrittenModelAnswerStatus Status { get; set; } = WrittenModelAnswerStatus.Draft;
    public Guid UpdatedById { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<WrittenModelAnswerReference> References { get; set; } = [];
}

public sealed class WrittenModelAnswerReference
{
    public Guid Id { get; set; }
    public Guid ModelAnswerId { get; set; }
    public WrittenModelAnswer ModelAnswer { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Citation { get; set; }
    public WrittenReferenceStatus VerificationStatus { get; set; }
}

public sealed class WrittenAnswer
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid QuestionId { get; set; }
    public WrittenQuestion Question { get; set; } = null!;
    public WrittenAnswerStatus Status { get; set; } = WrittenAnswerStatus.Draft;
    public string Content { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public int TimeSpentSeconds { get; set; }
    public string? SelfAssessmentJson { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public ICollection<WrittenAnswerRevision> Revisions { get; set; } = [];
}

public sealed class WrittenAnswerRevision
{
    public Guid Id { get; set; }
    public Guid AnswerId { get; set; }
    public WrittenAnswer Answer { get; set; } = null!;
    public int RevisionNumber { get; set; }
    public WrittenAnswerRevisionKind Kind { get; set; }
    public string Content { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public int TimeSpentSeconds { get; set; }
    public string? SelfAssessmentJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class WrittenBookmark
{
    public Guid UserId { get; set; }
    public Guid QuestionId { get; set; }
    public WrittenQuestion Question { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class WrittenGoal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int TargetSubmissions { get; set; }
    public int TargetWords { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class WrittenStudyNote
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SyllabusItemId { get; set; }
    public SyllabusItem SyllabusItem { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
