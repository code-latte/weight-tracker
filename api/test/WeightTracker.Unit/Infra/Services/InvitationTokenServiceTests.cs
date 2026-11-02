using FluentAssertions;
using Microsoft.Extensions.Options;
using WeightTracker.Infra.Services;
using Xunit;

namespace WeightTracker.Unit.Infra.Services;

public class InvitationTokenServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc);

    private static InvitationTokenService AService(int expirationDays = 7)
        => new(Options.Create(new TrainerInvitationOptions { ExpirationDays = expirationDays }));

    [Fact]
    public void Generate_ShouldExpireTheConfiguredNumberOfDaysLater()
    {
        // Arrange
        var service = AService(expirationDays: 3);

        // Act
        var token = service.Generate(Now);

        // Assert
        token.ExpiresAtUtc.Should().Be(Now.AddDays(3));
    }

    [Fact]
    public void Generate_ShouldStoreAHashDifferentFromTheRawValue()
    {
        // Arrange
        var service = AService();

        // Act
        var token = service.Generate(Now);

        // Assert
        token.RawValue.Should().NotBeNullOrWhiteSpace();
        token.Hash.Should().NotBe(token.RawValue);
        token.Hash.Should().Be(service.Hash(token.RawValue));
        token.Hash.Length.Should().BeLessThanOrEqualTo(128);
    }

    [Fact]
    public void Generate_ShouldIssueADifferentTokenEachTime()
    {
        // Arrange
        var service = AService();
        var first = service.Generate(Now);

        // Act
        var second = service.Generate(Now);

        // Assert
        first.RawValue.Should().NotBe(second.RawValue);
    }

    [Fact]
    public void Hash_ShouldBeDeterministic()
    {
        // Arrange
        var service = AService();

        // Act
        var hash = service.Hash("raw-invitation-token");

        // Assert
        hash.Should().Be(service.Hash("raw-invitation-token"));
        hash.Should().NotBe(service.Hash("another-token"));
    }
}
