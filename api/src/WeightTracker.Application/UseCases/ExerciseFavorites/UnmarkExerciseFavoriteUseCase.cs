using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.ExerciseFavorites;

public class UnmarkExerciseFavoriteUseCase
{
    private IExerciseRepository _exercises;

    private IExerciseFavoriteRepository _favorites;

    private ExerciseVisibility _visibility;

    private IUnitOfWork _unitOfWork;

    public UnmarkExerciseFavoriteUseCase(
        IExerciseRepository exercises,
        IExerciseFavoriteRepository favorites,
        ExerciseVisibility visibility,
        IUnitOfWork unitOfWork
    )
    {
        _exercises = exercises;
        _favorites = favorites;
        _visibility = visibility;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Removes an exercise from the favorites of the caller. Removing an exercise that is not a favorite does
    /// nothing. An archived exercise can still be removed.
    /// </summary>
    /// <exception cref="ExerciseNotFoundException">When the exercise does not exist, or is a custom exercise the caller cannot see.</exception>
    public async Task Execute(Id callerId, Role callerRole, Id exerciseId, CancellationToken cancellationToken)
    {
        var exercise = await _exercises.GetById(exerciseId, cancellationToken)
            ?? throw new ExerciseNotFoundException(exerciseId);

        await _visibility.EnsureVisible(exercise, callerId, callerRole, cancellationToken);

        var favorite = await _favorites.GetByUserAndExercise(callerId, exerciseId, cancellationToken);

        if (favorite is null)
            return;

        await _favorites.Remove(favorite, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
