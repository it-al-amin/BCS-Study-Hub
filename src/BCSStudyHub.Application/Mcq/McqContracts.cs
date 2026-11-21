using System.ComponentModel.DataAnnotations;
using BCSStudyHub.Application.Common;
using BCSStudyHub.Domain.Entities;

namespace BCSStudyHub.Application.Mcq;

public sealed record McqQuestionInput : IValidatableObject
{
    [Required]
    public Guid SyllabusItemId { get; init; }

    [Required, StringLength(5000, MinimumLength = 1)]
    public string Prompt { get; init; } = string.Empty;

    [StringLength(8000)]
    public string? Explanation { get; init; }

    [StringLength(1024)]
    public string? ImageUrl { get; init; }

    [Required]
    public string Type { get; init; } = string.Empty;

    [Required]
    public string Difficulty { get; init; } = string.Empty;

    [Range(1900, 2200)]
    public int? BcsExamYear { get; init; }

    [Range(1, 100)]
    public int? ExamNumber { get; init; }

    [StringLength(160)]
    public string? ExamReference { get; init; }

    [Required, MinLength(2), MaxLength(8)]
    public IReadOnlyList<McqOptionInput> Options { get; init; } = [];

    [Required, MinLength(1), MaxLength(10)]
    public IReadOnlyList<McqQuestionSourceInput> Sources { get; init; } = [];

    [MaxLength(10)]
    public IReadOnlyList<string> Tags { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Options is null)
        {
            yield return new ValidationResult("At least two options are required.", [nameof(Options)]);
            yield break;
        }

        if (Options.Any(option => option is null))
        {
            yield return new ValidationResult("Question options cannot be null.", [nameof(Options)]);
            yield break;
        }

        if (Sources is null || Sources.Count == 0)
        {
            yield return new ValidationResult("At least one source reference is required.", [nameof(Sources)]);
        }
        else if (Sources.Any(source => source is null))
        {
            yield return new ValidationResult("Question sources cannot be null.", [nameof(Sources)]);
        }

        if (Tags is null)
        {
            yield return new ValidationResult("The tag list cannot be null.", [nameof(Tags)]);
            yield break;
        }

        var correctCount = Options.Count(option => option.IsCorrect);
        if (string.Equals(Type, "SingleAnswer", StringComparison.OrdinalIgnoreCase) && correctCount != 1)
        {
            yield return new ValidationResult(
                "A single-answer question must have exactly one correct option.",
                [nameof(Options)]);
        }

        if (string.Equals(Type, "MultipleAnswer", StringComparison.OrdinalIgnoreCase) && correctCount < 2)
        {
            yield return new ValidationResult(
                "A multiple-answer question must have at least two correct options.",
                [nameof(Options)]);
        }

        if (!Enum.TryParse<McqQuestionType>(Type, true, out _))
        {
            yield return new ValidationResult("The question type is not supported.", [nameof(Type)]);
        }

        if (!Enum.TryParse<McqDifficulty>(Difficulty, true, out _))
        {
            yield return new ValidationResult("The difficulty is not supported.", [nameof(Difficulty)]);
        }

