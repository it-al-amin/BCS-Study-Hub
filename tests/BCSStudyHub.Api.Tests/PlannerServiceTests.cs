using BCSStudyHub.Application.Planner;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Planner;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BCSStudyHub.Api.Tests;

public sealed class PlannerServiceTests : IDisposable
{
    // Saturday 21 Nov 2026, 10:00 in Dhaka.
    private readonly FixedTimeProvider clock = new(new DateTimeOffset(2026, 11, 21, 4, 0, 0, TimeSpan.Zero));
    private readonly ApplicationDbContext dbContext;
    private readonly PlannerService planner;
    private readonly RevisionService revision;
    private readonly ProgressService progress;
    private readonly Guid alice = Guid.NewGuid();
    private readonly Guid bob = Guid.NewGuid();
    private readonly Guid subjectId = Guid.NewGuid();
    private readonly Guid otherSubjectId = Guid.NewGuid();
    private readonly Guid topicId = Guid.NewGuid();
    private readonly Guid versionId = Guid.NewGuid();
    private readonly Guid mcqId = Guid.NewGuid();
    private readonly Guid draftMcqId = Guid.NewGuid();

    public PlannerServiceTests()
    {
        dbContext = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        Seed();
        var store = new PlannerSettingsStore(dbContext);
        var metrics = new StudyMetrics(dbContext);
        planner = new PlannerService(dbContext, store, metrics, clock);
        revision = new RevisionService(dbContext, store, metrics, clock);
        progress = new ProgressService(dbContext, store, metrics, planner, clock);
    }

    private DateOnly Today => new(2026, 11, 21);

    [Fact]
    public async Task TasksAreOwnerScopedAndTopicsFillSubjectAndStage()
    {
        var task = await planner.CreateTaskAsync(alice, Task("Read constitution") with { SyllabusItemId = topicId }, default);
        Assert.Equal(subjectId, task.SubjectId);
        Assert.Equal("Preliminary", task.Stage);

        await Assert.ThrowsAsync<PlannerException>(() => planner.CreateTaskAsync(alice, Task("x") with { SyllabusItemId = topicId, SubjectId = otherSubjectId }, default));
        await Assert.ThrowsAsync<PlannerException>(() => planner.CreateTaskAsync(alice, Task("x") with { SyllabusItemId = topicId, Stage = "Viva" }, default));

        Assert.Null(await planner.UpdateTaskAsync(bob, task.Id, Task("Hijack"), default));
        Assert.False(await planner.DeleteTaskAsync(bob, task.Id, default));
        Assert.Empty((await planner.GetTasksAsync(bob, new StudyTaskQuery(null, null, null, null, null, 1, 50), default)).Items);
    }

    [Fact]
    public async Task SessionTimerTracksActualTaskTime()
    {
        var task = await planner.CreateTaskAsync(alice, Task("Practice MCQ") with { PlannedMinutes = 60 }, default);
        var session = await planner.StartSessionAsync(alice, new StartSessionInput { Activity = "Practice", TaskId = task.Id }, default);
        Assert.True(session.IsRunning);
        await Assert.ThrowsAsync<PlannerException>(() => planner.StartSessionAsync(alice, new StartSessionInput { Activity = "Reading" }, default));
        Assert.Null(await planner.StopSessionAsync(bob, session.Id, null, default));

        clock.Now = clock.Now.AddMinutes(25.3);
        var stopped = await planner.StopSessionAsync(alice, session.Id, "Finished set 1", default);
        Assert.Equal(26, stopped!.DurationMinutes);

        var refreshed = (await planner.GetTasksAsync(alice, new StudyTaskQuery(Today, Today, null, null, null, 1, 50), default)).Items.Single();
        Assert.Equal(26, refreshed.ActualMinutes);
        Assert.Equal("InProgress", refreshed.Status);
        Assert.True(refreshed.HasSessions);
        await Assert.ThrowsAsync<PlannerException>(() => planner.SetTaskStatusAsync(alice, task.Id, new StudyTaskStatusInput { Status = "Completed", ActualMinutes = 90 }, default));

        await planner.DeleteSessionAsync(alice, session.Id, default);
        var manual = await planner.SetTaskStatusAsync(alice, task.Id, new StudyTaskStatusInput { Status = "Completed", ActualMinutes = 50 }, default);
        Assert.Equal(50, manual!.ActualMinutes);
        Assert.NotNull(manual.CompletedAt);
    }

    [Fact]
    public async Task LoggedSessionsCannotEndInTheFuture()
    {
        await Assert.ThrowsAsync<PlannerException>(() => planner.LogSessionAsync(alice,
            new StudySessionInput { StartedAt = clock.Now.AddMinutes(-10), DurationMinutes = 60, Activity = "Reading" }, default));
    }

