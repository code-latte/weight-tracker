namespace WeightTracker.Domain.Common;

/// <summary>
/// Rules shared by every opaque token the system stores only as a digest (refresh tokens, trainer invitations).
/// </summary>
public static class HashedTokens
{
    /// <summary>Length of a hex encoded SHA-256 digest. Every token hash column must stay aligned.</summary>
    public const int MaxLength = 128;
}
