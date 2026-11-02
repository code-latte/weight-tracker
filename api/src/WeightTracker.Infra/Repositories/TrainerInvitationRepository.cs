using Microsoft.EntityFrameworkCore;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.AthleteInvitations;
using WeightTracker.Domain.Common;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Mappers;
using WeightTracker.Infra.Persistence;

namespace WeightTracker.Infra.Repositories;

public class TrainerInvitationRepository : ITrainerInvitationRepository
{
    private const int PendingStatus = (int)TrainerInvitationStatus.PENDING;

    private readonly WeightTrackerDbContext _context;

    public TrainerInvitationRepository(WeightTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TrainerInvitation>> GetAll(CancellationToken cancellationToken)
    {
        var entities = await NewestFirst(_context.TrainerInvitations.AsNoTracking())
            .ToListAsync(cancellationToken);

        return entities.Select(TrainerInvitationMapper.MapToDomain).ToList();
    }

    public async Task<IEnumerable<TrainerInvitation>> GetAllByTrainerId(Id trainerId, CancellationToken cancellationToken)
    {
        var entities = await NewestFirst(_context.TrainerInvitations.AsNoTracking()
                .Where(invitation => invitation.TrainerId == trainerId.Value))
            .ToListAsync(cancellationToken);

        return entities.Select(TrainerInvitationMapper.MapToDomain).ToList();
    }

    public async Task<TrainerInvitation?> GetById(Id invitationId, CancellationToken cancellationToken)
    {
        var entity = await _context.TrainerInvitations.FirstOrDefaultAsync(
            invitation => invitation.Id == invitationId.Value, cancellationToken);

        return entity is null ? null : TrainerInvitationMapper.MapToDomain(entity);
    }

    public async Task<IEnumerable<TrainerInvitation>> GetAllPendingByTrainerIdAndEmail(
        Id trainerId, Email inviteEmail, CancellationToken cancellationToken)
    {
        var entities = await _context.TrainerInvitations
            .AsNoTracking()
            .Where(invitation => invitation.TrainerId == trainerId.Value
                && invitation.InviteEmail == inviteEmail.Value
                && invitation.Status == PendingStatus)
            .ToListAsync(cancellationToken);

        return entities.Select(TrainerInvitationMapper.MapToDomain).ToList();
    }

    // Id breaks ties between invitations created at the same instant, so the order is stable
    private static IQueryable<TrainerInvitationEntity> NewestFirst(IQueryable<TrainerInvitationEntity> invitations)
        => invitations.OrderByDescending(invitation => invitation.CreatedAtUtc).ThenBy(invitation => invitation.Id);

    public async Task Save(TrainerInvitation invitation, CancellationToken cancellationToken)
    {
        var entity = TrainerInvitationMapper.MapToEntity(invitation);

        if (invitation.IsNew)
        {
            await _context.TrainerInvitations.AddAsync(entity, cancellationToken);
            return;
        }

        var tracked = await _context.TrainerInvitations.FirstOrDefaultAsync(stored => stored.Id == entity.Id, cancellationToken);

        if (tracked is null)
        {
            _context.TrainerInvitations.Update(entity);
            return;
        }

        // Revoking, accepting and rejecting only ever change the status and the acceptance date
        tracked.Status = entity.Status;
        tracked.AcceptedAtUtc = entity.AcceptedAtUtc;
    }
}
