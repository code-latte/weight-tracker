using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Users;

namespace WeightTracker.Infra.Services;

/// <summary>
/// Issues HS256 access tokens and opaque refresh tokens.
/// </summary>
public class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly IClock _clock;

    public JwtTokenService(IOptions<JwtOptions> options, IClock clock)
    {
        _options = options.Value;
        _clock = clock;
    }

    public GeneratedAccessToken GenerateAccessToken(User user)
    {
        var expiresInSeconds = _options.AccessTokenMinutes * 60;
        var issuedAt = _clock.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = issuedAt.AddMinutes(_options.AccessTokenMinutes),
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email.Value),
                // ClaimTypes.Role so [Authorize(Roles = ...)] resolves without extra mapping
                new Claim(ClaimTypes.Role, user.Role.ToString()),
            ]),
            SigningCredentials = new SigningCredentials(SigningKey(), SecurityAlgorithms.HmacSha256),
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        return new GeneratedAccessToken(token, expiresInSeconds);
    }

    public GeneratedRefreshToken GenerateRefreshToken()
    {
        var rawValue = OpaqueToken.NewRawValue();

        return new GeneratedRefreshToken(
            rawValue,
            HashRefreshToken(rawValue),
            _clock.UtcNow.AddDays(_options.RefreshTokenDays));
    }

    public string HashRefreshToken(string rawRefreshToken) => OpaqueToken.Hash(rawRefreshToken);

    private SymmetricSecurityKey SigningKey()
        => new(Encoding.UTF8.GetBytes(_options.SigningKey));
}
