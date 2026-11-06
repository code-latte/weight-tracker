namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when a workout is not placed in a mesocycle week and no date is sent for it.
/// </summary>
public sealed class MissingWorkoutDateException : ApplicationLayerException
{
    public MissingWorkoutDateException()
        : base("The workout date is required when the workout is not placed in a mesocycle week")
    { }
}
