using WeightTracker.Domain.AthleteInvitations;
using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Interfaces;

public interface ITrainerInvitationRepository
{

    /// <summary>
    /// Every invitation of every trainer, in any status, newest first.
    /// </summary>
    public Task<IEnumerable<TrainerInvitation>> GetAll(CancellationToken cancellationToken);

    /// <summary>
    /// Every invitation the trainer sent, in any status, newest first.
    /// </summary>
    public Task<IEnumerable<TrainerInvitation>> GetAllByTrainerId(Id trainerId, CancellationToken cancellationToken);

    public Task<TrainerInvitation?> GetById(Id invitationId, CancellationToken cancellationToken);

    /// <summary>
    /// Invitations from this trainer to this email still in <see cref="TrainerInvitationStatus.PENDING"/>, expired
    /// or not. Whether one is still actionable is the aggregate's call.
    /// </summary>
    public Task<IEnumerable<TrainerInvitation>> GetAllPendingByTrainerIdAndEmail(
        Id trainerId, Email inviteEmail, CancellationToken cancellationToken);

    public Task Save(TrainerInvitation invitation, CancellationToken cancellationToken);

}
