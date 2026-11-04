using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

public class AddWorkoutExerciseUseCase
{
    private IWorkoutRepository _workouts;

    private IExerciseRepository _exercises;

    private WorkoutEditAuthorizer _authorizer;

    private ExerciseVisibility _visibility;

    private IUnitOfWork _unitOfWork;

    public AddWorkoutExerciseUseCase(
        IWorkoutRepository workouts,
        IExerciseRepository exercises,
        WorkoutEditAuthorizer authorizer,
        ExerciseVisibility visibility,
        IUnitOfWork unitOfWork
    )
    {
        _workouts = workouts;
        _exercises = exercises;
        _authorizer = authorizer;
        _visibility = visibility;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Appends a catalog exercise to an existing workout.
    /// </summary>
    /// <returns>The entry created in the workout.</returns>
    /// <exception cref="WorkoutNotFoundException">When no workout has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not edit the workout.</exception>
    /// <exception cref="WorkoutLockedException">When the caller may edit the workout but it has already started.</exception>
    /// <exception cref="ExerciseNotFoundException">When the exercise does not exist, or is a custom exercise the caller cannot see.</exception>
    /// <exception cref="ExerciseArchivedException">When the exercise is archived.</exception>
    /// <exception cref="ExerciseNotUsableInWorkoutException">When the exercise is a custom exercise owned by someone other than the workout's editor of record.</exception>
    /// <exception cref="ExerciseAlreadyInWorkoutException">When the exercise is already in the workout.</exception>
    /// <exception cref="InvalidEffortException">When the effort is not a defined value.</exception>
    /// <exception cref="WorkoutExerciseRequiresSetsException">When no set prescription is given.</exception>
    public async Task<WorkoutExercise> Execute(
        Id callerId,
        Role callerRole,
        Id workoutId,
        Id exerciseId,
        Effort effort,
        IReadOnlyList<SetPrescription> sets,
        bool perSide,
        TimeInMinutes? rest,
        CancellationToken cancellationToken)
    {
        var workout = await _workouts.GetById(workoutId, cancellationToken)
            ?? throw new WorkoutNotFoundException(workoutId);

        _authorizer.EnsureCanEdit(workout, callerId, callerRole, "POST /workouts/{workoutId}/exercises");

        var exercise = await _exercises.GetById(exerciseId, cancellationToken)
            ?? throw new ExerciseNotFoundException(exerciseId);

        await _visibility.EnsureVisible(exercise, callerId, callerRole, cancellationToken);

        EnsureUsableIn(workout, exercise);

        workout.AddExercise(exerciseId, effort, sets, perSide, rest);

        await _workouts.Save(workout, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return workout.Exercises.Single(entry => entry.ExerciseId == exerciseId);
    }

    /// <summary>
    /// A custom exercise can only go into a workout edited by its owner: the prescribing trainer when the workout
    /// has one, otherwise the workout's owner. An archived exercise can no longer be added at all.
    /// </summary>
    private static void EnsureUsableIn(Workout workout, Exercise exercise)
    {
        if (exercise.IsSystem)
            return;

        if (exercise.IsArchived)
            throw new ExerciseArchivedException(exercise.Id);

        if (exercise.OwnerId != (workout.TrainerId ?? workout.UserId))
            throw new ExerciseNotUsableInWorkoutException(exercise.Id, workout.Id);
    }
}
