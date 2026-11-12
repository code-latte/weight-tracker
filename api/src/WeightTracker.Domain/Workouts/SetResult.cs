namespace WeightTracker.Domain.Workouts;

/// <summary>
/// What was actually measured in one performed set: the reps done or the seconds held. The weight is kept apart,
/// because both kinds may carry a load or none. Shaped like <see cref="SetTarget"/>: one record per kind.
/// </summary>
public abstract record SetResult
{
    /// <summary>
    /// Reps performed. Only valid for an exercise prescribed by reps.
    /// </summary>
    public sealed record Reps(RepCount Count) : SetResult
    {
        public override string ToString() => $"{Count} reps.";
    }

    /// <summary>
    /// Seconds held or spent. Only valid for an exercise prescribed by time.
    /// </summary>
    public sealed record Duration(DurationInSeconds Value) : SetResult
    {
        public override string ToString() => $"{Value} s.";
    }
}
