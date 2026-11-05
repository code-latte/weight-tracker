using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using Xunit;

namespace WeightTracker.Unit.Domain.Common;

public class WeekNumberTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(52)]
    public void From_ShouldAcceptAPositiveValue(int value)
    {
        // Arrange
        var number = value;

        // Act
        var result = WeekNumber.From(number);

        // Assert
        result.Value.Should().Be(number);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void From_ShouldRejectAValueLowerThanOne(int value)
    {
        // Arrange
        var number = value;

        // Act
        var act = () => WeekNumber.From(number);

        // Assert
        act.Should().Throw<InvalidWeekNumberException>();
    }

    [Fact]
    public void From_ShouldCompareByValue()
    {
        // Arrange
        var first = WeekNumber.From(3);

        // Act
        var second = WeekNumber.From(3);

        // Assert
        second.Should().Be(first);
    }
}