    [Fact]
    public async Task SchedulesGenerateTasksOnMatchingDaysOnlyOnce()
    {
        var schedule = await planner.CreateScheduleAsync(alice, new StudyScheduleInput
        {
            Title = "Bangla grammar",
            Days = ["Monday", "Wednesday"],
            PlannedMinutes = 45,
            StartDate = Today,
        }, default);

        var range = new GenerateTasksInput { From = new DateOnly(2026, 11, 22), To = new DateOnly(2026, 12, 5) };
        Assert.Equal(4, await planner.GenerateTasksAsync(alice, schedule.Id, range, default));
        Assert.Equal(0, await planner.GenerateTasksAsync(alice, schedule.Id, range, default));
        Assert.Null(await planner.GenerateTasksAsync(bob, schedule.Id, range, default));
        await Assert.ThrowsAsync<PlannerException>(() => planner.GenerateTasksAsync(alice, schedule.Id,
            new GenerateTasksInput { From = Today, To = Today.AddDays(200) }, default));

        var tasks = (await planner.GetTasksAsync(alice, new StudyTaskQuery(range.From, range.To, null, null, null, 1, 50), default)).Items;
        Assert.All(tasks, task => Assert.Contains(task.ScheduledDate.DayOfWeek, new[] { DayOfWeek.Monday, DayOfWeek.Wednesday }));
        Assert.All(tasks, task => Assert.Equal(45, task.PlannedMinutes));
    }

    [Fact]
    public async Task GoalProgressIsComputedFromRecordedActivity()
    {
        await LogAsync(alice, Today.AddDays(-5), 40, subjectId);
        await LogAsync(alice, Today, 30, subjectId);
        await LogAsync(alice, Today, 50, otherSubjectId);
        await LogAsync(bob, Today, 120, subjectId);

        var goal = await planner.CreateGoalAsync(alice, new StudyGoalInput
        {
            Period = "Weekly", Date = Today, Title = "Bangladesh affairs time", Metric = "StudyMinutes", TargetValue = 120, SubjectId = subjectId,
        }, default);

        // Week of 16–22 Nov: 40 + 30 minutes in the subject; other subjects and other users do not count.
        Assert.Equal(new DateOnly(2026, 11, 16), goal.PeriodStart);
        Assert.Equal(70, goal.CurrentValue);
        Assert.Equal(58, goal.PercentComplete);

        var all = await planner.CreateGoalAsync(alice, new StudyGoalInput { Period = "Daily", Date = Today, Title = "Daily time", Metric = "StudyMinutes", TargetValue = 60 }, default);
        Assert.Equal(80, all.CurrentValue);
        Assert.Equal(100, all.PercentComplete);
        await Assert.ThrowsAsync<PlannerException>(() => planner.CreateGoalAsync(alice, new StudyGoalInput
        {
            Period = "Weekly", Date = Today, Title = "Viva", Metric = "VivaSessions", TargetValue = 2, SubjectId = subjectId,
        }, default));
    }

    [Fact]
    public async Task RevisionUsesTheUsersPolicyAndLogsHistory()
    {
        await planner.SaveSettingsAsync(alice, new PlannerSettingsInput { RevisionIntervalsDays = [2, 6, 15], GoodStepDelta = 1, EasyStepDelta = 2, RetireAfterStep = 3 }, default);

        var item = await revision.AddItemAsync(alice, new RevisionItemInput { SourceType = "McqQuestion", SourceId = mcqId }, default);
        Assert.True(item.IsDue);
        Assert.Equal(topicId, item.SyllabusItemId);
        Assert.Equal(item.Id, (await revision.AddItemAsync(alice, new RevisionItemInput { SourceType = "McqQuestion", SourceId = mcqId }, default)).Id);
        await Assert.ThrowsAsync<PlannerException>(() => revision.AddItemAsync(alice, new RevisionItemInput { SourceType = "McqQuestion", SourceId = draftMcqId }, default));

        var first = await revision.ReviewAsync(alice, item.Id, new RevisionReviewInput { Outcome = "Good" }, default);
        Assert.Equal((1, 2), (first!.Step, first.IntervalDays));
        Assert.Equal(Today.AddDays(2), first.DueOn);
        Assert.Empty((await revision.GetQueueAsync(alice, true, null, null, 1, 50, default)).Items);

        clock.Now = clock.Now.AddMinutes(1);
        var lapse = await revision.ReviewAsync(alice, item.Id, new RevisionReviewInput { Outcome = "Again" }, default);
        Assert.Equal(1, lapse!.LapseCount);
        clock.Now = clock.Now.AddMinutes(1);
        var retired = await revision.ReviewAsync(alice, item.Id, new RevisionReviewInput { Outcome = "Easy" }, default);
        Assert.Equal("Retired", retired!.Status);
        await Assert.ThrowsAsync<PlannerException>(() => revision.ReviewAsync(alice, item.Id, new RevisionReviewInput { Outcome = "Good" }, default));

        var history = await revision.GetHistoryAsync(alice, null, null, 1, 50, default);
        Assert.Equal(["Easy", "Again", "Good"], history.Items.Select(log => log.Outcome).ToArray());
        Assert.Empty((await revision.GetHistoryAsync(bob, null, null, 1, 50, default)).Items);
        Assert.Null(await revision.ReviewAsync(bob, item.Id, new RevisionReviewInput { Outcome = "Good" }, default));
    }

