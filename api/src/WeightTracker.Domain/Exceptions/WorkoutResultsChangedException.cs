using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

/// <summary>
/// Raised when a review is based on results that changed after the reviewer read them.
/// </summary>
public sealed class WorkoutResultsChangedException : DomainException
{
    public WorkoutResultsChangedException(Id workoutId)
        : base($"The results of workout '{workoutId}' changed after they were read, read them again before reviewing")
    { }
}
