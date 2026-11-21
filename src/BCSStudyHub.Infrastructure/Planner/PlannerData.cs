using System.Text.Json;
using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.Planner;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Infrastructure.Planner;

/// <summary>Loads a user's planner settings, falling back to the documented defaults when none are saved.</summary>
internal sealed class PlannerSettingsStore(ApplicationDbContext dbContext)
{
    public static PlannerSettingsDto Defaults
    {
        get
        {
            var policy = RevisionPolicy.Default;
            return new PlannerSettingsDto(
                policy.IntervalsDays, policy.AgainStep, policy.HardStepDelta, policy.GoodStepDelta, policy.EasyStepDelta,
                policy.RetireAfterStep, 50, 15, 60, 5, true, IsDefault: true);
        }
    }

    public async Task<PlannerSettingsDto> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var row = await dbContext.PlannerSettings.AsNoTracking().SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        return row is null ? Defaults : ToDto(row);
    }

    public async Task<RevisionPolicy> GetPolicyAsync(Guid userId, CancellationToken cancellationToken) =>
        ToPolicy(await GetAsync(userId, cancellationToken));

    public static RevisionPolicy ToPolicy(PlannerSettingsDto settings) =>
        new(settings.RevisionIntervalsDays, settings.AgainStep, settings.HardStepDelta, settings.GoodStepDelta, settings.EasyStepDelta, settings.RetireAfterStep);

    public static PlannerSettingsDto ToDto(PlannerSettings row)
    {
        var intervals = JsonSerializer.Deserialize<int[]>(row.RevisionIntervalsJson) ?? [];
        var dto = new PlannerSettingsDto(
            intervals, row.AgainStep, row.HardStepDelta, row.GoodStepDelta, row.EasyStepDelta, row.RetireAfterStep,
            row.DailyRevisionLimit, row.StreakMinimumMinutes, row.WeakTopicAccuracyPercent, row.WeakTopicMinimumAnswers,
            row.AutoQueueMistakes, IsDefault: false);

        // A corrupted or outdated row must never break scheduling; fall back to the defaults' policy.
        return ToPolicy(dto).Validate() is null
            ? dto
            : dto with
            {
                RevisionIntervalsDays = Defaults.RevisionIntervalsDays,
                AgainStep = Defaults.AgainStep,
                HardStepDelta = Defaults.HardStepDelta,
                GoodStepDelta = Defaults.GoodStepDelta,
                EasyStepDelta = Defaults.EasyStepDelta,
                RetireAfterStep = Defaults.RetireAfterStep,
            };
    }
}

internal sealed record SessionRow(DateTimeOffset StartedAt, int Minutes, Guid? SubjectId, StudyStage? Stage, StudyActivity Activity, Guid? SyllabusItemId);

/// <summary>MCQ answers for one topic: how many were answered and how many were correct.</summary>
internal sealed record McqTopicStats(Guid SyllabusItemId, int Answered, int Correct);

internal sealed record TopicInfo(Guid Id, string Title, Guid SubjectId, string SubjectName, string StageCode);

/// <summary>Read-only aggregations over recorded activity. Nothing here is cached or hardcoded.</summary>
internal sealed class StudyMetrics(ApplicationDbContext dbContext)
{
    public async Task<List<SessionRow>> SessionsAsync(Guid userId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken) =>
        await dbContext.StudySessions.AsNoTracking()
            .Where(session => session.UserId == userId && session.EndedAt != null && session.StartedAt >= start && session.StartedAt < end)
            .Select(session => new SessionRow(session.StartedAt, session.DurationMinutes, session.SubjectId, session.Stage, session.Activity, session.SyllabusItemId))
            .ToListAsync(cancellationToken);

