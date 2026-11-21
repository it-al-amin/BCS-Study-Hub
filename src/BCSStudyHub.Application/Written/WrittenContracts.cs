using System.ComponentModel.DataAnnotations;
using BCSStudyHub.Application.Common;

namespace BCSStudyHub.Application.Written;

public sealed record WrittenQuestionInput : IValidatableObject
{
    [Required]
    public Guid SyllabusItemId { get; init; }

    [Required, StringLength(12000, MinimumLength = 1)]
    public string Prompt { get; init; } = string.Empty;

    [Required]
    public string Type { get; init; } = string.Empty;

    [Range(1900, 2200)]
    public int? BcsExamYear { get; init; }

    [Range(1, 100)]
    public int? ExamNumber { get; init; }

    [StringLength(160)]
    public string? ExamReference { get; init; }

    [Range(typeof(decimal), "0", "1000")]
    public decimal? Marks { get; init; }

    [StringLength(8000)]
    public string? AnswerGuidance { get; init; }

    [Required, MinLength(1), MaxLength(10)]
    public IReadOnlyList<WrittenSourceInput> Sources { get; init; } = [];

    [MaxLength(20)]
    public IReadOnlyList<WrittenResourceInput> Resources { get; init; } = [];

    [MaxLength(10)]
    public IReadOnlyList<string> Tags { get; init; } = [];

    [StringLength(100000)]
    public string? ModelAnswerContent { get; init; }

    [MaxLength(10)]
    public IReadOnlyList<WrittenSourceInput> ModelAnswerReferences { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Sources is null || Sources.Count == 0 || Sources.Any(source => source is null))
        {
            yield return new ValidationResult("At least one valid question source is required.", [nameof(Sources)]);
        }

        if (Resources is null || Resources.Any(resource => resource is null))
        {
            yield return new ValidationResult("The related-resource list cannot contain null values.", [nameof(Resources)]);
        }

        if (Tags is null ||
            Tags.Any(tag => string.IsNullOrWhiteSpace(tag)) ||
            Tags.Select(tag => tag.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Tags.Count)
        {
            yield return new ValidationResult("Tags must be non-empty and unique.", [nameof(Tags)]);
        }

        if (!Enum.TryParse<Domain.Entities.WrittenQuestionType>(Type, true, out _))
        {
            yield return new ValidationResult("The Written question type is not supported.", [nameof(Type)]);
        }

        if (!string.IsNullOrWhiteSpace(ModelAnswerContent) &&
            (ModelAnswerReferences is null || ModelAnswerReferences.Count == 0 || ModelAnswerReferences.Any(source => source is null)))
        {
            yield return new ValidationResult("A model answer requires at least one valid reference.", [nameof(ModelAnswerReferences)]);
        }
    }
}

public sealed record WrittenSourceInput
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

