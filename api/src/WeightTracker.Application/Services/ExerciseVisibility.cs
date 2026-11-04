using WeightTracker.Application.Exceptions;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.Services;

/// <summary>
/// Decides which exercises a caller may see. Every read, edit and workout addition that depends on it goes
/// through it, so the rule cannot drift between endpoints.
/// </summary>
/// <remarks>
/// A system exercise is visible to everyone, anonymous callers included. A custom exercise is visible to its
/// owner, to admins, and to any caller who can see a workout that uses it.
/// </remarks>
public class ExerciseVisibility
{
    private readonly WorkoutVisibility _workoutVisibility;

    public ExerciseVisibility(WorkoutVisibility workoutVisibility)
    {
        _workoutVisibility = workoutVisibility;
    }

    /// <param name="exercise">Exercise being asked about.</param>
    /// <param name="caller">The caller, or <c>null</c> for an anonymous request.</param>
    public Task<bool> IsVisible(Exercise exercise, AuthenticatedUser? caller, CancellationToken cancellationToken)
        => IsVisible(exercise, caller?.Id, caller?.Role, cancellationToken);

    /// <param name="exercise">Exercise being asked about.</param>
    /// <param name="callerId">Identifier of the caller, <c>null</c> for an anonymous request.</param>
    /// <param name="callerRole">Role of the caller, <c>null</c> for an anonymous request.</param>
    public async Task<bool> IsVisible(Exercise exercise, Id? callerId, Role? callerRole, CancellationToken cancellationToken)
    {
        if (exercise.IsSystem)
            return true;

        if (callerId is null || callerRole is null)
            return false;

        if (callerRole == Role.ADMIN || exercise.OwnerId == callerId)
            return true;

        var workouts = await _workoutVisibility.VisibleTo(callerId, callerRole.Value, cancellationToken);

        return workouts.Any(workout => workout.Exercises.Any(entry => entry.ExerciseId == exercise.Id));
    }

    /// <summary>
    /// Fails as if the exercise did not exist, so a caller who may not see it never learns that it does.
    /// </summary>
    /// <exception cref="ExerciseNotFoundException">When the caller may not see the exercise.</exception>
    public async Task EnsureVisible(Exercise exercise, AuthenticatedUser? caller, CancellationToken cancellationToken)
    {
        if (await IsVisible(exercise, caller, cancellationToken) == false)
            throw new ExerciseNotFoundException(exercise.Id);
    }

    /// <inheritdoc cref="EnsureVisible(Exercise, AuthenticatedUser?, CancellationToken)"/>
    public async Task EnsureVisible(Exercise exercise, Id callerId, Role callerRole, CancellationToken cancellationToken)
    {
        if (await IsVisible(exercise, callerId, callerRole, cancellationToken) == false)
            throw new ExerciseNotFoundException(exercise.Id);
    }
}
