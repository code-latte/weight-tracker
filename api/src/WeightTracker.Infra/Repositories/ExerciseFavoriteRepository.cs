using Microsoft.EntityFrameworkCore;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.ExerciseFavorites;
using WeightTracker.Infra.Mappers;
using WeightTracker.Infra.Persistence;

namespace WeightTracker.Infra.Repositories;

public class ExerciseFavoriteRepository : IExerciseFavoriteRepository
{
    private readonly WeightTrackerDbContext _context;

    public ExerciseFavoriteRepository(WeightTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<ExerciseFavorite?> GetByUserAndExercise(Id userId, Id exerciseId, CancellationToken cancellationToken)
    {
        var entity = await _context.FavoriteExercises
            .AsNoTracking()
            .FirstOrDefaultAsync(
                favorite => favorite.UserId == userId.Value && favorite.ExerciseId == exerciseId.Value,
                cancellationToken);

        return entity is null ? null : ExerciseFavoriteMapper.MapToDomain(entity);
    }

    public async Task<IEnumerable<ExerciseFavorite>> GetAllByUser(Id userId, CancellationToken cancellationToken)
    {
        var entities = await _context.FavoriteExercises
            .AsNoTracking()
            .Where(favorite => favorite.UserId == userId.Value)
            .OrderByDescending(favorite => favorite.CreatedAtUtc)
            .ThenBy(favorite => favorite.Id)
            .ToListAsync(cancellationToken);

        return entities.Select(ExerciseFavoriteMapper.MapToDomain).ToList();
    }

    public async Task Save(ExerciseFavorite favorite, CancellationToken cancellationToken)
    {
        if (favorite.IsNew == false)
            return;

        await _context.FavoriteExercises.AddAsync(ExerciseFavoriteMapper.MapToEntity(favorite), cancellationToken);
    }

    public Task Remove(ExerciseFavorite favorite, CancellationToken cancellationToken)
    {
        _context.FavoriteExercises.Remove(ExerciseFavoriteMapper.MapToEntity(favorite));

        return Task.CompletedTask;
    }
}
