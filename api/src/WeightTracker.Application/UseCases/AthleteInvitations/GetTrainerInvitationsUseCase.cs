using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.AthleteInvitations;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.AthleteInvitations;

/// <summary>
/// Lists the invitations the caller is entitled to see.
/// </summary>
/// <remarks>
/// The caller is passed explicitly instead of through an ambient accessor, so the
/// Application layer stays free of request-scoped infrastructure.
/// </remarks>
public class GetTrainerInvitationsUseCase
{
    private ITrainerInvitationRepository _invitations;

    private InvitationViewLoader _views;

    public GetTrainerInvitationsUseCase(ITrainerInvitationRepository invitations, InvitationViewLoader views)
    {
        _invitations = invitations;
        _views = views;
    }

    /// <summary>
    /// An administrator gets every invitation; a trainer only the ones they sent. Newest first, each with its trainer
    /// and invitee.
    /// </summary>
    /// <exception cref="UnauthorizedOperationException">When the caller is neither an administrator nor a trainer.</exception>
    public async Task<IReadOnlyList<InvitationView>> Execute(Id callerId, Role callerRole, CancellationToken cancellationToken)
    {
        var invitations = callerRole switch
        {
            Role.ADMIN => await _invitations.GetAll(cancellationToken),
            Role.TRAINER => await _invitations.GetAllByTrainerId(callerId, cancellationToken),
            _ => throw new UnauthorizedOperationException("GET /athlete-invitations"),
        };

        return await _views.Load(invitations, cancellationToken);
    }
}
