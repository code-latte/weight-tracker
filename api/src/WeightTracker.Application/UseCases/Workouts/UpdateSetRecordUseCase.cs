using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

public class UpdateSetRecordUseCase
{
    private IWorkoutRepository _workouts;

    private WorkoutRecordingAuthorizer _authorizer;

    private IUnitOfWork _unitOfWork;

    private IClock _clock;

    public UpdateSetRecordUseCase(
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
    /// Corrects the result and weight of a set already recorded. Its id, registration time and position are kept.
    /// The result must be given: a correction states its value, it is never copied from the prescription.
    /// </summary>
    /// <param name="exerciseId">Catalog exercise of the workout, not the workout entry id.</param>
    /// <param name="reps">Reps performed, for an exercise prescribed by reps. Null when not given.</param>
    /// <param name="durationSeconds">Seconds held, for an exercise prescribed by time. Null when not given.</param>
    /// <param name="weight">Weight lifted in kilograms. Null clears it.</param>
    /// <returns>The updated workout.</returns>
    /// <exception cref="SetResultRequiredException">When neither reps nor seconds are given.</exception>
    /// <exception cref="InvalidRepCountException">When the reps are not valid.</exception>
    /// <exception cref="InvalidDurationInSecondsException">When the seconds are not valid.</exception>
    /// <exception cref="InvalidWeightException">When the weight is not valid.</exception>
    /// <exception cref="WorkoutNotFoundException">When no workout has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not record sets in the workout.</exception>
    /// <exception cref="WorkoutResultsLockedException">When the workout is not started or completed.</exception>
    /// <exception cref="ExerciseNotInWorkoutException">When the exercise is not in the workout.</exception>
    /// <exception cref="SetRecordNotFoundException">When the set record is not recorded for that exercise.</exception>
    /// <exception cref="SetRecordNotAllowedForTargetException">When the exercise mixes rep-based and time-based prescriptions, or the result is not of its kind.</exception>
    public async Task<Workout> Execute(
        Id callerId,
        Role callerRole,
        Id workoutId,
        Id exerciseId,
        Id setRecordId,
        int? reps,
        int? durationSeconds,
        decimal? weight,
        CancellationToken cancellationToken)
    {
        var result = SetResultInput.ToResult(reps, durationSeconds)
            ?? throw new SetResultRequiredException(exerciseId, "a correction must state the reps or the seconds performed");
        var kilograms = weight is null ? null : Weight.From(weight.Value);

        var workout = await _workouts.GetById(workoutId, cancellationToken)
            ?? throw new WorkoutNotFoundException(workoutId);

        _authorizer.EnsureCanRecord(workout, callerId, callerRole, "PUT /workouts/{workoutId}/exercises/{exerciseId}/set-records/{setRecordId}");

        workout.UpdateSetRecord(exerciseId, setRecordId, result, kilograms, _clock.UtcNow);

        await _workouts.Save(workout, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return workout;
    }
}
