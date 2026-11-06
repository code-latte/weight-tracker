using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.WorkoutTemplates;

public class GetAllWorkoutTemplatesUseCase
{
    private const string Operation = "GET /workout-templates";

    private IWorkoutTemplateRepository _templates;

    public GetAllWorkoutTemplatesUseCase(IWorkoutTemplateRepository templates)
    {
        _templates = templates;
    }

    /// <summary>
    /// Lists the templates the caller may see: a trainer only their own, an admin every template.
    /// </summary>
    /// <param name="trainerId">Admin only: restricts the list to one trainer. Ignored for a trainer, who only ever sees their own.</param>
    /// <exception cref="UnauthorizedOperationException">When the caller is a regular user.</exception>
    public async Task<IEnumerable<WorkoutTemplate>> Execute(
        Id callerId,
        Role callerRole,
        Id? trainerId,
        CancellationToken cancellationToken)
        => callerRole switch
        {
            Role.TRAINER => await _templates.GetAllByTrainerId(callerId, cancellationToken),
            Role.ADMIN when trainerId is not null => await _templates.GetAllByTrainerId(trainerId, cancellationToken),
            Role.ADMIN => await _templates.GetAll(cancellationToken),
            _ => throw new UnauthorizedOperationException(Operation),
        };
}
