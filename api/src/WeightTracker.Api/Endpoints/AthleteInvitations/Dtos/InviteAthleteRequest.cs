using System.ComponentModel.DataAnnotations;

namespace WeightTracker.Api.Endpoints.AthleteInvitations.Dtos;

/// <summary>
/// Body of <c>POST /athlete-invitations</c>. The email format is enforced by the domain.
/// </summary>
public sealed record InviteAthleteRequest
{
    [Required]
    public string Email { get; init; } = string.Empty;
}
