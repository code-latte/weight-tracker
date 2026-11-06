using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.WorkoutTemplates;

public class ReorderWorkoutTemplateExercisesUseCase
{
    private const string Operation = "PUT /workout-templates/{templateId}/exercises/order";

    private IWorkoutTemplateRepository _templates;

    private WorkoutTemplateEditAuthorizer _authorizer;

    private IUnitOfWork _unitOfWork;

    public ReorderWorkoutTemplateExercisesUseCase(
        IWorkoutTemplateRepository templates,
        WorkoutTemplateEditAuthorizer authorizer,
        IUnitOfWork unitOfWork
    )
    {
        _templates = templates;
        _authorizer = authorizer;
        _unitOfWork = unitOfWork;
    }

    /// <returns>The reordered template.</returns>
    /// <exception cref="WorkoutTemplateNotFoundException">When no template has the given id, or it belongs to another trainer.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller is a regular user.</exception>
    /// <exception cref="WorkoutExerciseOrderMismatchException">When the ids are not exactly the exercises of the template.</exception>
    public async Task<WorkoutTemplate> Execute(
        Id callerId,
        Role callerRole,
        Id templateId,
        IReadOnlyList<Id> exerciseIds,
        CancellationToken cancellationToken)
    {
        var template = await _templates.GetById(templateId, cancellationToken)
            ?? throw new WorkoutTemplateNotFoundException(templateId);

        _authorizer.EnsureCanEdit(template, callerId, callerRole, Operation);

        template.ReorderExercises(exerciseIds);

        await _templates.Save(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return template;
    }
}
