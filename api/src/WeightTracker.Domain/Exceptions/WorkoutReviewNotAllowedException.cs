using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Domain.Exceptions;

/// <summary>
/// Raised when a workout is reviewed while it is not completed, or when it has no prescribing trainer to review it.
/// </summary>
public sealed class WorkoutReviewNotAllowedException : DomainException
{
    public WorkoutReviewNotAllowedException(Id workoutId, WorkoutStatus status, string reason)
        : base($"Workout '{workoutId}' is {status} and cannot be reviewed: {reason}")
    { }
}
