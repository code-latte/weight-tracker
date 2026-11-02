using WeightTracker.Application.UseCases.AthleteInvitations;

namespace WeightTracker.Api.Endpoints.AthleteInvitations.Dtos;

/// <summary>
/// A new invitation with its raw <c>Token</c>. This is the only response that ever carries it. Carries the names of
/// the trainer and of the invited user, which are null when the email is not registered.
/// </summary>
public sealed record CreatedInvitationResponse(
    Guid Id,
    Guid TrainerId,
    string TrainerName,
    string TrainerSurname,
    string InviteEmail,
    Guid? UserId,
    string? UserName,
    string? UserSurname,
    string Status,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    string Token)
{
    public static CreatedInvitationResponse From(CreatedInvitation created)
    {
        var view = created.View;

        return new(
            view.Invitation.Id.Value, view.Invitation.TrainerId.Value, view.Trainer.Name.Value, view.Trainer.Surname.Value,
            view.Invitation.InviteEmail.Value, view.Invitation.UserId?.Value, view.Invitee?.Name.Value,
            view.Invitee?.Surname.Value, view.Invitation.Status.ToString(), view.Invitation.CreatedAtUtc,
            view.Invitation.ExpiresAtUtc, created.Token);
    }
}
