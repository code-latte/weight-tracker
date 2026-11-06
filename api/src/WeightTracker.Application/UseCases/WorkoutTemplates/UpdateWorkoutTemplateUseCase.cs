using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.WorkoutTemplates;

public class UpdateWorkoutTemplateUseCase
{
    private const string Operation = "PUT /workout-templates/{id}";

    private IWorkoutTemplateRepository _templates;

    private WorkoutTemplateEditAuthorizer _authorizer;

    private IUnitOfWork _unitOfWork;

    public UpdateWorkoutTemplateUseCase(
        IWorkoutTemplateRepository templates,
        WorkoutTemplateEditAuthorizer authorizer,
        IUnitOfWork unitOfWork
    )
    {
        _templates = templates;
        _authorizer = authorizer;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Replaces the name and the description of a template. The trainer and the exercises are untouched.
    /// </summary>
    /// <param name="description">New description, <c>null</c> to clear it.</param>
    /// <returns>The updated template.</returns>
    /// <exception cref="WorkoutTemplateNotFoundException">When no template has the given id, or it belongs to another trainer.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller is a regular user.</exception>
    public async Task<WorkoutTemplate> Execute(
        Id callerId,
        Role callerRole,
        Id templateId,
        WorkoutName name,
        WorkoutDescription? description,
        CancellationToken cancellationToken)
    {
        var template = await _templates.GetById(templateId, cancellationToken)
            ?? throw new WorkoutTemplateNotFoundException(templateId);

        _authorizer.EnsureCanEdit(template, callerId, callerRole, Operation);

        template.UpdateDetails(name, description);

        await _templates.Save(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return template;
    }
}
