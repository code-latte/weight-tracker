using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Workouts.Observations;

/// <summary>
/// The initial product thresholds of the observation rules and of the highlight eligibility. They are not universal
/// scientific indicators, so they are configured, never hardcoded in the rules.
/// </summary>
public sealed record ObservationThresholds
{
    /// <summary>
    /// Extra executions loaded beyond the longest streak, so sessions skipped as not comparable rarely starve a rule.
    /// </summary>
    public const int HistoryMargin = 4;

    /// <summary>Comparable executions in a row, the current one included, that missed a rep target to flag a repeated failure.</summary>
    public int RepeatedMissCount { get; }

    /// <summary>Comparable executions in a row, the current one included, without exceeding the maximum weight before them.</summary>
    public int PlateauCount { get; }

    /// <summary>Consecutive comparable executions, the current one included, where the maximum weight moved the same way.</summary>
    public int TrendCount { get; }

    /// <summary>Share of prescribed sets recorded below which a workout is severely non-compliant.</summary>
    public decimal SevereNonCompliancePercent { get; }

    /// <summary>Drop in a metric that, confirmed in two consecutive executions, is a significant decline.</summary>
    public decimal SignificantDeclinePercent { get; }

    /// <summary>Margin over the previous record that makes a weight record significant.</summary>
    public decimal SignificantRecordPercent { get; }

    /// <summary>Improvement over the previous execution that counts as exceptional progression.</summary>
    public decimal ExceptionalProgressionPercent { get; }

    /// <summary>
    /// How many earlier executions of each exercise are loaded.
    /// </summary>
    public int HistoryDepth => Math.Max(Math.Max(RepeatedMissCount, PlateauCount), TrendCount) + HistoryMargin;

    /// <exception cref="InvalidObservationThresholdsException">When a count is lower than 2 or a percent is not between 0 and 100, both excluded.</exception>
    public ObservationThresholds(
        int repeatedMissCount,
        int plateauCount,
        int trendCount,
        decimal severeNonCompliancePercent,
        decimal significantDeclinePercent,
        decimal significantRecordPercent,
        decimal exceptionalProgressionPercent)
    {
        EnsureCount(nameof(RepeatedMissCount), repeatedMissCount);
        EnsureCount(nameof(PlateauCount), plateauCount);
        EnsureCount(nameof(TrendCount), trendCount);
        EnsurePercent(nameof(SevereNonCompliancePercent), severeNonCompliancePercent);
        EnsurePercent(nameof(SignificantDeclinePercent), significantDeclinePercent);
        EnsurePercent(nameof(SignificantRecordPercent), significantRecordPercent);
        EnsurePercent(nameof(ExceptionalProgressionPercent), exceptionalProgressionPercent);

        RepeatedMissCount = repeatedMissCount;
        PlateauCount = plateauCount;
        TrendCount = trendCount;
        SevereNonCompliancePercent = severeNonCompliancePercent;
        SignificantDeclinePercent = significantDeclinePercent;
        SignificantRecordPercent = significantRecordPercent;
        ExceptionalProgressionPercent = exceptionalProgressionPercent;
    }

    private static void EnsureCount(string name, int value)
    {
        if (value < 2)
            throw new InvalidObservationThresholdsException($"{name} must be at least 2, got {value}");
    }

    private static void EnsurePercent(string name, decimal value)
    {
        if (value <= 0 || value >= 100)
            throw new InvalidObservationThresholdsException($"{name} must be between 0 and 100, got {value}");
    }
}
