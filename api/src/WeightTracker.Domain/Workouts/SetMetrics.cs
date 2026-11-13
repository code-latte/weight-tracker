namespace WeightTracker.Domain.Workouts;

/// <summary>
/// Numbers derived from one performed set. Only a set measured in reps and carrying a weight has them: a timed set
/// or a set without weight has none, which is not the same as 0 (a bodyweight set at 0 kg has a volume of 0).
/// </summary>
public static class SetMetrics
{
    private const decimal EpleyDivisor = 30m;

    /// <summary>
    /// Reps multiplied by the weight in kilograms, null when the set has no reps or no weight.
    /// </summary>
    public static decimal? Volume(SetResult result, Weight? weight)
        => result is SetResult.Reps reps && weight is not null
            ? reps.Count.Value * weight.Value
            : null;

    /// <summary>
    /// Estimated one-rep max by the Epley formula, <c>weight × (1 + reps / 30)</c>, rounded to two decimals away from
    /// zero and applied as given also to a single rep. Null when the set has no reps or no weight.
    /// </summary>
    public static decimal? EstimatedOneRepMax(SetResult result, Weight? weight)
        => result is SetResult.Reps reps && weight is not null
            ? decimal.Round(weight.Value * (EpleyDivisor + reps.Count.Value) / EpleyDivisor, 2, MidpointRounding.AwayFromZero)
            : null;
}
