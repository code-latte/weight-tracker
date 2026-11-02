using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

public class UpdateWorkoutExerciseUseCase
{
    private IWorkoutRepository _workouts;

    private WorkoutEditAuthorizer _authorizer;

    private IUnitOfWork _unitOfWork;

    public UpdateWorkoutExerciseUseCase(
        IWorkoutRepository workouts,
        WorkoutEditAuthorizer authorizer,
        IUnitOfWork unitOfWork
    )
    {
        _workouts = workouts;
        _authorizer = authorizer;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Replaces the effort, the per-side flag, the rest and every set prescription of an exercise of an existing workout.
    /// </summary>
    /// <param name="routeExerciseId">Catalog exercise addressed by the route.</param>
    /// <param name="bodyExerciseId">Catalog exercise named by the body. It must be the same one.</param>
    /// <returns>The updated entry.</returns>
    /// <exception cref="ExerciseIdMismatchException">When the route and body name different exercises.</exception>
    /// <exception cref="WorkoutNotFoundException">When no workout has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not edit the workout.</exception>
    /// <exception cref="WorkoutLockedException">When the caller may edit the workout but it has already started.</exception>
    /// <exception cref="ExerciseNotInWorkoutException">When the exercise is not in the workout.</exception>
    /// <exception cref="SuperSeriesSetCountMismatchException">When the new total of sets breaks the entry's superseries.</exception>
    /// <exception cref="InvalidEffortException">When the effort is not a defined value.</exception>
    /// <exception cref="WorkoutExerciseRequiresSetsException">When no set prescription is given.</exception>
    public async Task<WorkoutExercise> Execute(
        Id callerId,
        Role callerRole,
        Id workoutId,
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

        var workout = await _workouts.GetById(workoutId, cancellationToken)
            ?? throw new WorkoutNotFoundException(workoutId);

        _authorizer.EnsureCanEdit(workout, callerId, callerRole, "PUT /workouts/{workoutId}/exercises/{exerciseId}");

        workout.UpdateExercise(routeExerciseId, effort, sets, perSide, rest);

        await _workouts.Save(workout, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return workout.Exercises.Single(entry => entry.ExerciseId == routeExerciseId);
    }
}
