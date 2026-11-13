namespace WeightTracker.Domain.Workouts.Observations;

/// <summary>
/// What the sets of an exercise measure. Executions of different kinds are never compared.
/// </summary>
public enum ResultKind
{
    REPS = 1,
    DURATION = 2,
}
