namespace WeightTracker.Domain.Workouts;

/// <summary>
/// Names of the <see cref="SetTarget"/> kinds, shared by persistence, requests and responses.
/// </summary>
public static class SetTargetTypes
{
    public const string Reps = "Reps";
    public const string Duration = "Duration";
    public const string MaxReps = "MaxReps";
    public const string RepsRange = "RepsRange";
    public const string Minutes = "Minutes";
}
