using WeightTracker.Application.Exceptions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.Services;

/// <summary>
/// Decides who may record the sets actually performed in a workout: add, edit, remove and reorder its set records.
/// Every use case that changes set records goes through it, so the rule cannot drift between endpoints.
/// </summary>
/// <remarks>
/// Recording is not a content edit, so <see cref="WorkoutEditAuthorizer"/> and its lock do not apply.
/// An admin may always record. Otherwise only the workout's owner may, and never a trainer, not even the
/// prescribing trainer or a trainer who owns the workout. The status is checked afterwards by the aggregate,
/// so a caller who may not record never learns it.
/// </remarks>
public class WorkoutRecordingAuthorizer
{
    /// <param name="operation">Operation being attempted, used in the error message.</param>
    /// <exception cref="UnauthorizedOperationException">When the caller may not record sets in the workout.</exception>
    public void EnsureCanRecord(Workout workout, Id callerId, Role callerRole, string operation)
    {
        if (CanRecord(workout, callerId, callerRole) == false)
            throw new UnauthorizedOperationException(operation);
    }

    private static bool CanRecord(Workout workout, Id callerId, Role callerRole)
    {
        if (callerRole == Role.ADMIN)
            return true;

        if (callerRole == Role.TRAINER)
            return false;

        return workout.UserId == callerId;
    }
}
