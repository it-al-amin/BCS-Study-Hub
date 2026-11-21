using BCSStudyHub.Application.CurrentAffairs;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.CurrentAffairs;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BCSStudyHub.Api.Tests;

public sealed class CurrentAffairsServiceTests : IDisposable
{
    private readonly ApplicationDbContext dbContext;
    private readonly FixedTimeProvider clock = new(new DateTimeOffset(2026, 11, 21, 6, 0, 0, TimeSpan.Zero));
    private readonly CurrentAffairsService service;
    private readonly OfficialSourceService sources;
    private readonly Guid editor = Guid.NewGuid();
    private readonly Guid alice = Guid.NewGuid();
    private readonly Guid bob = Guid.NewGuid();
    private readonly Guid topicId = Guid.NewGuid();
    private readonly Guid subjectId = Guid.NewGuid();
    private readonly Guid publishedMcqId = Guid.NewGuid();
    private readonly Guid draftVivaId = Guid.NewGuid();
    private readonly Guid writtenId = Guid.NewGuid();
    private readonly Guid sharedResourceId = Guid.NewGuid();
    private readonly Guid privateResourceId = Guid.NewGuid();

    public CurrentAffairsServiceTests()
    {
        dbContext = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        Seed();
        service = new CurrentAffairsService(dbContext, new HttpContextAccessor(), clock, new BCSStudyHub.Infrastructure.Planner.PlannerSettingsStore(dbContext));
        sources = new OfficialSourceService(dbContext, new HttpContextAccessor(), clock);
    }

    [Fact]
    public async Task DraftsAreHiddenAndPublishingRequiresAVerifiedSource()
    {
        var draft = await service.CreateEntryAsync(CurrentAffairsValidationTests.ValidEntry(), editor, default);
        var id = draft.Entry.Id;
        Assert.Equal("Draft", draft.Entry.Status);
        Assert.Null(await service.GetEntryAsync(id, null, default));
        Assert.Empty((await service.GetEntriesAsync(Query(), null, default)).Items);

        await Assert.ThrowsAsync<CurrentAffairsException>(() => service.SetStatusAsync(id, "Published", editor, default));

        await service.UpdateEntryAsync(id, CurrentAffairsValidationTests.ValidEntry() with { SourceStatus = "Verified" }, editor, default);
        await service.SetStatusAsync(id, "Published", editor, default);
        Assert.NotNull(await service.GetEntryAsync(id, null, default));

        await Assert.ThrowsAsync<CurrentAffairsException>(() =>
            service.UpdateEntryAsync(id, CurrentAffairsValidationTests.ValidEntry() with { SourceStatus = "Unverified" }, editor, default));
        await Assert.ThrowsAsync<CurrentAffairsException>(() => service.DeleteEntryAsync(id, editor, default));

        await service.SetStatusAsync(id, "Archived", editor, default);
        Assert.Null(await service.GetEntryAsync(id, null, default));
        Assert.True(await service.DeleteEntryAsync(id, editor, default));
    }

    [Fact]
    public async Task OnlySharedPublishedLibraryResourcesCanBeAttached()
    {
        await Assert.ThrowsAsync<CurrentAffairsException>(() => service.CreateEntryAsync(
            CurrentAffairsValidationTests.ValidEntry() with { LibraryResourceIds = [privateResourceId] }, editor, default));

        var entry = await service.CreateEntryAsync(
            CurrentAffairsValidationTests.ValidEntry() with { LibraryResourceIds = [sharedResourceId] }, editor, default);
        Assert.Single(entry.LibraryResources);
    }

    [Fact]
    public async Task RelatedQuestionsMustExistAndLearnersOnlySeePublishedOnes()
    {
        await Assert.ThrowsAsync<CurrentAffairsException>(() => service.CreateEntryAsync(
            CurrentAffairsValidationTests.ValidEntry() with { Questions = [new() { Type = "Mcq", QuestionId = Guid.NewGuid() }] }, editor, default));

        // The ID exists, but as a Viva question, not an MCQ.
        await Assert.ThrowsAsync<CurrentAffairsException>(() => service.CreateEntryAsync(
            CurrentAffairsValidationTests.ValidEntry() with { Questions = [new() { Type = "Mcq", QuestionId = draftVivaId }] }, editor, default));

        var id = await PublishAsync(CurrentAffairsValidationTests.ValidEntry() with
        {
            Questions =
            [
                new() { Type = "Mcq", QuestionId = publishedMcqId },
                new() { Type = "Viva", QuestionId = draftVivaId },
                new() { Type = "Written", QuestionId = writtenId },
            ],
        });

        var admin = await service.GetAdminEntryAsync(id, default);
        var learner = await service.GetEntryAsync(id, alice, default);
        Assert.Equal(3, admin!.Questions.Count);
        Assert.Equal(["Mcq", "Written"], learner!.Questions.Select(question => question.Type).Order().ToArray());
    }

