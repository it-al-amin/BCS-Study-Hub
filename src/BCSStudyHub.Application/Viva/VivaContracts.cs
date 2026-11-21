using System.ComponentModel.DataAnnotations;
using BCSStudyHub.Application.Common;

namespace BCSStudyHub.Application.Viva;

public sealed record VivaCategoryInput : IValidatableObject
{
    [Required, StringLength(80, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Required, StringLength(80, MinimumLength = 2)]
    public string Slug { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }

    [Range(0, 10000)]
    public int DisplayOrder { get; init; }

    public bool IsActive { get; init; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Slug.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
        {
            yield return new ValidationResult("Category slugs may contain only ASCII letters, numbers, and hyphens.", [nameof(Slug)]);
        }
    }
}

public sealed record VivaQuestionReferenceInput
{
    [Required]
    public string Type { get; init; } = string.Empty;

    [Required, StringLength(300, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [Required, StringLength(180, MinimumLength = 1)]
    public string Publisher { get; init; } = string.Empty;

    [Required, StringLength(512, MinimumLength = 8)]
    public string Url { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Citation { get; init; }

    [Required]
    public string VerificationStatus { get; init; } = "Unverified";
}

public sealed record VivaQuestionInput : IValidatableObject
{
    [Required]
    public Guid CategoryId { get; init; }

    [StringLength(160)]
    public string? Topic { get; init; }

    [Required, StringLength(10000, MinimumLength = 2)]
    public string Prompt { get; init; } = string.Empty;

    [Range(1900, 2200)]
    public int? BcsExamYear { get; init; }

    [Range(1, 100)]
    public int? ExamNumber { get; init; }

    [StringLength(160)]
    public string? ExamReference { get; init; }

    [MaxLength(20)]
    public IReadOnlyList<VivaQuestionReferenceInput> References { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CategoryId == Guid.Empty)
        {
            yield return new ValidationResult("A Viva category is required.", [nameof(CategoryId)]);
        }

        if (References is null || References.Count > 20 || References.Any(reference => reference is null))
        {
            yield return new ValidationResult("A question may have at most twenty valid references.", [nameof(References)]);
        }
    }
}

public sealed record VivaQuestionQuery(
    string? Search,
    Guid? CategoryId,
    string? Topic,
    bool BookmarkedOnly,
    int PageNumber,
    int PageSize,
    string SortBy);

public sealed record VivaCategoryDto(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    int DisplayOrder,
    bool IsActive,
    int QuestionCount);

public sealed record VivaQuestionReferenceDto(
    Guid Id,
    string Type,
    string Title,
    string Publisher,
    string Url,
    string? Citation,
    string VerificationStatus);

public sealed record VivaQuestionDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string? Topic,
    string Prompt,
    int? BcsExamYear,
    int? ExamNumber,
    string? ExamReference,
    bool IsBookmarked,
    IReadOnlyList<VivaQuestionReferenceDto> References);

public sealed record VivaAdminQuestionDto(VivaQuestionDto Question, string Status);

public sealed record VivaAnswerNoteInput(
    [property: StringLength(20000)] string Content);

public sealed record VivaAnswerNoteRevisionDto(int RevisionNumber, string Content, DateTimeOffset CreatedAt);

public sealed record VivaAnswerNoteDto(
    Guid QuestionId,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<VivaAnswerNoteRevisionDto> Revisions);

public sealed record VivaQuestionHistoryDto(
    Guid QuestionId,
    string Prompt,
    string CategoryName,
    string? Topic,
    DateTimeOffset LastViewedAt,
    int ViewCount);

public sealed record VivaMockSessionInput : IValidatableObject
{
    [Required, StringLength(160, MinimumLength = 2)]
    public string Title { get; init; } = string.Empty;

    [Required, MinLength(1), MaxLength(50)]
    public IReadOnlyList<Guid> QuestionIds { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (QuestionIds is null || QuestionIds.Count is < 1 or > 50 ||
            QuestionIds.Any(id => id == Guid.Empty) ||
            QuestionIds.Distinct().Count() != QuestionIds.Count)
        {
            yield return new ValidationResult("Select one to fifty unique published Viva questions.", [nameof(QuestionIds)]);
        }
    }
}

public sealed record VivaMockSessionQuestionInput(
    [property: StringLength(20000)] string AnswerNotes,
    bool NeedsRevision);

public sealed record VivaSessionEvaluationInput : IValidatableObject
{
    [Range(1, 5)]
    public int? CommunicationRating { get; init; }

    [Range(1, 5)]
    public int? SubjectKnowledgeRating { get; init; }

    [Range(1, 5)]
    public int? ClarityRating { get; init; }

    [Range(1, 5)]
    public int? ConfidenceRating { get; init; }

    [StringLength(4000)]
    public string? FeedbackNotes { get; init; }

