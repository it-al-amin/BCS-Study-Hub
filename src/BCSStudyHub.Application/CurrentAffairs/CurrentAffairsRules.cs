using System.Globalization;
using BCSStudyHub.Domain.Entities;

namespace BCSStudyHub.Application.CurrentAffairs;

public enum DigestPeriod
{
    Day,
    Week,
    Month,
    Year,
}

/// <summary>Pure rules for current-affairs digests, revision scheduling, and directory verification.</summary>
public static class CurrentAffairsRules
{
    /// <summary>Bangladesh Standard Time (UTC+6, no daylight saving) defines "today" for digests and revision.</summary>
    public static readonly TimeSpan BangladeshOffset = TimeSpan.FromHours(6);

    public static DateOnly Today(TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetUtcNow().ToOffset(BangladeshOffset).DateTime);

    /// <summary>A directory entry whose last manual verification is older than this must be re-verified before publishing.</summary>
    public const int MaxVerificationAgeDays = 365;

    /// <summary>Returns the inclusive date range of the period containing <paramref name="anchor"/>. Weeks are ISO weeks (Monday to Sunday).</summary>
    public static (DateOnly From, DateOnly To) GetRange(DigestPeriod period, DateOnly anchor) =>
        period switch
        {
            DigestPeriod.Day => (anchor, anchor),
            DigestPeriod.Week => WeekRange(anchor),
            DigestPeriod.Month => (new DateOnly(anchor.Year, anchor.Month, 1),
                new DateOnly(anchor.Year, anchor.Month, DateTime.DaysInMonth(anchor.Year, anchor.Month))),
            DigestPeriod.Year => (new DateOnly(anchor.Year, 1, 1), new DateOnly(anchor.Year, 12, 31)),
            _ => throw new ArgumentOutOfRangeException(nameof(period)),
        };

    private static (DateOnly From, DateOnly To) WeekRange(DateOnly anchor)
    {
        var offset = ((int)anchor.DayOfWeek + 6) % 7;
        var monday = anchor.AddDays(-offset);
        return (monday, monday.AddDays(6));
    }

    /// <summary>Returns an anchor date inside the period before (-1) or after (+1) the given one.</summary>
    public static DateOnly Shift(DigestPeriod period, DateOnly anchor, int direction) =>
        period switch
        {
            DigestPeriod.Day => anchor.AddDays(direction),
            DigestPeriod.Week => anchor.AddDays(7 * direction),
            DigestPeriod.Month => anchor.AddMonths(direction),
            DigestPeriod.Year => anchor.AddYears(direction),
            _ => throw new ArgumentOutOfRangeException(nameof(period)),
        };

    public static string Label(DigestPeriod period, DateOnly anchor)
    {
        var (from, to) = GetRange(period, anchor);
        return period switch
        {
            DigestPeriod.Day => from.ToString("d MMMM yyyy", CultureInfo.InvariantCulture),
            DigestPeriod.Week => $"Week {ISOWeek.GetWeekOfYear(from.ToDateTime(TimeOnly.MinValue))}, {ISOWeek.GetYear(from.ToDateTime(TimeOnly.MinValue))} ({from:d MMM} – {to:d MMM yyyy})",
            DigestPeriod.Month => from.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
            _ => from.Year.ToString(CultureInfo.InvariantCulture),
        };
    }

    /// <summary>Whether a directory entry may be published given its manual verification record.</summary>
    public static string? ValidateSourceCanPublish(
        OfficialSourceVerificationStatus status,
        DateOnly? lastVerifiedOn,
        DateOnly today)
    {
        if (status != OfficialSourceVerificationStatus.Verified || lastVerifiedOn is null)
        {
            return "Verify the official website URL in a browser and record the verification before publishing.";
        }

        if (lastVerifiedOn.Value.AddDays(MaxVerificationAgeDays) < today)
        {
            return $"The last verification is older than {MaxVerificationAgeDays} days. Re-verify the URL before publishing.";
        }

        return null;
    }
}
