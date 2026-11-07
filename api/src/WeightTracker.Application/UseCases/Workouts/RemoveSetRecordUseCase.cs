using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

public class RemoveSetRecordUseCase
{
    private IWorkoutRepository _workouts;

    private WorkoutRecordingAuthorizer _authorizer;

    private IUnitOfWork _unitOfWork;

    private IClock _clock;

    public RemoveSetRecordUseCase(
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
    /// Removes a set already recorded. The other set records of the exercise keep their order.
    /// </summary>
    /// <param name="exerciseId">Catalog exercise of the workout, not the workout entry id.</param>
    /// <returns>The updated workout.</returns>
    /// <exception cref="WorkoutNotFoundException">When no workout has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not record sets in the workout.</exception>
    /// <exception cref="WorkoutResultsLockedException">When the workout is not started or completed.</exception>
    /// <exception cref="ExerciseNotInWorkoutException">When the exercise is not in the workout.</exception>
    /// <exception cref="SetRecordNotFoundException">When the set record is not recorded for that exercise.</exception>
    public async Task<Workout> Execute(
        Id callerId,
        Role callerRole,
        Id workoutId,
        Id exerciseId,
        Id setRecordId,
        CancellationToken cancellationToken)
    {
        var workout = await _workouts.GetById(workoutId, cancellationToken)
            ?? throw new WorkoutNotFoundException(workoutId);

        _authorizer.EnsureCanRecord(workout, callerId, callerRole, "DELETE /workouts/{workoutId}/exercises/{exerciseId}/set-records/{setRecordId}");

        workout.RemoveSetRecord(exerciseId, setRecordId, _clock.UtcNow);

        await _workouts.Save(workout, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return workout;
    }
}
