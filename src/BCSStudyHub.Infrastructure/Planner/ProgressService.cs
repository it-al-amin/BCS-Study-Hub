using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.CurrentAffairs;
using BCSStudyHub.Application.Planner;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Infrastructure.Planner;

/// <summary>Study tracking, reports, and the dashboard. Every figure is computed from stored activity records.</summary>
internal sealed class ProgressService(
    ApplicationDbContext dbContext,
    PlannerSettingsStore settingsStore,
    StudyMetrics metrics,
    PlannerService planner,
    TimeProvider timeProvider) : IProgressService
{
    private const int StreakLookbackDays = 730;

    public async Task<ProgressOverviewDto> GetOverviewAsync(Guid userId, CancellationToken cancellationToken)
    {
        var settings = await settingsStore.GetAsync(userId, cancellationToken);
        var today = BangladeshTime.Today(timeProvider);
        var sessions = await dbContext.StudySessions.AsNoTracking()
            .Where(session => session.UserId == userId && session.EndedAt != null)
            .Select(session => new { session.DurationMinutes, session.SubjectId })
            .ToListAsync(cancellationToken);
        var answers = await metrics.McqAnswersAsync(userId, null, null, cancellationToken);
        var stages = await StageProgressAsync(userId, sessions.Where(row => row.SubjectId != null)
            .GroupBy(row => row.SubjectId!.Value).ToDictionary(group => group.Key, group => group.Sum(row => row.DurationMinutes)),
            await metrics.SubjectAccuracyAsync(answers, cancellationToken), cancellationToken);
        var activeDays = await metrics.ActiveDaysAsync(userId, settings.StreakMinimumMinutes, today.AddDays(-StreakLookbackDays), today, cancellationToken);
        var (current, longest) = StreakCalculator.Compute(activeDays, today);
        return new ProgressOverviewDto(
            stages,
            sessions.Sum(row => row.DurationMinutes),
            sessions.Count,
            new StreakDto(current, longest, settings.StreakMinimumMinutes),
            await metrics.PracticeAsync(userId, null, null, cancellationToken));
    }

    public async Task<IReadOnlyList<TopicStatusDto>> GetTopicsAsync(Guid userId, Guid versionId, Guid? subjectId, string? status, CancellationToken cancellationToken)
    {
        if (!await dbContext.SyllabusVersions.AnyAsync(version => version.Id == versionId && version.Status == SyllabusVersionStatus.Published, cancellationToken))
        {
            throw new PlannerException(StatusCodes.Status404NotFound, "Not found", "The syllabus version is not published.");
        }

        var filter = (status ?? "all").ToLowerInvariant();
        if (filter is not ("all" or "completed" or "pending"))
        {
            throw PlannerValidation.Invalid("Status must be all, completed, or pending.");
        }

        var items = await dbContext.SyllabusItems.AsNoTracking()
            .Where(item => item.SyllabusSubject.SyllabusVersionId == versionId && (subjectId == null || item.SyllabusSubject.SubjectId == subjectId))
            .OrderBy(item => item.SyllabusSubject.SortOrder).ThenBy(item => item.SortOrder)
            .Select(item => new { item.Id, item.Title, item.Kind, item.ParentId, item.SyllabusSubject.SubjectId, SubjectName = item.SyllabusSubject.Subject.Name })
            .ToListAsync(cancellationToken);
        var ids = items.Select(item => item.Id).ToArray();
        var progress = await dbContext.TopicProgress.AsNoTracking()
            .Where(row => row.UserId == userId && ids.Contains(row.SyllabusItemId))
            .ToDictionaryAsync(row => row.SyllabusItemId, cancellationToken);
        var answers = (await metrics.McqAnswersAsync(userId, null, null, cancellationToken))
            .Where(answer => ids.Contains(answer.SyllabusItemId))
            .GroupBy(answer => answer.SyllabusItemId)
            .ToDictionary(group => group.Key, group => (Answered: group.Sum(answer => answer.Answered), Correct: group.Sum(answer => answer.Correct)));
        var minutes = (await dbContext.StudySessions.AsNoTracking()
                .Where(session => session.UserId == userId && session.EndedAt != null && session.SyllabusItemId != null && ids.Contains(session.SyllabusItemId.Value))
                .Select(session => new { ItemId = session.SyllabusItemId!.Value, session.DurationMinutes }).ToListAsync(cancellationToken))
            .GroupBy(row => row.ItemId).ToDictionary(group => group.Key, group => group.Sum(row => row.DurationMinutes));

        return items
            .Select(item =>
            {
                var row = progress.GetValueOrDefault(item.Id);
                var (answered, correct) = answers.GetValueOrDefault(item.Id);
                return new TopicStatusDto(item.Id, item.Title, item.Kind.ToString(), item.ParentId, item.SubjectId, item.SubjectName,
                    row?.IsCompleted ?? false, row?.CompletedAt, row?.Mastery, answered, PlannerRules.AccuracyPercent(correct, answered), minutes.GetValueOrDefault(item.Id));
            })
            .Where(topic => filter == "all" || (filter == "completed") == topic.IsCompleted)
            .ToArray();
    }

    public async Task<TopicStatusDto?> SetMasteryAsync(Guid userId, Guid syllabusItemId, MasteryInput input, CancellationToken cancellationToken)
    {
        PlannerValidation.Validate(input);
        var item = await dbContext.SyllabusItems.AsNoTracking()
            .Where(candidate => candidate.Id == syllabusItemId && candidate.SyllabusSubject.SyllabusVersion.Status == SyllabusVersionStatus.Published)
            .Select(candidate => new { candidate.SyllabusSubject.SyllabusVersionId, candidate.SyllabusSubject.SubjectId })
            .SingleOrDefaultAsync(cancellationToken);
        if (item is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        var progress = await dbContext.TopicProgress.SingleOrDefaultAsync(row => row.UserId == userId && row.SyllabusItemId == syllabusItemId, cancellationToken);
        if (progress is null)
        {
            progress = new TopicProgress { Id = Guid.NewGuid(), UserId = userId, SyllabusItemId = syllabusItemId };
            dbContext.TopicProgress.Add(progress);
        }

        progress.Mastery = input.Mastery;
        progress.MasteryUpdatedAt = input.Mastery is null ? null : now;
        progress.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetTopicsAsync(userId, item.SyllabusVersionId, item.SubjectId, "all", cancellationToken)).Single(topic => topic.SyllabusItemId == syllabusItemId);
    }

    public async Task<StudyReportDto> GetReportAsync(Guid userId, string period, DateOnly? date, CancellationToken cancellationToken)
    {
        PlannerPeriod plannerPeriod;
        try
        {
            plannerPeriod = PlannerRules.ParsePeriodAlias(period);
        }
        catch (ArgumentException exception)
        {
            throw PlannerValidation.Invalid(exception.Message);
        }

        var digestPeriod = PlannerRules.ToDigestPeriod(plannerPeriod);
        var anchor = date ?? BangladeshTime.Today(timeProvider);
        var (from, to) = CurrentAffairsRules.GetRange(digestPeriod, anchor);
        var (start, end) = BangladeshTime.Range(from, to);
        var settings = await settingsStore.GetAsync(userId, cancellationToken);

        var sessions = await metrics.SessionsAsync(userId, start, end, cancellationToken);
        var subjectNames = await SubjectNamesAsync(sessions.Select(row => row.SubjectId).OfType<Guid>(), cancellationToken);
        var minutesByDay = new List<DailyMinutesDto>();
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            var current = day;
            minutesByDay.Add(new DailyMinutesDto(day, sessions.Where(row => BangladeshTime.ToLocalDate(row.StartedAt) == current).Sum(row => row.Minutes)));
        }

        var tasks = await dbContext.StudyTasks.AsNoTracking()
            .Where(task => task.UserId == userId && task.ScheduledDate >= from && task.ScheduledDate <= to)
            .Select(task => new { task.Status, task.PlannedMinutes, task.ActualMinutes })
            .ToListAsync(cancellationToken);
        var completedTopics = await dbContext.TopicProgress.AsNoTracking()
            .Where(progress => progress.UserId == userId && progress.IsCompleted && progress.CompletedAt >= start && progress.CompletedAt < end)
            .OrderBy(progress => progress.CompletedAt)
            .Select(progress => progress.SyllabusItem.Title)
            .ToListAsync(cancellationToken);
        var answers = await metrics.McqAnswersAsync(userId, start, end, cancellationToken);
        var bySubject = await metrics.SubjectAccuracyAsync(answers, cancellationToken);
        var activeDays = await metrics.ActiveDaysAsync(userId, settings.StreakMinimumMinutes, from, to, cancellationToken);
        var goals = await dbContext.StudyGoals.AsNoTracking().Include(goal => goal.Subject)
            .Where(goal => goal.UserId == userId && goal.Period == plannerPeriod && goal.PeriodStart == from)
            .ToListAsync(cancellationToken);
        var goalDtos = new List<StudyGoalDto>();
        foreach (var goal in goals)
        {
            goalDtos.Add(await planner.ToGoalDtoAsync(userId, goal, cancellationToken));
        }

        return new StudyReportDto(
            plannerPeriod.ToString(),
            CurrentAffairsRules.Label(digestPeriod, anchor),
            from,
            to,
            sessions.Sum(row => row.Minutes),
            sessions.Count,
            activeDays.Count,
            minutesByDay,
            sessions.GroupBy(row => row.SubjectId)
                .Select(group => new NamedMinutesDto(group.Key?.ToString() ?? "none", group.Key is Guid id ? subjectNames.GetValueOrDefault(id, "Unknown subject") : "No subject", group.Sum(row => row.Minutes)))
                .OrderByDescending(item => item.Minutes).ToArray(),
            sessions.GroupBy(row => row.Stage)
                .Select(group => new NamedMinutesDto(group.Key?.ToString() ?? "none", group.Key?.ToString() ?? "No stage", group.Sum(row => row.Minutes)))
                .OrderByDescending(item => item.Minutes).ToArray(),
            sessions.GroupBy(row => row.Activity)
                .Select(group => new NamedMinutesDto(group.Key.ToString(), group.Key.ToString(), group.Sum(row => row.Minutes)))
                .OrderByDescending(item => item.Minutes).ToArray(),
            new TaskSummaryDto(
                tasks.Count,
                tasks.Count(task => task.Status == StudyTaskStatus.Completed),
                tasks.Count(task => task.Status == StudyTaskStatus.Skipped),
                tasks.Count(task => task.Status is StudyTaskStatus.Planned or StudyTaskStatus.InProgress),
                tasks.Sum(task => task.PlannedMinutes),
                tasks.Sum(task => task.ActualMinutes)),
            completedTopics.Count,
            completedTopics.Take(50).ToArray(),
            await metrics.PracticeAsync(userId, start, end, cancellationToken),
            bySubject,
            StudyMetrics.WeakSubjects(bySubject, settings),
            await RevisionStatsAsync(userId, start, end, cancellationToken),
            goalDtos);
    }

    public async Task<DashboardDto> GetDashboardAsync(Guid userId, CancellationToken cancellationToken)
    {
        var settings = await settingsStore.GetAsync(userId, cancellationToken);
        var today = BangladeshTime.Today(timeProvider);
        var todayTasks = await dbContext.StudyTasks.AsNoTracking().Include(task => task.Subject).Include(task => task.SyllabusItem)
            .Where(task => task.UserId == userId && task.ScheduledDate == today)
            .OrderBy(task => task.StartTime).ThenByDescending(task => task.Priority)
            .ToListAsync(cancellationToken);
        var active = await planner.GetActiveSessionAsync(userId, cancellationToken);

        var weekStart = CurrentAffairsRules.GetRange(DigestPeriod.Week, today).From;
        var monthStart = CurrentAffairsRules.GetRange(DigestPeriod.Month, today).From;
        var monthSessions = await metrics.SessionsAsync(userId, BangladeshTime.StartOf(weekStart < monthStart ? weekStart : monthStart), BangladeshTime.StartOf(today.AddDays(1)), cancellationToken);
        int MinutesSince(DateOnly day) => monthSessions.Where(row => BangladeshTime.ToLocalDate(row.StartedAt) >= day).Sum(row => row.Minutes);

        var activeDays = await metrics.ActiveDaysAsync(userId, settings.StreakMinimumMinutes, today.AddDays(-StreakLookbackDays), today, cancellationToken);
        var (current, longest) = StreakCalculator.Compute(activeDays, today);

        var goals = new List<StudyGoalDto>();
        var goalRows = await dbContext.StudyGoals.AsNoTracking().Include(goal => goal.Subject)
            .Where(goal => goal.UserId == userId &&
                           ((goal.Period == PlannerPeriod.Daily && goal.PeriodStart == today) ||
                            (goal.Period == PlannerPeriod.Weekly && goal.PeriodStart == weekStart) ||
                            (goal.Period == PlannerPeriod.Monthly && goal.PeriodStart == monthStart)))
            .ToListAsync(cancellationToken);
        foreach (var goal in goalRows)
        {
            goals.Add(await planner.ToGoalDtoAsync(userId, goal, cancellationToken));
        }

        var thirtyDaysAgo = BangladeshTime.StartOf(today.AddDays(-29));
        var recentAnswers = await metrics.McqAnswersAsync(userId, thirtyDaysAgo, null, cancellationToken);
        var allSessions = await dbContext.StudySessions.AsNoTracking()
            .Where(session => session.UserId == userId && session.EndedAt != null && session.SubjectId != null)
            .Select(session => new { SubjectId = session.SubjectId!.Value, session.DurationMinutes })
            .ToListAsync(cancellationToken);
        var allAccuracy = await metrics.SubjectAccuracyAsync(await metrics.McqAnswersAsync(userId, null, null, cancellationToken), cancellationToken);
        var stages = await StageProgressAsync(userId,
            allSessions.GroupBy(row => row.SubjectId).ToDictionary(group => group.Key, group => group.Sum(row => row.DurationMinutes)),
            allAccuracy, cancellationToken);

        return new DashboardDto(
            today,
            await planner.ToTaskDtosAsync(todayTasks, cancellationToken),
            active,
            MinutesSince(today),
            MinutesSince(weekStart),
            MinutesSince(monthStart),
            new StreakDto(current, longest, settings.StreakMinimumMinutes),
            await dbContext.RevisionItems.AsNoTracking().CountAsync(item => item.UserId == userId && item.Status == RevisionItemStatus.Active && item.DueOn <= today, cancellationToken),
            await dbContext.CurrentAffairsRevisions.AsNoTracking().CountAsync(item => item.UserId == userId && item.NextReviewOn <= today && item.Entry.Status == CurrentAffairsStatus.Published, cancellationToken),
            await dbContext.MistakeEntries.AsNoTracking().CountAsync(mistake => mistake.UserId == userId && mistake.Status == MistakeStatus.Open, cancellationToken),
            goals,
            stages,
            await metrics.PracticeAsync(userId, thirtyDaysAgo, null, cancellationToken),
            StudyMetrics.WeakSubjects(await metrics.SubjectAccuracyAsync(recentAnswers, cancellationToken), settings).Take(3).ToArray(),
            await RecentActivityAsync(userId, cancellationToken));
    }

    private async Task<IReadOnlyList<StageProgressDto>> StageProgressAsync(
        Guid userId,
        IReadOnlyDictionary<Guid, int> minutesBySubject,
        IReadOnlyList<SubjectAccuracyDto> accuracyBySubject,
        CancellationToken cancellationToken)
    {
        var versions = await dbContext.SyllabusVersions.AsNoTracking()
            .Where(version => version.Status == SyllabusVersionStatus.Published)
            .OrderBy(version => version.ExamStage.SortOrder).ThenBy(version => version.TrackCode)
            .Select(version => new
            {
                version.Id,
                version.VersionLabel,
                version.TrackCode,
                StageCode = version.ExamStage.Code,
                StageName = version.ExamStage.Name,
                Subjects = version.Subjects.OrderBy(subject => subject.SortOrder).Select(subject => new { subject.SubjectId, subject.Subject.Name }).ToList(),
            })
            .ToListAsync(cancellationToken);
        var versionIds = versions.Select(version => version.Id).ToArray();
        var items = await dbContext.SyllabusItems.AsNoTracking()
            .Where(item => versionIds.Contains(item.SyllabusSubject.SyllabusVersionId))
            .Select(item => new { item.Id, VersionId = item.SyllabusSubject.SyllabusVersionId, item.SyllabusSubject.SubjectId })
            .ToListAsync(cancellationToken);
        var itemIds = items.Select(item => item.Id).ToArray();
        var progress = await dbContext.TopicProgress.AsNoTracking()
            .Where(row => row.UserId == userId && itemIds.Contains(row.SyllabusItemId))
            .ToDictionaryAsync(row => row.SyllabusItemId, row => (row.IsCompleted, row.Mastery), cancellationToken);
        var accuracy = accuracyBySubject.ToDictionary(item => item.SubjectId);

        return versions.Select(version =>
        {
            var versionItems = items.Where(item => item.VersionId == version.Id).ToArray();
            var subjects = version.Subjects.Select(subject =>
            {
                var subjectItems = versionItems.Where(item => item.SubjectId == subject.SubjectId).ToArray();
                var completed = subjectItems.Count(item => progress.TryGetValue(item.Id, out var row) && row.IsCompleted);
                var masteries = subjectItems.Select(item => progress.TryGetValue(item.Id, out var row) ? row.Mastery : null).OfType<int>().ToArray();
                var subjectAccuracy = accuracy.GetValueOrDefault(subject.SubjectId);
                return new SubjectProgressDto(
                    subject.SubjectId, subject.Name, subjectItems.Length, completed, Percent(completed, subjectItems.Length),
                    masteries.Length == 0 ? null : Math.Round(masteries.Average(), 1), minutesBySubject.GetValueOrDefault(subject.SubjectId),
                    subjectAccuracy?.Answered ?? 0, subjectAccuracy?.AccuracyPercent);
            }).ToArray();
            var completedTotal = subjects.Sum(subject => subject.CompletedTopics);
            return new StageProgressDto(version.StageCode, version.StageName, version.Id, version.VersionLabel, version.TrackCode,
                versionItems.Length, completedTotal, Percent(completedTotal, versionItems.Length), subjects);
        }).ToArray();
    }

    private async Task<RevisionStatsDto> RevisionStatsAsync(Guid userId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken)
    {
        var today = BangladeshTime.Today(timeProvider);
        var outcomes = await dbContext.RevisionLogs.AsNoTracking()
            .Where(log => log.UserId == userId && log.ReviewedAt >= start && log.ReviewedAt < end)
            .Select(log => log.Outcome)
            .ToListAsync(cancellationToken);
        var byOutcome = Enum.GetValues<RevisionOutcome>().ToDictionary(outcome => outcome.ToString(), outcome => outcomes.Count(value => value == outcome));
        var recalled = outcomes.Count(outcome => outcome is RevisionOutcome.Good or RevisionOutcome.Easy);
        var items = await dbContext.RevisionItems.AsNoTracking().Where(item => item.UserId == userId)
            .Select(item => new { item.Status, item.DueOn }).ToListAsync(cancellationToken);
        return new RevisionStatsDto(
            outcomes.Count,
            byOutcome,
            PlannerRules.AccuracyPercent(recalled, outcomes.Count),
            items.Count(item => item.Status == RevisionItemStatus.Active),
            items.Count(item => item.Status == RevisionItemStatus.Active && item.DueOn <= today),
            items.Count(item => item.Status == RevisionItemStatus.Active && item.DueOn < today),
            items.Count(item => item.Status == RevisionItemStatus.Retired));
    }

    private async Task<IReadOnlyList<ActivityDto>> RecentActivityAsync(Guid userId, CancellationToken cancellationToken)
    {
        const int take = 10;
        var activity = new List<ActivityDto>();
        activity.AddRange((await dbContext.StudySessions.AsNoTracking()
                .Where(session => session.UserId == userId && session.EndedAt != null)
                .OrderByDescending(session => session.EndedAt).Take(take)
                .Select(session => new { session.EndedAt, session.Activity, session.DurationMinutes, Subject = session.Subject != null ? session.Subject.Name : null })
                .ToListAsync(cancellationToken))
            .Select(row => new ActivityDto("StudySession", row.Subject is null ? $"{row.Activity} session" : $"{row.Activity}: {row.Subject}", row.EndedAt!.Value, $"{row.DurationMinutes} min")));
        activity.AddRange((await dbContext.McqAttempts.AsNoTracking()
                .Where(attempt => attempt.UserId == userId && attempt.Status == McqAttemptStatus.Submitted)
                .OrderByDescending(attempt => attempt.SubmittedAt).Take(take)
                .Select(attempt => new { attempt.SubmittedAt, attempt.ExamSetNameSnapshot, attempt.CorrectCount, attempt.IncorrectCount, attempt.UnansweredCount })
                .ToListAsync(cancellationToken))
            .Select(row => new ActivityDto("McqAttempt", row.ExamSetNameSnapshot, row.SubmittedAt!.Value,
                $"{row.CorrectCount} correct of {row.CorrectCount + row.IncorrectCount + row.UnansweredCount}")));
        activity.AddRange((await dbContext.WrittenAnswers.AsNoTracking()
                .Where(answer => answer.UserId == userId && answer.Status == WrittenAnswerStatus.Submitted)
                .OrderByDescending(answer => answer.SubmittedAt).Take(take)
                .Select(answer => new { answer.SubmittedAt, answer.Question.Prompt, answer.WordCount })
                .ToListAsync(cancellationToken))
            .Select(row => new ActivityDto("WrittenSubmission", Clip(row.Prompt), row.SubmittedAt!.Value, $"{row.WordCount} words")));
        activity.AddRange((await dbContext.VivaMockSessions.AsNoTracking()
                .Where(session => session.UserId == userId && session.Status == VivaSessionStatus.Completed)
                .OrderByDescending(session => session.CompletedAt).Take(take)
                .Select(session => new { session.CompletedAt, session.Title })
                .ToListAsync(cancellationToken))
            .Select(row => new ActivityDto("VivaSession", row.Title, row.CompletedAt!.Value, null)));
        activity.AddRange((await dbContext.RevisionLogs.AsNoTracking()
                .Where(log => log.UserId == userId)
                .OrderByDescending(log => log.ReviewedAt).Take(take)
                .Select(log => new { log.ReviewedAt, log.Title, log.Outcome, log.IntervalDays })
                .ToListAsync(cancellationToken))
            .Select(row => new ActivityDto("RevisionReview", row.Title, row.ReviewedAt, $"{row.Outcome} · next in {row.IntervalDays} d")));
        return activity.OrderByDescending(item => item.OccurredAt).Take(take).ToArray();
    }

    private async Task<Dictionary<Guid, string>> SubjectNamesAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var idList = ids.Distinct().ToArray();
        return idList.Length == 0
            ? []
            : await dbContext.Subjects.AsNoTracking().Where(subject => idList.Contains(subject.Id)).ToDictionaryAsync(subject => subject.Id, subject => subject.Name, cancellationToken);
    }

    private static int Percent(int part, int total) => total == 0 ? 0 : (int)Math.Round(part * 100d / total);

    private static string Clip(string value) => value.Length <= 120 ? value : value[..119].TrimEnd() + "…";
}
