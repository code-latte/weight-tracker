using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

/// <summary>
/// Lists the workouts the caller is entitled to see.
/// </summary>
/// <remarks>
/// The caller is passed explicitly instead of through an ambient accessor, so the
/// Application layer stays free of request-scoped infrastructure.
/// </remarks>
public class GetAllWorkoutsUseCase
{
    private WorkoutVisibility _visibility;

    private IExerciseRepository _exercises;

    private IUserRepository _users;

    public GetAllWorkoutsUseCase(
        WorkoutVisibility visibility,
        IExerciseRepository exercises,
        IUserRepository users
    )
    {
        _visibility = visibility;
        _exercises = exercises;
        _users = users;
    }

    /// <exception cref="UnauthorizedOperationException">When the caller's role may not list workouts.</exception>
    /// <exception cref="InvalidOperationException">When an owner or trainer of a listed workout does not exist.</exception>
    public async Task<WorkoutListDTO> Execute(Id callerId, Role callerRole, CancellationToken cancellationToken)
    {
        var workouts = await _visibility.VisibleTo(callerId, callerRole, cancellationToken);

        // Only exercises referenced by the scoped workouts, so the join never widens visibility
        var exerciseIds = workouts
            .SelectMany(workout => workout.Exercises)
            .Select(entry => entry.ExerciseId)
            .Distinct()
            .ToList();

        var exercises = await _exercises.GetAllByIds(exerciseIds, cancellationToken);

        // Only owners and trainers of the scoped workouts, so the join never widens visibility either
        var userIds = workouts
            .Select(workout => workout.UserId)
            .Concat(workouts.Where(workout => workout.TrainerId is not null).Select(workout => workout.TrainerId!))
            .Distinct()
            .ToList();

        var users = (await _users.GetAllByIds(userIds, cancellationToken)).ToDictionary(user => user.Id);

        // Foreign keys keep every referenced user in place, so a missing one is a data-integrity failure
        var missing = userIds.FirstOrDefault(userId => users.ContainsKey(userId) == false);

        if (missing is not null)
            throw new InvalidOperationException($"User '{missing}' referenced by a listed workout does not exist");

        return new WorkoutListDTO(workouts, exercises.ToDictionary(exercise => exercise.Id), users);
    }
}
