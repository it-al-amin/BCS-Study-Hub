using System.ComponentModel.DataAnnotations;
using BCSStudyHub.Application.CurrentAffairs;
using BCSStudyHub.Domain.Entities;

namespace BCSStudyHub.Api.Tests;

public sealed class CurrentAffairsRulesTests
{
    [Fact]
    public void WeeksAreIsoWeeksSpanningMonthAndYearBoundaries()
    {
        // 1 January 2026 is a Thursday, so its ISO week runs Monday 29 Dec 2025 – Sunday 4 Jan 2026.
        var (from, to) = CurrentAffairsRules.GetRange(DigestPeriod.Week, new DateOnly(2026, 1, 1));

        Assert.Equal(new DateOnly(2025, 12, 29), from);
        Assert.Equal(new DateOnly(2026, 1, 4), to);
        Assert.Equal(DayOfWeek.Monday, from.DayOfWeek);
        Assert.Equal(CurrentAffairsRules.GetRange(DigestPeriod.Week, new DateOnly(2026, 1, 4)), (from, to));
    }

    [Fact]
    public void MonthAndYearRangesAreInclusiveAndLeapAware()
    {
        Assert.Equal((new DateOnly(2028, 2, 1), new DateOnly(2028, 2, 29)), CurrentAffairsRules.GetRange(DigestPeriod.Month, new DateOnly(2028, 2, 14)));
        Assert.Equal((new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28)), CurrentAffairsRules.GetRange(DigestPeriod.Month, new DateOnly(2026, 2, 14)));
        Assert.Equal((new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), CurrentAffairsRules.GetRange(DigestPeriod.Year, new DateOnly(2026, 7, 4)));
        Assert.Equal((new DateOnly(2026, 7, 4), new DateOnly(2026, 7, 4)), CurrentAffairsRules.GetRange(DigestPeriod.Day, new DateOnly(2026, 7, 4)));
    }

    [Fact]
    public void ShiftMovesToTheAdjacentPeriod()
    {
        var anchor = new DateOnly(2026, 1, 31);
        Assert.Equal(new DateOnly(2026, 2, 28), CurrentAffairsRules.Shift(DigestPeriod.Month, anchor, 1));
        Assert.Equal(new DateOnly(2026, 1, 24), CurrentAffairsRules.Shift(DigestPeriod.Week, anchor, -1));
        Assert.Equal(new DateOnly(2025, 1, 31), CurrentAffairsRules.Shift(DigestPeriod.Year, anchor, -1));
    }

    [Fact]
    public void TodayUsesBangladeshStandardTime()
    {
        // 19:00 UTC on 21 Nov is 01:00 on 22 Nov in Dhaka (UTC+6).
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 11, 21, 19, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 11, 22), CurrentAffairsRules.Today(clock));
    }

    [Fact]
    public void DirectoryEntriesNeedACurrentVerificationToPublish()
    {
        var today = new DateOnly(2026, 11, 21);

        Assert.NotNull(CurrentAffairsRules.ValidateSourceCanPublish(OfficialSourceVerificationStatus.Unverified, null, today));
        Assert.NotNull(CurrentAffairsRules.ValidateSourceCanPublish(OfficialSourceVerificationStatus.Unreachable, today, today));
        Assert.NotNull(CurrentAffairsRules.ValidateSourceCanPublish(OfficialSourceVerificationStatus.Verified, null, today));
        Assert.NotNull(CurrentAffairsRules.ValidateSourceCanPublish(OfficialSourceVerificationStatus.Verified, today.AddDays(-366), today));
        Assert.Null(CurrentAffairsRules.ValidateSourceCanPublish(OfficialSourceVerificationStatus.Verified, today.AddDays(-365), today));
    }
}

public sealed class CurrentAffairsValidationTests
{
    [Fact]
    public void ValidEntryPasses()
    {
        Assert.True(IsValid(ValidEntry()));
    }

    [Theory]
    [InlineData("Category", "Gossip")]
    [InlineData("Category", "3")]
    [InlineData("SourceStatus", "Trusted")]
    [InlineData("SourceUrl", "javascript:alert(1)")]
    [InlineData("SourceUrl", "https://user:pass@example.org/news")]
    public void EntryRejectsUnsupportedValuesAndUnsafeUrls(string field, string value)
    {
        var input = field switch
        {
            "Category" => ValidEntry() with { Category = value },
            "SourceStatus" => ValidEntry() with { SourceStatus = value },
            _ => ValidEntry() with { SourceUrl = value },
        };

        Assert.False(IsValid(input));
    }

    [Fact]
    public void EntryRequiresAnEventDate()
    {
        Assert.False(IsValid(ValidEntry() with { EventDate = default }));
    }

    [Fact]
    public void EntryRejectsDuplicateOrUntypedQuestionsAndUnsafeReferences()
    {
        var id = Guid.NewGuid();
        Assert.False(IsValid(ValidEntry() with { Questions = [new() { Type = "Mcq", QuestionId = id }, new() { Type = "mcq", QuestionId = id }] }));
        Assert.False(IsValid(ValidEntry() with { Questions = [new() { Type = "Essay", QuestionId = id }] }));
        Assert.True(IsValid(ValidEntry() with { Questions = [new() { Type = "Mcq", QuestionId = id }, new() { Type = "Written", QuestionId = id }] }));
        Assert.False(IsValid(ValidEntry() with { References = [new() { Kind = "Pdf", Title = "Budget", Url = "file:///c:/budget.pdf" }] }));
        Assert.False(IsValid(ValidEntry() with { References = [new() { Kind = "Video", Title = "Budget", Url = "https://mof.gov.bd/budget.pdf" }] }));
    }

    [Fact]
    public void DirectoryInputValidatesCategoryAndUrl()
    {
        var valid = new OfficialSourceInput { Name = "Bangladesh Bank", Category = "CentralBank", Url = "https://www.bb.org.bd/" };
        Assert.True(IsValid(valid));
        Assert.False(IsValid(valid with { Category = "Blog" }));
        Assert.False(IsValid(valid with { Url = "ftp://bb.org.bd" }));
    }

    [Fact]
    public void VerificationMustRecordAnOutcomeAndNote()
    {
        Assert.False(IsValid(new OfficialSourceVerificationInput { Status = "Unverified", Note = "Checked" }));
        Assert.False(IsValid(new OfficialSourceVerificationInput { Status = "Verified", Note = "" }));
        Assert.True(IsValid(new OfficialSourceVerificationInput { Status = "Verified", Note = "Opened in a browser; page identifies BPSC." }));
    }

    internal static CurrentAffairsEntryInput ValidEntry() =>
        new()
        {
            Title = "National budget presented",
            Summary = "The budget for the coming fiscal year was placed in Parliament.",
            EventDate = new DateOnly(2026, 6, 5),
            Category = "Economy",
            SourceName = "Finance Division",
            SourceUrl = "https://mof.gov.bd/",
        };

    private static bool IsValid(object instance)
    {
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}
