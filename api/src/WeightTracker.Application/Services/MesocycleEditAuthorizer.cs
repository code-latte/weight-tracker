using WeightTracker.Application.Exceptions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.Services;

/// <summary>
/// Decides who may change an existing mesocycle, its weeks, and which workouts are placed in it. Every use case
/// that does any of that goes through it, so the rule cannot drift between endpoints.
/// </summary>
/// <remarks>
/// An admin may always. Otherwise only the mesocycle's trainer may, whether or not their relationship with the
/// athlete is still active. The athlete may not.
/// </remarks>
public class MesocycleEditAuthorizer
{
    /// <param name="operation">Operation being attempted, used in the error message.</param>
    /// <exception cref="UnauthorizedOperationException">When the caller may not change the mesocycle.</exception>
    public void EnsureCanEdit(Mesocycle mesocycle, Id callerId, Role callerRole, string operation)
    {
        var canEdit = callerRole == Role.ADMIN
            || (callerRole == Role.TRAINER && mesocycle.TrainerId == callerId);

        if (canEdit == false)
            throw new UnauthorizedOperationException(operation);
    }
}