    [Fact]
    public async Task MistakeNotebookQueuesReviewsAndResolutionRetiresThem()
    {
        var mistake = await revision.CreateMistakeAsync(alice, new MistakeInput
        {
            SourceType = "Mcq", QuestionId = mcqId, QuestionText = "Which article…?", CorrectAnswerReference = "Article 7", Explanation = "Mixed up articles", Category = "Recall",
        }, default);
        Assert.Equal(topicId, mistake.SyllabusItemId);
        Assert.NotNull(mistake.RevisionItemId);

        var reviewed = await revision.ReviewAsync(alice, mistake.RevisionItemId!.Value, new RevisionReviewInput { Outcome = "Good" }, default);
        var afterReview = await revision.GetMistakeAsync(alice, mistake.Id, default);
        Assert.Equal(reviewed!.DueOn, afterReview!.ReviewOn);
        Assert.Single(afterReview.History);

        var resolved = await revision.SetMistakeStatusAsync(alice, mistake.Id, "Resolved", default);
        Assert.Equal("Resolved", resolved!.Status);
        Assert.Equal(RevisionItemStatus.Retired, (await dbContext.RevisionItems.SingleAsync()).Status);
        Assert.Null(await revision.GetMistakeAsync(bob, mistake.Id, default));
        await Assert.ThrowsAsync<PlannerException>(() => revision.CreateMistakeAsync(alice, new MistakeInput
        {
            SourceType = "Written", QuestionId = mcqId, QuestionText = "x", CorrectAnswerReference = "y", Category = "Concept",
        }, default));
    }

    [Fact]
    public async Task ImportsQueueBookmarksAndWeakTopicsUsingUserThresholds()
    {
        dbContext.McqBookmarks.Add(new McqBookmark { UserId = alice, QuestionId = mcqId, CreatedAt = clock.Now });
        dbContext.McqBookmarks.Add(new McqBookmark { UserId = alice, QuestionId = draftMcqId, CreatedAt = clock.Now });
        await dbContext.SaveChangesAsync();
        SeedAnswers(alice, correct: 2, incorrect: 4);

        var bookmarks = await revision.ImportAsync(alice, new RevisionImportInput { Source = "McqBookmarks" }, default);
        Assert.Equal(1, bookmarks.Added);
        Assert.Equal(0, (await revision.ImportAsync(alice, new RevisionImportInput { Source = "McqBookmarks" }, default)).Added);

        var weak = await revision.GetWeakTopicsAsync(alice, default);
        Assert.Equal(33, Assert.Single(weak).AccuracyPercent);

        await planner.SaveSettingsAsync(alice, new PlannerSettingsInput { RevisionIntervalsDays = [1, 3], WeakTopicMinimumAnswers = 10 }, default);
        Assert.Empty(await revision.GetWeakTopicsAsync(alice, default));
        await planner.SaveSettingsAsync(alice, new PlannerSettingsInput { RevisionIntervalsDays = [1, 3], WeakTopicMinimumAnswers = 5, WeakTopicAccuracyPercent = 30 }, default);
        Assert.Empty(await revision.GetWeakTopicsAsync(alice, default));
        await Assert.ThrowsAsync<PlannerException>(() => revision.ImportAsync(alice, new RevisionImportInput { Source = "Everything" }, default));
    }

