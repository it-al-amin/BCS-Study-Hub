using System.ComponentModel.DataAnnotations;
using BCSStudyHub.Application.Common;
using BCSStudyHub.Domain.Entities;

namespace BCSStudyHub.Application.Planner;

public sealed class PlannerException(int statusCode, string title, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
}

internal static class PlannerEnum
{
    public static bool Valid<T>(string? value) where T : struct, Enum =>
        Enum.TryParse<T>(value, true, out var parsed) && Enum.IsDefined(parsed) && !int.TryParse(value, out _);

    public static bool ValidOptional<T>(string? value) where T : struct, Enum => string.IsNullOrWhiteSpace(value) || Valid<T>(value);
}

// ---------- Settings ----------

public sealed record PlannerSettingsInput : IValidatableObject
{
    [Required, MinLength(1), MaxLength(RevisionPolicy.MaxSteps)]
    public IReadOnlyList<int> RevisionIntervalsDays { get; init; } = [];

    public int AgainStep { get; init; } = 1;
    public int HardStepDelta { get; init; }
    public int GoodStepDelta { get; init; } = 1;
    public int EasyStepDelta { get; init; } = 2;
    public int? RetireAfterStep { get; init; }

    [Range(1, 500)]
    public int DailyRevisionLimit { get; init; } = 50;

    [Range(1, 600)]
    public int StreakMinimumMinutes { get; init; } = 15;

    [Range(1, 100)]
    public int WeakTopicAccuracyPercent { get; init; } = 60;

    [Range(1, 1000)]
    public int WeakTopicMinimumAnswers { get; init; } = 5;

    public bool AutoQueueMistakes { get; init; } = true;

    public RevisionPolicy ToPolicy() =>
        new(RevisionIntervalsDays, AgainStep, HardStepDelta, GoodStepDelta, EasyStepDelta, RetireAfterStep);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ToPolicy().Validate() is { } error)
        {
            yield return new ValidationResult(error, [nameof(RevisionIntervalsDays)]);
        }
    }
}

public sealed record PlannerSettingsDto(
    IReadOnlyList<int> RevisionIntervalsDays,
    int AgainStep,
    int HardStepDelta,
    int GoodStepDelta,
    int EasyStepDelta,
    int? RetireAfterStep,
    int DailyRevisionLimit,
    int StreakMinimumMinutes,
    int WeakTopicAccuracyPercent,
    int WeakTopicMinimumAnswers,
    bool AutoQueueMistakes,
    bool IsDefault);

// ---------- Goals ----------

public sealed record StudyGoalInput : IValidatableObject
{
    [Required]
    public string Period { get; init; } = nameof(PlannerPeriod.Weekly);

    /// <summary>Any date inside the period; it is normalised to the period start.</summary>
    public DateOnly Date { get; init; }

    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [Required]
    public string Metric { get; init; } = nameof(GoalMetric.StudyMinutes);

    [Range(1, 100000)]
    public int TargetValue { get; init; }

    public Guid? SubjectId { get; init; }
    public string? Stage { get; init; }

    [StringLength(1000)]
    public string? Notes { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!PlannerEnum.Valid<PlannerPeriod>(Period)) yield return new("The goal period must be Daily, Weekly, or Monthly.", [nameof(Period)]);
        if (!PlannerEnum.Valid<GoalMetric>(Metric)) yield return new("The goal metric is not supported.", [nameof(Metric)]);
        if (!PlannerEnum.ValidOptional<StudyStage>(Stage)) yield return new("The stage is not supported.", [nameof(Stage)]);
        if (Date.Year is < 2000 or > 2200) yield return new("A date within the goal period is required.", [nameof(Date)]);
    }
}

public sealed record StudyGoalDto(
    Guid Id,
    string Period,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string Title,
    string Metric,
    int TargetValue,
    int CurrentValue,
    int PercentComplete,
    Guid? SubjectId,
    string? SubjectName,
    string? Stage,
    string? Notes);

// ---------- Tasks and schedules ----------

