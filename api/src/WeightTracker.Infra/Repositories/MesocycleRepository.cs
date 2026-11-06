using Microsoft.EntityFrameworkCore;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Mappers;
using WeightTracker.Infra.Persistence;

namespace WeightTracker.Infra.Repositories;

public class MesocycleRepository : IMesocycleRepository
{
    private readonly WeightTrackerDbContext _context;

    public MesocycleRepository(WeightTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<Mesocycle?> GetById(Id mesocycleId, CancellationToken cancellationToken)
    {
        var entity = await _context.Mesocycles
            .AsNoTracking()
            .Include(mesocycle => mesocycle.Weeks)
            .FirstOrDefaultAsync(mesocycle => mesocycle.Id == mesocycleId.Value, cancellationToken);

        if (entity is null)
            return null;

        return MesocycleMapper.MapToDomain(entity);
    }

    public Task<IEnumerable<Mesocycle>> GetAll(CancellationToken cancellationToken)
        => Query(mesocycles => mesocycles, cancellationToken);

    public Task<IEnumerable<Mesocycle>> GetAllByTrainerId(Id trainerId, CancellationToken cancellationToken)
        => Query(mesocycles => mesocycles.Where(mesocycle => mesocycle.TrainerId == trainerId.Value), cancellationToken);

    public Task<IEnumerable<Mesocycle>> GetAllByAthleteId(Id athleteId, CancellationToken cancellationToken)
        => Query(mesocycles => mesocycles.Where(mesocycle => mesocycle.AthleteId == athleteId.Value), cancellationToken);

    public Task<IEnumerable<Mesocycle>> GetAllByTrainerIdAndAthleteId(Id trainerId, Id athleteId, CancellationToken cancellationToken)
        => Query(mesocycles => mesocycles.Where(mesocycle =>
            mesocycle.TrainerId == trainerId.Value && mesocycle.AthleteId == athleteId.Value), cancellationToken);

    public async Task Save(Mesocycle mesocycle, CancellationToken cancellationToken)
    {
        var entity = MesocycleMapper.MapToEntity(mesocycle);

        if (mesocycle.IsNew)
        {
            await _context.Mesocycles.AddAsync(entity, cancellationToken);
            return;
        }

        var tracked = await _context.Mesocycles
            .Include(stored => stored.Weeks)
            .FirstOrDefaultAsync(stored => stored.Id == entity.Id, cancellationToken);

        if (tracked is null)
        {
            await _context.Mesocycles.AddAsync(entity, cancellationToken);
            return;
        }

        tracked.TrainerId = entity.TrainerId;
        tracked.AthleteId = entity.AthleteId;
        tracked.Name = entity.Name;
        tracked.Objective = entity.Objective;
        tracked.StartDate = entity.StartDate;
        tracked.DurationInWeeks = entity.DurationInWeeks;

        Reconcile(tracked, entity);
    }

    /// <summary>
    /// Brings the persisted weeks in line with the incoming state.
    /// </summary>
    /// <remarks>
    /// Weeks are reconciled by the identity of the <c>MesocycleWeek</c> entity, so a redefined week keeps its row.
    /// A week removed and defined again with the same number is a new entity: its old row is deleted and a new one
    /// inserted. <c>(MesocycleId, WeekNumber)</c> is a unique index in the EF model, so EF orders that delete before
    /// the insert within the same commit. Removals detach from the navigation so EF deletes them as orphans.
    /// </remarks>
    private static void Reconcile(MesocycleEntity tracked, MesocycleEntity incoming)
    {
        var incomingById = incoming.Weeks.ToDictionary(week => week.Id);

        var dropped = tracked.Weeks
            .Where(week => incomingById.ContainsKey(week.Id) == false)
            .ToList();

        foreach (var week in dropped)
            tracked.Weeks.Remove(week);

        foreach (var incomingWeek in incoming.Weeks)
        {
            var existing = tracked.Weeks.FirstOrDefault(week => week.Id == incomingWeek.Id);

            if (existing is null)
            {
                tracked.Weeks.Add(incomingWeek);
                continue;
            }

            existing.TrainingWeekType = incomingWeek.TrainingWeekType;
            existing.Objective = incomingWeek.Objective;
        }
    }

    private async Task<IEnumerable<Mesocycle>> Query(
        Func<IQueryable<MesocycleEntity>, IQueryable<MesocycleEntity>> filter,
        CancellationToken cancellationToken)
    {
        // Id breaks ties so mesocycles starting the same day keep a stable order
        var entities = await filter(_context.Mesocycles.AsNoTracking())
            .Include(mesocycle => mesocycle.Weeks)
            .OrderByDescending(mesocycle => mesocycle.StartDate)
            .ThenBy(mesocycle => mesocycle.Id)
            .ToListAsync(cancellationToken);

        return entities.Select(MesocycleMapper.MapToDomain).ToList();
    }
}
