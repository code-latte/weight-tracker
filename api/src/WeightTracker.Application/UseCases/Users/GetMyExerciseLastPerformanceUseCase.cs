using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Domain.Common;

namespace WeightTracker.Application.UseCases.Users;

/// <summary>
/// Reads what the caller did the last time they finished a workout with an exercise, so they can compare while they
/// train. Only the caller's own workouts are read, whoever prescribed them.
/// </summary>
/// <remarks>
/// The exercise is checked first, so an unknown or hidden exercise fails even when the caller has no record of it.
/// </remarks>
public class GetMyExerciseLastPerformanceUseCase
{
    private readonly IExerciseRepository _exercises;

    private readonly ExerciseVisibility _visibility;

    private readonly IExerciseLastPerformanceQuery _lastPerformance;

    public GetMyExerciseLastPerformanceUseCase(
        IExerciseRepository exercises,
        ExerciseVisibility visibility,
        IExerciseLastPerformanceQuery lastPerformance)
    {
        _exercises = exercises;
        _visibility = visibility;
        _lastPerformance = lastPerformance;
    }

    /// <returns>The last performance, or <c>null</c> when the caller never finished a workout with records for the exercise.</returns>
    /// <exception cref="ExerciseNotFoundException">When no exercise has the given id, or the caller may not see it.</exception>
    public async Task<ExerciseLastPerformanceView?> Execute(Id exerciseId, AuthenticatedUser caller, CancellationToken cancellationToken)
    {
        var exercise = await _exercises.GetById(exerciseId, cancellationToken)
            ?? throw new ExerciseNotFoundException(exerciseId);

        await _visibility.EnsureVisible(exercise, caller, cancellationToken);

        return await _lastPerformance.Get(caller.Id, exerciseId, cancellationToken);
    }
}
