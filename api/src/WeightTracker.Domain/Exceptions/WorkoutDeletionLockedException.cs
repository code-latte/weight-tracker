using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Domain.Exceptions;

/// <summary>
/// Raised when a workout that is no longer a draft would be deleted. Applies to every caller, an admin included.
/// </summary>
public sealed class WorkoutDeletionLockedException : DomainException
{
    public WorkoutDeletionLockedException(Id workoutId, WorkoutStatus status)
        : base($"Workout '{workoutId}' is {status} and can no longer be deleted")
    { }
}