    [StringLength(4000)]
    public string? ImprovementAreas { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CommunicationRating is null && SubjectKnowledgeRating is null && ClarityRating is null &&
            ConfidenceRating is null && string.IsNullOrWhiteSpace(FeedbackNotes) && string.IsNullOrWhiteSpace(ImprovementAreas))
        {
            yield return new ValidationResult("Provide a self-evaluation rating or feedback before completing the mock Viva.", [nameof(FeedbackNotes)]);
        }
    }
}

public sealed record VivaMockSessionQuestionDto(
    Guid Id,
    Guid QuestionId,
    int Sequence,
    string Prompt,
    string CategoryName,
    string? Topic,
    string AnswerNotes,
    bool NeedsRevision);

public sealed record VivaMockSessionDto(
    Guid Id,
    string Title,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    int? CommunicationRating,
    int? SubjectKnowledgeRating,
    int? ClarityRating,
    int? ConfidenceRating,
    string? FeedbackNotes,
    string? ImprovementAreas,
    IReadOnlyList<VivaMockSessionQuestionDto> Questions);

public sealed record VivaMockSessionListDto(
    Guid Id,
    string Title,
    string Status,
    int QuestionCount,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    int? OverallRating);

public sealed record VivaWeakTopicDto(string CategoryName, string? Topic, int RevisionCount);

public sealed record VivaProfileInput(
    [property: StringLength(5000)] string SelfIntroduction,
    [property: StringLength(5000)] string AcademicBackground,
    [property: StringLength(5000)] string UniversityAndSubject,
    [property: StringLength(5000)] string HomeDistrict,
    [property: StringLength(5000)] string ProfessionalExperience,
    [property: StringLength(5000)] string CadrePreferences,
    [property: StringLength(5000)] string InterestsAndHobbies,
    [property: StringLength(5000)] string StrengthsAndDevelopmentAreas);

public sealed record VivaProfileDto(
    string SelfIntroduction,
    string AcademicBackground,
    string UniversityAndSubject,
    string HomeDistrict,
    string ProfessionalExperience,
    string CadrePreferences,
    string InterestsAndHobbies,
    string StrengthsAndDevelopmentAreas,
    DateTimeOffset? UpdatedAt);

public interface IVivaService
{
    Task<IReadOnlyList<VivaCategoryDto>> GetCategoriesAsync(bool includeInactive, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetTopicsAsync(Guid? categoryId, CancellationToken cancellationToken);
    Task<PagedResponse<VivaQuestionDto>> GetQuestionsAsync(VivaQuestionQuery query, Guid? userId, CancellationToken cancellationToken);
    Task<VivaQuestionDto?> GetQuestionAsync(Guid questionId, Guid? userId, CancellationToken cancellationToken);
    Task<PagedResponse<VivaAdminQuestionDto>> GetAdminQuestionsAsync(string? search, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<VivaAdminQuestionDto?> GetAdminQuestionAsync(Guid questionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<VivaCategoryDto>> GetAdminCategoriesAsync(CancellationToken cancellationToken);
    Task<VivaCategoryDto> CreateCategoryAsync(VivaCategoryInput input, Guid actorId, CancellationToken cancellationToken);
    Task<VivaCategoryDto?> UpdateCategoryAsync(Guid id, VivaCategoryInput input, Guid actorId, CancellationToken cancellationToken);
    Task<VivaAdminQuestionDto> CreateQuestionAsync(VivaQuestionInput input, Guid actorId, CancellationToken cancellationToken);
    Task<VivaAdminQuestionDto?> UpdateQuestionAsync(Guid id, VivaQuestionInput input, Guid actorId, CancellationToken cancellationToken);
    Task<VivaAdminQuestionDto?> SetQuestionStatusAsync(Guid id, string status, Guid actorId, CancellationToken cancellationToken);
    Task<bool> AddBookmarkAsync(Guid userId, Guid questionId, CancellationToken cancellationToken);
    Task<bool> RemoveBookmarkAsync(Guid userId, Guid questionId, CancellationToken cancellationToken);
    Task<VivaAnswerNoteDto?> GetAnswerNoteAsync(Guid userId, Guid questionId, CancellationToken cancellationToken);
    Task<VivaAnswerNoteDto> SaveAnswerNoteAsync(Guid userId, Guid questionId, VivaAnswerNoteInput input, CancellationToken cancellationToken);
    Task<PagedResponse<VivaQuestionHistoryDto>> GetQuestionHistoryAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<PagedResponse<VivaMockSessionListDto>> GetSessionsAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<VivaMockSessionDto> CreateSessionAsync(Guid userId, VivaMockSessionInput input, CancellationToken cancellationToken);
    Task<VivaMockSessionDto?> GetSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);
    Task<VivaMockSessionDto?> UpdateSessionQuestionAsync(Guid userId, Guid sessionId, Guid sessionQuestionId, VivaMockSessionQuestionInput input, CancellationToken cancellationToken);
    Task<VivaMockSessionDto?> CompleteSessionAsync(Guid userId, Guid sessionId, VivaSessionEvaluationInput input, CancellationToken cancellationToken);
    Task<IReadOnlyList<VivaWeakTopicDto>> GetWeakTopicsAsync(Guid userId, CancellationToken cancellationToken);
    Task<VivaProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken);
    Task<VivaProfileDto> SaveProfileAsync(Guid userId, VivaProfileInput input, CancellationToken cancellationToken);
}
