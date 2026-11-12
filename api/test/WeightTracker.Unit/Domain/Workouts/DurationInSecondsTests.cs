using FluentAssertions;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Domain.Workouts;

public class DurationInSecondsTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(45)]
    [InlineData(1800)]
    public void From_ShouldAcceptAtLeastOneSecond(int value)
    {
        // Arrange
        var seconds = value;

        // Act
        var result = DurationInSeconds.From(seconds);

        // Assert
        result.Value.Should().Be(seconds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void From_ShouldRejectFewerThanOneSecond(int value)
    {
        // Arrange
        var seconds = value;

        // Act
        var act = () => DurationInSeconds.From(seconds);

        // Assert
        act.Should().Throw<InvalidDurationInSecondsException>();
    }
}
