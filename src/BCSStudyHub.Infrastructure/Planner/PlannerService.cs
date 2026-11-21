using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.CurrentAffairs;
using BCSStudyHub.Application.Planner;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Infrastructure.Planner;

/// <summary>Personal planner. Every record is owner-scoped; another user's IDs behave as not found.</summary>
internal sealed class PlannerService(
    ApplicationDbContext dbContext,
    PlannerSettingsStore settingsStore,
    StudyMetrics metrics,
    TimeProvider timeProvider) : IPlannerService
{
    private const int MaxSessionMinutes = 1440;

    public Task<PlannerSettingsDto> GetSettingsAsync(Guid userId, CancellationToken cancellationToken) =>
        settingsStore.GetAsync(userId, cancellationToken);

    public async Task<PlannerSettingsDto> SaveSettingsAsync(Guid userId, PlannerSettingsInput input, CancellationToken cancellationToken)
    {
        PlannerValidation.Validate(input);
        var row = await dbContext.PlannerSettings.SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (row is null)
        {
            row = new PlannerSettings { UserId = userId };
            dbContext.PlannerSettings.Add(row);
        }

        row.RevisionIntervalsJson = JsonSerializer.Serialize(input.RevisionIntervalsDays);
        row.AgainStep = input.AgainStep;
        row.HardStepDelta = input.HardStepDelta;
        row.GoodStepDelta = input.GoodStepDelta;
        row.EasyStepDelta = input.EasyStepDelta;
        row.RetireAfterStep = input.RetireAfterStep;
        row.DailyRevisionLimit = input.DailyRevisionLimit;
        row.StreakMinimumMinutes = input.StreakMinimumMinutes;
        row.WeakTopicAccuracyPercent = input.WeakTopicAccuracyPercent;
        row.WeakTopicMinimumAnswers = input.WeakTopicMinimumAnswers;
        row.AutoQueueMistakes = input.AutoQueueMistakes;
        row.UpdatedAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);
        return PlannerSettingsStore.ToDto(row);
    }

    // ---------- Goals ----------

    public async Task<IReadOnlyList<StudyGoalDto>> GetGoalsAsync(Guid userId, string? period, DateOnly? date, CancellationToken cancellationToken)
    {
        var goals = dbContext.StudyGoals.AsNoTracking().Include(goal => goal.Subject).Where(goal => goal.UserId == userId);
        if (!string.IsNullOrWhiteSpace(period))
        {
            var parsed = PlannerValidation.Parse<PlannerPeriod>(period, "goal period");
            goals = goals.Where(goal => goal.Period == parsed);
            if (date is DateOnly anchor)
            {
                var start = PeriodStart(parsed, anchor);
                goals = goals.Where(goal => goal.PeriodStart == start);
            }
        }
        else if (date is DateOnly anchor)
        {
            var starts = Enum.GetValues<PlannerPeriod>().Select(value => (value, PeriodStart(value, anchor))).ToArray();
            var daily = starts[0].Item2;
            var weekly = starts[1].Item2;
            var monthly = starts[2].Item2;
            goals = goals.Where(goal =>
                (goal.Period == PlannerPeriod.Daily && goal.PeriodStart == daily) ||
                (goal.Period == PlannerPeriod.Weekly && goal.PeriodStart == weekly) ||
                (goal.Period == PlannerPeriod.Monthly && goal.PeriodStart == monthly));
        }

        var rows = await goals.OrderByDescending(goal => goal.PeriodStart).ThenBy(goal => goal.Period).Take(200).ToListAsync(cancellationToken);
        var result = new List<StudyGoalDto>(rows.Count);
        foreach (var goal in rows)
        {
            result.Add(await ToGoalDtoAsync(userId, goal, cancellationToken));
        }

        return result;
    }

    public async Task<StudyGoalDto> CreateGoalAsync(Guid userId, StudyGoalInput input, CancellationToken cancellationToken)
    {
        var goal = new StudyGoal { Id = Guid.NewGuid(), UserId = userId };
        await ApplyGoalAsync(goal, input, cancellationToken);
        dbContext.StudyGoals.Add(goal);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await ToGoalDtoAsync(userId, goal, cancellationToken);
    }

    public async Task<StudyGoalDto?> UpdateGoalAsync(Guid userId, Guid goalId, StudyGoalInput input, CancellationToken cancellationToken)
    {
        var goal = await dbContext.StudyGoals.SingleOrDefaultAsync(item => item.Id == goalId && item.UserId == userId, cancellationToken);
        if (goal is null)
        {
            return null;
        }

        await ApplyGoalAsync(goal, input, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await ToGoalDtoAsync(userId, goal, cancellationToken);
    }

    public async Task<bool> DeleteGoalAsync(Guid userId, Guid goalId, CancellationToken cancellationToken) =>
        await dbContext.StudyGoals.Where(goal => goal.Id == goalId && goal.UserId == userId).ExecuteDeleteSafeAsync(dbContext, cancellationToken);

    private async Task ApplyGoalAsync(StudyGoal goal, StudyGoalInput input, CancellationToken cancellationToken)
    {
        PlannerValidation.Validate(input);
        var metric = PlannerValidation.Parse<GoalMetric>(input.Metric, "goal metric");
        if (metric == GoalMetric.VivaSessions && input.SubjectId is not null)
        {
            throw PlannerValidation.Invalid("Viva sessions are not linked to subjects; remove the subject filter.");
        }

        await PlannerValidation.ValidateSubjectAsync(dbContext, input.SubjectId, cancellationToken);
        goal.Period = PlannerValidation.Parse<PlannerPeriod>(input.Period, "goal period");
        goal.PeriodStart = PeriodStart(goal.Period, input.Date);
        goal.Title = input.Title.Trim();
        goal.Metric = metric;
        goal.TargetValue = input.TargetValue;
        goal.SubjectId = input.SubjectId;
        goal.Stage = PlannerValidation.ParseOptional<StudyStage>(input.Stage, "stage");
        goal.Notes = PlannerValidation.Normalize(input.Notes);
    }

    internal async Task<StudyGoalDto> ToGoalDtoAsync(Guid userId, StudyGoal goal, CancellationToken cancellationToken)
    {
        var (from, to) = CurrentAffairsRules.GetRange(PlannerRules.ToDigestPeriod(goal.Period), goal.PeriodStart);
        var value = await metrics.GoalValueAsync(userId, goal, from, to, cancellationToken);
        var subjectName = goal.Subject?.Name ?? (goal.SubjectId is Guid subjectId
            ? await dbContext.Subjects.Where(subject => subject.Id == subjectId).Select(subject => subject.Name).SingleOrDefaultAsync(cancellationToken)
            : null);
        return new StudyGoalDto(
            goal.Id, goal.Period.ToString(), from, to, goal.Title, goal.Metric.ToString(), goal.TargetValue, value,
            (int)Math.Min(100, Math.Round(value * 100d / goal.TargetValue)), goal.SubjectId, subjectName, goal.Stage?.ToString(), goal.Notes);
    }

    internal static DateOnly PeriodStart(PlannerPeriod period, DateOnly anchor) =>
        CurrentAffairsRules.GetRange(PlannerRules.ToDigestPeriod(period), anchor).From;

    // ---------- Tasks ----------

    public async Task<PagedResponse<StudyTaskDto>> GetTasksAsync(Guid userId, StudyTaskQuery query, CancellationToken cancellationToken)
    {
        var tasks = TaskQuery(userId);
        if (query.From is DateOnly from)
        {
            tasks = tasks.Where(task => task.ScheduledDate >= from);
        }

        if (query.To is DateOnly to)
        {
            tasks = tasks.Where(task => task.ScheduledDate <= to);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = PlannerValidation.Parse<StudyTaskStatus>(query.Status, "task status");
            tasks = tasks.Where(task => task.Status == status);
        }

        if (query.SubjectId is Guid subjectId)
        {
            tasks = tasks.Where(task => task.SubjectId == subjectId);
        }

        if (!string.IsNullOrWhiteSpace(query.Stage))
        {
            var stage = PlannerValidation.Parse<StudyStage>(query.Stage, "stage");
            tasks = tasks.Where(task => task.Stage == stage);
        }

        tasks = tasks.OrderBy(task => task.ScheduledDate).ThenBy(task => task.StartTime).ThenByDescending(task => task.Priority).ThenBy(task => task.Id);
        var total = await tasks.LongCountAsync(cancellationToken);
        var page = new PageRequest(query.PageNumber, query.PageSize);
        var rows = await tasks.Skip(page.Offset).Take(page.EffectivePageSize).ToListAsync(cancellationToken);
        return new PagedResponse<StudyTaskDto>(await ToTaskDtosAsync(rows, cancellationToken), page.EffectivePageNumber, page.EffectivePageSize, total);
    }

    public async Task<StudyTaskDto> CreateTaskAsync(Guid userId, StudyTaskInput input, CancellationToken cancellationToken)
    {
        var task = new StudyTask { Id = Guid.NewGuid(), UserId = userId };
        await ApplyTaskAsync(task, input, cancellationToken);
        dbContext.StudyTasks.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetTaskDtoAsync(userId, task.Id, cancellationToken))!;
    }

    public async Task<StudyTaskDto?> UpdateTaskAsync(Guid userId, Guid taskId, StudyTaskInput input, CancellationToken cancellationToken)
    {
        var task = await dbContext.StudyTasks.SingleOrDefaultAsync(item => item.Id == taskId && item.UserId == userId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        await ApplyTaskAsync(task, input, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetTaskDtoAsync(userId, task.Id, cancellationToken);
    }

    public async Task<StudyTaskDto?> SetTaskStatusAsync(Guid userId, Guid taskId, StudyTaskStatusInput input, CancellationToken cancellationToken)
    {
        PlannerValidation.Validate(input);
        var task = await dbContext.StudyTasks.SingleOrDefaultAsync(item => item.Id == taskId && item.UserId == userId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        task.Status = PlannerValidation.Parse<StudyTaskStatus>(input.Status, "task status");
        task.CompletedAt = task.Status == StudyTaskStatus.Completed ? task.CompletedAt ?? timeProvider.GetUtcNow() : null;
        var hasSessions = await dbContext.StudySessions.AnyAsync(session => session.TaskId == taskId, cancellationToken);
        if (input.ActualMinutes is int minutes)
        {
            if (hasSessions)
            {
                throw PlannerValidation.Invalid("Actual time is calculated from the sessions linked to this task.");
            }

            task.ActualMinutes = minutes;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetTaskDtoAsync(userId, task.Id, cancellationToken);
    }

    public async Task<bool> DeleteTaskAsync(Guid userId, Guid taskId, CancellationToken cancellationToken) =>
        await dbContext.StudyTasks.Where(task => task.Id == taskId && task.UserId == userId).ExecuteDeleteSafeAsync(dbContext, cancellationToken);

    private async Task ApplyTaskAsync(StudyTask task, StudyTaskInput input, CancellationToken cancellationToken)
    {
        PlannerValidation.Validate(input);
        var (subjectId, stage) = await PlannerValidation.ResolveScopeAsync(dbContext, input.SubjectId, input.SyllabusItemId, input.Stage, cancellationToken);
        task.Title = input.Title.Trim();
        task.Description = PlannerValidation.Normalize(input.Description);
        task.Priority = PlannerValidation.Parse<TaskPriority>(input.Priority, "priority");
        task.ScheduledDate = input.ScheduledDate;
        task.StartTime = input.StartTime;
        task.PlannedMinutes = input.PlannedMinutes;
        task.SubjectId = subjectId;
        task.SyllabusItemId = input.SyllabusItemId;
        task.Stage = stage;
    }

    private IQueryable<StudyTask> TaskQuery(Guid userId) =>
        dbContext.StudyTasks.AsNoTracking()
            .Include(task => task.Subject)
            .Include(task => task.SyllabusItem)
            .Where(task => task.UserId == userId);

    private async Task<StudyTaskDto?> GetTaskDtoAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        var task = await TaskQuery(userId).SingleOrDefaultAsync(item => item.Id == taskId, cancellationToken);
        return task is null ? null : (await ToTaskDtosAsync([task], cancellationToken))[0];
    }

    internal async Task<IReadOnlyList<StudyTaskDto>> ToTaskDtosAsync(IReadOnlyList<StudyTask> tasks, CancellationToken cancellationToken)
    {
        var ids = tasks.Select(task => task.Id).ToArray();
        var withSessions = ids.Length == 0
            ? []
            : (await dbContext.StudySessions.AsNoTracking().Where(session => session.TaskId != null && ids.Contains(session.TaskId.Value))
                .Select(session => session.TaskId!.Value).Distinct().ToListAsync(cancellationToken)).ToHashSet();
        return tasks.Select(task => new StudyTaskDto(
            task.Id, task.Title, task.Description, task.Priority.ToString(), task.Status.ToString(), task.ScheduledDate, task.StartTime,
            task.PlannedMinutes, task.ActualMinutes, withSessions.Contains(task.Id), task.SubjectId, task.Subject?.Name,
            task.SyllabusItemId, task.SyllabusItem?.Title, task.Stage?.ToString(), task.ScheduleId, task.CompletedAt)).ToArray();
    }

    // ---------- Schedules ----------

    public async Task<IReadOnlyList<StudyScheduleDto>> GetSchedulesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rows = await dbContext.StudySchedules.AsNoTracking().Include(schedule => schedule.Subject).Include(schedule => schedule.SyllabusItem)
            .Where(schedule => schedule.UserId == userId).OrderByDescending(schedule => schedule.IsActive).ThenBy(schedule => schedule.Title)
            .ToListAsync(cancellationToken);
        return rows.Select(ToScheduleDto).ToArray();
    }

    public async Task<StudyScheduleDto> CreateScheduleAsync(Guid userId, StudyScheduleInput input, CancellationToken cancellationToken)
    {
        var schedule = new StudySchedule { Id = Guid.NewGuid(), UserId = userId };
        await ApplyScheduleAsync(schedule, input, cancellationToken);
        dbContext.StudySchedules.Add(schedule);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetSchedulesAsync(userId, cancellationToken)).Single(item => item.Id == schedule.Id);
    }

    public async Task<StudyScheduleDto?> UpdateScheduleAsync(Guid userId, Guid scheduleId, StudyScheduleInput input, CancellationToken cancellationToken)
    {
        var schedule = await dbContext.StudySchedules.SingleOrDefaultAsync(item => item.Id == scheduleId && item.UserId == userId, cancellationToken);
        if (schedule is null)
        {
            return null;
        }

        await ApplyScheduleAsync(schedule, input, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetSchedulesAsync(userId, cancellationToken)).Single(item => item.Id == schedule.Id);
    }

    public async Task<bool> DeleteScheduleAsync(Guid userId, Guid scheduleId, CancellationToken cancellationToken) =>
        await dbContext.StudySchedules.Where(schedule => schedule.Id == scheduleId && schedule.UserId == userId).ExecuteDeleteSafeAsync(dbContext, cancellationToken);

    /// <summary>Creates dated tasks for each matching weekday in the range, skipping dates that already have one.</summary>
    public async Task<int?> GenerateTasksAsync(Guid userId, Guid scheduleId, GenerateTasksInput input, CancellationToken cancellationToken)
    {
        if (input.To < input.From || input.To.DayNumber - input.From.DayNumber >= PlannerRules.MaxGenerateDays)
        {
            throw PlannerValidation.Invalid($"Generate tasks for a range of at most {PlannerRules.MaxGenerateDays} days.");
        }

        var schedule = await dbContext.StudySchedules.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == scheduleId && item.UserId == userId, cancellationToken);
        if (schedule is null)
        {
            return null;
        }

        if (!schedule.IsActive)
        {
            throw PlannerValidation.Invalid("Activate the schedule before generating tasks.");
        }

        var existing = (await dbContext.StudyTasks.AsNoTracking()
            .Where(task => task.ScheduleId == scheduleId && task.ScheduledDate >= input.From && task.ScheduledDate <= input.To)
            .Select(task => task.ScheduledDate).ToListAsync(cancellationToken)).ToHashSet();
        var created = 0;
        for (var date = input.From; date <= input.To; date = date.AddDays(1))
        {
            if (date < schedule.StartDate || (schedule.EndDate is DateOnly end && date > end) ||
                !PlannerRules.RunsOn(schedule.DaysOfWeek, date) || existing.Contains(date))
            {
                continue;
            }

            dbContext.StudyTasks.Add(new StudyTask
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = schedule.Title,
                Priority = schedule.Priority,
                ScheduledDate = date,
                StartTime = schedule.StartTime,
                PlannedMinutes = schedule.PlannedMinutes,
                SubjectId = schedule.SubjectId,
                SyllabusItemId = schedule.SyllabusItemId,
                Stage = schedule.Stage,
                ScheduleId = schedule.Id,
            });
            created++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return created;
    }

    private async Task ApplyScheduleAsync(StudySchedule schedule, StudyScheduleInput input, CancellationToken cancellationToken)
    {
        PlannerValidation.Validate(input);
        var (subjectId, stage) = await PlannerValidation.ResolveScopeAsync(dbContext, input.SubjectId, input.SyllabusItemId, input.Stage, cancellationToken);
        schedule.Title = input.Title.Trim();
        schedule.DaysOfWeek = input.DaysMask();
        schedule.StartTime = input.StartTime;
        schedule.PlannedMinutes = input.PlannedMinutes;
        schedule.Priority = PlannerValidation.Parse<TaskPriority>(input.Priority, "priority");
        schedule.SubjectId = subjectId;
        schedule.SyllabusItemId = input.SyllabusItemId;
        schedule.Stage = stage;
        schedule.StartDate = input.StartDate;
        schedule.EndDate = input.EndDate;
        schedule.IsActive = input.IsActive;
    }

    private static StudyScheduleDto ToScheduleDto(StudySchedule schedule) =>
        new(
            schedule.Id,
            schedule.Title,
            Enum.GetValues<DayOfWeek>().Where(day => (schedule.DaysOfWeek & (1 << (int)day)) != 0).Select(day => day.ToString()).ToArray(),
            schedule.StartTime,
            schedule.PlannedMinutes,
            schedule.Priority.ToString(),
            schedule.SubjectId,
            schedule.Subject?.Name,
            schedule.SyllabusItemId,
            schedule.SyllabusItem?.Title,
            schedule.Stage?.ToString(),
            schedule.StartDate,
            schedule.EndDate,
            schedule.IsActive);

    // ---------- Sessions ----------

    public async Task<PagedResponse<StudySessionDto>> GetSessionsAsync(Guid userId, DateOnly? from, DateOnly? to, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var sessions = SessionQuery(userId);
        if (from is DateOnly fromDate)
        {
            var start = BangladeshTime.StartOf(fromDate);
            sessions = sessions.Where(session => session.StartedAt >= start);
        }

        if (to is DateOnly toDate)
        {
            var end = BangladeshTime.StartOf(toDate.AddDays(1));
            sessions = sessions.Where(session => session.StartedAt < end);
        }

        sessions = sessions.OrderByDescending(session => session.StartedAt).ThenBy(session => session.Id);
        var total = await sessions.LongCountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var rows = await sessions.Skip(page.Offset).Take(page.EffectivePageSize).ToListAsync(cancellationToken);
        return new PagedResponse<StudySessionDto>(rows.Select(ToSessionDto).ToArray(), page.EffectivePageNumber, page.EffectivePageSize, total);
    }

    public async Task<StudySessionDto?> GetActiveSessionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var session = await SessionQuery(userId).FirstOrDefaultAsync(item => item.EndedAt == null, cancellationToken);
        return session is null ? null : ToSessionDto(session);
    }

    public async Task<StudySessionDto> StartSessionAsync(Guid userId, StartSessionInput input, CancellationToken cancellationToken)
    {
        PlannerValidation.Validate(input);
        if (await dbContext.StudySessions.AnyAsync(session => session.UserId == userId && session.EndedAt == null, cancellationToken))
        {
            throw new PlannerException(StatusCodes.Status409Conflict, "Session already running", "Stop the running study session before starting another.");
        }

        await ValidateTaskAsync(userId, input.TaskId, cancellationToken);
        var (subjectId, stage) = await PlannerValidation.ResolveScopeAsync(dbContext, input.SubjectId, input.SyllabusItemId, input.Stage, cancellationToken);
        var session = new StudySession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StartedAt = timeProvider.GetUtcNow(),
            Activity = PlannerValidation.Parse<StudyActivity>(input.Activity, "activity"),
            TaskId = input.TaskId,
            SubjectId = subjectId,
            SyllabusItemId = input.SyllabusItemId,
            Stage = stage,
            CreatedAt = timeProvider.GetUtcNow(),
        };
        dbContext.StudySessions.Add(session);
        await MarkTaskInProgressAsync(input.TaskId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetSessionDtoAsync(userId, session.Id, cancellationToken))!;
    }

    public async Task<StudySessionDto?> StopSessionAsync(Guid userId, Guid sessionId, string? notes, CancellationToken cancellationToken)
    {
        if (notes is { Length: > 2000 })
        {
            throw PlannerValidation.Invalid("Session notes cannot exceed 2000 characters.");
        }

        var session = await dbContext.StudySessions.SingleOrDefaultAsync(item => item.Id == sessionId && item.UserId == userId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        if (session.EndedAt is not null)
        {
            throw PlannerValidation.Invalid("This session has already been stopped.");
        }

        var now = timeProvider.GetUtcNow();
        session.EndedAt = now;

        // A forgotten timer is capped at one day so it cannot distort reports.
        session.DurationMinutes = Math.Min(MaxSessionMinutes, PlannerRules.DurationMinutes(session.StartedAt, now));
        session.Notes = PlannerValidation.Normalize(notes) ?? session.Notes;
        await dbContext.SaveChangesAsync(cancellationToken);
        await RecalculateTaskMinutesAsync(session.TaskId, cancellationToken);
        return await GetSessionDtoAsync(userId, session.Id, cancellationToken);
    }

    public async Task<StudySessionDto> LogSessionAsync(Guid userId, StudySessionInput input, CancellationToken cancellationToken)
    {
        var session = new StudySession { Id = Guid.NewGuid(), UserId = userId, CreatedAt = timeProvider.GetUtcNow() };
        await ApplySessionAsync(userId, session, input, cancellationToken);
        dbContext.StudySessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);
        await RecalculateTaskMinutesAsync(session.TaskId, cancellationToken);
        return (await GetSessionDtoAsync(userId, session.Id, cancellationToken))!;
    }

    public async Task<StudySessionDto?> UpdateSessionAsync(Guid userId, Guid sessionId, StudySessionInput input, CancellationToken cancellationToken)
    {
        var session = await dbContext.StudySessions.SingleOrDefaultAsync(item => item.Id == sessionId && item.UserId == userId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        if (session.EndedAt is null)
        {
            throw PlannerValidation.Invalid("Stop the running session before editing it.");
        }

        var previousTask = session.TaskId;
        await ApplySessionAsync(userId, session, input, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await RecalculateTaskMinutesAsync(previousTask, cancellationToken);
        if (session.TaskId != previousTask)
        {
            await RecalculateTaskMinutesAsync(session.TaskId, cancellationToken);
        }

        return await GetSessionDtoAsync(userId, session.Id, cancellationToken);
    }

    public async Task<bool> DeleteSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await dbContext.StudySessions.SingleOrDefaultAsync(item => item.Id == sessionId && item.UserId == userId, cancellationToken);
        if (session is null)
        {
            return false;
        }

        dbContext.StudySessions.Remove(session);
        await dbContext.SaveChangesAsync(cancellationToken);
        await RecalculateTaskMinutesAsync(session.TaskId, cancellationToken);
        return true;
    }

    private async Task ApplySessionAsync(Guid userId, StudySession session, StudySessionInput input, CancellationToken cancellationToken)
    {
        PlannerValidation.Validate(input);
        var end = input.StartedAt.AddMinutes(input.DurationMinutes);
        if (end > timeProvider.GetUtcNow().AddMinutes(5))
        {
            throw PlannerValidation.Invalid("Logged sessions cannot end in the future.");
        }

        await ValidateTaskAsync(userId, input.TaskId, cancellationToken);
        var (subjectId, stage) = await PlannerValidation.ResolveScopeAsync(dbContext, input.SubjectId, input.SyllabusItemId, input.Stage, cancellationToken);
        session.StartedAt = input.StartedAt;
        session.EndedAt = end;
        session.DurationMinutes = input.DurationMinutes;
        session.Activity = PlannerValidation.Parse<StudyActivity>(input.Activity, "activity");
        session.TaskId = input.TaskId;
        session.SubjectId = subjectId;
        session.SyllabusItemId = input.SyllabusItemId;
        session.Stage = stage;
        session.Notes = PlannerValidation.Normalize(input.Notes);
    }

    private async Task ValidateTaskAsync(Guid userId, Guid? taskId, CancellationToken cancellationToken)
    {
        if (taskId is Guid id && !await dbContext.StudyTasks.AnyAsync(task => task.Id == id && task.UserId == userId, cancellationToken))
        {
            throw PlannerValidation.Invalid("The linked task was not found in your planner.");
        }
    }

    private async Task MarkTaskInProgressAsync(Guid? taskId, CancellationToken cancellationToken)
    {
        if (taskId is Guid id)
        {
            var task = await dbContext.StudyTasks.SingleAsync(item => item.Id == id, cancellationToken);
            if (task.Status == StudyTaskStatus.Planned)
            {
                task.Status = StudyTaskStatus.InProgress;
            }
        }
    }

    private async Task RecalculateTaskMinutesAsync(Guid? taskId, CancellationToken cancellationToken)
    {
        if (taskId is not Guid id)
        {
            return;
        }

        var task = await dbContext.StudyTasks.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (task is null)
        {
            return;
        }

        task.ActualMinutes = await dbContext.StudySessions.Where(session => session.TaskId == id && session.EndedAt != null)
            .SumAsync(session => (int?)session.DurationMinutes, cancellationToken) ?? 0;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<StudySession> SessionQuery(Guid userId) =>
        dbContext.StudySessions.AsNoTracking()
            .Include(session => session.Task)
            .Include(session => session.Subject)
            .Include(session => session.SyllabusItem)
            .Where(session => session.UserId == userId);

    private async Task<StudySessionDto?> GetSessionDtoAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await SessionQuery(userId).SingleOrDefaultAsync(item => item.Id == sessionId, cancellationToken);
        return session is null ? null : ToSessionDto(session);
    }

    internal static StudySessionDto ToSessionDto(StudySession session) =>
        new(session.Id, session.StartedAt, session.EndedAt, session.DurationMinutes, session.EndedAt is null, session.Activity.ToString(),
            session.TaskId, session.Task?.Title, session.SubjectId, session.Subject?.Name, session.SyllabusItemId, session.SyllabusItem?.Title,
            session.Stage?.ToString(), session.Notes);

    // ---------- Calendar ----------

    public async Task<CalendarDto> GetCalendarAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        if (to < from || to.DayNumber - from.DayNumber >= PlannerRules.MaxCalendarDays)
        {
            throw PlannerValidation.Invalid($"The calendar range must be between 1 and {PlannerRules.MaxCalendarDays} days.");
        }

        var tasks = await TaskQuery(userId).Where(task => task.ScheduledDate >= from && task.ScheduledDate <= to)
            .OrderBy(task => task.StartTime).ThenByDescending(task => task.Priority).ToListAsync(cancellationToken);
        var taskDtos = await ToTaskDtosAsync(tasks, cancellationToken);
        var (start, end) = BangladeshTime.Range(from, to);
        var minutesByDay = (await metrics.SessionsAsync(userId, start, end, cancellationToken))
            .GroupBy(row => BangladeshTime.ToLocalDate(row.StartedAt))
            .ToDictionary(group => group.Key, group => group.Sum(row => row.Minutes));

        // Overdue revision is counted on today so the calendar shows what actually needs doing.
        var today = BangladeshTime.Today(timeProvider);
        var dueDates = await dbContext.RevisionItems.AsNoTracking()
            .Where(item => item.UserId == userId && item.Status == RevisionItemStatus.Active && item.DueOn <= to)
            .Select(item => item.DueOn).ToListAsync(cancellationToken);
        dueDates.AddRange(await dbContext.CurrentAffairsRevisions.AsNoTracking()
            .Where(item => item.UserId == userId && item.Entry.Status == CurrentAffairsStatus.Published && item.NextReviewOn <= to)
            .Select(item => item.NextReviewOn).ToListAsync(cancellationToken));
        var dueByDay = dueDates.Select(date => date < today ? today : date).GroupBy(date => date).ToDictionary(group => group.Key, group => group.Count());

        var settings = await settingsStore.GetAsync(userId, cancellationToken);
        var activeDays = await metrics.ActiveDaysAsync(userId, settings.StreakMinimumMinutes, from, to, cancellationToken);
        var days = new List<CalendarDayDto>();
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            days.Add(new CalendarDayDto(
                date,
                taskDtos.Where(task => task.ScheduledDate == date).ToArray(),
                minutesByDay.GetValueOrDefault(date),
                date < today ? 0 : dueByDay.GetValueOrDefault(date),
                activeDays.Contains(date)));
        }

        return new CalendarDto(from, to, days);
    }
}