    [Fact]
    public async Task ReportsAndDashboardReflectRealActivity()
    {
        await LogAsync(alice, Today, 30, subjectId);
        await LogAsync(alice, Today.AddDays(-1), 20, subjectId);
        await LogAsync(alice, Today.AddDays(-2), 5, subjectId);
        SeedAnswers(alice, correct: 3, incorrect: 1);
        await planner.CreateTaskAsync(alice, Task("Today's task"), default);

        var report = await progress.GetReportAsync(alice, "weekly", Today, default);
        Assert.Equal(new DateOnly(2026, 11, 16), report.From);
        Assert.Equal(55, report.StudyMinutes);
        Assert.Equal(3, report.SessionCount);
        Assert.Equal(7, report.MinutesByDay.Count);
        Assert.Equal(4, report.Practice.McqAnswered);
        Assert.Equal(75, report.Practice.McqAccuracyPercent);
        Assert.Equal(1, report.Tasks.Planned);
        await Assert.ThrowsAsync<PlannerException>(() => progress.GetReportAsync(alice, "fortnightly", Today, default));

        var dashboard = await progress.GetDashboardAsync(alice, default);
        Assert.Equal(30, dashboard.TodayMinutes);
        Assert.Equal(55, dashboard.WeekMinutes);
        Assert.Single(dashboard.TodayTasks);

        // Default minimum is 15 minutes, so the 5-minute day does not count but the MCQ attempt today does.
        Assert.Equal(2, dashboard.Streak.Current);
        Assert.NotEmpty(dashboard.RecentActivity);
        var stage = Assert.Single(dashboard.Stages);
        Assert.Equal(1, stage.TotalTopics);

        var empty = await progress.GetDashboardAsync(bob, default);
        Assert.Equal(0, empty.TodayMinutes);
        Assert.Equal(0, empty.Streak.Current);
        Assert.Empty(empty.RecentActivity);
    }

    [Fact]
    public async Task MasteryRatingsFeedTopicTracking()
    {
        var topic = await progress.SetMasteryAsync(alice, topicId, new MasteryInput { Mastery = 2 }, default);
        Assert.Equal(2, topic!.Mastery);
        Assert.False(topic.IsCompleted);
        Assert.Contains(await revision.GetWeakTopicsAsync(alice, default), weak => weak.SyllabusItemId == topicId);
        Assert.Single(await progress.GetTopicsAsync(alice, versionId, null, "pending", default));
        Assert.Empty(await progress.GetTopicsAsync(alice, versionId, null, "completed", default));
        Assert.Null((await progress.GetTopicsAsync(bob, versionId, null, "all", default)).Single().Mastery);
    }

    public void Dispose() => dbContext.Dispose();

    private static StudyTaskInput Task(string title) =>
        new() { Title = title, Priority = "High", ScheduledDate = new DateOnly(2026, 11, 21), PlannedMinutes = 30 };

    private async Task LogAsync(Guid userId, DateOnly day, int minutes, Guid subject) =>
        await planner.LogSessionAsync(userId, new StudySessionInput
        {
            StartedAt = new DateTimeOffset(day.ToDateTime(new TimeOnly(7, 0)), TimeSpan.FromHours(6)),
            DurationMinutes = minutes,
            Activity = "Reading",
            SubjectId = subject,
        }, default);

    private void SeedAnswers(Guid userId, int correct, int incorrect)
    {
        var attempt = new McqAttempt { Id = Guid.NewGuid(), UserId = userId, Status = McqAttemptStatus.Submitted, StartedAt = clock.Now.AddMinutes(-30), SubmittedAt = clock.Now.AddMinutes(-1), ExamSetNameSnapshot = "Practice" };
        dbContext.McqAttempts.Add(attempt);
        for (var index = 0; index < correct + incorrect; index++)
        {
            var question = new McqAttemptQuestion { Id = Guid.NewGuid(), Attempt = attempt, QuestionId = mcqId, SyllabusItemIdSnapshot = topicId, Position = index };
            dbContext.McqAttemptQuestions.Add(question);
            dbContext.McqAttemptAnswers.Add(new McqAttemptAnswer { AttemptQuestion = question, IsCorrect = index < correct });
        }

        dbContext.SaveChanges();
    }

    private void Seed()
    {
        var stage = new ExamStage { Id = Guid.NewGuid(), Code = "Preliminary", Name = "Preliminary", LocalizedName = "প্রিলিমিনারি", SortOrder = 1 };
        var subject = new Subject { Id = subjectId, Code = "BA", Name = "Bangladesh Affairs" };
        var other = new Subject { Id = otherSubjectId, Code = "IA", Name = "International Affairs" };
        var version = new SyllabusVersion { Id = versionId, ExamStage = stage, VersionLabel = "2026", Status = SyllabusVersionStatus.Published };
        var syllabusSubject = new SyllabusSubject { Id = Guid.NewGuid(), SyllabusVersion = version, Subject = subject };
        dbContext.AddRange(other, new SyllabusItem { Id = topicId, SyllabusSubject = syllabusSubject, Kind = SyllabusItemKind.Topic, Title = "Constitution" });
        dbContext.Add(new McqQuestion { Id = mcqId, SyllabusItemId = topicId, Prompt = "Which article declares the supremacy of the Constitution?", Status = McqContentStatus.Published });
        dbContext.Add(new McqQuestion { Id = draftMcqId, SyllabusItemId = topicId, Prompt = "Draft question", Status = McqContentStatus.Draft });
        dbContext.SaveChanges();
    }
}
