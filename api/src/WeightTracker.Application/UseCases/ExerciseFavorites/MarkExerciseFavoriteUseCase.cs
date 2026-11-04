using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.ExerciseFavorites;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.ExerciseFavorites;

public class MarkExerciseFavoriteUseCase
{
    private IExerciseRepository _exercises;

    private IExerciseFavoriteRepository _favorites;

    private ExerciseVisibility _visibility;

    private IClock _clock;

    private IUnitOfWork _unitOfWork;

    public MarkExerciseFavoriteUseCase(
        IExerciseRepository exercises,
        IExerciseFavoriteRepository favorites,
        ExerciseVisibility visibility,
        IClock clock,
        IUnitOfWork unitOfWork
    )
    {
        _exercises = exercises;
        _favorites = favorites;
        _visibility = visibility;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Marks an exercise as a favorite of the caller. Marking an exercise that already is one returns the stored
    /// favorite, with its original date, and stores nothing.
    /// </summary>
    /// <returns>The favorite, new or already stored.</returns>
    /// <exception cref="ExerciseNotFoundException">When the exercise does not exist, or is a custom exercise the caller cannot see.</exception>
    /// <exception cref="ExerciseArchivedException">When the exercise is archived.</exception>
    public async Task<ExerciseFavorite> Execute(Id callerId, Role callerRole, Id exerciseId, CancellationToken cancellationToken)
    {
        var exercise = await _exercises.GetById(exerciseId, cancellationToken)
            ?? throw new ExerciseNotFoundException(exerciseId);

        await _visibility.EnsureVisible(exercise, callerId, callerRole, cancellationToken);

        if (exercise.IsArchived)
            throw new ExerciseArchivedException(exercise.Id);

        var existing = await _favorites.GetByUserAndExercise(callerId, exerciseId, cancellationToken);

        if (existing is not null)
            return existing;

        var favorite = ExerciseFavorite.Create(callerId, exerciseId, _clock.UtcNow);

        await _favorites.Save(favorite, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return favorite;
    }
}
