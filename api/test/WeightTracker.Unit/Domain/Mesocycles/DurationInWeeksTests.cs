using FluentAssertions;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Mesocycles;
using Xunit;

namespace WeightTracker.Unit.Domain.Mesocycles;

public class DurationInWeeksTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(52)]
    public void From_ShouldAcceptAValueInRange(int value)
    {
        // Arrange
        var weeks = value;

        // Act
        var result = DurationInWeeks.From(weeks);

        // Assert
        result.Value.Should().Be(weeks);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(53)]
    public void From_ShouldRejectAValueOutOfRange(int value)
    {
        // Arrange
        var weeks = value;

        // Act
        var act = () => DurationInWeeks.From(weeks);

        // Assert
        act.Should().Throw<InvalidDurationInWeeksException>();
    }
}
