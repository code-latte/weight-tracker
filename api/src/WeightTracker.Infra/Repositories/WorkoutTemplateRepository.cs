using Microsoft.EntityFrameworkCore;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Mappers;
using WeightTracker.Infra.Persistence;

namespace WeightTracker.Infra.Repositories;

public class WorkoutTemplateRepository : IWorkoutTemplateRepository
{
    private readonly WeightTrackerDbContext _context;

    public WorkoutTemplateRepository(WeightTrackerDbContext context)
    {
        _context = context;
    }

    public Task<IEnumerable<WorkoutTemplate>> GetAll(CancellationToken cancellationToken)
        => Query(templates => templates, cancellationToken);

    public Task<IEnumerable<WorkoutTemplate>> GetAllByTrainerId(Id trainerId, CancellationToken cancellationToken)
        => Query(templates => templates.Where(template => template.TrainerId == trainerId.Value), cancellationToken);

    public async Task<WorkoutTemplate?> GetById(Id templateId, CancellationToken cancellationToken)
    {
        var entity = await WithAggregateTables(_context.WorkoutTemplates.AsNoTracking())
            .FirstOrDefaultAsync(template => template.Id == templateId.Value, cancellationToken);

        if (entity is null)
            return null;

        return WorkoutTemplateMapper.MapToDomain(entity);
    }

    public async Task Save(WorkoutTemplate template, CancellationToken cancellationToken)
    {
        var entity = WorkoutTemplateMapper.MapToEntity(template);

        if (template.IsNew)
        {
            await _context.WorkoutTemplates.AddAsync(entity, cancellationToken);
            return;
        }

        var tracked = await WithAggregateTables(_context.WorkoutTemplates)
            .FirstOrDefaultAsync(stored => stored.Id == entity.Id, cancellationToken);

        if (tracked is null)
        {
            await _context.WorkoutTemplates.AddAsync(entity, cancellationToken);
            return;
        }

        tracked.TrainerId = entity.TrainerId;
        tracked.Name = entity.Name;
        tracked.Description = entity.Description;

        Reconcile(tracked, entity);
    }

    /// <summary>
    /// Removes the stored root only: its exercises and sets go with it through the cascading foreign keys.
    /// The mapped graph is not used, because its set rows carry fresh ids that were never stored.
    /// </summary>
    public async Task Remove(WorkoutTemplate template, CancellationToken cancellationToken)
    {
        var tracked = await _context.WorkoutTemplates
            .FirstOrDefaultAsync(stored => stored.Id == template.Id.Value, cancellationToken);

        if (tracked is not null)
            _context.WorkoutTemplates.Remove(tracked);
    }

    /// <summary>
    /// Brings the persisted children of the aggregate in line with the incoming state, as
    /// <see cref="WorkoutRepository"/> does for workouts: entries are matched by identity and sets are replaced.
    /// </summary>
    private static void Reconcile(WorkoutTemplateEntity tracked, WorkoutTemplateEntity incoming)
    {
        var incomingById = incoming.Exercises.ToDictionary(item => item.Id);

        var dropped = tracked.Exercises
            .Where(item => incomingById.ContainsKey(item.Id) == false)
            .ToList();

        foreach (var item in dropped)
            tracked.Exercises.Remove(item);

        foreach (var incomingItem in incoming.Exercises)
        {
            var existing = tracked.Exercises
                .FirstOrDefault(item => item.Id == incomingItem.Id);

            if (existing is null)
            {
                tracked.Exercises.Add(incomingItem);
                continue;
            }

            existing.Position = incomingItem.Position;
            existing.Effort = incomingItem.Effort;
            existing.PerSide = incomingItem.PerSide;
            existing.RestMinutes = incomingItem.RestMinutes;
            existing.SuperSeriesId = incomingItem.SuperSeriesId;

            // Sets carry no identity outside the aggregate, so they are always replaced
            existing.Sets.Clear();

            foreach (var set in incomingItem.Sets)
            {
                set.WorkoutTemplateExerciseId = existing.Id;
                existing.Sets.Add(set);
            }
        }
    }

    private async Task<IEnumerable<WorkoutTemplate>> Query(
        Func<IQueryable<WorkoutTemplateEntity>, IQueryable<WorkoutTemplateEntity>> filter,
        CancellationToken cancellationToken)
    {
        var entities = await WithAggregateTables(filter(_context.WorkoutTemplates.AsNoTracking()))
            .OrderBy(template => template.Name)
            .ThenBy(template => template.Id)
            .ToListAsync(cancellationToken);

        return entities.Select(WorkoutTemplateMapper.MapToDomain).ToList();
    }

    private static IQueryable<WorkoutTemplateEntity> WithAggregateTables(IQueryable<WorkoutTemplateEntity> templates)
        => templates
            .Include(template => template.Exercises)
            .ThenInclude(templateExercise => templateExercise.Sets);
}
