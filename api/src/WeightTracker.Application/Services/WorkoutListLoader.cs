using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.Workouts;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.Services;

/// <summary>
/// Joins already-scoped workouts with the exercises they reference and their owners and trainers. Shared by every
/// use case that returns a workout list; orchestration only.
/// </summary>
/// <remarks>
/// Only the exercises and users referenced by the given workouts are read, so the join never widens visibility.
/// </remarks>
public class WorkoutListLoader
{
    private IExerciseRepository _exercises;

    private IUserRepository _users;

    public WorkoutListLoader(IExerciseRepository exercises, IUserRepository users)
    {
        _exercises = exercises;
        _users = users;
    }

    /// <returns>The workouts in the given order, with their exercises and users keyed by identifier.</returns>
    /// <exception cref="InvalidOperationException">When an owner or trainer of a listed workout does not exist.</exception>
    public async Task<WorkoutListDTO> Load(IReadOnlyList<Workout> workouts, CancellationToken cancellationToken)
    {
        var exerciseIds = workouts
            .SelectMany(workout => workout.Exercises)
            .Select(entry => entry.ExerciseId)
            .Distinct()
            .ToList();

        var exercises = await _exercises.GetAllByIds(exerciseIds, cancellationToken);

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