        if (Options.Select(option => option.Text.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Options.Count)
        {
            yield return new ValidationResult("Question options must be unique.", [nameof(Options)]);
        }

        if (Options.Any(option => string.IsNullOrWhiteSpace(option.Text)))
        {
            yield return new ValidationResult("Question options must not be empty.", [nameof(Options)]);
        }

        if (Tags.Any(tag => string.IsNullOrWhiteSpace(tag)) ||
            Tags.Select(tag => tag.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Tags.Count)
        {
            yield return new ValidationResult("Question tags must be non-empty and unique.", [nameof(Tags)]);
        }
    }
}

public sealed record McqOptionInput
{
    [Required, StringLength(2000, MinimumLength = 1)]
    public string Text { get; init; } = string.Empty;
    public bool IsCorrect { get; init; }
}

public sealed record McqQuestionSourceInput
{
    [Required, StringLength(300, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [Required, StringLength(180, MinimumLength = 1)]
    public string Publisher { get; init; } = string.Empty;

    [Required, StringLength(512, MinimumLength = 8)]
    public string Url { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Citation { get; init; }

    public DateOnly? PublishedOn { get; init; }

    [Required]
    public string VerificationStatus { get; init; } = "Unverified";
}

public sealed record McqQuestionQuery(
    string? Search,
    Guid? SyllabusVersionId,
    Guid? SubjectId,
    Guid? SyllabusItemId,
    int? BcsExamYear,
    string? Difficulty,
    string? Tag,
    bool BookmarkedOnly,
    int PageNumber,
    int PageSize,
    string SortBy);

public sealed record McqQuestionDto(
    Guid Id,
    Guid SyllabusItemId,
    Guid SubjectId,
    string SubjectName,
    string TopicTitle,
    string? ParentTopicTitle,
    string Prompt,
    string? ImageUrl,
    string Type,
    string Difficulty,
    string Status,
    int? BcsExamYear,
    int? ExamNumber,
    string? ExamReference,
    bool IsBookmarked,
    IReadOnlyList<McqOptionDto> Options,
    IReadOnlyList<McqQuestionSourceDto> Sources,
    IReadOnlyList<string> Tags);

public sealed record McqOptionDto(Guid Id, string Text, int SortOrder, bool? IsCorrect = null);

public sealed record McqQuestionSourceDto(
    Guid Id,
    string Title,
    string Publisher,
    string Url,
    string? Citation,
    DateOnly? PublishedOn,
    string VerificationStatus);

public sealed record McqAdminQuestionDto(
    Guid Id,
    Guid SyllabusItemId,
    Guid SyllabusVersionId,
    string Prompt,
    string? Explanation,
    string? ImageUrl,
    string Type,
    string Difficulty,
    string Status,
    int? BcsExamYear,
    int? ExamNumber,
    string? ExamReference,
    IReadOnlyList<McqOptionDto> Options,
    IReadOnlyList<McqQuestionSourceDto> Sources,
    IReadOnlyList<string> Tags);

public sealed record McqExamSetInput : IValidatableObject
{
    [Required]
    public Guid SyllabusVersionId { get; init; }

    public Guid? SubjectId { get; init; }
    public Guid? SyllabusItemId { get; init; }

    [Required, StringLength(180, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(3000)]
    public string Instructions { get; init; } = string.Empty;

    [Required]
    public string Mode { get; init; } = string.Empty;

    [Range(1, 200)]
    public int QuestionCount { get; init; }

    [Range(1, 86400)]
    public int? DurationSeconds { get; init; }

    [Range(1900, 2200)]
    public int? BcsExamYear { get; init; }

    public string? Difficulty { get; init; }

    [Range(typeof(decimal), "-100", "100")]
    public decimal MarksCorrect { get; init; }

    [Range(typeof(decimal), "-100", "100")]
    public decimal MarksIncorrect { get; init; }

    [Range(typeof(decimal), "-100", "100")]
    public decimal MarksUnanswered { get; init; }

    public bool ShowFeedbackImmediately { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enum.TryParse<McqPracticeMode>(Mode, true, out _))
        {
            yield return new ValidationResult("The practice mode is not supported.", [nameof(Mode)]);
        }

        if (!string.IsNullOrWhiteSpace(Difficulty) &&
            !Enum.TryParse<McqDifficulty>(Difficulty, true, out _))
        {
            yield return new ValidationResult("The difficulty is not supported.", [nameof(Difficulty)]);
        }
    }
}

public sealed record McqExamSetDto(
    Guid Id,
    Guid SyllabusVersionId,
    string SyllabusVersionLabel,
    Guid? SubjectId,
    string? SubjectName,
    Guid? SyllabusItemId,
    string? SyllabusItemTitle,
    string Name,
    string Instructions,
    string Mode,
    string Status,
    int QuestionCount,
    int? DurationSeconds,
    int? BcsExamYear,
    string? Difficulty,
    decimal MarksCorrect,
    decimal MarksIncorrect,
    decimal MarksUnanswered,
    bool ShowFeedbackImmediately);

public sealed record McqAttemptQuestionDto(
    Guid Id,
    Guid QuestionId,
    int Position,
    string Prompt,
    string? ImageUrl,
    string Type,
    string Difficulty,
    IReadOnlyList<McqOptionDto> Options,
    IReadOnlyList<Guid> SelectedOptionIds,
    bool IsMarkedForReview);

public sealed record McqAttemptDto(
    Guid Id,
    Guid ExamSetId,
    string ExamSetName,
    string Mode,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? SubmittedAt,
    bool IsTimedOut,
    decimal? Score,
    decimal MaximumScore,
    int CorrectCount,
    int IncorrectCount,
    int UnansweredCount,
    IReadOnlyList<McqAttemptQuestionDto> Questions);

public sealed record UpdateMcqAnswerRequest(
    [Required, MaxLength(8)] IReadOnlyList<Guid> SelectedOptionIds,
    bool IsMarkedForReview);

public sealed record McqAnswerUpdateDto(
    Guid QuestionId,
    IReadOnlyList<Guid> SelectedOptionIds,
    bool IsMarkedForReview,
    bool? IsCorrect,
    string? Explanation);

public sealed record McqAttemptReviewQuestionDto(
    Guid QuestionId,
    int Position,
    string Prompt,
    string? ImageUrl,
    string? Explanation,
    string Type,
    string Difficulty,
    IReadOnlyList<McqOptionDto> Options,
    IReadOnlyList<Guid> SelectedOptionIds,
    bool IsMarkedForReview,
    bool IsCorrect,
    decimal MarksAwarded,
    Guid SyllabusItemId,
    string TopicTitle,
    string? ParentTopicTitle,
    IReadOnlyList<McqQuestionSourceDto> Sources);

public sealed record McqAttemptReviewDto(McqAttemptDto Attempt, IReadOnlyList<McqAttemptReviewQuestionDto> Questions);

public sealed record McqAttemptListItemDto(
    Guid Id,
    Guid ExamSetId,
    string ExamSetName,
    string Mode,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? SubmittedAt,
    bool IsTimedOut,
    decimal? Score,
    decimal MaximumScore,
    int CorrectCount,
    int IncorrectCount,
    int UnansweredCount);

public sealed record McqMistakeDto(
    Guid QuestionId,
    string Prompt,
    string Difficulty,
    Guid SyllabusItemId,
    string TopicTitle,
    string? ParentTopicTitle,
    string Status,
    string? Notes,
    Guid? LastAttemptId,
    DateTimeOffset UpdatedAt);

public sealed record UpdateMcqMistakeRequest(
    [Required] string Status,
    [StringLength(2000)] string? Notes);

public sealed record McqTopicPerformanceDto(
    Guid SyllabusItemId,
    string TopicTitle,
    long AnswerCount,
    long CorrectCount,
    decimal AccuracyPercent);

public sealed record McqAnalyticsDto(
    long AttemptCount,
    long AnswerCount,
    long CorrectCount,
    long IncorrectCount,
    long UnansweredCount,
    decimal AccuracyPercent,
    decimal? AverageScore,
    IReadOnlyList<McqTopicPerformanceDto> TopicPerformance,
    IReadOnlyList<McqAttemptListItemDto> RecentAttempts);

public interface IMcqService
{
    Task<PagedResponse<McqQuestionDto>> GetQuestionsAsync(
        McqQuestionQuery query,
        Guid? userId,
        CancellationToken cancellationToken);
    Task<McqQuestionDto?> GetQuestionAsync(Guid id, Guid? userId, CancellationToken cancellationToken);
    Task<PagedResponse<McqAdminQuestionDto>> GetAdminQuestionsAsync(
        string? search,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
    Task<McqAdminQuestionDto?> GetAdminQuestionAsync(Guid id, CancellationToken cancellationToken);
    Task<McqAdminQuestionDto> CreateQuestionAsync(
        McqQuestionInput input,
        Guid actorId,
        CancellationToken cancellationToken);
    Task<McqAdminQuestionDto?> UpdateQuestionAsync(
        Guid id,
        McqQuestionInput input,
        Guid actorId,
        CancellationToken cancellationToken);
    Task<McqAdminQuestionDto?> SetQuestionStatusAsync(
        Guid id,
        string status,
        Guid actorId,
        CancellationToken cancellationToken);
    Task<PagedResponse<McqExamSetDto>> GetExamSetsAsync(
        bool includeUnpublished,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
    Task<McqExamSetDto?> GetAdminExamSetAsync(Guid id, CancellationToken cancellationToken);
    Task<McqExamSetDto> CreateExamSetAsync(
        McqExamSetInput input,
        Guid actorId,
        CancellationToken cancellationToken);
    Task<McqExamSetDto?> UpdateExamSetAsync(
        Guid id,
        McqExamSetInput input,
        Guid actorId,
        CancellationToken cancellationToken);
    Task<McqExamSetDto?> SetExamSetStatusAsync(
        Guid id,
        string status,
        Guid actorId,
        CancellationToken cancellationToken);
    Task<McqAttemptDto> CreateAttemptAsync(Guid userId, Guid examSetId, CancellationToken cancellationToken);
    Task<McqAttemptDto?> GetAttemptAsync(Guid userId, Guid attemptId, CancellationToken cancellationToken);
    Task<McqAnswerUpdateDto?> UpdateAnswerAsync(
        Guid userId,
        Guid attemptId,
        Guid attemptQuestionId,
        UpdateMcqAnswerRequest request,
        CancellationToken cancellationToken);
    Task<McqAttemptDto?> SubmitAttemptAsync(Guid userId, Guid attemptId, CancellationToken cancellationToken);
    Task<McqAttemptReviewDto?> GetAttemptReviewAsync(Guid userId, Guid attemptId, CancellationToken cancellationToken);
    Task<PagedResponse<McqAttemptListItemDto>> GetAttemptHistoryAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
    Task<PagedResponse<McqQuestionDto>> GetBookmarksAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
    Task<bool> AddBookmarkAsync(Guid userId, Guid questionId, CancellationToken cancellationToken);
    Task<bool> RemoveBookmarkAsync(Guid userId, Guid questionId, CancellationToken cancellationToken);
    Task<PagedResponse<McqMistakeDto>> GetMistakesAsync(
        Guid userId,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
    Task<McqMistakeDto?> UpdateMistakeAsync(
        Guid userId,
        Guid questionId,
        UpdateMcqMistakeRequest request,
        CancellationToken cancellationToken);
    Task<McqAnalyticsDto> GetAnalyticsAsync(Guid userId, CancellationToken cancellationToken);
}
