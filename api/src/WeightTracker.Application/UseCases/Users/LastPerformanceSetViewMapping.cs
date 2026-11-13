using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Users;

/// <summary>
/// Converts between the raw values of a recorded set, as the read projections return them, and the domain values the
/// stats rules work on, so every use case applies those rules to the same domain types.
/// </summary>
public static class LastPerformanceSetViewMapping
{
    /// <summary>
    /// The result of the set: its reps when it has them, otherwise its seconds.
    /// </summary>
    /// <exception cref="InvalidOperationException">When the set has neither reps nor seconds, which the stored data forbids.</exception>
    public static SetResult ToSetResult(this LastPerformanceSetView set)
    {
        if (set.Reps is not null)
            return new SetResult.Reps(RepCount.From(set.Reps.Value));

        if (set.DurationSeconds is not null)
            return new SetResult.Duration(DurationInSeconds.From(set.DurationSeconds.Value));

        throw new InvalidOperationException("A stored set record holds neither reps nor seconds");
    }

    /// <summary>
    /// The weight of the set, null when none was tracked.
    /// </summary>
    public static Weight? ToWeight(this LastPerformanceSetView set)
        => set.Weight is null ? null : Weight.From(set.Weight.Value);

    /// <summary>
    /// The raw values of a domain result and weight.
    /// </summary>
    public static LastPerformanceSetView ToView(SetResult result, Weight? weight)
        => new(
            (result as SetResult.Reps)?.Count.Value,
            (result as SetResult.Duration)?.Value.Value,
            weight?.Value);
}
