using Microsoft.EntityFrameworkCore;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Mappers;
using WeightTracker.Infra.Persistence;

namespace WeightTracker.Infra.Repositories;

public class ExerciseRepository : IExerciseRepository
{
    private readonly WeightTrackerDbContext _context;

    public ExerciseRepository(WeightTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Exercise>> GetAll(CancellationToken cancellationToken)
    {
        var entities = await WithAggregateTables(_context.Exercises.AsNoTracking()).ToListAsync(cancellationToken);

        return entities.Select(ExerciseMapper.MapToDomain).ToList();
    }

    public async Task<IEnumerable<Exercise>> GetAllByIds(IEnumerable<Id> exerciseIds, CancellationToken cancellationToken)
    {
        var ids = exerciseIds.Select(id => id.Value).Distinct().ToList();

        if (ids.Count == 0)
            return [];

        var entities = await WithAggregateTables(_context.Exercises.AsNoTracking())
            .Where(exercise => ids.Contains(exercise.Id))
            .ToListAsync(cancellationToken);

        return entities.Select(ExerciseMapper.MapToDomain).ToList();
    }

    public async Task<IEnumerable<Exercise>> GetSystemAndOwnedBy(Id? ownerId, CancellationToken cancellationToken)
    {
        var owner = ownerId?.Value;

        var entities = await WithAggregateTables(_context.Exercises.AsNoTracking())
            .Where(exercise => exercise.OwnerId == null || exercise.OwnerId == owner)
            .ToListAsync(cancellationToken);

        return entities.Select(ExerciseMapper.MapToDomain).ToList();
    }

    public async Task<Exercise?> GetById(Id exerciseId, CancellationToken cancellationToken)
    {
        var entity = await WithAggregateTables(_context.Exercises.AsNoTracking())
            .FirstOrDefaultAsync(exercise => exercise.Id == exerciseId.Value, cancellationToken);

        return entity is null ? null : ExerciseMapper.MapToDomain(entity);
    }

    public async Task Save(Exercise exercise, CancellationToken cancellationToken)
    {
        var entity = ExerciseMapper.MapToEntity(exercise);

        if (exercise.IsNew)
        {
            await _context.Exercises.AddAsync(entity, cancellationToken);
            return;
        }

        var tracked = await _context.Exercises
            .Include(stored => stored.Translations)
            .FirstOrDefaultAsync(stored => stored.Id == entity.Id, cancellationToken);

        if (tracked is null)
        {
            _context.Exercises.Update(entity);
            return;
        }

        tracked.VideoUrl = entity.VideoUrl;
        tracked.Muscle = entity.Muscle;
        tracked.Type = entity.Type;
        tracked.ArchivedAtUtc = entity.ArchivedAtUtc;

        Reconcile(tracked, entity);
    }

    /// <summary>
    /// Brings the persisted translations in line with the incoming ones, by identity. A translation replaced for
    /// the same language is a new entity: its old row is deleted and a new one inserted. EF orders that delete
    /// before the insert because <c>(ExerciseId, LanguageCode)</c> is a unique index in the model.
    /// </summary>
    private static void Reconcile(ExerciseEntity tracked, ExerciseEntity incoming)
    {
        var incomingById = incoming.Translations.ToDictionary(translation => translation.Id);

        foreach (var dropped in tracked.Translations.Where(translation => incomingById.ContainsKey(translation.Id) == false).ToList())
            tracked.Translations.Remove(dropped);

        foreach (var translation in incoming.Translations)
        {
            var existing = tracked.Translations.FirstOrDefault(stored => stored.Id == translation.Id);

            if (existing is null)
            {
                tracked.Translations.Add(translation);
                continue;
            }

            existing.LanguageCode = translation.LanguageCode;
            existing.Name = translation.Name;
        }
    }

    private static IQueryable<ExerciseEntity> WithAggregateTables(IQueryable<ExerciseEntity> exercises)
        => exercises.Include(exercise => exercise.Translations);
}