public sealed record StudyTaskInput : IValidatableObject
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; init; }

    [Required]
    public string Priority { get; init; } = nameof(TaskPriority.Medium);

    public DateOnly ScheduledDate { get; init; }
    public TimeOnly? StartTime { get; init; }

    [Range(0, 1440)]
    public int PlannedMinutes { get; init; }

    public Guid? SubjectId { get; init; }
    public Guid? SyllabusItemId { get; init; }
    public string? Stage { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!PlannerEnum.Valid<TaskPriority>(Priority)) yield return new("The priority is not supported.", [nameof(Priority)]);
        if (!PlannerEnum.ValidOptional<StudyStage>(Stage)) yield return new("The stage is not supported.", [nameof(Stage)]);
        if (ScheduledDate.Year is < 2000 or > 2200) yield return new("A scheduled date is required.", [nameof(ScheduledDate)]);
    }
}

public sealed record StudyTaskStatusInput : IValidatableObject
{
    [Required]
    public string Status { get; init; } = string.Empty;

    /// <summary>Only used when no sessions are linked to the task.</summary>
    [Range(0, 1440)]
    public int? ActualMinutes { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!PlannerEnum.Valid<StudyTaskStatus>(Status)) yield return new("The task status is not supported.", [nameof(Status)]);
    }
}

public sealed record StudyTaskDto(
    Guid Id,
    string Title,
    string? Description,
    string Priority,
    string Status,
    DateOnly ScheduledDate,
    TimeOnly? StartTime,
    int PlannedMinutes,
    int ActualMinutes,
    bool HasSessions,
    Guid? SubjectId,
    string? SubjectName,
    Guid? SyllabusItemId,
    string? TopicTitle,
    string? Stage,
    Guid? ScheduleId,
    DateTimeOffset? CompletedAt);

public sealed record StudyTaskQuery(DateOnly? From, DateOnly? To, string? Status, Guid? SubjectId, string? Stage, int PageNumber, int PageSize);

public sealed record StudyScheduleInput : IValidatableObject
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [MinLength(1), MaxLength(7)]
    public IReadOnlyList<string> Days { get; init; } = [];

    public TimeOnly? StartTime { get; init; }

    [Range(5, 1440)]
    public int PlannedMinutes { get; init; }

    [Required]
    public string Priority { get; init; } = nameof(TaskPriority.Medium);

    public Guid? SubjectId { get; init; }
    public Guid? SyllabusItemId { get; init; }
    public string? Stage { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool IsActive { get; init; } = true;

    public int DaysMask() =>
        Days.Aggregate(0, (mask, day) => Enum.TryParse<DayOfWeek>(day, true, out var parsed) ? mask | (1 << (int)parsed) : mask);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Days is null || Days.Count == 0 || Days.Any(day => !PlannerEnum.Valid<DayOfWeek>(day)))
            yield return new("Choose at least one valid weekday.", [nameof(Days)]);
        if (!PlannerEnum.Valid<TaskPriority>(Priority)) yield return new("The priority is not supported.", [nameof(Priority)]);
        if (!PlannerEnum.ValidOptional<StudyStage>(Stage)) yield return new("The stage is not supported.", [nameof(Stage)]);
        if (StartDate.Year is < 2000 or > 2200) yield return new("A start date is required.", [nameof(StartDate)]);
        if (EndDate is DateOnly end && end < StartDate) yield return new("The end date cannot be before the start date.", [nameof(EndDate)]);
    }
}

public sealed record StudyScheduleDto(
    Guid Id,
    string Title,
    IReadOnlyList<string> Days,
    TimeOnly? StartTime,
    int PlannedMinutes,
    string Priority,
    Guid? SubjectId,
    string? SubjectName,
    Guid? SyllabusItemId,
    string? TopicTitle,
    string? Stage,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsActive);

public sealed record GenerateTasksInput
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
}

// ---------- Sessions ----------

public sealed record StudySessionInput : IValidatableObject
{
    public DateTimeOffset StartedAt { get; init; }

    [Range(1, 1440)]
    public int DurationMinutes { get; init; }

    [Required]
    public string Activity { get; init; } = nameof(StudyActivity.Reading);

