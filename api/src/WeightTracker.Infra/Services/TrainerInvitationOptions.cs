namespace WeightTracker.Infra.Services;

/// <summary>
/// Settings of the <c>TrainerInvitations</c> configuration section.
/// </summary>
public class TrainerInvitationOptions
{
    public const string SectionName = "TrainerInvitations";

    /// <summary>An invitation must outlive the request that creates it.</summary>
    public const int MinimumExpirationDays = 1;

    /// <summary>Days an invitation can be answered before it expires.</summary>
    public int ExpirationDays { get; set; } = 7;
}
