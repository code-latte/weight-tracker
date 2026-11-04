using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;

namespace WeightTracker.Application.UseCases.Exercises;

public class GetExerciseByIdUseCase
{
    private IExerciseRepository _exercises;

    private ExerciseVisibility _visibility;

    public GetExerciseByIdUseCase(
        IExerciseRepository exercises,
        ExerciseVisibility visibility
    )
    {
        _exercises = exercises;
        _visibility = visibility;
    }

    /// <param name="caller">The caller, or <c>null</c> for an anonymous request.</param>
    /// <exception cref="ExerciseNotFoundException">When no exercise has the given id, or the caller may not see it.</exception>
    public async Task<Exercise> Execute(Id id, AuthenticatedUser? caller, CancellationToken cancellationToken)
    {
        var exercise = await _exercises.GetById(id, cancellationToken);

        if (exercise is null)
            throw new ExerciseNotFoundException(id);

        await _visibility.EnsureVisible(exercise, caller, cancellationToken);

        return exercise;
    }
}
