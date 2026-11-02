using Microsoft.EntityFrameworkCore;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.TrainerAthletes;
using WeightTracker.Infra.Mappers;
using WeightTracker.Infra.Persistence;

namespace WeightTracker.Infra.Repositories;

public class TrainerAthleteRepository : ITrainerAthleteRepository
{
    private const int ActiveStatus = (int)TrainerAthleteStatus.ACTIVE;

    private readonly WeightTrackerDbContext _context;

    public TrainerAthleteRepository(WeightTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<TrainerAthlete?> GetActiveByTrainerIdAndUserId(Id trainerId, Id userId, CancellationToken cancellationToken)
    {
        var entity = await _context.TrainerAthletes.FirstOrDefaultAsync(
            relationship => relationship.TrainerId == trainerId.Value
                && relationship.UserId == userId.Value
                && relationship.Status == ActiveStatus,
            cancellationToken);

        return entity is null ? null : TrainerAthleteMapper.MapToDomain(entity);
    }

    public Task<bool> ExistsActiveByUserId(Id userId, CancellationToken cancellationToken)
        => _context.TrainerAthletes.AnyAsync(
            relationship => relationship.UserId == userId.Value && relationship.Status == ActiveStatus,
            cancellationToken);

    public async Task<IEnumerable<TrainerAthlete>> GetAllActiveByTrainerId(Id trainerId, CancellationToken cancellationToken)
    {
        var entities = await _context.TrainerAthletes
            .AsNoTracking()
            .Where(relationship => relationship.TrainerId == trainerId.Value && relationship.Status == ActiveStatus)
            .ToListAsync(cancellationToken);

        return entities.Select(TrainerAthleteMapper.MapToDomain).ToList();
    }

    public async Task<IEnumerable<TrainerAthlete>> GetAllActiveByUserIds(IReadOnlyCollection<Id> userIds, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
            return [];

        var ids = userIds.Select(id => id.Value).ToList();

        var entities = await _context.TrainerAthletes
            .AsNoTracking()
            .Where(relationship => ids.Contains(relationship.UserId) && relationship.Status == ActiveStatus)
            .ToListAsync(cancellationToken);

        return entities.Select(TrainerAthleteMapper.MapToDomain).ToList();
    }

    public async Task Save(TrainerAthlete relationship, CancellationToken cancellationToken)
    {
        var entity = TrainerAthleteMapper.MapToEntity(relationship);

        if (relationship.IsNew)
        {
            await _context.TrainerAthletes.AddAsync(entity, cancellationToken);
            return;
        }

        var tracked = await _context.TrainerAthletes.FirstOrDefaultAsync(stored => stored.Id == entity.Id, cancellationToken);

        if (tracked is null)
        {
            _context.TrainerAthletes.Update(entity);
            return;
        }

        // Ending only ever changes the status and the end date
        tracked.Status = entity.Status;
        tracked.FinishedAtUtc = entity.FinishedAtUtc;
    }
}
