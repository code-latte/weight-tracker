using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.WorkoutTemplates;

public class AddWorkoutTemplateExerciseUseCase
{
    private const string Operation = "POST /workout-templates/{templateId}/exercises";

    private IWorkoutTemplateRepository _templates;

    private IExerciseRepository _exercises;

    private WorkoutTemplateEditAuthorizer _authorizer;

    private ExerciseVisibility _visibility;

    private IUnitOfWork _unitOfWork;

    public AddWorkoutTemplateExerciseUseCase(
        IWorkoutTemplateRepository templates,
        IExerciseRepository exercises,
        WorkoutTemplateEditAuthorizer authorizer,
        ExerciseVisibility visibility,
        IUnitOfWork unitOfWork
    )
    {
        _templates = templates;
        _exercises = exercises;
        _authorizer = authorizer;
        _visibility = visibility;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Appends a catalog exercise to an existing template.
    /// </summary>
    /// <returns>The entry created in the template.</returns>
    /// <exception cref="WorkoutTemplateNotFoundException">When no template has the given id, or it belongs to another trainer.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller is a regular user.</exception>
    /// <exception cref="ExerciseNotFoundException">When the exercise does not exist, or is a custom exercise the caller cannot see.</exception>
    /// <exception cref="ExerciseArchivedException">When the exercise is archived.</exception>
    /// <exception cref="ExerciseNotUsableInWorkoutException">When the exercise is a custom exercise owned by someone other than the template's trainer.</exception>
    /// <exception cref="ExerciseAlreadyInWorkoutException">When the exercise is already in the template.</exception>
    /// <exception cref="InvalidEffortException">When the effort is not a defined value.</exception>
    /// <exception cref="WorkoutExerciseRequiresSetsException">When no set prescription is given.</exception>
    public async Task<WorkoutExercise> Execute(
        Id callerId,
        Role callerRole,
        Id templateId,
        Id exerciseId,
        Effort effort,
        IReadOnlyList<SetPrescription> sets,
        bool perSide,
        TimeInMinutes? rest,
        CancellationToken cancellationToken)
    {
        var template = await _templates.GetById(templateId, cancellationToken)
            ?? throw new WorkoutTemplateNotFoundException(templateId);

        _authorizer.EnsureCanEdit(template, callerId, callerRole, Operation);

        var exercise = await _exercises.GetById(exerciseId, cancellationToken)
            ?? throw new ExerciseNotFoundException(exerciseId);

        await _visibility.EnsureVisible(exercise, callerId, callerRole, cancellationToken);

        EnsureUsableIn(template, exercise);

        template.AddExercise(exerciseId, effort, sets, perSide, rest);

        await _templates.Save(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return template.Exercises.Single(entry => entry.ExerciseId == exerciseId);
    }

    /// <summary>
    /// A custom exercise can only go into a template of its owner, who is the editor of record of every workout
    /// created from it. An archived exercise can no longer be added at all.
    /// </summary>
    private static void EnsureUsableIn(WorkoutTemplate template, Exercise exercise)
    {
        if (exercise.IsSystem)
            return;

        if (exercise.IsArchived)
            throw new ExerciseArchivedException(exercise.Id);

        if (exercise.OwnerId != template.TrainerId)
            throw new ExerciseNotUsableInWorkoutException(exercise.Id, template.Id);
    }
}
