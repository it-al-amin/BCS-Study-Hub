namespace BCSStudyHub.Domain.Entities;

public enum StudyStage
{
    Preliminary,
    Written,
    Viva,
    General,
}

public enum PlannerPeriod
{
    Daily,
    Weekly,
    Monthly,
}

/// <summary>What a goal counts. Every metric is computed from recorded activity, never stored.</summary>
public enum GoalMetric
{
    StudyMinutes,
    CompletedTasks,
    CompletedTopics,
    McqQuestionsAnswered,
    WrittenSubmissions,
    VivaSessions,
    RevisionReviews,
}

public enum TaskPriority
{
    Low,
    Medium,
    High,
    Urgent,
}

public enum StudyTaskStatus
{
    Planned,
    InProgress,
    Completed,
    Skipped,
}

public enum StudyActivity
{
    Reading,
    Practice,
    Writing,
    Revision,
    VivaPractice,
    Other,
}

public enum RevisionSourceType
{
    Topic,
    McqQuestion,
    WrittenQuestion,
    VivaQuestion,
    Mistake,
    WrittenNote,
    LibraryNote,
    VivaNote,
    CurrentAffairs,
    Custom,
}

public enum RevisionReason
{
    Manual,
    Bookmark,
    Mistake,
    WeakTopic,
    Note,
}

public enum RevisionItemStatus
{
    Active,
    Suspended,
    Retired,
}

public enum RevisionOutcome
{
    Again,
    Hard,
    Good,
    Easy,
}

public enum MistakeSourceType
{
    Mcq,
    Written,
    Viva,
    Other,
}

public enum MistakeCategory
{
    Concept,
    Recall,
    Calculation,
    Misreading,
    Careless,
    TimePressure,
    Language,
    Other,
}

public enum MistakeStatus
{
    Open,
    Resolved,
}

public sealed class StudyGoal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public PlannerPeriod Period { get; set; }

    /// <summary>First day of the period (Monday for weekly, the 1st for monthly).</summary>
    public DateOnly PeriodStart { get; set; }

    public string Title { get; set; } = string.Empty;
    public GoalMetric Metric { get; set; }
    public int TargetValue { get; set; }
    public Guid? SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public StudyStage? Stage { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>A recurring study block that generates dated tasks on request.</summary>
public sealed class StudySchedule
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;

    /// <summary>Bit flags: Sunday = 1, Monday = 2, … Saturday = 64 (matching <see cref="DayOfWeek"/>).</summary>
    public int DaysOfWeek { get; set; }

    public TimeOnly? StartTime { get; set; }
    public int PlannedMinutes { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public Guid? SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public Guid? SyllabusItemId { get; set; }
    public SyllabusItem? SyllabusItem { get; set; }
    public StudyStage? Stage { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class StudyTask
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public StudyTaskStatus Status { get; set; } = StudyTaskStatus.Planned;
    public DateOnly ScheduledDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public int PlannedMinutes { get; set; }

    /// <summary>Minutes actually studied: the sum of linked sessions, or a manually entered value when none are linked.</summary>
    public int ActualMinutes { get; set; }

    public Guid? SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public Guid? SyllabusItemId { get; set; }
    public SyllabusItem? SyllabusItem { get; set; }
    public StudyStage? Stage { get; set; }
    public Guid? ScheduleId { get; set; }
    public StudySchedule? Schedule { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class StudySession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>Null while a timer is running.</summary>
    public DateTimeOffset? EndedAt { get; set; }

    public int DurationMinutes { get; set; }
    public StudyActivity Activity { get; set; }
    public Guid? TaskId { get; set; }
    public StudyTask? Task { get; set; }
    public Guid? SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public Guid? SyllabusItemId { get; set; }
    public SyllabusItem? SyllabusItem { get; set; }
    public StudyStage? Stage { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Per-user preferences for scheduling and tracking. Revision behaviour is fully defined here (interval ladder and
/// per-outcome step changes), so the scheduler contains no fixed intervals of its own.
/// </summary>
public sealed class PlannerSettings
{
    public Guid UserId { get; set; }

    /// <summary>JSON array of strictly increasing day intervals, e.g. [1,3,7,14,30].</summary>
    public string RevisionIntervalsJson { get; set; } = "[]";

    public int AgainStep { get; set; }
    public int HardStepDelta { get; set; }
    public int GoodStepDelta { get; set; }
    public int EasyStepDelta { get; set; }
    public int? RetireAfterStep { get; set; }
    public int DailyRevisionLimit { get; set; }
    public int StreakMinimumMinutes { get; set; }
    public int WeakTopicAccuracyPercent { get; set; }
    public int WeakTopicMinimumAnswers { get; set; }
    public bool AutoQueueMistakes { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class RevisionItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public RevisionSourceType SourceType { get; set; }

    /// <summary>The revised record; null only for custom items.</summary>
    public Guid? SourceId { get; set; }

    /// <summary>Snapshot so the queue stays readable if the source changes.</summary>
    public string Title { get; set; } = string.Empty;

    public Guid? SyllabusItemId { get; set; }
    public SyllabusItem? SyllabusItem { get; set; }
    public RevisionReason Reason { get; set; }
    public RevisionItemStatus Status { get; set; } = RevisionItemStatus.Active;

    /// <summary>Position on the user's interval ladder; 0 means never reviewed.</summary>
    public int Step { get; set; }

    public int IntervalDays { get; set; }
    public DateOnly DueOn { get; set; }
    public int ReviewCount { get; set; }
    public int LapseCount { get; set; }
    public RevisionOutcome? LastOutcome { get; set; }
    public DateTimeOffset? LastReviewedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Immutable review history across the general queue and current-affairs revision.</summary>
public sealed class RevisionLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? RevisionItemId { get; set; }
    public RevisionItem? RevisionItem { get; set; }
    public RevisionSourceType SourceType { get; set; }
    public Guid? SourceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid? SyllabusItemId { get; set; }
    public RevisionOutcome Outcome { get; set; }
    public int StepBefore { get; set; }
    public int StepAfter { get; set; }
    public int IntervalDays { get; set; }
    public DateTimeOffset ReviewedAt { get; set; }
}

public sealed class MistakeEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public MistakeSourceType SourceType { get; set; }

    /// <summary>The MCQ/Written/Viva question, when the mistake came from one.</summary>
    public Guid? QuestionId { get; set; }

    public string QuestionText { get; set; } = string.Empty;
    public string? YourAnswer { get; set; }
    public string CorrectAnswerReference { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public MistakeCategory Category { get; set; }
    public MistakeStatus Status { get; set; } = MistakeStatus.Open;
    public Guid? SyllabusItemId { get; set; }
    public SyllabusItem? SyllabusItem { get; set; }
    public DateOnly? ReviewOn { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
