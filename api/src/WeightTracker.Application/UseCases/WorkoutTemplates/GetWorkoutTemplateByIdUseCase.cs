using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.WorkoutTemplates;

public class GetWorkoutTemplateByIdUseCase
{
    private const string Operation = "GET /workout-templates/{id}";

    private IWorkoutTemplateRepository _templates;

    private WorkoutTemplateEditAuthorizer _authorizer;

    public GetWorkoutTemplateByIdUseCase(IWorkoutTemplateRepository templates, WorkoutTemplateEditAuthorizer authorizer)
    {
        _templates = templates;
        _authorizer = authorizer;
    }

    /// <exception cref="WorkoutTemplateNotFoundException">When no template has the given id, or it belongs to another trainer.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller is a regular user.</exception>
    public async Task<WorkoutTemplate> Execute(Id callerId, Role callerRole, Id templateId, CancellationToken cancellationToken)
    {
        var template = await _templates.GetById(templateId, cancellationToken)
            ?? throw new WorkoutTemplateNotFoundException(templateId);

        _authorizer.EnsureCanEdit(template, callerId, callerRole, Operation);

        return template;
    }
}
