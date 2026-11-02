namespace WeightTracker.Application.UseCases.AthleteInvitations;

/// <param name="View">The stored invitation with its trainer and invitee.</param>
/// <param name="Token">Raw token, handed to the trainer once. Never persisted.</param>
public sealed record CreatedInvitation(InvitationView View, string Token);
