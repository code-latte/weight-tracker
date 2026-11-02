using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

public class AddWorkoutExerciseUseCase
{
    private IWorkoutRepository _workouts;

    private IExerciseRepository _exercises;

    private WorkoutEditAuthorizer _authorizer;

    private IUnitOfWork _unitOfWork;

    public AddWorkoutExerciseUseCase(
        IWorkoutRepository workouts,
        IExerciseRepository exercises,
        WorkoutEditAuthorizer authorizer,
        IUnitOfWork unitOfWork
    )
    {
        _workouts = workouts;
        _exercises = exercises;
        _authorizer = authorizer;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Appends a catalog exercise to an existing workout.
    /// </summary>
    /// <returns>The entry created in the workout.</returns>
    /// <exception cref="WorkoutNotFoundException">When no workout has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not edit the workout.</exception>
    /// <exception cref="WorkoutLockedException">When the caller may edit the workout but it has already started.</exception>
    /// <exception cref="ExerciseNotFoundException">When the exercise is not in the catalog.</exception>
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

        _ = await _exercises.GetById(exerciseId, cancellationToken)
            ?? throw new ExerciseNotFoundException(exerciseId);

        workout.AddExercise(exerciseId, effort, sets, perSide, rest);

        await _workouts.Save(workout, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return workout.Exercises.Single(entry => entry.ExerciseId == exerciseId);
    }
}
