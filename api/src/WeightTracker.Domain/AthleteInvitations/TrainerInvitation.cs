using System.Security.Cryptography;
using System.Text;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.AthleteInvitations;

/// <summary>
/// A trainer's request for an email address to become their athlete.
/// </summary>
/// <remarks>
/// Only the hash of the invitation token is held. The raw value exists solely in the HTTP response that created the
/// invitation. Expiry is not a stored status: an invitation past <see cref="ExpiresAtUtc"/> stays
/// <see cref="TrainerInvitationStatus.PENDING"/> but can no longer be acted on.
/// </remarks>
public sealed class TrainerInvitation : AggregateRoot
{
    /// <summary>Length of a hex encoded SHA-256 digest. The TrainerInvitations.TokenHash column must stay aligned.</summary>
    public const int TokenHashMaxLength = HashedTokens.MaxLength;

    public Id TrainerId { get; private set; }

    public Email InviteEmail { get; private set; }

    /// <summary>
    /// The invited user when the email was registered as an athlete at invitation time; otherwise null.
    /// </summary>
    public Id? UserId { get; private set; }

    public string TokenHash { get; private set; }

    public TrainerInvitationStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? AcceptedAtUtc { get; private set; }

    private TrainerInvitation(
        Id id,
        Id trainerId,
        Email inviteEmail,
        Id? userId,
        string tokenHash,
        TrainerInvitationStatus status,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        DateTime? acceptedAtUtc
    )
    {
        this.Id = id;
        TrainerId = trainerId;
        InviteEmail = inviteEmail;
        UserId = userId;
        TokenHash = tokenHash;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        AcceptedAtUtc = acceptedAtUtc;
    }

    /// <summary>
    /// Creates a pending invitation.
    /// </summary>
    /// <param name="tokenHash">Digest of the raw token, already hashed by the infrastructure.</param>
    /// <exception cref="InvalidTrainerInvitationStateException">
    /// When the hash is empty or too long, or the invitation would not expire after it is created.
    /// </exception>
    public static TrainerInvitation Create(
        Id trainerId,
        Email inviteEmail,
        Id? userId,
        string tokenHash,
        DateTime nowUtc,
        DateTime expiresAtUtc
    )
    {
        var id = Id.New();

        EnsureValidTokenHash(id, tokenHash);
        EnsureExpiresAfterCreation(id, nowUtc, expiresAtUtc);

        return new TrainerInvitation(
            id,
            trainerId,
            inviteEmail,
            userId,
            tokenHash,
            TrainerInvitationStatus.PENDING,
            nowUtc,
            expiresAtUtc,
            null)
        {
            IsNew = true,
        };
    }

    /// <exception cref="InvalidTrainerInvitationStateException">
    /// When the hash is empty or too long, the expiry is not after the creation, or <paramref name="acceptedAtUtc"/>
    /// is set exactly when the invitation is not accepted.
    /// </exception>
    public static TrainerInvitation Rehydrate(
        Id id,
        Id trainerId,
        Email inviteEmail,
        Id? userId,
        string tokenHash,
        TrainerInvitationStatus status,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        DateTime? acceptedAtUtc
    )
    {
        EnsureValidTokenHash(id, tokenHash);
        EnsureExpiresAfterCreation(id, createdAtUtc, expiresAtUtc);

        if ((status == TrainerInvitationStatus.ACCEPTED) != (acceptedAtUtc is not null))
            throw new InvalidTrainerInvitationStateException(id, "it must have an acceptance date exactly when accepted");

        return new TrainerInvitation(
            id,
            trainerId,
            inviteEmail,
            userId,
            tokenHash,
            status,
            createdAtUtc,
            expiresAtUtc,
            acceptedAtUtc)
        {
            IsNew = false,
        };
    }

    /// <summary>
    /// True while the invitation is pending and has not expired, the only time it can be acted on.
    /// </summary>
    public bool IsPendingAt(DateTime nowUtc) => Status == TrainerInvitationStatus.PENDING && nowUtc < ExpiresAtUtc;

    /// <summary>
    /// Whether the given digest is the one stored for this invitation. Compared in constant time.
    /// </summary>
    public bool MatchesToken(string tokenHash)
        => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(TokenHash), Encoding.UTF8.GetBytes(tokenHash));

    /// <exception cref="InvalidTrainerInvitationStatusTransitionException">When the invitation is not pending.</exception>
    /// <exception cref="TrainerInvitationExpiredException">When the invitation has expired.</exception>
    public void Revoke(DateTime nowUtc)
    {
        EnsureCanMoveTo(TrainerInvitationStatus.REVOKED, nowUtc);

        Status = TrainerInvitationStatus.REVOKED;
    }

    /// <exception cref="InvalidTrainerInvitationStatusTransitionException">When the invitation is not pending.</exception>
    /// <exception cref="TrainerInvitationExpiredException">When the invitation has expired.</exception>
    public void Accept(DateTime nowUtc)
    {
        EnsureCanMoveTo(TrainerInvitationStatus.ACCEPTED, nowUtc);

        Status = TrainerInvitationStatus.ACCEPTED;
        AcceptedAtUtc = nowUtc;
    }

    /// <exception cref="InvalidTrainerInvitationStatusTransitionException">When the invitation is not pending.</exception>
    /// <exception cref="TrainerInvitationExpiredException">When the invitation has expired.</exception>
    public void Reject(DateTime nowUtc)
    {
        EnsureCanMoveTo(TrainerInvitationStatus.REJECTED, nowUtc);

        Status = TrainerInvitationStatus.REJECTED;
    }

    private static void EnsureValidTokenHash(Id id, string tokenHash)
    {
        if (string.IsNullOrWhiteSpace(tokenHash) || tokenHash.Length > TokenHashMaxLength)
            throw new InvalidTrainerInvitationStateException(
                id, $"its token hash must be non-empty and at most {TokenHashMaxLength} characters");
    }

    private static void EnsureExpiresAfterCreation(Id id, DateTime createdAtUtc, DateTime expiresAtUtc)
    {
        if (expiresAtUtc <= createdAtUtc)
            throw new InvalidTrainerInvitationStateException(id, "it must expire after it is created");
    }

    private void EnsureCanMoveTo(TrainerInvitationStatus target, DateTime nowUtc)
    {
        if (Status != TrainerInvitationStatus.PENDING)
            throw new InvalidTrainerInvitationStatusTransitionException(Status, target);

        if (nowUtc >= ExpiresAtUtc)
            throw new TrainerInvitationExpiredException(Id);
    }
}
