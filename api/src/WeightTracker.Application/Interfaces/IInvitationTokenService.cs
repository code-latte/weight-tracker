namespace WeightTracker.Application.Interfaces;

/// <summary>
/// Issues the secret a trainer invitation is answered with.
/// </summary>
public interface IInvitationTokenService
{
    /// <summary>
    /// Generates a new token that expires the configured number of days after <paramref name="nowUtc"/>.
    /// </summary>
    GeneratedInvitationToken Generate(DateTime nowUtc);

    /// <summary>
    /// Hashes a raw invitation token so it can be matched against the stored hash.
    /// </summary>
    string Hash(string rawToken);
}

/// <param name="RawValue">Token handed to the trainer. Never persisted.</param>
/// <param name="Hash">Digest persisted in place of the raw value.</param>
/// <param name="ExpiresAtUtc">Absolute expiry of the invitation.</param>
public sealed record GeneratedInvitationToken(string RawValue, string Hash, DateTime ExpiresAtUtc);
