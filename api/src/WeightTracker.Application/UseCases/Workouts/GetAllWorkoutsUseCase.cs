using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
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
    private IWorkoutRepository _workouts;

    private IExerciseRepository _exercises;

    private IUserRepository _users;

    public GetAllWorkoutsUseCase(
        IWorkoutRepository workouts,
        IExerciseRepository exercises,
        IUserRepository users
    )
    {
        _workouts = workouts;
        _exercises = exercises;
        _users = users;
    }

    /// <exception cref="UnauthorizedOperationException">When the caller's role may not list workouts.</exception>
    /// <exception cref="InvalidOperationException">When an owner or trainer of a listed workout does not exist.</exception>
    public async Task<WorkoutListDTO> Execute(Id callerId, Role callerRole, CancellationToken cancellationToken)
    {
        var workouts = (await ScopedWorkouts(callerId, callerRole, cancellationToken)).ToList();

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

    private async Task<IEnumerable<Workout>> ScopedWorkouts(Id callerId, Role callerRole, CancellationToken cancellationToken)
    {
        return callerRole switch
        {
            Role.ADMIN => await _workouts.GetAll(cancellationToken),
            Role.TRAINER => PublishedToCaller(await _workouts.GetAllByUserOrTrainerId(callerId, cancellationToken), callerId),
            Role.USER => PublishedToCaller(await _workouts.GetAllByUserId(callerId, cancellationToken), callerId),
            _ => throw new UnauthorizedOperationException("GET /workouts"),
        };
    }

    /// <summary>
    /// Leaves out the prescribed drafts of which the caller is the athlete: their trainer has not published them yet.
    /// </summary>
    private static IEnumerable<Workout> PublishedToCaller(IEnumerable<Workout> workouts, Id callerId)
        => workouts.Where(workout => workout.IsHiddenFrom(callerId) == false);
}
