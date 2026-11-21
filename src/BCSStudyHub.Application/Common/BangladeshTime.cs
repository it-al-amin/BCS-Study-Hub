namespace BCSStudyHub.Application.Common;

/// <summary>Bangladesh Standard Time (UTC+6, no daylight saving) defines calendar days for planning and reports.</summary>
public static class BangladeshTime
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(6);

    public static DateOnly Today(TimeProvider timeProvider) => ToLocalDate(timeProvider.GetUtcNow());

    public static DateOnly ToLocalDate(DateTimeOffset instant) => DateOnly.FromDateTime(instant.ToOffset(Offset).DateTime);

    /// <summary>The instant local midnight starts on <paramref name="date"/>.</summary>
    public static DateTimeOffset StartOf(DateOnly date) => new(date.ToDateTime(TimeOnly.MinValue), Offset);

    /// <summary>Half-open UTC instant range [start of <paramref name="from"/>, start of the day after <paramref name="to"/>).</summary>
    public static (DateTimeOffset Start, DateTimeOffset End) Range(DateOnly from, DateOnly to) =>
        (StartOf(from), StartOf(to.AddDays(1)));
}
