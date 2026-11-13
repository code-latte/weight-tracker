using FluentAssertions;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Domain.Workouts;

public class SetMetricsTests
{
    private static SetResult Reps(int reps) => new SetResult.Reps(RepCount.From(reps));

    private static SetResult Seconds(int seconds) => new SetResult.Duration(DurationInSeconds.From(seconds));

    [Fact]
    public void Volume_ShouldMultiplyRepsByWeight()
    {
        // Arrange
        var result = Reps(10);
        var weight = Weight.From(40m);

        // Act
        var volume = SetMetrics.Volume(result, weight);

        // Assert
        volume.Should().Be(400m);
    }

    [Fact]
    public void Volume_ShouldBeZeroForABodyweightSet()
    {
        // Arrange
        var result = Reps(12);
        var weight = Weight.From(0m);

        // Act
        var volume = SetMetrics.Volume(result, weight);

        // Assert
        volume.Should().Be(0m);
    }

    [Fact]
    public void Volume_ShouldBeNullWithoutWeight()
    {
        // Arrange
        var result = Reps(10);

        // Act
        var volume = SetMetrics.Volume(result, null);

        // Assert
        volume.Should().BeNull();
    }

    [Fact]
    public void Volume_ShouldBeNullForATimedSet()
    {
        // Arrange
        var result = Seconds(45);
        var weight = Weight.From(10m);

        // Act
        var volume = SetMetrics.Volume(result, weight);

        // Assert
        volume.Should().BeNull();
    }

    [Fact]
    public void EstimatedOneRepMax_ShouldApplyEpley()
    {
        // Arrange
        var result = Reps(10);
        var weight = Weight.From(60m);

        // Act
        var estimate = SetMetrics.EstimatedOneRepMax(result, weight);

        // Assert
        estimate.Should().Be(80m);
    }

    [Fact]
    public void EstimatedOneRepMax_ShouldApplyTheFormulaToASingleRep()
    {
        // Arrange
        var result = Reps(1);
        var weight = Weight.From(60m);

        // Act
        var estimate = SetMetrics.EstimatedOneRepMax(result, weight);

        // Assert
        estimate.Should().Be(62m);
    }

    [Fact]
    public void EstimatedOneRepMax_ShouldRoundToTwoDecimals()
    {
        // Arrange
        var result = Reps(7);
        var weight = Weight.From(52.5m);

        // Act
        var estimate = SetMetrics.EstimatedOneRepMax(result, weight);

        // Assert
        estimate.Should().Be(64.75m);
    }

    [Fact]
    public void EstimatedOneRepMax_ShouldRoundAMidpointAwayFromZero()
    {
        // Arrange
        var result = Reps(1);
        var weight = Weight.From(0.45m);

        // Act
        var estimate = SetMetrics.EstimatedOneRepMax(result, weight);

        // Assert
        estimate.Should().Be(0.47m);
    }

    [Fact]
    public void EstimatedOneRepMax_ShouldBeZeroForABodyweightSet()
    {
        // Arrange
        var result = Reps(12);
        var weight = Weight.From(0m);

        // Act
        var estimate = SetMetrics.EstimatedOneRepMax(result, weight);

        // Assert
        estimate.Should().Be(0m);
    }

    [Fact]
    public void EstimatedOneRepMax_ShouldBeNullWithoutWeight()
    {
        // Arrange
        var result = Reps(10);

        // Act
        var estimate = SetMetrics.EstimatedOneRepMax(result, null);

        // Assert
        estimate.Should().BeNull();
    }

    [Fact]
    public void EstimatedOneRepMax_ShouldBeNullForATimedSet()
    {
        // Arrange
        var result = Seconds(45);
        var weight = Weight.From(10m);

        // Act
        var estimate = SetMetrics.EstimatedOneRepMax(result, weight);

        // Assert
        estimate.Should().BeNull();
    }
}
