using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

/// <summary>
/// Raised when a workout would be restored with only one of its mesocycle id and mesocycle week number.
/// </summary>
public sealed class IncompleteWorkoutPlacementException : DomainException
{
    public IncompleteWorkoutPlacementException(Id workoutId)
        : base($"Workout '{workoutId}' must have both a mesocycle and a mesocycle week number, or neither")
    { }
}
