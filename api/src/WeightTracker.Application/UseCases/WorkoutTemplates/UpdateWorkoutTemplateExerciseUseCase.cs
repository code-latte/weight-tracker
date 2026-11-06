using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.WorkoutTemplates;

public class UpdateWorkoutTemplateExerciseUseCase
{
    private const string Operation = "PUT /workout-templates/{templateId}/exercises/{exerciseId}";

    private IWorkoutTemplateRepository _templates;

    private WorkoutTemplateEditAuthorizer _authorizer;

    private IUnitOfWork _unitOfWork;

    public UpdateWorkoutTemplateExerciseUseCase(
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
    /// Replaces the effort, the per-side flag, the rest and every set of an exercise of a template.
    /// </summary>
    /// <returns>The updated entry.</returns>
    /// <exception cref="ExerciseIdMismatchException">When the route and body name different exercises.</exception>
    /// <exception cref="WorkoutTemplateNotFoundException">When no template has the given id, or it belongs to another trainer.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller is a regular user.</exception>
    /// <exception cref="ExerciseNotInWorkoutException">When the exercise is not in the template.</exception>
    /// <exception cref="SuperSeriesSetCountMismatchException">When the entry is in a superseries and the new total of sets differs.</exception>
    public async Task<WorkoutExercise> Execute(
        Id callerId,
        Role callerRole,
        Id templateId,
        Id routeExerciseId,
        Id bodyExerciseId,
        Effort effort,
        IReadOnlyList<SetPrescription> sets,
        bool perSide,
        TimeInMinutes? rest,
        CancellationToken cancellationToken)
    {
        if (routeExerciseId != bodyExerciseId)
            throw new ExerciseIdMismatchException(routeExerciseId, bodyExerciseId);

        var template = await _templates.GetById(templateId, cancellationToken)
            ?? throw new WorkoutTemplateNotFoundException(templateId);

        _authorizer.EnsureCanEdit(template, callerId, callerRole, Operation);

        template.UpdateExercise(routeExerciseId, effort, sets, perSide, rest);

        await _templates.Save(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return template.Exercises.Single(entry => entry.ExerciseId == routeExerciseId);
    }
}
