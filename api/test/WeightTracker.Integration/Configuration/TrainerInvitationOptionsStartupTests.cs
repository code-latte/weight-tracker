using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Configuration;

/// <summary>
/// The API refuses to start with an invitation lifetime that would make every invitation invalid.
/// </summary>
public class TrainerInvitationOptionsStartupTests : IntegrationTest
{
    public TrainerInvitationOptionsStartupTests(ApiFactory factory) : base(factory)
    { }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Startup_ShouldFailWithAnExpirationBelowOneDay(string expirationDays)
    {
        // Arrange
        var misconfigured = Factory.WithWebHostBuilder(builder =>
            builder.UseSetting("TrainerInvitations:ExpirationDays", expirationDays));

        // Act
        var act = () => misconfigured.CreateClient();

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*TrainerInvitations:ExpirationDays*");
    }
}