    public Guid? TaskId { get; init; }
    public Guid? SubjectId { get; init; }
    public Guid? SyllabusItemId { get; init; }
    public string? Stage { get; init; }

    [StringLength(2000)]
    public string? Notes { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!PlannerEnum.Valid<StudyActivity>(Activity)) yield return new("The activity is not supported.", [nameof(Activity)]);
        if (!PlannerEnum.ValidOptional<StudyStage>(Stage)) yield return new("The stage is not supported.", [nameof(Stage)]);
        if (StartedAt.Year is < 2000 or > 2200) yield return new("A start time is required.", [nameof(StartedAt)]);
    }
}

public sealed record StartSessionInput : IValidatableObject
{
    [Required]
    public string Activity { get; init; } = nameof(StudyActivity.Reading);

    public Guid? TaskId { get; init; }
    public Guid? SubjectId { get; init; }
    public Guid? SyllabusItemId { get; init; }
    public string? Stage { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!PlannerEnum.Valid<StudyActivity>(Activity)) yield return new("The activity is not supported.", [nameof(Activity)]);
        if (!PlannerEnum.ValidOptional<StudyStage>(Stage)) yield return new("The stage is not supported.", [nameof(Stage)]);
    }
}

public sealed record StudySessionDto(
    Guid Id,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    int DurationMinutes,
    bool IsRunning,
    string Activity,
    Guid? TaskId,
    string? TaskTitle,
    Guid? SubjectId,
    string? SubjectName,
    Guid? SyllabusItemId,
    string? TopicTitle,
    string? Stage,
    string? Notes);

// ---------- Calendar ----------

public sealed record CalendarDayDto(
    DateOnly Date,
    IReadOnlyList<StudyTaskDto> Tasks,
    int StudyMinutes,
    int RevisionDue,
    bool IsActiveDay);

public sealed record CalendarDto(DateOnly From, DateOnly To, IReadOnlyList<CalendarDayDto> Days);

// ---------- Revision ----------

public sealed record RevisionItemInput : IValidatableObject
{
    [Required]
    public string SourceType { get; init; } = string.Empty;

    public Guid? SourceId { get; init; }

    [StringLength(300)]
    public string? Title { get; init; }

    public DateOnly? DueOn { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!PlannerEnum.Valid<RevisionSourceType>(SourceType))
        {
            yield return new("The revision source type is not supported.", [nameof(SourceType)]);
        }
        else if (Enum.Parse<RevisionSourceType>(SourceType, true) == RevisionSourceType.Custom)
        {
            if (string.IsNullOrWhiteSpace(Title)) yield return new("Custom revision items need a title.", [nameof(Title)]);
        }
        else if (SourceId is null || SourceId == Guid.Empty)
        {
            yield return new("Choose the record to revise.", [nameof(SourceId)]);
        }
    }
}

public sealed record RevisionReviewInput
{
    [Required]
    public string Outcome { get; init; } = string.Empty;
}

public sealed record RevisionImportInput
{
    /// <summary>McqBookmarks, WrittenBookmarks, VivaBookmarks, McqMistakes, MistakeNotebook, WeakTopics, WrittenNotes, LibraryNotes, VivaNotes.</summary>
    [Required]
    public string Source { get; init; } = string.Empty;
}

public sealed record RevisionItemDto(
    Guid Id,
    string SourceType,
    Guid? SourceId,
    string Title,
    Guid? SyllabusItemId,
    string? TopicTitle,
    string Reason,
    string Status,
    int Step,
    int IntervalDays,
    DateOnly DueOn,
    bool IsDue,
    int ReviewCount,
    int LapseCount,
    string? LastOutcome,
    DateTimeOffset? LastReviewedAt);

public sealed record RevisionQueueDto(
    IReadOnlyList<RevisionItemDto> Items,
    int DueCount,
    int OverdueCount,
    int ShownLimit,
    int CurrentAffairsDue,
    long TotalCount,
    int PageNumber,
    int PageSize);

public sealed record RevisionLogDto(
    Guid Id,
    string SourceType,
    Guid? SourceId,
    string Title,
    string Outcome,
    int StepBefore,
    int StepAfter,
    int IntervalDays,
    DateTimeOffset ReviewedAt);

