using WeightTracker.Domain.Common;
using WeightTracker.Domain.ExerciseFavorites;

namespace WeightTracker.Application.Interfaces;

/// <summary>
/// Loads, saves and removes the ExerciseFavorite aggregate. None of the writes commits; the use case calls the
/// unit of work.
/// </summary>
public interface IExerciseFavoriteRepository
{
    /// <summary>
    /// The favorite a user has for an exercise, or null when the exercise is not one of their favorites.
    /// </summary>
    public Task<ExerciseFavorite?> GetByUserAndExercise(Id userId, Id exerciseId, CancellationToken cancellationToken);

    /// <summary>
    /// Every favorite of the user, newest first. Other users' favorites are never returned.
    /// </summary>
    public Task<IEnumerable<ExerciseFavorite>> GetAllByUser(Id userId, CancellationToken cancellationToken);

    /// <summary>
    /// Stages a new favorite. A favorite is never edited, so a stored one is left untouched.
    /// </summary>
    public Task Save(ExerciseFavorite favorite, CancellationToken cancellationToken);

    /// <summary>
    /// Stages the deletion of a favorite. Unlike the other aggregates, a favorite is deleted instead of changed:
    /// it has no history worth keeping.
    /// </summary>
    public Task Remove(ExerciseFavorite favorite, CancellationToken cancellationToken);
}
