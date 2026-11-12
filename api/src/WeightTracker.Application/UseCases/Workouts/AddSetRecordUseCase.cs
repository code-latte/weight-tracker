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
    /// records of that exercise and is registered at the current time. Without reps nor seconds the set counts as
    /// done as prescribed, and the aggregate copies the value of the next prescribed set.
    /// </summary>
    /// <param name="exerciseId">Catalog exercise of the workout, not the workout entry id.</param>
    /// <param name="reps">Reps performed, for an exercise prescribed by reps. Null when not given.</param>
    /// <param name="durationSeconds">Seconds held, for an exercise prescribed by time. Null when not given.</param>
    /// <param name="weight">Weight lifted in kilograms. Null when it is not tracked.</param>
    /// <returns>The updated workout.</returns>
    /// <exception cref="InvalidRepCountException">When the reps are not valid.</exception>
    /// <exception cref="InvalidDurationInSecondsException">When the seconds are not valid.</exception>
    /// <exception cref="InvalidWeightException">When the weight is not valid.</exception>
    /// <exception cref="WorkoutNotFoundException">When no workout has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not record sets in the workout.</exception>
    /// <exception cref="WorkoutResultsLockedException">When the workout is not started or completed.</exception>
    /// <exception cref="ExerciseNotInWorkoutException">When the exercise is not in the workout.</exception>
    /// <exception cref="SetRecordNotAllowedForTargetException">When the exercise mixes rep-based and time-based prescriptions, or the result is not of its kind.</exception>
    /// <exception cref="SetResultRequiredException">When no result is given and the next prescribed set is a range or max reps, or there is none left.</exception>
    public async Task<Workout> Execute(
        Id callerId,
        Role callerRole,
        Id workoutId,
        Id exerciseId,
        int? reps,
        int? durationSeconds,
        decimal? weight,
        CancellationToken cancellationToken)
    {
        var result = SetResultInput.ToResult(reps, durationSeconds);
        var kilograms = weight is null ? null : Weight.From(weight.Value);

        var workout = await _workouts.GetById(workoutId, cancellationToken)
            ?? throw new WorkoutNotFoundException(workoutId);

        _authorizer.EnsureCanRecord(workout, callerId, callerRole, "POST /workouts/{workoutId}/exercises/{exerciseId}/set-records");

        workout.AddSetRecord(exerciseId, result, kilograms, _clock.UtcNow);

        await _workouts.Save(workout, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return workout;
    }
}
