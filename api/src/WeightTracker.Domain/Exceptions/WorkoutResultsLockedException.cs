using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Domain.Exceptions;

public sealed class WorkoutResultsLockedException : DomainException
{
    public WorkoutResultsLockedException(Id workoutId, WorkoutStatus status)
        : base($"Workout '{workoutId}' is {status}, sets can only be recorded while it is {WorkoutStatus.STARTED} or {WorkoutStatus.COMPLETED}")
    { }
}
