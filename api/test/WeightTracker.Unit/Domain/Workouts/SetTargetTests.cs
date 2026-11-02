using FluentAssertions;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Domain.Workouts;

public class SetTargetTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Reps_ShouldRejectAValueBelowOne(int value)
    {
        // Arrange & Act
        var act = () => new SetTarget.Reps(value);

        // Assert
        act.Should().Throw<InvalidSetPrescriptionException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Duration_ShouldRejectAValueBelowOne(int value)
    {
        // Arrange & Act
        var act = () => new SetTarget.Duration(value);

        // Assert
        act.Should().Throw<InvalidSetPrescriptionException>();
    }

    [Fact]
    public void Reps_ShouldAcceptAValueOfOne()
    {
        // Arrange & Act
        var target = new SetTarget.Reps(1);

        // Assert
        target.value.Should().Be(1);
    }

    [Fact]
    public void Duration_ShouldKeepTheSeconds()
    {
        // Arrange & Act
        var target = new SetTarget.Duration(40);

        // Assert
        target.value.Should().Be(40);
    }

    [Fact]
    public void MaxReps_ShouldCarryNoValue()
    {
        // Arrange & Act
        var target = new SetTarget.MaxReps();

        // Assert
        target.Should().Be(new SetTarget.MaxReps());
    }

    [Fact]
    public void RepsRange_ShouldKeepBothBounds()
    {
        // Arrange & Act
        var target = new SetTarget.RepsRange(8, 12);

        // Assert
        target.min.Should().Be(8);
        target.max.Should().Be(12);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void RepsRange_ShouldRejectAMinBelowOne(int min)
    {
        // Arrange & Act
        var act = () => new SetTarget.RepsRange(min, 12);

        // Assert
        act.Should().Throw<InvalidSetPrescriptionException>();
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(10, 8)]
    public void RepsRange_ShouldRejectAMaxThatIsNotAboveTheMin(int min, int max)
    {
        // Arrange & Act
        var act = () => new SetTarget.RepsRange(min, max);

        // Assert
        act.Should().Throw<InvalidSetPrescriptionException>();
    }

    [Fact]
    public void RepsRange_ShouldDescribeBothBounds()
    {
        // Arrange
        var target = new SetTarget.RepsRange(8, 12);

        // Act
        var description = target.ToString();

        // Assert
        description.Should().Be("8-12 reps.");
    }

    [Fact]
    public void Minutes_ShouldBeEqualWhenTheirValuesAreEqual()
    {
        // Arrange
        var first = new SetTarget.Minutes(TimeInMinutes.From(1.25m));
        var second = new SetTarget.Minutes(TimeInMinutes.From(1.25m));

        // Act
        var areEqual = first == second;

        // Assert
        areEqual.Should().BeTrue();
    }

    [Fact]
    public void Minutes_ShouldDifferFromAnotherValue()
    {
        // Arrange
        var first = new SetTarget.Minutes(TimeInMinutes.From(1.25m));
        var second = new SetTarget.Minutes(TimeInMinutes.From(1.5m));

        // Act
        var areEqual = first == second;

        // Assert
        areEqual.Should().BeFalse();
    }

    [Fact]
    public void Minutes_ShouldDescribeItselfInMinutes()
    {
        // Arrange
        var target = new SetTarget.Minutes(TimeInMinutes.From(30m));

        // Act
        var text = target.ToString();

        // Assert
        text.Should().Be("30 min.");
    }
}