public sealed record RevisionImportResultDto(string Source, int Added, int AlreadyQueued);

public sealed record WeakTopicDto(
    Guid SyllabusItemId,
    string Title,
    string SubjectName,
    string StageCode,
    int Answered,
    int Correct,
    int? AccuracyPercent,
    int? Mastery,
    int OpenMistakes,
    IReadOnlyList<string> Reasons);

// ---------- Mistake notebook ----------

public sealed record MistakeInput : IValidatableObject
{
    [Required]
    public string SourceType { get; init; } = nameof(MistakeSourceType.Other);

    public Guid? QuestionId { get; init; }

    [Required, StringLength(8000, MinimumLength = 1)]
    public string QuestionText { get; init; } = string.Empty;

    [StringLength(4000)]
    public string? YourAnswer { get; init; }

    [Required, StringLength(4000, MinimumLength = 1)]
    public string CorrectAnswerReference { get; init; } = string.Empty;

    [StringLength(8000)]
    public string? Explanation { get; init; }

    [Required]
    public string Category { get; init; } = nameof(MistakeCategory.Concept);

    public Guid? SyllabusItemId { get; init; }
    public DateOnly? ReviewOn { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!PlannerEnum.Valid<MistakeSourceType>(SourceType)) yield return new("The mistake source is not supported.", [nameof(SourceType)]);
        if (!PlannerEnum.Valid<MistakeCategory>(Category)) yield return new("The mistake category is not supported.", [nameof(Category)]);
        if (PlannerEnum.Valid<MistakeSourceType>(SourceType) &&
            Enum.Parse<MistakeSourceType>(SourceType, true) != MistakeSourceType.Other && QuestionId is null)
        {
            yield return new("Link the question this mistake came from, or choose 'Other'.", [nameof(QuestionId)]);
        }
    }
}

public sealed record MistakeQuery(string? Search, string? Status, string? Category, string? SourceType, Guid? SyllabusItemId, int PageNumber, int PageSize);

public sealed record MistakeDto(
    Guid Id,
    string SourceType,
    Guid? QuestionId,
    string QuestionText,
    string? YourAnswer,
    string CorrectAnswerReference,
    string? Explanation,
    string Category,
    string Status,
    Guid? SyllabusItemId,
    string? TopicTitle,
    DateOnly? ReviewOn,
    Guid? RevisionItemId,
    IReadOnlyList<RevisionLogDto> History,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ResolvedAt);

// ---------- Progress, reports, dashboard ----------

public sealed record SubjectProgressDto(
    Guid SubjectId,
    string SubjectName,
    int TotalTopics,
    int CompletedTopics,
    int Percent,
    double? AverageMastery,
    int StudyMinutes,
    int McqAnswered,
    int? McqAccuracyPercent);

public sealed record StageProgressDto(
    string StageCode,
    string StageName,
    Guid SyllabusVersionId,
    string VersionLabel,
    string TrackCode,
    int TotalTopics,
    int CompletedTopics,
    int Percent,
    IReadOnlyList<SubjectProgressDto> Subjects);

public sealed record PracticePerformanceDto(
    int McqAttempts,
    int McqAnswered,
    int McqCorrect,
    int? McqAccuracyPercent,
    int WrittenDrafts,
    int WrittenSubmissions,
    int WrittenWords,
    int VivaSessionsCompleted,
    double? VivaAverageRating);

public sealed record StreakDto(int Current, int Longest, int MinimumMinutes);

public sealed record ProgressOverviewDto(
    IReadOnlyList<StageProgressDto> Stages,
    int TotalStudyMinutes,
    int TotalSessions,
    StreakDto Streak,
    PracticePerformanceDto Practice);

public sealed record TopicStatusDto(
    Guid SyllabusItemId,
    string Title,
    string Kind,
    Guid? ParentId,
    Guid SubjectId,
    string SubjectName,
    bool IsCompleted,
    DateTimeOffset? CompletedAt,
    int? Mastery,
    int McqAnswered,
    int? McqAccuracyPercent,
    int StudyMinutes);

