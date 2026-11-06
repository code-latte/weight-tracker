using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Workouts;

/// <summary>
/// Reads one workout with its owner and trainer, provided the caller is entitled to see it.
/// </summary>
/// <remarks>
/// The visibility rule is the one of <see cref="GetAllWorkoutsUseCase"/>, kept in <see cref="WorkoutVisibility"/>:
/// admins see every workout, trainers the ones they own or prescribed, users the ones they own. Anyone but an admin
/// is also refused a prescribed draft of which they are the athlete, since the trainer has not published it yet.
/// The people are resolved only once the workout is visible, so a refused read discloses nobody.
/// </remarks>
public class GetWorkoutByIdUseCase
{
    private readonly IWorkoutRepository _workouts;

    private readonly WorkoutVisibility _visibility;

    private readonly IUserRepository _users;

    public GetWorkoutByIdUseCase(IWorkoutRepository workouts, WorkoutVisibility visibility, IUserRepository users)
    {
        _workouts = workouts;
        _visibility = visibility;
        _users = users;
    }

    /// <exception cref="WorkoutNotFoundException">When no workout has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not see the workout.</exception>
    /// <exception cref="InvalidOperationException">When the owner or trainer of the workout does not exist.</exception>
    public async Task<WorkoutView> Execute(Id callerId, Role callerRole, Id workoutId, CancellationToken cancellationToken)
    {
        var workout = await _workouts.GetById(workoutId, cancellationToken)
            ?? throw new WorkoutNotFoundException(workoutId);

        if (_visibility.CanSee(workout, callerId, callerRole) == false)
        {
            throw new UnauthorizedOperationException($"GET /workouts/{workoutId}");
        }

        var userIds = workout.TrainerId is null ? new[] { workout.UserId } : new[] { workout.UserId, workout.TrainerId };

        var users = (await _users.GetAllByIds(userIds, cancellationToken)).ToDictionary(user => user.Id);

        return new WorkoutView(
            workout,
            Find(users, workout.UserId),
            workout.TrainerId is null ? null : Find(users, workout.TrainerId));
    }

    // Foreign keys keep every referenced user in place, so a missing one is a data-integrity failure
    private static User Find(Dictionary<Id, User> users, Id userId)
        => users.TryGetValue(userId, out var user)
            ? user
            : throw new InvalidOperationException($"User '{userId}' referenced by a workout does not exist");
}
