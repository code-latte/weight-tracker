using FluentAssertions;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Domain.Workouts;

public class WeightTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(22.75)]
    [InlineData(62.5)]
    [InlineData(9999.99)]
    public void From_ShouldAcceptAValueWithUpToTwoDecimalsInRange(double value)
    {
        // Arrange
        var kilograms = (decimal)value;

        // Act
        var result = Weight.From(kilograms);

        // Assert
        result.Value.Should().Be(kilograms);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-5)]
    [InlineData(10000)]
    public void From_ShouldRejectAValueOutOfRange(double value)
    {
        // Arrange
        var kilograms = (decimal)value;

        // Act
        var act = () => Weight.From(kilograms);

        // Assert
        act.Should().Throw<InvalidWeightException>();
    }

    [Theory]
    [InlineData(20.125)]
    [InlineData(0.001)]
    public void From_ShouldRejectMoreThanTwoDecimals(double value)
    {
        // Arrange
        var kilograms = (decimal)value;

        // Act
        var act = () => Weight.From(kilograms);

        // Assert
        act.Should().Throw<InvalidWeightException>();
    }
}