internal static class PlannerValidation
{
    public static void Validate(object model)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true))
        {
            throw Invalid(results[0].ErrorMessage ?? "The request is not valid.");
        }
    }

    public static T Parse<T>(string? value, string field) where T : struct, Enum =>
        Enum.TryParse<T>(value, true, out var parsed) && Enum.IsDefined(parsed) && !int.TryParse(value, out _)
            ? parsed
            : throw Invalid($"The requested {field} is not supported.");

    public static T? ParseOptional<T>(string? value, string field) where T : struct, Enum =>
        string.IsNullOrWhiteSpace(value) ? null : Parse<T>(value, field);

    public static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static PlannerException Invalid(string message) => new(StatusCodes.Status400BadRequest, "Invalid planner request", message);

    public static async Task ValidateSubjectAsync(ApplicationDbContext dbContext, Guid? subjectId, CancellationToken cancellationToken)
    {
        if (subjectId is Guid id && !await dbContext.Subjects.AnyAsync(subject => subject.Id == id && subject.IsActive, cancellationToken))
        {
            throw Invalid("The subject must be an active subject in the syllabus catalogue.");
        }
    }

    /// <summary>
    /// Validates the subject/topic/stage combination. A topic fills in its own subject and stage, and must agree with
    /// any subject that was also given, so subject-wise and stage-wise reports stay consistent.
    /// </summary>
    public static async Task<(Guid? SubjectId, StudyStage? Stage)> ResolveScopeAsync(
        ApplicationDbContext dbContext,
        Guid? subjectId,
        Guid? syllabusItemId,
        string? stage,
        CancellationToken cancellationToken)
    {
        var parsedStage = ParseOptional<StudyStage>(stage, "stage");
        if (syllabusItemId is not Guid itemId)
        {
            await ValidateSubjectAsync(dbContext, subjectId, cancellationToken);
            return (subjectId, parsedStage);
        }

        var topic = await dbContext.SyllabusItems.AsNoTracking()
            .Where(item => item.Id == itemId && item.SyllabusSubject.SyllabusVersion.Status == SyllabusVersionStatus.Published)
            .Select(item => new { item.SyllabusSubject.SubjectId, StageCode = item.SyllabusSubject.SyllabusVersion.ExamStage.Code })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw Invalid("The topic must belong to a published syllabus version.");
        if (subjectId is Guid given && given != topic.SubjectId)
        {
            throw Invalid("The topic does not belong to the selected subject.");
        }

        var topicStage = Enum.TryParse<StudyStage>(topic.StageCode, true, out var resolved) ? resolved : StudyStage.General;
        if (parsedStage is StudyStage explicitStage && explicitStage != topicStage && explicitStage != StudyStage.General)
        {
            throw Invalid("The topic belongs to a different exam stage.");
        }

        return (topic.SubjectId, parsedStage ?? topicStage);
    }
}

internal static class QueryableDeleteExtensions
{
    /// <summary>Loads and removes matching rows through the change tracker so cascades and SetNull rules also work in tests.</summary>
    public static async Task<bool> ExecuteDeleteSafeAsync<T>(this IQueryable<T> query, ApplicationDbContext dbContext, CancellationToken cancellationToken)
        where T : class
    {
        var rows = await query.ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return false;
        }

        dbContext.RemoveRange(rows);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
