using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Domain.Exceptions;

/// <summary>
/// Raised when the mesocycle placement of a started or finished workout would change.
/// Its planning context is kept for traceability.
/// </summary>
public sealed class WorkoutPlacementLockedException : DomainException
{
    public WorkoutPlacementLockedException(Id workoutId, WorkoutStatus status)
        : base($"Workout '{workoutId}' is {status} and its mesocycle placement can no longer change")
    { }
}
