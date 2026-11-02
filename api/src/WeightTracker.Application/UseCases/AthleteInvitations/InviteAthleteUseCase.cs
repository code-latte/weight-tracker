using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.AthleteInvitations;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Application.UseCases.AthleteInvitations;

public class InviteAthleteUseCase
{
    private IUserRepository _users;

    private ITrainerAthleteRepository _trainerAthletes;

    private ITrainerInvitationRepository _invitations;

    private IInvitationTokenService _tokens;

    private IClock _clock;

    private IUnitOfWork _unitOfWork;

    public InviteAthleteUseCase(
        IUserRepository users,
        ITrainerAthleteRepository trainerAthletes,
        ITrainerInvitationRepository invitations,
        IInvitationTokenService tokens,
        IClock clock,
        IUnitOfWork unitOfWork
    )
    {
        _users = users;
        _trainerAthletes = trainerAthletes;
        _invitations = invitations;
        _tokens = tokens;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Invites an email to become the caller's athlete. A registered athlete is linked to the invitation by id;
    /// an unregistered email is invited without one.
    /// </summary>
    /// <returns>The pending invitation with its trainer and invitee, and its raw token.</returns>
    /// <exception cref="InvalidEmailException">When the email is malformed.</exception>
    /// <exception cref="UserNotFoundException">When the caller no longer exists.</exception>
    /// <exception cref="InvalidTrainerAthleteRoleException">
    /// When the caller is not an active trainer, or the email belongs to a trainer, an admin or a deactivated athlete.
    /// </exception>
    /// <exception cref="AthleteAlreadyHasTrainerException">When the invited athlete already has an active trainer.</exception>
    /// <exception cref="PendingInvitationAlreadyExistsException">When the caller already has a live invitation for that email.</exception>
    public async Task<CreatedInvitation> Execute(Id callerId, string email, CancellationToken cancellationToken)
    {
        var inviteEmail = Email.From(email);

        var trainer = await _users.GetById(callerId, cancellationToken)
            ?? throw new UserNotFoundException(callerId);

        if (trainer.CanBeTrainer() == false)
            throw new InvalidTrainerAthleteRoleException("Only an active trainer can invite athletes");

        var invited = await _users.GetByEmail(inviteEmail, cancellationToken);

        if (invited is not null)
        {
            if (invited.CanBeAthlete() == false)
                throw new InvalidTrainerAthleteRoleException("Only an active user with the USER role can be invited");

            if (await _trainerAthletes.ExistsActiveByUserId(invited.Id, cancellationToken))
                throw new AthleteAlreadyHasTrainerException(invited.Id);
        }

        var nowUtc = _clock.UtcNow;

        var pending = await _invitations.GetAllPendingByTrainerIdAndEmail(callerId, inviteEmail, cancellationToken);
        if (pending.Any(invitation => invitation.IsPendingAt(nowUtc)))
            throw new PendingInvitationAlreadyExistsException(inviteEmail);

        var token = _tokens.Generate(nowUtc);

        var created = TrainerInvitation.Create(callerId, inviteEmail, invited?.Id, token.Hash, nowUtc, token.ExpiresAtUtc);

        await _invitations.Save(created, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreatedInvitation(new InvitationView(created, trainer, invited), token.RawValue);
    }
}