public sealed record WrittenResourceInput
{
    [Required, StringLength(300, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [Required, StringLength(512, MinimumLength = 8)]
    public string Url { get; init; } = string.Empty;
}

public sealed record WrittenQuestionQuery(
    string? Search,
    Guid? SyllabusVersionId,
    Guid? SubjectId,
    Guid? SyllabusItemId,
    string? Type,
    int? BcsExamYear,
    string? Tag,
    bool BookmarkedOnly,
    int PageNumber,
    int PageSize,
    string SortBy);

public sealed record WrittenQuestionSourceDto(
    Guid Id,
    string Title,
    string Publisher,
    string Url,
    string? Citation,
    DateOnly? PublishedOn,
    string VerificationStatus);

public sealed record WrittenQuestionResourceDto(Guid Id, string Title, string Url);

public sealed record WrittenQuestionDto(
    Guid Id,
    Guid SyllabusItemId,
    Guid SubjectId,
    string SubjectName,
    Guid SyllabusVersionId,
    string SyllabusVersionLabel,
    string TopicTitle,
    string? ParentTopicTitle,
    Guid? PaperId,
    string? PaperName,
    string Prompt,
    string Type,
    int? BcsExamYear,
    int? ExamNumber,
    string? ExamReference,
    decimal? Marks,
    string? AnswerGuidance,
    string Status,
    bool IsBookmarked,
    bool HasModelAnswer,
    IReadOnlyList<WrittenQuestionSourceDto> Sources,
    IReadOnlyList<WrittenQuestionResourceDto> Resources,
    IReadOnlyList<string> Tags);

public sealed record WrittenAdminQuestionDto(
    WrittenQuestionDto Question,
    string? ModelAnswerContent,
    string? ModelAnswerStatus,
    IReadOnlyList<WrittenQuestionSourceDto> ModelAnswerReferences);

public sealed record WrittenModelAnswerDto(
    Guid QuestionId,
    string Content,
    IReadOnlyList<WrittenQuestionSourceDto> References);

public sealed record WrittenSelfAssessmentInput : IValidatableObject
{
    [Range(1, 5)]
    public int? ContentCoverage { get; init; }

    [Range(1, 5)]
    public int? Organization { get; init; }

    [Range(1, 5)]
    public int? Clarity { get; init; }

    [Range(1, 5)]
    public int? LanguageAccuracy { get; init; }

    [StringLength(2000)]
    public string? Reflection { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ContentCoverage is null && Organization is null && Clarity is null && LanguageAccuracy is null &&
            string.IsNullOrWhiteSpace(Reflection))
        {
            yield return new ValidationResult("Provide at least one personal self-assessment value.", [nameof(ContentCoverage)]);
        }
    }
}

public sealed record UpdateWrittenAnswerRequest : IValidatableObject
{
    [Required, StringLength(50000, MinimumLength = 1)]
    public string Content { get; init; } = string.Empty;

    [Range(0, 86400)]
    public int TimeSpentSeconds { get; init; }

    public WrittenSelfAssessmentInput? SelfAssessment { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        SelfAssessment?.Validate(validationContext) ?? [];
}

public sealed record WrittenAnswerDto(
    Guid Id,
    Guid QuestionId,
    string QuestionPrompt,
    string QuestionType,
    Guid SyllabusItemId,
    string TopicTitle,
    string Status,
    string Content,
    int WordCount,
    int TimeSpentSeconds,
    WrittenSelfAssessmentInput? SelfAssessment,
    DateTimeOffset StartedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? SubmittedAt);

public sealed record WrittenAnswerListItemDto(
    Guid Id,
    Guid QuestionId,
    string QuestionPrompt,
    string QuestionType,
    Guid SyllabusItemId,
    string TopicTitle,
    string Status,
    int WordCount,
    DateTimeOffset StartedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? SubmittedAt);

public sealed record WrittenAnswerRevisionDto(
    int RevisionNumber,
    string Kind,
    string Content,
    int WordCount,
    int TimeSpentSeconds,
    WrittenSelfAssessmentInput? SelfAssessment,
    DateTimeOffset CreatedAt);

public sealed record WrittenBookmarkDto(
    Guid QuestionId,
    string Prompt,
    string Type,
    Guid SyllabusItemId,
    string TopicTitle,
    DateTimeOffset CreatedAt);

public sealed record WrittenGoalInput : IValidatableObject
{
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }

    [Range(1, 100)]
    public int TargetSubmissions { get; init; }

    [Range(1, 1000000)]
    public int TargetWords { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate < StartDate || EndDate.DayNumber - StartDate.DayNumber > 366)
        {
            yield return new ValidationResult("A writing goal must end after it starts and span no more than one year.", [nameof(EndDate)]);
        }
    }
}

public sealed record WrittenGoalDto(
    Guid Id,
    DateOnly StartDate,
    DateOnly EndDate,
    int TargetSubmissions,
    int TargetWords,
    int CompletedSubmissions,
    int CompletedWords);

public sealed record WrittenTopicProgressDto(
    Guid SyllabusItemId,
    string TopicTitle,
    int SubmittedAnswers,
    int? AverageSelfAssessment);

public sealed record WrittenProgressDto(
    DateOnly From,
    DateOnly To,
    int DraftCount,
    int SubmittedCount,
    int SubmittedWords,
    int WritingTimeSeconds,
    IReadOnlyList<WrittenTopicProgressDto> Topics);

