using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Auth;

/// <summary>
/// A long lived credential allowing a user to obtain a new access token.
/// </summary>
/// <remarks>
/// Only the hash of the token is held. The raw value exists solely in the HTTP
/// response that issued it. State changes through system behaviour (rotation on
/// refresh), never through direct user editing.
/// </remarks>
public sealed class RefreshToken : AggregateRoot
{
    /// <summary>Length of a hex encoded SHA-256 digest. The RefreshTokens.TokenHash column must stay aligned.</summary>
    public const int TokenHashMaxLength = HashedTokens.MaxLength;

    public Id UserId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public Id? ReplacedByTokenId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private RefreshToken(
        Id id,
        Id userId,
        string tokenHash,
        DateTime expiresAt,
        DateTime? revokedAt,
        Id? replacedByTokenId,
        DateTime createdAt
    )
    {
        this.Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        RevokedAt = revokedAt;
        ReplacedByTokenId = replacedByTokenId;
        CreatedAt = createdAt;
    }

    public static RefreshToken Create(Id userId, string tokenHash, DateTime expiresAtUtc, DateTime createdAtUtc)
    {
        return new RefreshToken(Id.New(), userId, tokenHash, expiresAtUtc, null, null, createdAtUtc)
        {
            IsNew = true,
        };
    }

    public static RefreshToken Rehydrate(
        Id id,
        Id userId,
        string tokenHash,
        DateTime expiresAt,
        DateTime? revokedAt,
        Id? replacedByTokenId,
        DateTime createdAt
    )
    {
        return new RefreshToken(id, userId, tokenHash, expiresAt, revokedAt, replacedByTokenId, createdAt)
        {
            IsNew = false,
        };
    }

    /// <summary>
    /// A token can only be redeemed while it has not expired and has not been revoked.
    /// </summary>
    public bool IsActive(DateTime utcNow) => RevokedAt is null && ExpiresAt > utcNow;

    /// <summary>
    /// Revokes this token and links it to the token that replaced it.
    /// </summary>
    /// <remarks>
    /// Guards the act of revoking, not only the resulting state: revoking twice is
    /// a replay attempt and must be rejected even though the end state would match.
    /// </remarks>
    /// <exception cref="RefreshTokenAlreadyRevokedException">When the token was already revoked.</exception>
    public void Revoke(Id replacedByTokenId, DateTime utcNow)
    {
        if (RevokedAt is not null)
            throw new RefreshTokenAlreadyRevokedException(this.Id);

        RevokedAt = utcNow;
        ReplacedByTokenId = replacedByTokenId;
    }
}
