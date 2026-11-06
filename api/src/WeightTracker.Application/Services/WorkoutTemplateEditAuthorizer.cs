using WeightTracker.Application.Exceptions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.Services;

/// <summary>
/// Decides who may read, change, delete or use an existing workout template. Every template use case goes
/// through it, so the rule cannot drift between endpoints.
/// </summary>
/// <remarks>
/// An admin may always act on a template. A trainer may only act on the templates they own; another trainer's
/// template is reported as not found, so its existence is never confirmed. Regular users never use templates.
/// </remarks>
public class WorkoutTemplateEditAuthorizer
{
    /// <param name="operation">Operation being attempted, used in the error message.</param>
    /// <exception cref="UnauthorizedOperationException">When the caller is a regular user.</exception>
    /// <exception cref="WorkoutTemplateNotFoundException">When the caller is a trainer who does not own the template.</exception>
    public void EnsureCanEdit(WorkoutTemplate template, Id callerId, Role callerRole, string operation)
    {
        switch (callerRole)
        {
            case Role.ADMIN:
                return;

            case Role.TRAINER when template.TrainerId == callerId:
                return;

            case Role.TRAINER:
                throw new WorkoutTemplateNotFoundException(template.Id);

            default:
                throw new UnauthorizedOperationException(operation);
        }
    }
}