    public async Task<List<McqTopicStats>> McqAnswersAsync(Guid userId, DateTimeOffset? start, DateTimeOffset? end, CancellationToken cancellationToken)
    {
        var answers = dbContext.McqAttemptAnswers.AsNoTracking()
            .Where(answer => answer.IsCorrect != null &&
                             answer.AttemptQuestion.Attempt.UserId == userId &&
                             answer.AttemptQuestion.Attempt.Status == McqAttemptStatus.Submitted &&
                             answer.AttemptQuestion.Attempt.SubmittedAt != null);
        if (start is DateTimeOffset from)
        {
            answers = answers.Where(answer => answer.AttemptQuestion.Attempt.SubmittedAt >= from);
        }

        if (end is DateTimeOffset to)
        {
            answers = answers.Where(answer => answer.AttemptQuestion.Attempt.SubmittedAt < to);
        }

        // Aggregated per topic in SQL: the result size is bounded by the number of topics practised,
        // not by how many questions a user has ever answered.
        var grouped = await answers
            .GroupBy(answer => answer.AttemptQuestion.SyllabusItemIdSnapshot)
            .Select(group => new { group.Key, Answered = group.Count(), Correct = group.Count(answer => answer.IsCorrect == true) })
            .ToListAsync(cancellationToken);
        return grouped.Select(row => new McqTopicStats(row.Key, row.Answered, row.Correct)).ToList();
    }

    public async Task<Dictionary<Guid, TopicInfo>> TopicsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var idList = ids.Distinct().ToArray();
        if (idList.Length == 0)
        {
            return [];
        }

