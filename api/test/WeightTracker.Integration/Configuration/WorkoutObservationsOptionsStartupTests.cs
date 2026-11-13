using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Configuration;

/// <summary>
/// The API refuses to start when an observation threshold is missing or out of range, so the rules never run with
/// values nobody chose.
/// </summary>
public class WorkoutObservationsOptionsStartupTests : IntegrationTest
{
    public WorkoutObservationsOptionsStartupTests(ApiFactory factory) : base(factory)
    { }

    [Theory]
    [InlineData("TrendCount", "0")]
    [InlineData("RepeatedMissCount", "1")]
    [InlineData("PlateauCount", "-3")]
    [InlineData("SevereNonCompliancePercent", "0")]
    [InlineData("SignificantDeclinePercent", "100")]
    [InlineData("SignificantRecordPercent", "-5")]
    [InlineData("ExceptionalProgressionPercent", "150")]
    public void Startup_ShouldFailWithAnOutOfRangeThreshold(string name, string value)
    {
        // Arrange
        var misconfigured = Factory.WithWebHostBuilder(builder =>
            builder.UseSetting($"WorkoutObservations:{name}", value));

        // Act
        var act = () => misconfigured.CreateClient();

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*WorkoutObservations*");
    }

    [Fact]
    public void Startup_ShouldFailWithAMissingThreshold()
    {
        // Arrange
        var misconfigured = Factory.WithWebHostBuilder(builder =>
            builder.UseSetting("WorkoutObservations:TrendCount", null));

        // Act
        var act = () => misconfigured.CreateClient();

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*WorkoutObservations:TrendCount*");
    }

    [Fact]
    public void Startup_ShouldSucceedWithTheDefaultThresholds()
    {
        // Arrange
        // Act
        var act = () => Factory.CreateClient();

        // Assert
        act.Should().NotThrow();
    }
}
