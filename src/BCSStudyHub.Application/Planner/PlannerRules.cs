using BCSStudyHub.Domain.Entities;

namespace BCSStudyHub.Application.Planner;

/// <summary>
/// A user-defined revision policy. The scheduler has no built-in intervals: every interval and every outcome's
/// effect comes from this policy. <see cref="Default"/> is only the starting point offered to a new user.
/// </summary>
public sealed record RevisionPolicy(
    IReadOnlyList<int> IntervalsDays,
    int AgainStep,
    int HardStepDelta,
    int GoodStepDelta,
    int EasyStepDelta,
    int? RetireAfterStep)
{
    public const int MaxSteps = 20;
    public const int MaxIntervalDays = 3650;

    public static RevisionPolicy Default { get; } = new([1, 3, 7, 14, 30, 60, 120], 1, 0, 1, 2, null);

    public string? Validate()
    {
        if (IntervalsDays is null || IntervalsDays.Count is < 1 or > MaxSteps)
        {
            return $"Provide between 1 and {MaxSteps} revision intervals.";
        }

        for (var index = 0; index < IntervalsDays.Count; index++)
        {
            if (IntervalsDays[index] is < 1 or > MaxIntervalDays)
            {
                return $"Each interval must be between 1 and {MaxIntervalDays} days.";
            }

            if (index > 0 && IntervalsDays[index] <= IntervalsDays[index - 1])
            {
                return "Intervals must be strictly increasing.";
            }
        }

        var steps = IntervalsDays.Count;
        if (AgainStep < 1 || AgainStep > steps)
        {
            return "The 'Again' step must point to one of the intervals.";
        }

        if (Math.Abs(HardStepDelta) > steps || Math.Abs(GoodStepDelta) > steps || Math.Abs(EasyStepDelta) > steps)
        {
            return "Step changes cannot exceed the number of intervals.";
        }

        if (!(HardStepDelta <= GoodStepDelta && GoodStepDelta <= EasyStepDelta))
        {
            return "Step changes must not decrease from Hard to Good to Easy.";
        }

        if (EasyStepDelta < 1)
        {
            return "At least the 'Easy' outcome must move an item forward, otherwise nothing can progress.";
        }

        if (RetireAfterStep is int retire && (retire < 1 || retire > steps))
        {
            return "The retirement step must point to one of the intervals.";
        }

        return null;
    }

    /// <summary>Computes the next ladder position. Step 0 means the item has never been reviewed.</summary>
    public (int Step, int IntervalDays, bool Retire) Next(int currentStep, RevisionOutcome outcome)
    {
        var target = outcome switch
        {
            RevisionOutcome.Again => AgainStep,
            RevisionOutcome.Hard => currentStep + HardStepDelta,
            RevisionOutcome.Good => currentStep + GoodStepDelta,
            RevisionOutcome.Easy => currentStep + EasyStepDelta,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
        };
        var step = Math.Clamp(target, 1, IntervalsDays.Count);
        var retire = outcome != RevisionOutcome.Again && RetireAfterStep is int threshold && step >= threshold;
        return (step, IntervalsDays[step - 1], retire);
    }
}

public static class StreakCalculator
{
    /// <summary>
    /// Current streak counts back from today, or from yesterday when today has no activity yet,
    /// so a streak is not broken before the day is over.
    /// </summary>
    public static (int Current, int Longest) Compute(IEnumerable<DateOnly> activeDays, DateOnly today)
    {
        var days = activeDays.Where(day => day <= today).ToHashSet();
        if (days.Count == 0)
        {
            return (0, 0);
        }

        var cursor = days.Contains(today) ? today : today.AddDays(-1);
        var current = 0;
        while (days.Contains(cursor))
        {
            current++;
            cursor = cursor.AddDays(-1);
        }

        var longest = 0;
        var run = 0;
        DateOnly? previous = null;
        foreach (var day in days.Order())
        {
            run = previous is DateOnly last && last.AddDays(1) == day ? run + 1 : 1;
            longest = Math.Max(longest, run);
            previous = day;
        }

        return (current, longest);
    }
}

public static class PlannerRules
{
    public const int MaxCalendarDays = 62;
    public const int MaxGenerateDays = 92;

    public static PlannerPeriod ParsePeriodAlias(string value) => value.ToLowerInvariant() switch
    {
        "day" or "daily" => PlannerPeriod.Daily,
        "week" or "weekly" => PlannerPeriod.Weekly,
        "month" or "monthly" => PlannerPeriod.Monthly,
        _ => throw new ArgumentException("The period must be daily, weekly, or monthly.", nameof(value)),
    };

    public static CurrentAffairs.DigestPeriod ToDigestPeriod(PlannerPeriod period) => period switch
    {
        PlannerPeriod.Daily => CurrentAffairs.DigestPeriod.Day,
        PlannerPeriod.Weekly => CurrentAffairs.DigestPeriod.Week,
        _ => CurrentAffairs.DigestPeriod.Month,
    };

    /// <summary>Returns true when a schedule with the given day flags runs on <paramref name="date"/>.</summary>
    public static bool RunsOn(int daysOfWeekMask, DateOnly date) => (daysOfWeekMask & (1 << (int)date.DayOfWeek)) != 0;

    /// <summary>Session length in whole minutes, rounding a partial minute up.</summary>
    public static int DurationMinutes(DateTimeOffset start, DateTimeOffset end) =>
        Math.Max(0, (int)Math.Ceiling((end - start).TotalMinutes));

    public static int? AccuracyPercent(int correct, int answered) =>
        answered == 0 ? null : (int)Math.Round(correct * 100d / answered);
}