    [Fact]
    public async Task TopicMappingsMustBePublishedAndDriveFiltering()
    {
        await Assert.ThrowsAsync<CurrentAffairsException>(() => service.CreateEntryAsync(
            CurrentAffairsValidationTests.ValidEntry() with { SyllabusItemIds = [Guid.NewGuid()] }, editor, default));

        await PublishAsync(CurrentAffairsValidationTests.ValidEntry() with { SyllabusItemIds = [topicId], SubjectIds = [subjectId] });
        await PublishAsync(CurrentAffairsValidationTests.ValidEntry() with { Title = "Unmapped" });

        Assert.Single((await service.GetEntriesAsync(Query() with { SyllabusItemId = topicId }, null, default)).Items);
        Assert.Single((await service.GetEntriesAsync(Query() with { SubjectId = subjectId }, null, default)).Items);
        Assert.Equal(2, (await service.GetEntriesAsync(Query(), null, default)).TotalCount);
    }

    [Fact]
    public async Task DigestsAndArchivesGroupPublishedEntriesByPeriodAndCategory()
    {
        await PublishAsync(CurrentAffairsValidationTests.ValidEntry() with { Title = "Mon economy", EventDate = new DateOnly(2026, 11, 16), Category = "Economy" });
        await PublishAsync(CurrentAffairsValidationTests.ValidEntry() with { Title = "Sun sports", EventDate = new DateOnly(2026, 11, 22), Category = "Sports" });
        await PublishAsync(CurrentAffairsValidationTests.ValidEntry() with { Title = "Next week", EventDate = new DateOnly(2026, 11, 23), Category = "Economy" });
        await PublishAsync(CurrentAffairsValidationTests.ValidEntry() with { Title = "October", EventDate = new DateOnly(2026, 10, 5), Category = "Awards" });
        await service.CreateEntryAsync(CurrentAffairsValidationTests.ValidEntry() with { Title = "Draft", EventDate = new DateOnly(2026, 11, 18) }, editor, default);

        var week = await service.GetDigestAsync("week", new DateOnly(2026, 11, 19), null, null, default);
        Assert.Equal(new DateOnly(2026, 11, 16), week.From);
        Assert.Equal(new DateOnly(2026, 11, 22), week.To);
        Assert.Equal(2, week.Total);
        Assert.Equal(["Economy", "Sports"], week.Groups.Select(group => group.Category).ToArray());
        Assert.Equal(new DateOnly(2026, 11, 9), week.PreviousAnchor);

        var today = await service.GetDigestAsync("day", null, null, null, default);
        Assert.Equal(new DateOnly(2026, 11, 21), today.From);
        Assert.Equal(0, today.Total);

        var month = await service.GetDigestAsync("Month", new DateOnly(2026, 11, 1), "Economy", null, default);
        Assert.Equal(2, month.Total);

        var archive = await service.GetArchiveAsync(default);
        Assert.Equal([(2026, 11, 3), (2026, 10, 1)], archive.Select(item => (item.Year, item.Month, item.Count)).ToArray());

        var ranged = await service.GetEntriesAsync(Query() with { From = new DateOnly(2026, 11, 20), To = new DateOnly(2026, 11, 30) }, null, default);
        Assert.Equal(2, ranged.TotalCount);
        await Assert.ThrowsAsync<CurrentAffairsException>(() =>
            service.GetEntriesAsync(Query() with { From = new DateOnly(2026, 11, 30), To = new DateOnly(2026, 11, 1) }, null, default));
    }

    [Fact]
    public async Task BookmarksNotesAndRevisionAreOwnerScopedAndFollowPublication()
    {
        var id = await PublishAsync(CurrentAffairsValidationTests.ValidEntry());

        Assert.True(await service.AddBookmarkAsync(alice, id, default));
        Assert.True(await service.AddBookmarkAsync(alice, id, default));
        await service.SaveNoteAsync(alice, id, new CurrentAffairsNoteInput { Content = "Budget size and deficit" }, default);
        await service.SaveNoteAsync(alice, id, new CurrentAffairsNoteInput { Content = "Updated note" }, default);
        await service.AddRevisionAsync(alice, id, default);

        var aliceView = await service.GetEntryAsync(id, alice, default);
        var bobView = await service.GetEntryAsync(id, bob, default);
        Assert.True(aliceView!.Entry.IsBookmarked);
        Assert.Equal("Updated note", aliceView.MyNote!.Content);
        Assert.NotNull(aliceView.MyRevision);
        Assert.False(bobView!.Entry.IsBookmarked);
        Assert.Null(bobView.MyNote);
        Assert.Single((await service.GetBookmarksAsync(alice, 1, 20, default)).Items);
        Assert.Empty((await service.GetBookmarksAsync(bob, 1, 20, default)).Items);
        Assert.Single((await service.GetEntriesAsync(Query() with { BookmarkedOnly = true }, alice, default)).Items);
        await Assert.ThrowsAsync<CurrentAffairsException>(() => service.GetEntriesAsync(Query() with { BookmarkedOnly = true }, null, default));

        await service.SetStatusAsync(id, "Archived", editor, default);
        Assert.Empty((await service.GetBookmarksAsync(alice, 1, 20, default)).Items);
        Assert.Empty((await service.GetNotesAsync(alice, 1, 20, default)).Items);
        Assert.False(await service.AddBookmarkAsync(bob, id, default));
    }

