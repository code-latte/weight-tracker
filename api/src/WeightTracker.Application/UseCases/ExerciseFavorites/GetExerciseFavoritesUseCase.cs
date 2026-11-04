using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.ExerciseFavorites;

namespace WeightTracker.Application.UseCases.ExerciseFavorites;

/// <summary>
/// Lists the favorite exercises of the caller. A caller only ever reads their own favorites, whatever their role,
/// so there is nothing to authorize, and a favorite stays listed whatever its exercise has become since.
/// </summary>
public class GetExerciseFavoritesUseCase
{
    private IExerciseFavoriteRepository _favorites;

    public GetExerciseFavoritesUseCase(
        IExerciseFavoriteRepository favorites
    )
    {
        _favorites = favorites;
    }

    /// <returns>The caller's favorites, newest first.</returns>
    public async Task<IReadOnlyList<ExerciseFavorite>> Execute(Id callerId, CancellationToken cancellationToken)
    {
        return (await _favorites.GetAllByUser(callerId, cancellationToken)).ToList();
    }
}
