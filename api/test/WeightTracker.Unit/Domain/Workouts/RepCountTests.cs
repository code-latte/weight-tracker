using FluentAssertions;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Domain.Workouts;

public class RepCountTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    [InlineData(100)]
    public void From_ShouldAcceptAtLeastOneRep(int value)
    {
        // Arrange
        var reps = value;

        // Act
        var result = RepCount.From(reps);

        // Assert
        result.Value.Should().Be(reps);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void From_ShouldRejectFewerThanOneRep(int value)
    {
        // Arrange
        var reps = value;

        // Act
        var act = () => RepCount.From(reps);

        // Assert
        act.Should().Throw<InvalidRepCountException>();
    }
}
