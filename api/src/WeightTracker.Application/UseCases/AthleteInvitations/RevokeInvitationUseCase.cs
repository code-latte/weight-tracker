using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.AthleteInvitations;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Application.UseCases.AthleteInvitations;

public class RevokeInvitationUseCase
{
    private ITrainerInvitationRepository _invitations;

    private InvitationViewLoader _views;

    private IClock _clock;

    private IUnitOfWork _unitOfWork;

    public RevokeInvitationUseCase(
        ITrainerInvitationRepository invitations,
        InvitationViewLoader views,
        IClock clock,
        IUnitOfWork unitOfWork
    )
    {
        _invitations = invitations;
        _views = views;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Revokes a pending invitation. Only the trainer who sent it may, and that is checked before its state.
    /// </summary>
    /// <returns>The revoked invitation with its trainer and invitee.</returns>
    /// <exception cref="TrainerInvitationNotFoundException">When no invitation has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller did not send the invitation.</exception>
    /// <exception cref="InvalidTrainerInvitationStatusTransitionException">When the invitation is no longer pending.</exception>
    /// <exception cref="TrainerInvitationExpiredException">When the invitation has expired.</exception>
    public async Task<InvitationView> Execute(Id callerId, Id invitationId, CancellationToken cancellationToken)
    {
        var invitation = await _invitations.GetById(invitationId, cancellationToken)
            ?? throw new TrainerInvitationNotFoundException(invitationId);

        if (invitation.TrainerId != callerId)
            throw new UnauthorizedOperationException("PUT /athlete-invitations/{invitationId}/revoke");

        var view = await _views.Load(invitation, cancellationToken);

        invitation.Revoke(_clock.UtcNow);

        await _invitations.Save(invitation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return view;
    }
}
