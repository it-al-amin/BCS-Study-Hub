using System.ComponentModel.DataAnnotations;
using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.Planner;
using BCSStudyHub.Domain.Entities;

namespace BCSStudyHub.Api.Tests;

public sealed class PlannerRulesTests
{
    private static readonly RevisionPolicy Custom = new([2, 5, 11, 25], AgainStep: 1, HardStepDelta: 0, GoodStepDelta: 1, EasyStepDelta: 2, RetireAfterStep: null);

    [Theory]
    [InlineData(0, RevisionOutcome.Good, 1, 2)]
    [InlineData(0, RevisionOutcome.Hard, 1, 2)]
    [InlineData(0, RevisionOutcome.Easy, 2, 5)]
    [InlineData(2, RevisionOutcome.Good, 3, 11)]
    [InlineData(2, RevisionOutcome.Hard, 2, 5)]
    [InlineData(3, RevisionOutcome.Again, 1, 2)]
    [InlineData(4, RevisionOutcome.Easy, 4, 25)]
    public void PolicyMovesAlongTheUsersOwnLadder(int step, RevisionOutcome outcome, int expectedStep, int expectedInterval)
    {
        var (next, interval, retire) = Custom.Next(step, outcome);

        Assert.Equal(expectedStep, next);
        Assert.Equal(expectedInterval, interval);
        Assert.False(retire);
    }

    [Fact]
    public void ChangingThePolicyChangesTheSchedule()
    {
        var aggressive = Custom with { IntervalsDays = [1, 2, 4], GoodStepDelta = 2, EasyStepDelta = 3, HardStepDelta = -1, AgainStep = 1 };

        Assert.Equal((3, 4, false), aggressive.Next(1, RevisionOutcome.Good));
        Assert.Equal((1, 1, false), aggressive.Next(2, RevisionOutcome.Hard));
    }

    [Fact]
    public void ItemsRetireAtTheConfiguredStepButNeverOnAgain()
    {
        var retiring = Custom with { RetireAfterStep = 3 };

        Assert.True(retiring.Next(2, RevisionOutcome.Good).Retire);
        Assert.False(retiring.Next(1, RevisionOutcome.Good).Retire);
        Assert.False((retiring with { AgainStep = 3 }).Next(4, RevisionOutcome.Again).Retire);
    }

    [Fact]
    public void DefaultPolicyIsValid()
    {
        Assert.Null(RevisionPolicy.Default.Validate());
    }

    [Theory]
    [InlineData(new[] { 1, 3, 3 }, 1, 0, 1, 2)]
    [InlineData(new[] { 0, 3 }, 1, 0, 1, 2)]
    [InlineData(new int[0], 1, 0, 1, 2)]
    [InlineData(new[] { 1, 3 }, 3, 0, 1, 2)]
    [InlineData(new[] { 1, 3 }, 1, 2, 1, 2)]
    [InlineData(new[] { 1, 3 }, 1, 0, 0, 0)]
    public void InvalidPoliciesAreRejected(int[] intervals, int again, int hard, int good, int easy)
    {
        Assert.NotNull(new RevisionPolicy(intervals, again, hard, good, easy, null).Validate());
    }

    [Fact]
    public void SettingsInputValidatesThePolicy()
    {
        var input = new PlannerSettingsInput { RevisionIntervalsDays = [1, 3, 7] };
        Assert.True(IsValid(input));
        Assert.False(IsValid(input with { RevisionIntervalsDays = [7, 3] }));
        Assert.False(IsValid(input with { DailyRevisionLimit = 0 }));
        Assert.False(IsValid(input with { WeakTopicAccuracyPercent = 101 }));
    }

    [Fact]
    public void StreakCountsBackFromTodayOrYesterday()
    {
        var today = new DateOnly(2026, 11, 21);
        DateOnly Day(int offset) => today.AddDays(offset);

        Assert.Equal((3, 3), StreakCalculator.Compute([Day(0), Day(-1), Day(-2)], today));
        Assert.Equal((2, 2), StreakCalculator.Compute([Day(-1), Day(-2)], today));
        Assert.Equal((0, 4), StreakCalculator.Compute([Day(-2), Day(-3), Day(-4), Day(-5)], today));
        Assert.Equal((1, 3), StreakCalculator.Compute([Day(0), Day(-5), Day(-6), Day(-7), Day(3)], today));
        Assert.Equal((0, 0), StreakCalculator.Compute([], today));
    }

    [Fact]
    public void ScheduleDaysAndDurationsAreComputedExactly()
    {
        var input = new StudyScheduleInput { Title = "x", Days = ["Monday", "friday"], PlannedMinutes = 30, StartDate = new DateOnly(2026, 11, 1) };
        var mask = input.DaysMask();

        Assert.True(PlannerRules.RunsOn(mask, new DateOnly(2026, 11, 23)));
        Assert.True(PlannerRules.RunsOn(mask, new DateOnly(2026, 11, 27)));
        Assert.False(PlannerRules.RunsOn(mask, new DateOnly(2026, 11, 24)));
        Assert.Equal(26, PlannerRules.DurationMinutes(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddMinutes(25.2)));
        Assert.Equal(0, PlannerRules.DurationMinutes(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddMinutes(-3)));
        Assert.False(IsValid(input with { Days = ["Funday"] }));
        Assert.False(IsValid(input with { EndDate = new DateOnly(2026, 10, 1) }));
    }

    [Fact]
    public void BangladeshDayBoundariesAreUtcPlusSix()
    {
        var (start, end) = BangladeshTime.Range(new DateOnly(2026, 11, 21), new DateOnly(2026, 11, 21));

        Assert.Equal(new DateTimeOffset(2026, 11, 20, 18, 0, 0, TimeSpan.Zero), start.ToUniversalTime());
        Assert.Equal(TimeSpan.FromDays(1), end - start);
        Assert.Equal(new DateOnly(2026, 11, 22), BangladeshTime.ToLocalDate(new DateTimeOffset(2026, 11, 21, 18, 30, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void MistakesFromQuestionsMustLinkTheQuestion()
    {
        var valid = new MistakeInput { SourceType = "Other", QuestionText = "Q", CorrectAnswerReference = "A", Category = "Recall" };
        Assert.True(IsValid(valid));
        Assert.False(IsValid(valid with { SourceType = "Mcq" }));
        Assert.True(IsValid(valid with { SourceType = "Mcq", QuestionId = Guid.NewGuid() }));
        Assert.False(IsValid(valid with { Category = "Unlucky" }));
    }

    [Fact]
    public void CustomRevisionItemsNeedATitleAndOthersNeedASource()
    {
        Assert.False(IsValid(new RevisionItemInput { SourceType = "Custom" }));
        Assert.True(IsValid(new RevisionItemInput { SourceType = "Custom", Title = "Constitution articles 1-10" }));
        Assert.False(IsValid(new RevisionItemInput { SourceType = "McqQuestion" }));
    }

    private static bool IsValid(object instance)
    {
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
    }
}
