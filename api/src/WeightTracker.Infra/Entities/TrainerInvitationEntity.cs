namespace WeightTracker.Infra.Entities;

/// <summary>EF representation of the <c>TrainerInvitations</c> table.</summary>
public class TrainerInvitationEntity
{
    public Guid Id { get; set; }

    public Guid TrainerId { get; set; }

    public string InviteEmail { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    /// <summary>Digest of the token. The raw value never reaches the database.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>Stored as the integer value of the domain <c>TrainerInvitationStatus</c>.</summary>
    public int Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? AcceptedAtUtc { get; set; }
}
