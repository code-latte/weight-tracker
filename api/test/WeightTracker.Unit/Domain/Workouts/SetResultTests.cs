using FluentAssertions;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Domain.Workouts;

public class SetResultTests
{
    [Fact]
    public void Reps_ShouldBeEqualForTheSameCount()
    {
        // Arrange
        var first = new SetResult.Reps(RepCount.From(10));
        var second = new SetResult.Reps(RepCount.From(10));

        // Act
        var equal = first == second;

        // Assert
        equal.Should().BeTrue();
    }

    [Fact]
    public void Duration_ShouldBeEqualForTheSameSeconds()
    {
        // Arrange
        var first = new SetResult.Duration(DurationInSeconds.From(45));
        var second = new SetResult.Duration(DurationInSeconds.From(45));

        // Act
        var equal = first == second;

        // Assert
        equal.Should().BeTrue();
    }

    [Fact]
    public void Reps_ShouldDifferForAnotherCount()
    {
        // Arrange
        var ten = new SetResult.Reps(RepCount.From(10));
        var eight = new SetResult.Reps(RepCount.From(8));

        // Act
        var equal = ten == eight;

        // Assert
        equal.Should().BeFalse();
    }

    [Fact]
    public void RepsAndDuration_ShouldNeverBeEqualEvenForTheSameNumber()
    {
        // Arrange
        SetResult reps = new SetResult.Reps(RepCount.From(45));
        SetResult duration = new SetResult.Duration(DurationInSeconds.From(45));

        // Act
        var equal = reps.Equals(duration);

        // Assert
        equal.Should().BeFalse();
    }
}
