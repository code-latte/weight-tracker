using System.ComponentModel.DataAnnotations;

namespace WeightTracker.Api.Endpoints.AthleteInvitations.Dtos;

/// <summary>
/// Body of <c>PUT /athlete-invitations/{invitationId}/accept</c> and <c>/reject</c>: the raw token the trainer
/// received when creating the invitation.
/// </summary>
public sealed record InvitationTokenRequest
{
    [Required]
    public string Token { get; init; } = string.Empty;
}
