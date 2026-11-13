using FluentAssertions;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Domain.Workouts;

public class TopSetComparerTests
{
    private static TopSetCandidate Reps(int reps, decimal? weight, int position)
        => new(new SetResult.Reps(RepCount.From(reps)), weight is null ? null : Weight.From(weight.Value), position);

    private static TopSetCandidate Seconds(int seconds, decimal? weight, int position)
        => new(new SetResult.Duration(DurationInSeconds.From(seconds)), weight is null ? null : Weight.From(weight.Value), position);

    [Fact]
    public void Best_ShouldPickTheHeaviestSet()
    {
        // Arrange
        var sets = new[] { Reps(10, 40m, 0), Reps(6, 50m, 1), Reps(8, 45m, 2) };

        // Act
        var best = TopSetComparer.Best(sets);

        // Assert
        best.Should().Be(sets[1]);
    }

    [Fact]
    public void Best_ShouldBreakAWeightTieWithMostReps()
    {
        // Arrange
        var sets = new[] { Reps(8, 50m, 0), Reps(10, 50m, 1) };

        // Act
        var best = TopSetComparer.Best(sets);

        // Assert
        best.Should().Be(sets[1]);
    }

    [Fact]
    public void Best_ShouldBreakARepsTieWithLongestDuration()
    {
        // Arrange
        var sets = new[] { Seconds(30, 10m, 0), Seconds(45, 10m, 1) };

        // Act
        var best = TopSetComparer.Best(sets);

        // Assert
        best.Should().Be(sets[1]);
    }

    [Fact]
    public void Best_ShouldBreakACompleteTieWithTheLowestPosition()
    {
        // Arrange
        var sets = new[] { Reps(8, 50m, 2), Reps(8, 50m, 0), Reps(8, 50m, 1) };

        // Act
        var best = TopSetComparer.Best(sets);

        // Assert
        best.Should().Be(sets[1]);
    }

    [Fact]
    public void Best_ShouldRankAMissingWeightBelowZero()
    {
        // Arrange
        var sets = new[] { Reps(15, null, 0), Reps(5, 0m, 1) };

        // Act
        var best = TopSetComparer.Best(sets);

        // Assert
        best.Should().Be(sets[1]);
    }

    [Fact]
    public void Best_ShouldPickByRepsWhenNoSetHasWeight()
    {
        // Arrange
        var sets = new[] { Reps(8, null, 0), Reps(12, null, 1) };

        // Act
        var best = TopSetComparer.Best(sets);

        // Assert
        best.Should().Be(sets[1]);
    }

    [Fact]
    public void Best_ShouldBeNullWithoutSets()
    {
        // Arrange
        var sets = Array.Empty<TopSetCandidate>();

        // Act
        var best = TopSetComparer.Best(sets);

        // Assert
        best.Should().BeNull();
    }
}
