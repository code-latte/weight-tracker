using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.AthleteInvitations;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Application.UseCases.AthleteInvitations;

public class RejectInvitationUseCase
{
    private ITrainerInvitationRepository _invitations;

    private InvitationViewLoader _views;

    private IInvitationTokenService _tokens;

    private IClock _clock;

    private IUnitOfWork _unitOfWork;

    public RejectInvitationUseCase(
        ITrainerInvitationRepository invitations,
        InvitationViewLoader views,
        IInvitationTokenService tokens,
        IClock clock,
        IUnitOfWork unitOfWork
    )
    {
        _invitations = invitations;
        _views = views;
        _tokens = tokens;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Rejects a pending invitation. Only the invited user may, holding its token; an invitation to an
    /// unregistered email has no invited user yet and cannot be answered. Who is asking is checked before the
    /// token, and the token before the state.
    /// </summary>
    /// <returns>The rejected invitation with its trainer and invitee.</returns>
    /// <exception cref="TrainerInvitationNotFoundException">When no invitation has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller is not the invited user.</exception>
    /// <exception cref="InvalidInvitationTokenException">When the token does not match the invitation.</exception>
    /// <exception cref="InvalidTrainerInvitationStatusTransitionException">When the invitation is no longer pending.</exception>
    /// <exception cref="TrainerInvitationExpiredException">When the invitation has expired.</exception>
    public async Task<InvitationView> Execute(
        Id callerId,
        Id invitationId,
        string rawToken,
        CancellationToken cancellationToken)
    {
        var invitation = await _invitations.GetById(invitationId, cancellationToken)
            ?? throw new TrainerInvitationNotFoundException(invitationId);

        if (invitation.UserId is null || invitation.UserId != callerId)
            throw new UnauthorizedOperationException("PUT /athlete-invitations/{invitationId}/reject");

        if (invitation.MatchesToken(_tokens.Hash(rawToken)) == false)
            throw new InvalidInvitationTokenException();

        var view = await _views.Load(invitation, cancellationToken);

        invitation.Reject(_clock.UtcNow);

        await _invitations.Save(invitation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return view;
    }
}