public sealed record MasteryInput
{
    [Range(1, 5)]
    public int? Mastery { get; init; }
}

public sealed record NamedMinutesDto(string Key, string Label, int Minutes);

public sealed record DailyMinutesDto(DateOnly Date, int Minutes);

public sealed record SubjectAccuracyDto(Guid SubjectId, string SubjectName, int Answered, int Correct, int? AccuracyPercent);

public sealed record TaskSummaryDto(int Planned, int Completed, int Skipped, int Open, int PlannedMinutes, int ActualMinutes);

public sealed record RevisionStatsDto(
    int Reviews,
    IReadOnlyDictionary<string, int> ByOutcome,
    int? RetentionPercent,
    int ActiveItems,
    int DueNow,
    int Overdue,
    int RetiredItems);

public sealed record StudyReportDto(
    string Period,
    string Label,
    DateOnly From,
    DateOnly To,
    int StudyMinutes,
    int SessionCount,
    int ActiveDays,
    IReadOnlyList<DailyMinutesDto> MinutesByDay,
    IReadOnlyList<NamedMinutesDto> MinutesBySubject,
    IReadOnlyList<NamedMinutesDto> MinutesByStage,
    IReadOnlyList<NamedMinutesDto> MinutesByActivity,
    TaskSummaryDto Tasks,
    int TopicsCompleted,
    IReadOnlyList<string> CompletedTopicTitles,
    PracticePerformanceDto Practice,
    IReadOnlyList<SubjectAccuracyDto> McqBySubject,
    IReadOnlyList<SubjectAccuracyDto> WeakSubjects,
    RevisionStatsDto Revision,
    IReadOnlyList<StudyGoalDto> Goals);

public sealed record ActivityDto(string Type, string Title, DateTimeOffset OccurredAt, string? Detail);

public sealed record DashboardDto(
    DateOnly Today,
    IReadOnlyList<StudyTaskDto> TodayTasks,
    StudySessionDto? ActiveSession,
    int TodayMinutes,
    int WeekMinutes,
    int MonthMinutes,
    StreakDto Streak,
    int RevisionDue,
    int CurrentAffairsDue,
    int OpenMistakes,
    IReadOnlyList<StudyGoalDto> Goals,
    IReadOnlyList<StageProgressDto> Stages,
    PracticePerformanceDto Last30Days,
    IReadOnlyList<SubjectAccuracyDto> WeakSubjects,
    IReadOnlyList<ActivityDto> RecentActivity);

public interface IPlannerService
{
    Task<PlannerSettingsDto> GetSettingsAsync(Guid userId, CancellationToken cancellationToken);
    Task<PlannerSettingsDto> SaveSettingsAsync(Guid userId, PlannerSettingsInput input, CancellationToken cancellationToken);

    Task<IReadOnlyList<StudyGoalDto>> GetGoalsAsync(Guid userId, string? period, DateOnly? date, CancellationToken cancellationToken);
    Task<StudyGoalDto> CreateGoalAsync(Guid userId, StudyGoalInput input, CancellationToken cancellationToken);
    Task<StudyGoalDto?> UpdateGoalAsync(Guid userId, Guid goalId, StudyGoalInput input, CancellationToken cancellationToken);
    Task<bool> DeleteGoalAsync(Guid userId, Guid goalId, CancellationToken cancellationToken);

    Task<PagedResponse<StudyTaskDto>> GetTasksAsync(Guid userId, StudyTaskQuery query, CancellationToken cancellationToken);
    Task<StudyTaskDto> CreateTaskAsync(Guid userId, StudyTaskInput input, CancellationToken cancellationToken);
    Task<StudyTaskDto?> UpdateTaskAsync(Guid userId, Guid taskId, StudyTaskInput input, CancellationToken cancellationToken);
    Task<StudyTaskDto?> SetTaskStatusAsync(Guid userId, Guid taskId, StudyTaskStatusInput input, CancellationToken cancellationToken);
    Task<bool> DeleteTaskAsync(Guid userId, Guid taskId, CancellationToken cancellationToken);

