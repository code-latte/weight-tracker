using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

/// <summary>
/// Reads one workout with its owner and trainer, the exercises its entries reference and the mesocycle it is placed
/// in, provided the caller is entitled to see it.
/// </summary>
/// <remarks>
/// The visibility rule is the one of <see cref="GetAllWorkoutsUseCase"/>, kept in <see cref="WorkoutVisibility"/>:
/// admins see every workout, trainers the ones they own or prescribed, users the ones they own. Anyone but an admin
/// is also refused a prescribed draft of which they are the athlete, since the trainer has not published it yet.
/// The people, exercises and mesocycle are resolved only once the workout is visible, so a refused read discloses
/// none of them. The mesocycle needs no visibility check of its own: a workout is only placed in a mesocycle of its
/// owner, and of its trainer when it has one, so whoever sees the workout also sees the mesocycle.
/// </remarks>
public class GetWorkoutByIdUseCase
{
    private readonly IWorkoutRepository _workouts;

    private readonly WorkoutVisibility _visibility;

    private readonly IUserRepository _users;

    private readonly IExerciseRepository _exercises;

    private readonly IMesocycleRepository _mesocycles;

    public GetWorkoutByIdUseCase(
        IWorkoutRepository workouts,
        WorkoutVisibility visibility,
        IUserRepository users,
        IExerciseRepository exercises,
        IMesocycleRepository mesocycles)
    {
        _workouts = workouts;
        _visibility = visibility;
        _users = users;
        _exercises = exercises;
        _mesocycles = mesocycles;
    }

    /// <exception cref="WorkoutNotFoundException">When no workout has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not see the workout.</exception>
    /// <exception cref="InvalidOperationException">
    /// When the owner, the trainer, a referenced exercise or the mesocycle of the workout does not exist.
    /// </exception>
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

        var exercises = await LoadExercises(workout, cancellationToken);

        var mesocycle = workout.MesocycleId is null ? null : await LoadMesocycle(workout.MesocycleId, cancellationToken);

        return new WorkoutView(
            workout,
            Find(users, workout.UserId),
            workout.TrainerId is null ? null : Find(users, workout.TrainerId),
            exercises,
            mesocycle);
    }

    // Foreign keys keep every referenced exercise in place, so a missing one is a data-integrity failure
    private async Task<IReadOnlyDictionary<Id, Exercise>> LoadExercises(Workout workout, CancellationToken cancellationToken)
    {
        var exerciseIds = workout.Exercises.Select(entry => entry.ExerciseId).ToList();

        var exercises = (await _exercises.GetAllByIds(exerciseIds, cancellationToken)).ToDictionary(exercise => exercise.Id);

        var missing = exerciseIds.FirstOrDefault(exerciseId => exercises.ContainsKey(exerciseId) == false);

        if (missing is not null)
            throw new InvalidOperationException($"Exercise '{missing}' referenced by a workout does not exist");

        return exercises;
    }

    // Foreign keys keep the referenced mesocycle in place, so a missing one is a data-integrity failure
    private async Task<Mesocycle> LoadMesocycle(Id mesocycleId, CancellationToken cancellationToken)
        => await _mesocycles.GetById(mesocycleId, cancellationToken)
            ?? throw new InvalidOperationException($"Mesocycle '{mesocycleId}' referenced by a workout does not exist");

    // Foreign keys keep every referenced user in place, so a missing one is a data-integrity failure
    private static User Find(Dictionary<Id, User> users, Id userId)
        => users.TryGetValue(userId, out var user)
            ? user
            : throw new InvalidOperationException($"User '{userId}' referenced by a workout does not exist");
}
