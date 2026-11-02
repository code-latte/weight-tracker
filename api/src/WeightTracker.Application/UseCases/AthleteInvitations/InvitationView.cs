using WeightTracker.Domain.AthleteInvitations;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.AthleteInvitations;

/// <summary>
/// An invitation together with the trainer who sent it and the invited user, so callers can show their names without
/// looking them up.
/// </summary>
/// <param name="Invitee">The invited user; null when the invitation went to an unregistered email.</param>
public sealed record InvitationView(TrainerInvitation Invitation, User Trainer, User? Invitee);