public sealed record WrittenStudyNoteInput(
    [property: Required] Guid SyllabusItemId,
    [property: Required, StringLength(200, MinimumLength = 1)] string Title,
    [property: Required, StringLength(30000, MinimumLength = 1)] string Content);

public sealed record WrittenStudyNoteDto(
    Guid Id,
    Guid SyllabusItemId,
    Guid SyllabusVersionId,
    string TopicTitle,
    string Title,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public interface IWrittenService
{
    Task<PagedResponse<WrittenQuestionDto>> GetQuestionsAsync(
        WrittenQuestionQuery query,
        Guid? userId,
        CancellationToken cancellationToken);
    Task<WrittenQuestionDto?> GetQuestionAsync(Guid id, Guid? userId, CancellationToken cancellationToken);
    Task<WrittenModelAnswerDto?> GetModelAnswerAsync(Guid questionId, CancellationToken cancellationToken);
    Task<PagedResponse<WrittenAdminQuestionDto>> GetAdminQuestionsAsync(
        string? search,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
    Task<WrittenAdminQuestionDto?> GetAdminQuestionAsync(Guid id, CancellationToken cancellationToken);
    Task<WrittenAdminQuestionDto> CreateQuestionAsync(WrittenQuestionInput input, Guid actorId, CancellationToken cancellationToken);
    Task<WrittenAdminQuestionDto?> UpdateQuestionAsync(Guid id, WrittenQuestionInput input, Guid actorId, CancellationToken cancellationToken);
    Task<WrittenAdminQuestionDto?> SetQuestionStatusAsync(Guid id, string status, Guid actorId, CancellationToken cancellationToken);
    Task<WrittenAdminQuestionDto?> SetModelAnswerStatusAsync(Guid questionId, string status, Guid actorId, CancellationToken cancellationToken);
    Task<WrittenAnswerDto> CreateAnswerAsync(Guid userId, Guid questionId, CancellationToken cancellationToken);
    Task<WrittenAnswerDto?> GetAnswerAsync(Guid userId, Guid answerId, CancellationToken cancellationToken);
    Task<PagedResponse<WrittenAnswerListItemDto>> GetAnswersAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<WrittenAnswerDto?> UpdateAnswerAsync(Guid userId, Guid answerId, UpdateWrittenAnswerRequest request, CancellationToken cancellationToken);
    Task<WrittenAnswerDto?> SubmitAnswerAsync(Guid userId, Guid answerId, UpdateWrittenAnswerRequest request, CancellationToken cancellationToken);
    Task<WrittenAnswerDto?> ReviseAnswerAsync(Guid userId, Guid answerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WrittenAnswerRevisionDto>?> GetAnswerRevisionsAsync(Guid userId, Guid answerId, CancellationToken cancellationToken);
    Task<PagedResponse<WrittenBookmarkDto>> GetBookmarksAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<bool> AddBookmarkAsync(Guid userId, Guid questionId, CancellationToken cancellationToken);
    Task<bool> RemoveBookmarkAsync(Guid userId, Guid questionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WrittenGoalDto>> GetGoalsAsync(Guid userId, CancellationToken cancellationToken);
    Task<WrittenGoalDto> CreateGoalAsync(Guid userId, WrittenGoalInput input, CancellationToken cancellationToken);
    Task<WrittenGoalDto?> UpdateGoalAsync(Guid userId, Guid goalId, WrittenGoalInput input, CancellationToken cancellationToken);
    Task<WrittenProgressDto> GetProgressAsync(Guid userId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken);
    Task<PagedResponse<WrittenStudyNoteDto>> GetStudyNotesAsync(
        Guid userId,
        Guid? syllabusItemId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
    Task<WrittenStudyNoteDto> CreateStudyNoteAsync(Guid userId, WrittenStudyNoteInput input, CancellationToken cancellationToken);
    Task<WrittenStudyNoteDto?> UpdateStudyNoteAsync(Guid userId, Guid noteId, WrittenStudyNoteInput input, CancellationToken cancellationToken);
    Task<bool> DeleteStudyNoteAsync(Guid userId, Guid noteId, CancellationToken cancellationToken);
}