    Task<IReadOnlyList<StudyScheduleDto>> GetSchedulesAsync(Guid userId, CancellationToken cancellationToken);
    Task<StudyScheduleDto> CreateScheduleAsync(Guid userId, StudyScheduleInput input, CancellationToken cancellationToken);
    Task<StudyScheduleDto?> UpdateScheduleAsync(Guid userId, Guid scheduleId, StudyScheduleInput input, CancellationToken cancellationToken);
    Task<bool> DeleteScheduleAsync(Guid userId, Guid scheduleId, CancellationToken cancellationToken);
    Task<int?> GenerateTasksAsync(Guid userId, Guid scheduleId, GenerateTasksInput input, CancellationToken cancellationToken);

    Task<PagedResponse<StudySessionDto>> GetSessionsAsync(Guid userId, DateOnly? from, DateOnly? to, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<StudySessionDto?> GetActiveSessionAsync(Guid userId, CancellationToken cancellationToken);
    Task<StudySessionDto> StartSessionAsync(Guid userId, StartSessionInput input, CancellationToken cancellationToken);
    Task<StudySessionDto?> StopSessionAsync(Guid userId, Guid sessionId, string? notes, CancellationToken cancellationToken);
    Task<StudySessionDto> LogSessionAsync(Guid userId, StudySessionInput input, CancellationToken cancellationToken);
    Task<StudySessionDto?> UpdateSessionAsync(Guid userId, Guid sessionId, StudySessionInput input, CancellationToken cancellationToken);
    Task<bool> DeleteSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);

    Task<CalendarDto> GetCalendarAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken);
}

public interface IRevisionService
{
    Task<RevisionPolicy> GetPolicyAsync(Guid userId, CancellationToken cancellationToken);
    Task<RevisionQueueDto> GetQueueAsync(Guid userId, bool dueOnly, string? sourceType, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<RevisionItemDto> AddItemAsync(Guid userId, RevisionItemInput input, CancellationToken cancellationToken);
    Task<RevisionItemDto?> ReviewAsync(Guid userId, Guid itemId, RevisionReviewInput input, CancellationToken cancellationToken);
    Task<RevisionItemDto?> SetItemStatusAsync(Guid userId, Guid itemId, string status, CancellationToken cancellationToken);
    Task<bool> DeleteItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken);
    Task<RevisionImportResultDto> ImportAsync(Guid userId, RevisionImportInput input, CancellationToken cancellationToken);
    Task<PagedResponse<RevisionLogDto>> GetHistoryAsync(Guid userId, DateOnly? from, DateOnly? to, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<WeakTopicDto>> GetWeakTopicsAsync(Guid userId, CancellationToken cancellationToken);

    Task<PagedResponse<MistakeDto>> GetMistakesAsync(Guid userId, MistakeQuery query, CancellationToken cancellationToken);
    Task<MistakeDto?> GetMistakeAsync(Guid userId, Guid mistakeId, CancellationToken cancellationToken);
    Task<MistakeDto> CreateMistakeAsync(Guid userId, MistakeInput input, CancellationToken cancellationToken);
    Task<MistakeDto?> UpdateMistakeAsync(Guid userId, Guid mistakeId, MistakeInput input, CancellationToken cancellationToken);
    Task<MistakeDto?> SetMistakeStatusAsync(Guid userId, Guid mistakeId, string status, CancellationToken cancellationToken);
    Task<bool> DeleteMistakeAsync(Guid userId, Guid mistakeId, CancellationToken cancellationToken);
    Task<int> ImportMcqMistakesAsync(Guid userId, CancellationToken cancellationToken);
}

public interface IProgressService
{
    Task<ProgressOverviewDto> GetOverviewAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TopicStatusDto>> GetTopicsAsync(Guid userId, Guid versionId, Guid? subjectId, string? status, CancellationToken cancellationToken);
    Task<TopicStatusDto?> SetMasteryAsync(Guid userId, Guid syllabusItemId, MasteryInput input, CancellationToken cancellationToken);
    Task<StudyReportDto> GetReportAsync(Guid userId, string period, DateOnly? date, CancellationToken cancellationToken);
    Task<DashboardDto> GetDashboardAsync(Guid userId, CancellationToken cancellationToken);
}
