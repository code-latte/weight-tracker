using WeightTracker.Application.Exceptions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.Services;

/// <summary>
/// Decides who may change an existing workout. Every use case that edits a workout goes through it,
/// so the rule cannot drift between endpoints.
/// </summary>
/// <remarks>
/// An admin may always edit. A prescribed workout may only be edited by its trainer, not even by its owner.
/// A workout without a trainer may be edited by its owner, whatever the owner's role.
/// Once a workout leaves draft nobody but an admin may edit it, and once it is reviewed nobody may, not even an admin.
/// </remarks>
public class WorkoutEditAuthorizer
{
    /// <param name="operation">Operation being attempted, used in the error message.</param>
    /// <exception cref="UnauthorizedOperationException">When the caller may not edit the workout.</exception>
    /// <exception cref="WorkoutLockedException">When the caller may edit the workout but it is no longer a draft, or it is reviewed.</exception>
    public void EnsureCanEdit(Workout workout, Id callerId, Role callerRole, string operation)
    {
        EnsureCanModify(workout, callerId, callerRole, operation);

        // Checked after the permission so a caller who may not edit never learns the status
        if (IsLockedFor(workout, callerRole))
            throw new WorkoutLockedException(workout.Id, workout.Status);
    }

    /// <summary>
    /// Checks only who may change the workout, not whether its status still allows it.
    /// Content edits must go through <see cref="EnsureCanEdit"/>; this is for operations that apply their own
    /// status rule, such as deleting a workout.
    /// </summary>
    /// <param name="operation">Operation being attempted, used in the error message.</param>
    /// <exception cref="UnauthorizedOperationException">When the caller may not change the workout.</exception>
    public void EnsureCanModify(Workout workout, Id callerId, Role callerRole, string operation)
    {
        if (CanEdit(workout, callerId, callerRole) == false)
            throw new UnauthorizedOperationException(operation);
    }

    // A reviewed workout keeps the prescription that was reviewed, so the admin bypass does not reach it
    private static bool IsLockedFor(Workout workout, Role callerRole)
        => workout.Status == WorkoutStatus.REVIEWED
            || (callerRole != Role.ADMIN && workout.IsLocked);

    private static bool CanEdit(Workout workout, Id callerId, Role callerRole)
    {
        if (callerRole == Role.ADMIN)
            return true;

        return workout.TrainerId is not null
            ? workout.TrainerId == callerId
            : workout.UserId == callerId;
    }
}
