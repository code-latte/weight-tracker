using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.WorkoutTemplates;

public class RemoveWorkoutTemplateExerciseUseCase
{
    private const string Operation = "DELETE /workout-templates/{templateId}/exercises/{exerciseId}";

    private IWorkoutTemplateRepository _templates;

    private WorkoutTemplateEditAuthorizer _authorizer;

    private IUnitOfWork _unitOfWork;

    public RemoveWorkoutTemplateExerciseUseCase(
        IWorkoutTemplateRepository templates,
        WorkoutTemplateEditAuthorizer authorizer,
        IUnitOfWork unitOfWork
    )
    {
        _templates = templates;
        _authorizer = authorizer;
        _unitOfWork = unitOfWork;
    }

    /// <exception cref="WorkoutTemplateNotFoundException">When no template has the given id, or it belongs to another trainer.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller is a regular user.</exception>
    /// <exception cref="ExerciseNotInWorkoutException">When the exercise is not in the template.</exception>
    public async Task Execute(
        Id callerId,
        Role callerRole,
        Id templateId,
        Id exerciseId,
        CancellationToken cancellationToken)
    {
        var template = await _templates.GetById(templateId, cancellationToken)
            ?? throw new WorkoutTemplateNotFoundException(templateId);

        _authorizer.EnsureCanEdit(template, callerId, callerRole, Operation);

        template.RemoveExercise(exerciseId);

        await _templates.Save(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