        return await dbContext.SyllabusItems.AsNoTracking()
            .Where(item => idList.Contains(item.Id))
            .Select(item => new TopicInfo(
                item.Id,
                item.Title,
                item.SyllabusSubject.SubjectId,
                item.SyllabusSubject.Subject.Name,
                item.SyllabusSubject.SyllabusVersion.ExamStage.Code))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
    }

    public async Task<int> GoalValueAsync(Guid userId, StudyGoal goal, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var (start, end) = BangladeshTime.Range(from, to);
        var stageCode = goal.Stage?.ToString();
        switch (goal.Metric)
        {
            case GoalMetric.StudyMinutes:
                return (await SessionsAsync(userId, start, end, cancellationToken))
                    .Where(row => (goal.SubjectId is null || row.SubjectId == goal.SubjectId) && (goal.Stage is null || row.Stage == goal.Stage))
                    .Sum(row => row.Minutes);
            case GoalMetric.CompletedTasks:
                return await dbContext.StudyTasks.AsNoTracking().CountAsync(task =>
                    task.UserId == userId && task.Status == StudyTaskStatus.Completed &&
                    task.ScheduledDate >= from && task.ScheduledDate <= to &&
                    (goal.SubjectId == null || task.SubjectId == goal.SubjectId) &&
                    (goal.Stage == null || task.Stage == goal.Stage), cancellationToken);
            case GoalMetric.CompletedTopics:
                return await dbContext.TopicProgress.AsNoTracking().CountAsync(progress =>
                    progress.UserId == userId && progress.IsCompleted &&
                    progress.CompletedAt >= start && progress.CompletedAt < end &&
                    (goal.SubjectId == null || progress.SyllabusItem.SyllabusSubject.SubjectId == goal.SubjectId) &&
                    (stageCode == null || progress.SyllabusItem.SyllabusSubject.SyllabusVersion.ExamStage.Code == stageCode), cancellationToken);
            case GoalMetric.McqQuestionsAnswered:
            {
                var rows = await McqAnswersAsync(userId, start, end, cancellationToken);
                if (goal.SubjectId is null && goal.Stage is null)
                {
                    return rows.Sum(row => row.Answered);
                }

                var topics = await TopicsAsync(rows.Select(row => row.SyllabusItemId), cancellationToken);
                return rows.Where(row => topics.TryGetValue(row.SyllabusItemId, out var topic) &&
                                         (goal.SubjectId is null || topic.SubjectId == goal.SubjectId) &&
                                         (stageCode is null || topic.StageCode == stageCode))
                    .Sum(row => row.Answered);
            }
            case GoalMetric.WrittenSubmissions:
                if (goal.Stage is not (null or StudyStage.Written))
                {
                    return 0;
                }

                return await dbContext.WrittenAnswers.AsNoTracking().CountAsync(answer =>
                    answer.UserId == userId && answer.Status == WrittenAnswerStatus.Submitted &&
                    answer.SubmittedAt >= start && answer.SubmittedAt < end &&
                    (goal.SubjectId == null || answer.Question.SyllabusItem.SyllabusSubject.SubjectId == goal.SubjectId), cancellationToken);
            case GoalMetric.VivaSessions:
                if (goal.Stage is not (null or StudyStage.Viva))
                {
                    return 0;
                }

                return await dbContext.VivaMockSessions.AsNoTracking().CountAsync(session =>
                    session.UserId == userId && session.Status == VivaSessionStatus.Completed &&
                    session.CompletedAt >= start && session.CompletedAt < end, cancellationToken);
            case GoalMetric.RevisionReviews:
            {
                var logs = await dbContext.RevisionLogs.AsNoTracking()
                    .Where(log => log.UserId == userId && log.ReviewedAt >= start && log.ReviewedAt < end)
                    .Select(log => log.SyllabusItemId)
                    .ToListAsync(cancellationToken);
                if (goal.SubjectId is null && goal.Stage is null)
                {
                    return logs.Count;
                }

                var topics = await TopicsAsync(logs.OfType<Guid>(), cancellationToken);
                return logs.Count(id => id is Guid topicId && topics.TryGetValue(topicId, out var topic) &&
                                        (goal.SubjectId is null || topic.SubjectId == goal.SubjectId) &&
                                        (stageCode is null || topic.StageCode == stageCode));
            }
            default:
                return 0;
        }
    }

    /// <summary>
    /// Days with qualifying activity: at least the user's minimum session minutes, or any submitted MCQ attempt,
    /// submitted Written answer, completed Viva session, or revision review.
    /// </summary>
    public async Task<HashSet<DateOnly>> ActiveDaysAsync(Guid userId, int minimumMinutes, DateOnly since, DateOnly until, CancellationToken cancellationToken)
    {
        var (start, end) = BangladeshTime.Range(since, until);
        var days = (await SessionsAsync(userId, start, end, cancellationToken))
            .GroupBy(row => BangladeshTime.ToLocalDate(row.StartedAt))
            .Where(group => group.Sum(row => row.Minutes) >= minimumMinutes)
            .Select(group => group.Key)
            .ToHashSet();

        var instants = new List<DateTimeOffset>();
        instants.AddRange(await dbContext.McqAttempts.AsNoTracking()
            .Where(attempt => attempt.UserId == userId && attempt.Status == McqAttemptStatus.Submitted && attempt.SubmittedAt >= start && attempt.SubmittedAt < end)
            .Select(attempt => attempt.SubmittedAt!.Value).ToListAsync(cancellationToken));
        instants.AddRange(await dbContext.WrittenAnswers.AsNoTracking()
            .Where(answer => answer.UserId == userId && answer.Status == WrittenAnswerStatus.Submitted && answer.SubmittedAt >= start && answer.SubmittedAt < end)
            .Select(answer => answer.SubmittedAt!.Value).ToListAsync(cancellationToken));
        instants.AddRange(await dbContext.VivaMockSessions.AsNoTracking()
            .Where(session => session.UserId == userId && session.Status == VivaSessionStatus.Completed && session.CompletedAt >= start && session.CompletedAt < end)
            .Select(session => session.CompletedAt!.Value).ToListAsync(cancellationToken));
        instants.AddRange(await dbContext.RevisionLogs.AsNoTracking()
            .Where(log => log.UserId == userId && log.ReviewedAt >= start && log.ReviewedAt < end)
            .Select(log => log.ReviewedAt).ToListAsync(cancellationToken));
        days.UnionWith(instants.Select(BangladeshTime.ToLocalDate));
        return days;
    }

    public async Task<PracticePerformanceDto> PracticeAsync(Guid userId, DateTimeOffset? start, DateTimeOffset? end, CancellationToken cancellationToken)
    {
        var answers = await McqAnswersAsync(userId, start, end, cancellationToken);
        var attempts = dbContext.McqAttempts.AsNoTracking().Where(attempt => attempt.UserId == userId && attempt.Status == McqAttemptStatus.Submitted);
        var written = dbContext.WrittenAnswers.AsNoTracking().Where(answer => answer.UserId == userId);
        var viva = dbContext.VivaMockSessions.AsNoTracking().Where(session => session.UserId == userId && session.Status == VivaSessionStatus.Completed);
        if (start is DateTimeOffset from)
        {
            attempts = attempts.Where(attempt => attempt.SubmittedAt >= from);
            written = written.Where(answer => answer.UpdatedAt >= from);
            viva = viva.Where(session => session.CompletedAt >= from);
        }

        if (end is DateTimeOffset to)
        {
            attempts = attempts.Where(attempt => attempt.SubmittedAt < to);
            written = written.Where(answer => answer.UpdatedAt < to);
            viva = viva.Where(session => session.CompletedAt < to);
        }

        var writtenRows = await written.Select(answer => new { answer.Status, answer.WordCount }).ToListAsync(cancellationToken);
        var ratings = await viva
            .Select(session => new { session.CommunicationRating, session.SubjectKnowledgeRating, session.ClarityRating, session.ConfidenceRating })
            .ToListAsync(cancellationToken);
        var ratingValues = ratings
            .SelectMany(rating => new[] { rating.CommunicationRating, rating.SubjectKnowledgeRating, rating.ClarityRating, rating.ConfidenceRating })
            .OfType<int>()
            .ToArray();
        var correct = answers.Sum(answer => answer.Correct);
        var answered = answers.Sum(answer => answer.Answered);
        return new PracticePerformanceDto(
            await attempts.CountAsync(cancellationToken),
            answered,
            correct,
            PlannerRules.AccuracyPercent(correct, answered),
            writtenRows.Count(row => row.Status == WrittenAnswerStatus.Draft),
            writtenRows.Count(row => row.Status == WrittenAnswerStatus.Submitted),
            writtenRows.Where(row => row.Status == WrittenAnswerStatus.Submitted).Sum(row => row.WordCount),
            ratings.Count,
            ratingValues.Length == 0 ? null : Math.Round(ratingValues.Average(), 1));
    }

    public async Task<List<SubjectAccuracyDto>> SubjectAccuracyAsync(List<McqTopicStats> answers, CancellationToken cancellationToken)
    {
        var topics = await TopicsAsync(answers.Select(answer => answer.SyllabusItemId), cancellationToken);
        return answers
            .Where(answer => topics.ContainsKey(answer.SyllabusItemId))
            .GroupBy(answer => topics[answer.SyllabusItemId].SubjectId)
            .Select(group =>
            {
                var answered = group.Sum(answer => answer.Answered);
                var correct = group.Sum(answer => answer.Correct);
                return new SubjectAccuracyDto(group.Key, topics[group.First().SyllabusItemId].SubjectName, answered, correct, PlannerRules.AccuracyPercent(correct, answered));
            })
            .OrderBy(item => item.SubjectName)
            .ToList();
    }

    /// <summary>Subjects whose MCQ accuracy is below the user's threshold, given enough answers to be meaningful.</summary>
    public static List<SubjectAccuracyDto> WeakSubjects(IEnumerable<SubjectAccuracyDto> subjects, PlannerSettingsDto settings) =>
        subjects
            .Where(subject => subject.Answered >= settings.WeakTopicMinimumAnswers && subject.AccuracyPercent < settings.WeakTopicAccuracyPercent)
            .OrderBy(subject => subject.AccuracyPercent)
            .ThenByDescending(subject => subject.Answered)
            .ToList();
}
