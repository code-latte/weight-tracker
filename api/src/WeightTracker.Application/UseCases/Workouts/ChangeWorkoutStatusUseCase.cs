using WeightTracker.Application.Common;
using WeightTracker.Application.Common.Dispatcher;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

public class ChangeWorkoutStatusUseCase
{
    private IWorkoutRepository _workouts;

    private WorkoutStatusAuthorizer _authorizer;

    private IUnitOfWork _unitOfWork;

    private IClock _clock;

    private IEventDispatcher _dispatcher;

    public ChangeWorkoutStatusUseCase(
        IWorkoutRepository workouts,
        WorkoutStatusAuthorizer authorizer,
        IUnitOfWork unitOfWork,
        IClock clock,
        IEventDispatcher dispatcher
    )
    {
        _workouts = workouts;
        _authorizer = authorizer;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// Moves an existing workout to another status. An admin may set any status; everybody else only
    /// follows the lifecycle of the workout. Completing records the server time as the completion time.
    /// The events the workout raised are dispatched once the change is committed, so a failing handler cannot undo it.
    /// </summary>
    /// <returns>The updated workout.</returns>
    /// <exception cref="WorkoutNotFoundException">When no workout has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not make that change.</exception>
    /// <exception cref="InvalidWorkoutStatusTransitionException">When a non-admin asks for a move outside the lifecycle.</exception>
    /// <exception cref="InvalidWorkoutStatusException">When the target is not a defined value.</exception>
    public async Task<Workout> Execute(
        Id callerId,
        Role callerRole,
        Id workoutId,
        WorkoutStatus target,
        CancellationToken cancellationToken)
    {
        var workout = await _workouts.GetById(workoutId, cancellationToken)
            ?? throw new WorkoutNotFoundException(workoutId);

        _authorizer.EnsureCanChange(workout, callerId, callerRole, target, "PUT /workouts/{workoutId}/status");

        if (callerRole == Role.ADMIN)
            workout.OverrideStatus(target, _clock.UtcNow);
        else
            workout.ChangeStatus(target, _clock.UtcNow);

        await _workouts.Save(workout, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.DispatchAsync(workout.PullDomainEvents(), cancellationToken);

        return workout;
    }
}
