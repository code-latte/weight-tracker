using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

public class ReorderSetRecordsUseCase
{
    private IWorkoutRepository _workouts;

    private WorkoutRecordingAuthorizer _authorizer;

    private IUnitOfWork _unitOfWork;

    private IClock _clock;

    public ReorderSetRecordsUseCase(
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
    /// Puts the set records of one exercise of a workout in a new order.
    /// </summary>
    /// <param name="exerciseId">Catalog exercise of the workout, not the workout entry id.</param>
    /// <param name="setRecordIds">Set records of that exercise, every one exactly once, in the new order.</param>
    /// <returns>The updated workout.</returns>
    /// <exception cref="WorkoutNotFoundException">When no workout has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not record sets in the workout.</exception>
    /// <exception cref="WorkoutResultsLockedException">When the workout is not started or completed.</exception>
    /// <exception cref="ExerciseNotInWorkoutException">When the exercise is not in the workout.</exception>
    /// <exception cref="SetRecordOrderMismatchException">When the ids are not exactly the set records of that exercise.</exception>
    public async Task<Workout> Execute(
        Id callerId,
        Role callerRole,
        Id workoutId,
        Id exerciseId,
        IReadOnlyList<Id> setRecordIds,
        CancellationToken cancellationToken)
    {
        var workout = await _workouts.GetById(workoutId, cancellationToken)
            ?? throw new WorkoutNotFoundException(workoutId);

        _authorizer.EnsureCanRecord(workout, callerId, callerRole, "PUT /workouts/{workoutId}/exercises/{exerciseId}/set-records/order");

        workout.ReorderSetRecords(exerciseId, setRecordIds, _clock.UtcNow);

        await _workouts.Save(workout, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return workout;
    }
}
