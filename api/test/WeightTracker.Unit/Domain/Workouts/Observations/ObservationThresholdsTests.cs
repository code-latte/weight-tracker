using FluentAssertions;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Workouts.Observations;
using Xunit;

namespace WeightTracker.Unit.Domain.Workouts.Observations;

public class ObservationThresholdsTests
{
    [Fact]
    public void Constructor_ShouldKeepTheGivenValues()
    {
        // Arrange
        // Act
        var thresholds = new ObservationThresholds(3, 4, 3, 70, 15, 5, 10);

        // Assert
        thresholds.RepeatedMissCount.Should().Be(3);
        thresholds.PlateauCount.Should().Be(4);
        thresholds.TrendCount.Should().Be(3);
        thresholds.SevereNonCompliancePercent.Should().Be(70);
        thresholds.SignificantDeclinePercent.Should().Be(15);
        thresholds.SignificantRecordPercent.Should().Be(5);
        thresholds.ExceptionalProgressionPercent.Should().Be(10);
    }

    [Theory]
    [InlineData(1, 4, 3)]
    [InlineData(3, 0, 3)]
    [InlineData(3, 4, -1)]
    public void Constructor_ShouldRejectACountBelowTwo(int repeatedMiss, int plateau, int trend)
    {
        // Arrange
        // Act
        var act = () => new ObservationThresholds(repeatedMiss, plateau, trend, 70, 15, 5, 10);

        // Assert
        act.Should().Throw<InvalidObservationThresholdsException>();
    }

    [Theory]
    [InlineData(0, 15, 5, 10)]
    [InlineData(100, 15, 5, 10)]
    [InlineData(70, 0, 5, 10)]
    [InlineData(70, 15, 100, 10)]
    [InlineData(70, 15, 5, -1)]
    public void Constructor_ShouldRejectAPercentOutsideZeroAndOneHundred(
        decimal severe, decimal decline, decimal record, decimal progression)
    {
        // Arrange
        // Act
        var act = () => new ObservationThresholds(3, 4, 3, severe, decline, record, progression);

        // Assert
        act.Should().Throw<InvalidObservationThresholdsException>();
    }

    [Fact]
    public void HistoryDepth_ShouldBeTheLongestStreakPlusTheMargin()
    {
        // Arrange
        var thresholds = new ObservationThresholds(3, 6, 3, 70, 15, 5, 10);

        // Act
        var depth = thresholds.HistoryDepth;

        // Assert
        depth.Should().Be(6 + ObservationThresholds.HistoryMargin);
    }
}
