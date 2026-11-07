using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

public class AddSetRecordUseCase
{
    private IWorkoutRepository _workouts;

    private WorkoutRecordingAuthorizer _authorizer;

    private IUnitOfWork _unitOfWork;

    private IClock _clock;

    public AddSetRecordUseCase(
        IWorkoutRepository workouts,
        WorkoutRecordingAuthorizer authorizer,
        IUnitOfWork unitOfWork,
        IClock clock
    )
    {
        _workouts = workouts;
        _authorizer = authorizer;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    /// <summary>
    /// Records a set actually performed for an exercise of a started workout. It goes after the existing set
    /// records of that exercise and is registered at the current time.
    /// </summary>
    /// <param name="exerciseId">Catalog exercise of the workout, not the workout entry id.</param>
    /// <returns>The updated workout.</returns>
    /// <exception cref="InvalidRepCountException">When the reps are not valid.</exception>
    /// <exception cref="InvalidWeightException">When the weight is not valid.</exception>
    /// <exception cref="WorkoutNotFoundException">When no workout has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not record sets in the workout.</exception>
    /// <exception cref="WorkoutResultsLockedException">When the workout is not started or completed.</exception>
    /// <exception cref="ExerciseNotInWorkoutException">When the exercise is not in the workout.</exception>
    /// <exception cref="SetRecordNotAllowedForTargetException">When the exercise is not prescribed by reps.</exception>
    public async Task<Workout> Execute(
        Id callerId,
        Role callerRole,
        Id workoutId,
        Id exerciseId,
        int reps,
        decimal weight,
        CancellationToken cancellationToken)
    {
        var repCount = RepCount.From(reps);
        var kilograms = Weight.From(weight);

        var workout = await _workouts.GetById(workoutId, cancellationToken)
            ?? throw new WorkoutNotFoundException(workoutId);

        _authorizer.EnsureCanRecord(workout, callerId, callerRole, "POST /workouts/{workoutId}/exercises/{exerciseId}/set-records");

        workout.AddSetRecord(exerciseId, repCount, kilograms, _clock.UtcNow);

        await _workouts.Save(workout, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return workout;
    }
}
