using WeightTracker.Application.Exceptions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.Services;

/// <summary>
/// Decides who may change the status of a workout. It is separate from <see cref="WorkoutEditAuthorizer"/>
/// because a status change is not an edit of the content: the athlete of a prescribed workout cannot
/// edit it but is the one who starts and finishes it.
/// </summary>
/// <remarks>
/// An admin may always change it. The prescribing trainer may publish and unpublish, and review a completed
/// workout. The athlete of a prescribed workout may only move it forward once it is published, and never
/// reviews it. The athlete of a workout without trainer may make every transition. Anybody else is refused.
/// A move outside the lifecycle is not judged here: the aggregate refuses it for every caller
/// that gets past this check.
/// </remarks>
public class WorkoutStatusAuthorizer
{
    /// <param name="operation">Operation being attempted, used in the error message.</param>
    /// <exception cref="UnauthorizedOperationException">When the caller may not change the workout to the target status.</exception>
    public void EnsureCanChange(Workout workout, Id callerId, Role callerRole, WorkoutStatus target, string operation)
    {
        if (CanChange(workout, callerId, callerRole, target) == false)
            throw new UnauthorizedOperationException(operation);
    }

    /// <summary>
    /// Only an admin or the workout's prescribing trainer may review it. Whether the workout can be
    /// reviewed at all is judged afterwards by the aggregate, so a refused caller never learns its status.
    /// </summary>
    /// <param name="operation">Operation being attempted, used in the error message.</param>
    /// <exception cref="UnauthorizedOperationException">When the caller may not review the workout.</exception>
    public void EnsureCanReview(Workout workout, Id callerId, Role callerRole, string operation)
    {
        if (callerRole != Role.ADMIN && workout.TrainerId != callerId)
            throw new UnauthorizedOperationException(operation);
    }

    private static bool CanChange(Workout workout, Id callerId, Role callerRole, WorkoutStatus target)
    {
        if (callerRole == Role.ADMIN)
            return true;

        var isTrainer = workout.TrainerId == callerId;
        var isAthlete = workout.UserId == callerId;

        if (isTrainer == false && isAthlete == false)
            return false;

        // The athlete of a prescribed draft does not get to see it, so cannot act on it
        if (workout.IsHiddenFrom(callerId))
            return false;

        if (workout.CanTransitionTo(target) == false)
            return true;

        var from = workout.Status;

        if (isTrainer)
            return IsPublishOrUnpublish(from, target);

        return workout.TrainerId is null || IsForward(from, target);
    }

    private static bool IsPublishOrUnpublish(WorkoutStatus from, WorkoutStatus to)
        => (from, to) is (WorkoutStatus.DRAFT, WorkoutStatus.PUBLISHED) or (WorkoutStatus.PUBLISHED, WorkoutStatus.DRAFT);

    private static bool IsForward(WorkoutStatus from, WorkoutStatus to)
        => (from, to) is (WorkoutStatus.PUBLISHED, WorkoutStatus.STARTED) or (WorkoutStatus.STARTED, WorkoutStatus.COMPLETED);
}
