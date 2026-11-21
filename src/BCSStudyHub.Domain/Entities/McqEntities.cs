namespace BCSStudyHub.Domain.Entities;

public enum McqQuestionType
{
    SingleAnswer,
    MultipleAnswer,
}

public enum McqDifficulty
{
    Easy,
    Medium,
    Hard,
}

public enum McqContentStatus
{
    Draft,
    Published,
    Archived,
}

public enum McqSourceVerificationStatus
{
    Unverified,
    Verified,
    NeedsReview,
}

public enum McqPracticeMode
{
    Topic,
    Subject,
    Mixed,
    MockExam,
}

public enum McqAttemptStatus
{
    InProgress,
    Submitted,
}

public enum McqMistakeStatus
{
    Open,
    Reviewed,
    Resolved,
}

public sealed class McqQuestion
{
    public Guid Id { get; set; }
    public Guid SyllabusItemId { get; set; }
    public SyllabusItem SyllabusItem { get; set; } = null!;
    public string Prompt { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public string? ImageUrl { get; set; }
    public McqQuestionType Type { get; set; }
    public McqDifficulty Difficulty { get; set; }
    public McqContentStatus Status { get; set; } = McqContentStatus.Draft;
    public int? BcsExamYear { get; set; }
    public int? ExamNumber { get; set; }
    public string? ExamReference { get; set; }
    public Guid CreatedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<McqQuestionOption> Options { get; set; } = [];
    public ICollection<McqQuestionSourceReference> Sources { get; set; } = [];
    public ICollection<McqQuestionTag> Tags { get; set; } = [];
}

public sealed class McqQuestionOption
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public McqQuestion Question { get; set; } = null!;
    public string Text { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsCorrect { get; set; }
}

public sealed class McqQuestionSourceReference
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public McqQuestion Question { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Citation { get; set; }
    public DateOnly? PublishedOn { get; set; }
    public McqSourceVerificationStatus VerificationStatus { get; set; }
}

public sealed class McqQuestionTag
{
    public Guid QuestionId { get; set; }
    public McqQuestion Question { get; set; } = null!;
    public string Tag { get; set; } = string.Empty;
}

public sealed class McqExamSet
{
    public Guid Id { get; set; }
    public Guid SyllabusVersionId { get; set; }
    public SyllabusVersion SyllabusVersion { get; set; } = null!;
    public Guid? SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public Guid? SyllabusItemId { get; set; }
    public SyllabusItem? SyllabusItem { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public McqPracticeMode Mode { get; set; }
    public McqContentStatus Status { get; set; } = McqContentStatus.Draft;
    public int QuestionCount { get; set; }
    public int? DurationSeconds { get; set; }
    public int? BcsExamYear { get; set; }
    public McqDifficulty? Difficulty { get; set; }
    public decimal MarksCorrect { get; set; }
    public decimal MarksIncorrect { get; set; }
    public decimal MarksUnanswered { get; set; }
    public bool ShowFeedbackImmediately { get; set; }
    public Guid CreatedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class McqAttempt
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ExamSetId { get; set; }
    public McqExamSet ExamSet { get; set; } = null!;
    public string ExamSetNameSnapshot { get; set; } = string.Empty;
    public McqPracticeMode Mode { get; set; }
    public McqAttemptStatus Status { get; set; } = McqAttemptStatus.InProgress;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public int? DurationSeconds { get; set; }
    public decimal MarksCorrect { get; set; }
    public decimal MarksIncorrect { get; set; }
    public decimal MarksUnanswered { get; set; }
    public bool ShowFeedbackImmediately { get; set; }
    public bool IsTimedOut { get; set; }
    public decimal? Score { get; set; }
    public decimal MaximumScore { get; set; }
    public int CorrectCount { get; set; }
    public int IncorrectCount { get; set; }
    public int UnansweredCount { get; set; }
    public ICollection<McqAttemptQuestion> Questions { get; set; } = [];
}

public sealed class McqAttemptQuestion
{
    public Guid Id { get; set; }
    public Guid AttemptId { get; set; }
    public McqAttempt Attempt { get; set; } = null!;
    public Guid QuestionId { get; set; }
    public Guid SyllabusItemIdSnapshot { get; set; }
    public string TopicTitleSnapshot { get; set; } = string.Empty;
    public string? ParentTopicTitleSnapshot { get; set; }
    public int Position { get; set; }
    public string PromptSnapshot { get; set; } = string.Empty;
    public string? ExplanationSnapshot { get; set; }
    public string? ImageUrlSnapshot { get; set; }
    public string SourcesSnapshotJson { get; set; } = "[]";
    public McqQuestionType Type { get; set; }
    public McqDifficulty Difficulty { get; set; }
    public ICollection<McqAttemptOption> Options { get; set; } = [];
    public McqAttemptAnswer? Answer { get; set; }
}

public sealed class McqAttemptOption
{
    public Guid Id { get; set; }
    public Guid AttemptQuestionId { get; set; }
    public McqAttemptQuestion AttemptQuestion { get; set; } = null!;
    public Guid SourceOptionId { get; set; }
    public string TextSnapshot { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsCorrect { get; set; }
}

public sealed class McqAttemptAnswer
{
    public Guid AttemptQuestionId { get; set; }
    public McqAttemptQuestion AttemptQuestion { get; set; } = null!;
    public string SelectedOptionIdsJson { get; set; } = "[]";
    public bool IsMarkedForReview { get; set; }
    public bool? IsCorrect { get; set; }
    public decimal? MarksAwarded { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class McqBookmark
{
    public Guid UserId { get; set; }
    public Guid QuestionId { get; set; }
    public McqQuestion Question { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class McqMistake
{
    public Guid UserId { get; set; }
    public Guid QuestionId { get; set; }
    public McqQuestion Question { get; set; } = null!;
    public Guid? LastAttemptId { get; set; }
    public McqAttempt? LastAttempt { get; set; }
    public McqMistakeStatus Status { get; set; } = McqMistakeStatus.Open;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
