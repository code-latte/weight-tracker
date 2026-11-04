using WeightTracker.Domain.ExerciseFavorites;

namespace WeightTracker.Api.Endpoints.ExerciseFavorites.Dtos;

/// <summary>
/// An exercise the caller has marked as a favorite. It carries the exercise identifier only: the client already
/// holds the exercises from <c>GET /exercises</c>.
/// </summary>
/// <param name="ExerciseId">Identifier of the favorite exercise.</param>
/// <param name="CreatedAtUtc">When the exercise was marked as a favorite.</param>
public sealed record ExerciseFavoriteResponse(Guid ExerciseId, DateTime CreatedAtUtc)
{
    public static ExerciseFavoriteResponse From(ExerciseFavorite favorite)
        => new(favorite.ExerciseId.Value, favorite.CreatedAtUtc);
}
