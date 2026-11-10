using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Workouts;

public class DeleteWorkoutUseCase
{
    private IWorkoutRepository _workouts;

    private WorkoutEditAuthorizer _authorizer;

    private IUnitOfWork _unitOfWork;

    public DeleteWorkoutUseCase(
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
    /// Permanently deletes a draft workout, with its exercises, sets and superseries.
    /// </summary>
    /// <exception cref="WorkoutNotFoundException">When no workout has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not change the workout.</exception>
    /// <exception cref="WorkoutDeletionLockedException">When the caller may change the workout but it is no longer a draft.</exception>
    /// <exception cref="WorkoutConcurrencyConflictException">When the workout changed since it was read.</exception>
    public async Task Execute(Id callerId, Role callerRole, Id workoutId, CancellationToken cancellationToken)
    {
        var workout = await _workouts.GetById(workoutId, cancellationToken)
            ?? throw new WorkoutNotFoundException(workoutId);

        _authorizer.EnsureCanModify(workout, callerId, callerRole, "DELETE /workouts/{workoutId}");

        // Checked after the permission so a caller who may not delete never learns the status
        workout.EnsureCanBeDeleted();

        await _workouts.Delete(workout, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
