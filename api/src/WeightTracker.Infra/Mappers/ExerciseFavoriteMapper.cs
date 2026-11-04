using WeightTracker.Domain.Common;
using WeightTracker.Domain.ExerciseFavorites;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Exceptions;

namespace WeightTracker.Infra.Mappers;

/// <summary>
/// Maps the ExerciseFavorite aggregate to its single table, <c>FavoriteExercises</c>.
/// </summary>
public static class ExerciseFavoriteMapper
{
    public static ExerciseFavoriteEntity MapToEntity(ExerciseFavorite favorite)
    {
        return new ExerciseFavoriteEntity
        {
            Id = favorite.Id.Value,
            UserId = favorite.UserId.Value,
            ExerciseId = favorite.ExerciseId.Value,
            CreatedAtUtc = DateTime.SpecifyKind(favorite.CreatedAtUtc, DateTimeKind.Utc),
        };
    }

    public static ExerciseFavorite MapToDomain(ExerciseFavoriteEntity entity)
    {
        try
        {
            return ExerciseFavorite.Rehydrate(
                Id.From(entity.Id),
                Id.From(entity.UserId),
                Id.From(entity.ExerciseId),
                entity.CreatedAtUtc);
        }
        catch (DomainException exception)
        {
            throw new PersistenceMappingException(
                $"Stored exercise favorite '{entity.Id}' could not be restored", exception);
        }
    }
}