    [Fact]
    public async Task RevisionReviewsAreScheduledFromTheCurrentDate()
    {
        var id = await PublishAsync(CurrentAffairsValidationTests.ValidEntry());
        var added = await service.AddRevisionAsync(alice, id, default);
        Assert.True(added!.IsDue);
        Assert.Single((await service.GetRevisionsAsync(alice, dueOnly: true, 1, 20, default)).Items);

        // Intervals come from the user's own revision settings, not from fixed values in code.
        dbContext.PlannerSettings.Add(new PlannerSettings
        {
            UserId = alice,
            RevisionIntervalsJson = "[2,5,11]",
            AgainStep = 1,
            HardStepDelta = 0,
            GoodStepDelta = 1,
            EasyStepDelta = 2,
            DailyRevisionLimit = 50,
            StreakMinimumMinutes = 15,
            WeakTopicAccuracyPercent = 60,
            WeakTopicMinimumAnswers = 5,
        });
        await dbContext.SaveChangesAsync();

        var reviewed = await service.ReviewAsync(alice, id, new CurrentAffairsReviewInput { Outcome = "Good" }, default);
        Assert.Equal(2, reviewed!.IntervalDays);
        Assert.Equal(new DateOnly(2026, 11, 23), reviewed.NextReviewOn);
        Assert.False(reviewed.IsDue);
        Assert.Empty((await service.GetRevisionsAsync(alice, dueOnly: true, 1, 20, default)).Items);

        clock.Now = clock.Now.AddDays(2);
        Assert.Single((await service.GetRevisionsAsync(alice, dueOnly: true, 1, 20, default)).Items);
        var again = await service.ReviewAsync(alice, id, new CurrentAffairsReviewInput { Outcome = "Good" }, default);
        Assert.Equal(5, again!.IntervalDays);
        Assert.Equal(2, again.ReviewCount);
        Assert.Equal(2, await dbContext.RevisionLogs.CountAsync(log => log.UserId == alice && log.SourceType == RevisionSourceType.CurrentAffairs));

        Assert.Null(await service.ReviewAsync(bob, id, new CurrentAffairsReviewInput { Outcome = "Easy" }, default));
        await Assert.ThrowsAsync<CurrentAffairsException>(() =>
            service.ReviewAsync(alice, id, new CurrentAffairsReviewInput { Outcome = "Perfect" }, default));
    }

    [Fact]
    public async Task DirectorySourcesRequireCurrentManualVerificationToPublish()
    {
        var created = await sources.CreateAsync(Source("https://www.bb.org.bd/"), editor, default);
        Assert.Equal("Draft", created.Status);
        Assert.Equal("Unverified", created.VerificationStatus);
        await Assert.ThrowsAsync<CurrentAffairsException>(() => sources.SetStatusAsync(created.Id, "Published", editor, default));

        await sources.RecordVerificationAsync(created.Id, new OfficialSourceVerificationInput { Status = "Verified", Note = "Opened in browser." }, editor, default);
        var published = await sources.SetStatusAsync(created.Id, "Published", editor, default);
        Assert.Equal("Published", published!.Status);
        Assert.Equal(new DateOnly(2026, 11, 21), published.LastVerifiedOn);
        Assert.Single(await sources.GetPublishedAsync(null, null, default));

        // Changing the URL voids the verification and withdraws the entry from learners.
        var moved = await sources.UpdateAsync(created.Id, Source("https://www.bb.org.bd/en/"), editor, default);
        Assert.Equal("Draft", moved!.Status);
        Assert.Equal("NeedsReview", moved.VerificationStatus);
        Assert.Null(moved.LastVerifiedOn);
        Assert.Empty(await sources.GetPublishedAsync(null, null, default));
    }

