using Microsoft.Extensions.Options;
using WeightTracker.Application.Interfaces;

namespace WeightTracker.Infra.Services;

/// <summary>
/// Issues opaque trainer invitation tokens, stored only as their SHA-256 digest.
/// </summary>
public class InvitationTokenService : IInvitationTokenService
{
    private readonly TrainerInvitationOptions _options;

    public InvitationTokenService(IOptions<TrainerInvitationOptions> options)
    {
        _options = options.Value;
    }

    public GeneratedInvitationToken Generate(DateTime nowUtc)
    {
        var rawValue = OpaqueToken.NewRawValue();

        return new GeneratedInvitationToken(rawValue, Hash(rawValue), nowUtc.AddDays(_options.ExpirationDays));
    }

    public string Hash(string rawToken) => OpaqueToken.Hash(rawToken);
}
