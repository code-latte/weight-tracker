using WeightTracker.Application.UseCases.AthleteInvitations;

namespace WeightTracker.Api.Endpoints.AthleteInvitations.Dtos;

/// <summary>
/// An invitation without its secret: neither the token nor its hash. Carries the names of the trainer and of the
/// invited user, which are null when the invitation went to an unregistered email.
/// </summary>
public sealed record InvitationResponse(
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
    DateTime? AcceptedAtUtc)
{
    public static InvitationResponse From(InvitationView view)
        => new(
            view.Invitation.Id.Value, view.Invitation.TrainerId.Value, view.Trainer.Name.Value, view.Trainer.Surname.Value,
            view.Invitation.InviteEmail.Value, view.Invitation.UserId?.Value, view.Invitee?.Name.Value,
            view.Invitee?.Surname.Value, view.Invitation.Status.ToString(), view.Invitation.CreatedAtUtc,
            view.Invitation.ExpiresAtUtc, view.Invitation.AcceptedAtUtc);
}