    [Fact]
    public async Task FailedOrStaleVerificationWithdrawsOrBlocksPublication()
    {
        var created = await sources.CreateAsync(Source("https://bpsc.gov.bd/"), editor, default);
        await sources.RecordVerificationAsync(created.Id, new OfficialSourceVerificationInput { Status = "Verified", Note = "Opened OK" }, editor, default);
        await sources.SetStatusAsync(created.Id, "Published", editor, default);

        var failed = await sources.RecordVerificationAsync(created.Id, new OfficialSourceVerificationInput { Status = "Unreachable", Note = "TLS error" }, editor, default);
        Assert.Equal("Draft", failed!.Status);

        await sources.RecordVerificationAsync(created.Id, new OfficialSourceVerificationInput { Status = "Verified", Note = "Opened OK again" }, editor, default);
        clock.Now = clock.Now.AddDays(400);
        await Assert.ThrowsAsync<CurrentAffairsException>(() => sources.SetStatusAsync(created.Id, "Published", editor, default));

        var duplicate = await Assert.ThrowsAsync<CurrentAffairsException>(() => sources.CreateAsync(Source("https://bpsc.gov.bd/"), editor, default));
        Assert.Equal(StatusCodes.Status409Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task SeederPublishesOnlyVerifiedSourcesAndIsIdempotent()
    {
        var seeder = new OfficialSourceSeeder(dbContext, NullLogger<OfficialSourceSeeder>.Instance);
        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var all = await dbContext.OfficialSources.AsNoTracking().ToListAsync();
        Assert.Equal(all.Count, all.Select(source => source.Url).Distinct().Count());
        Assert.All(all.Where(source => source.Status == OfficialSourceStatus.Published), source =>
        {
            Assert.Equal(OfficialSourceVerificationStatus.Verified, source.VerificationStatus);
            Assert.Equal(OfficialSourceSeeder.CheckedOn, source.LastVerifiedOn);
        });
        Assert.Contains(all, source => source.Status == OfficialSourceStatus.Draft && source.VerificationStatus == OfficialSourceVerificationStatus.Unverified);
        Assert.Contains(all, source => source.Url == "https://bpsc.gov.bd/" && source.Status == OfficialSourceStatus.Published);
    }

    public void Dispose() => dbContext.Dispose();

    private async Task<Guid> PublishAsync(CurrentAffairsEntryInput input)
    {
        var entry = await service.CreateEntryAsync(input with { SourceStatus = "Verified" }, editor, default);
        await service.SetStatusAsync(entry.Entry.Id, "Published", editor, default);
        return entry.Entry.Id;
    }

    private static OfficialSourceInput Source(string url) =>
        new() { Name = "Test source", Category = "CentralBank", Url = url };

    private static CurrentAffairsQuery Query() =>
        new(null, null, null, null, null, null, null, false, 1, 20, "recent");

    private void Seed()
    {
        var stage = new ExamStage { Id = Guid.NewGuid(), Code = "Written", Name = "Written", LocalizedName = "লিখিত" };
        var subject = new Subject { Id = subjectId, Code = "BA", Name = "Bangladesh Affairs" };
        var version = new SyllabusVersion { Id = Guid.NewGuid(), ExamStage = stage, VersionLabel = "2026", Status = SyllabusVersionStatus.Published };
        var syllabusSubject = new SyllabusSubject { Id = Guid.NewGuid(), SyllabusVersion = version, Subject = subject };
        var topic = new SyllabusItem { Id = topicId, SyllabusSubject = syllabusSubject, Kind = SyllabusItemKind.Topic, Title = "Economy of Bangladesh" };
        dbContext.Add(topic);
        dbContext.Add(new McqQuestion { Id = publishedMcqId, SyllabusItemId = topicId, Prompt = "Which body presents the budget?", Status = McqContentStatus.Published });
        dbContext.Add(new VivaQuestion { Id = draftVivaId, CategoryId = Guid.NewGuid(), Prompt = "Explain the budget process.", Status = VivaQuestionStatus.Draft });
        dbContext.Add(new WrittenQuestion { Id = writtenId, SyllabusItem = topic, Prompt = "Discuss fiscal policy.", Status = WrittenQuestionStatus.Published });
        dbContext.Add(new LibraryResource { Id = sharedResourceId, Kind = LibraryResourceKind.File, Title = "Budget speech", Visibility = LibraryVisibility.Shared, Status = LibraryResourceStatus.Published });
        dbContext.Add(new LibraryResource { Id = privateResourceId, Kind = LibraryResourceKind.File, Title = "My scan", Visibility = LibraryVisibility.Private, Status = LibraryResourceStatus.Published, OwnerId = alice });
        dbContext.SaveChanges();
    }
}
