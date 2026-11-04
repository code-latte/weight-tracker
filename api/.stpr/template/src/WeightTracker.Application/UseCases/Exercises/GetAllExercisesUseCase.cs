using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Exercises;

/// <summary>
/// Lists the exercises the caller is entitled to see.
/// </summary>
/// <remarks>
/// An anonymous caller sees the system exercises. An admin sees every exercise. Any other caller sees the system
/// exercises, their own, and the custom exercises used in the workouts they can see. Archived exercises are
/// included, so the client can still name them.
/// </remarks>
public class GetAllExercisesUseCase
{
    private IExerciseRepository _exercises;

    private WorkoutVisibility _workoutVisibility;

    public GetAllExercisesUseCase(
        IExerciseRepository exercises,
        WorkoutVisibility workoutVisibility
    )
    {
        _exercises = exercises;
        _workoutVisibility = workoutVisibility;
    }

    /// <param name="caller">The caller, or <c>null</c> for an anonymous request.</param>
    public async Task<IReadOnlyList<Exercise>> Execute(AuthenticatedUser? caller, CancellationToken cancellationToken)
    {
        if (caller is null)
            return (await _exercises.GetSystemAndOwnedBy(null, cancellationToken)).ToList();

        if (caller.Role == Role.ADMIN)
            return (await _exercises.GetAll(cancellationToken)).ToList();

        var visible = (await _exercises.GetSystemAndOwnedBy(caller.Id, cancellationToken)).ToList();

        // Only exercises referenced by the workouts the caller can see, so the join never widens visibility
        var workouts = await _workoutVisibility.VisibleTo(caller.Id, caller.Role, cancellationToken);

        var known = visible.Select(exercise => exercise.Id).ToHashSet();

        var usedElsewhere = workouts
            .SelectMany(workout => workout.Exercises)
            .Select(entry => entry.ExerciseId)
            .Where(exerciseId => known.Contains(exerciseId) == false)
            .Distinct()
            .ToList();

        if (usedElsewhere.Count > 0)
            visible.AddRange(await _exercises.GetAllByIds(usedElsewhere, cancellationToken));

        return visible;
    }
}
