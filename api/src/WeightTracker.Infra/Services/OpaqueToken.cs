using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace WeightTracker.Infra.Services;

/// <summary>
/// Generation and hashing shared by every opaque token stored only as a digest: refresh tokens and trainer
/// invitations.
/// </summary>
internal static class OpaqueToken
{
    /// <summary>Entropy of a token, in bytes.</summary>
    private const int TokenBytes = 32;

    /// <summary>A new random token, URL safe. Never persisted.</summary>
    public static string NewRawValue() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(TokenBytes));

    /// <summary>Lowercase hex SHA-256 digest of a raw token, the only form that is stored.</summary>
    public static string Hash(string rawValue)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawValue)));
}
