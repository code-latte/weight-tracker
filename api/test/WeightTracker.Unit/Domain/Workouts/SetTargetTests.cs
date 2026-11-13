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

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(11, true)]
    public void Reps_IsMetBy_ShouldRequireTheTargetRepsOrMore(int reps, bool expected)
    {
        // Arrange
        var target = new SetTarget.Reps(10);

        // Act
        var isMet = target.IsMetBy(new SetResult.Reps(RepCount.From(reps)));

        // Assert
        isMet.Should().Be(expected);
    }

    [Theory]
    [InlineData(7, false)]
    [InlineData(8, true)]
    [InlineData(12, true)]
    [InlineData(14, true)]
    public void RepsRange_IsMetBy_ShouldRequireTheMinimumRepsOrMore(int reps, bool expected)
    {
        // Arrange
        var target = new SetTarget.RepsRange(8, 12);

        // Act
        var isMet = target.IsMetBy(new SetResult.Reps(RepCount.From(reps)));

        // Assert
        isMet.Should().Be(expected);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    public void MaxReps_IsMetBy_ShouldAlwaysBeMetByARepsResult(int reps)
    {
        // Arrange
        var target = new SetTarget.MaxReps();

        // Act
        var isMet = target.IsMetBy(new SetResult.Reps(RepCount.From(reps)));

        // Assert
        isMet.Should().BeTrue();
    }

    [Theory]
    [InlineData(39, false)]
    [InlineData(40, true)]
    [InlineData(41, true)]
    public void Duration_IsMetBy_ShouldRequireTheTargetSecondsOrMore(int seconds, bool expected)
    {
        // Arrange
        var target = new SetTarget.Duration(40);

        // Act
        var isMet = target.IsMetBy(new SetResult.Duration(DurationInSeconds.From(seconds)));

        // Assert
        isMet.Should().Be(expected);
    }

    [Theory]
    [InlineData(89, false)]
    [InlineData(90, true)]
    [InlineData(91, true)]
    public void Minutes_IsMetBy_ShouldCompareInSeconds(int seconds, bool expected)
    {
        // Arrange
        var target = new SetTarget.Minutes(TimeInMinutes.From(1.5m));

        // Act
        var isMet = target.IsMetBy(new SetResult.Duration(DurationInSeconds.From(seconds)));

        // Assert
        isMet.Should().Be(expected);
    }

    [Fact]
    public void IsMetBy_ShouldNotBeMetByAResultOfAnotherKind()
    {
        // Arrange
        var repsResult = new SetResult.Reps(RepCount.From(50));
        var durationResult = new SetResult.Duration(DurationInSeconds.From(500));
        var timedTargets = new SetTarget[] { new SetTarget.Duration(10), new SetTarget.Minutes(TimeInMinutes.From(1m)) };
        var repsTargets = new SetTarget[] { new SetTarget.Reps(1), new SetTarget.RepsRange(1, 2), new SetTarget.MaxReps() };

        // Act
        var timedMetByReps = timedTargets.Select(target => target.IsMetBy(repsResult)).ToList();
        var repsMetByDuration = repsTargets.Select(target => target.IsMetBy(durationResult)).ToList();

        // Assert
        timedMetByReps.Should().AllBeEquivalentTo(false);
        repsMetByDuration.Should().AllBeEquivalentTo(false);
    }
}
