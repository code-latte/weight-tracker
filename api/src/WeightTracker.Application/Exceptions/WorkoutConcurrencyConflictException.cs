namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when a workout was changed by another request after it was loaded, so saving it would
/// overwrite that change. The caller reads the workout again and retries.
/// </summary>
public sealed class WorkoutConcurrencyConflictException : ApplicationLayerException
{
    public WorkoutConcurrencyConflictException(Exception innerException)
        : base("The workout was changed by another request after it was read", innerException)
    { }
}
