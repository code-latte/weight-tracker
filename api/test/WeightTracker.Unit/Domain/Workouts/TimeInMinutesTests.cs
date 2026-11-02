using FluentAssertions;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Domain.Workouts;

public class TimeInMinutesTests
{
    [Theory]
    [InlineData(0.01)]
    [InlineData(1.25)]
    [InlineData(30)]
    [InlineData(999.99)]
    public void From_ShouldAcceptAValueWithUpToTwoDecimalsInRange(double value)
    {
        // Arrange
        var minutes = (decimal)value;

        // Act
        var result = TimeInMinutes.From(minutes);

        // Assert
        result.Value.Should().Be(minutes);
    }

    [Fact]
    public void From_ShouldTreatTrailingZerosAsTheSameValue()
    {
        // Arrange
        var trailingZero = 1.250m;

        // Act
        var result = TimeInMinutes.From(trailingZero);

        // Assert
        result.Should().Be(TimeInMinutes.From(1.25m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000)]
    [InlineData(999.995)]
    public void From_ShouldRejectAValueOutOfRange(double value)
    {
        // Arrange
        var minutes = (decimal)value;

        // Act
        var act = () => TimeInMinutes.From(minutes);

        // Assert
        act.Should().Throw<InvalidTimeInMinutesException>();
    }

    [Theory]
    [InlineData(1.255)]
    [InlineData(1.333)]
    [InlineData(0.001)]
    public void From_ShouldRejectMoreThanTwoDecimals(double value)
    {
        // Arrange
        var minutes = (decimal)value;

        // Act
        var act = () => TimeInMinutes.From(minutes);

        // Assert
        act.Should().Throw<InvalidTimeInMinutesException>();
    }
}
