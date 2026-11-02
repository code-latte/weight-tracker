using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Domain.Workouts;

public class WorkoutExerciseTests
{
    private static readonly SetPrescription TwoByTen = new(2, new SetTarget.Reps(10));
    private static readonly SetPrescription TwoByEight = new(2, new SetTarget.Reps(8));

    [Fact]
    public void Create_ShouldRejectAnEntryWithoutSets()
    {
        // Arrange
        var exerciseId = Id.New();

        // Act
        var act = () => WorkoutExercise.Create(exerciseId, Effort.MID, []);

        // Assert
        act.Should().Throw<WorkoutExerciseRequiresSetsException>();
    }

    [Fact]
    public void Create_ShouldMarkTheEntryAsNewWithAFreshId()
    {
        // Arrange
        var exerciseId = Id.New();

        // Act
        var entry = WorkoutExercise.Create(exerciseId, Effort.MID, [TwoByTen]);

        // Assert
        entry.IsNew.Should().BeTrue();
        entry.Id.Should().NotBe(exerciseId);
    }

    [Fact]
    public void Create_ShouldKeepTheExerciseEffortAndSetOrder()
    {
        // Arrange
        var exerciseId = Id.New();

        // Act
        var entry = WorkoutExercise.Create(exerciseId, Effort.MIDHIGH, [TwoByTen, TwoByEight]);

        // Assert
        entry.ExerciseId.Should().Be(exerciseId);
        entry.Effort.Should().Be(Effort.MIDHIGH);
        entry.Sets.Should().ContainInOrder(TwoByTen, TwoByEight);
    }

    [Fact]
    public void Create_ShouldTellApartTheSameSetsInADifferentOrder()
    {
        // Arrange
        var exerciseId = Id.New();
        var tenThenEight = WorkoutExercise.Create(exerciseId, Effort.MID, [TwoByTen, TwoByEight]);

        // Act
        var eightThenTen = WorkoutExercise.Create(exerciseId, Effort.MID, [TwoByEight, TwoByTen]);

        // Assert
        eightThenTen.Sets.Should().NotEqual(tenThenEight.Sets);
    }

    [Fact]
    public void Rehydrate_ShouldKeepTheIdAndNotMarkTheEntryAsNew()
    {
        // Arrange
        var id = Id.New();

        // Act
        var entry = WorkoutExercise.Rehydrate(id, Id.New(), Effort.LOW, [TwoByTen]);

        // Assert
        entry.Id.Should().Be(id);
        entry.IsNew.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldCopyTheGivenSets()
    {
        // Arrange
        var sets = new List<SetPrescription> { TwoByTen };
        var entry = WorkoutExercise.Create(Id.New(), Effort.MID, sets);

        // Act
        sets.Add(TwoByEight);

        // Assert
        entry.Sets.Should().ContainSingle().Which.Should().Be(TwoByTen);
    }

    [Fact]
    public void Sets_ShouldNotExposeTheInternalCollectionForMutation()
    {
        // Arrange
        var entry = WorkoutExercise.Create(Id.New(), Effort.MID, [TwoByTen]);
        var exposed = (IList<SetPrescription>)entry.Sets;

        // Act
        exposed.Add(TwoByEight);

        // Assert
        entry.Sets.Should().ContainSingle().Which.Should().Be(TwoByTen);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Create_ShouldRejectAnUndefinedEffort(int effort)
    {
        // Arrange
        var exerciseId = Id.New();

        // Act
        var act = () => WorkoutExercise.Create(exerciseId, (Effort)effort, [TwoByTen]);

        // Assert
        act.Should().Throw<InvalidEffortException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Rehydrate_ShouldRejectAnUndefinedEffort(int effort)
    {
        // Arrange
        var id = Id.New();

        // Act
        var act = () => WorkoutExercise.Rehydrate(id, Id.New(), (Effort)effort, [TwoByTen]);

        // Assert
        act.Should().Throw<InvalidEffortException>();
    }

    [Fact]
    public void TotalSets_ShouldSumTheCountOfEveryPrescription()
    {
        // Arrange
        var entry = WorkoutExercise.Create(Id.New(), Effort.MID, [TwoByTen, new SetPrescription(1, new SetTarget.MaxReps())]);

        // Act
        var total = entry.TotalSets;

        // Assert
        total.Should().Be(3);
    }

    [Fact]
    public void Create_ShouldStartWithoutASuperSeries()
    {
        // Arrange & Act
        var entry = WorkoutExercise.Create(Id.New(), Effort.MID, [TwoByTen]);

        // Assert
        entry.SuperSeriesId.Should().BeNull();
    }

    [Fact]
    public void Rehydrate_ShouldKeepTheSuperSeriesId()
    {
        // Arrange
        var superSeriesId = SuperSeriesId.From(2);

        // Act
        var entry = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [TwoByTen], superSeriesId);

        // Assert
        entry.SuperSeriesId.Should().Be(superSeriesId);
    }

    [Fact]
    public void Create_ShouldDefaultToNotPerSideAndNoRest()
    {
        // Arrange
        var exerciseId = Id.New();

        // Act
        var entry = WorkoutExercise.Create(exerciseId, Effort.MID, [TwoByTen]);

        // Assert
        entry.PerSide.Should().BeFalse();
        entry.Rest.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldKeepThePerSideFlagAndTheRest()
    {
        // Arrange
        var exerciseId = Id.New();
        var rest = TimeInMinutes.From(1.5m);

        // Act
        var entry = WorkoutExercise.Create(exerciseId, Effort.MID, [TwoByTen], perSide: true, rest: rest);

        // Assert
        entry.PerSide.Should().BeTrue();
        entry.Rest.Should().Be(rest);
    }

    [Fact]
    public void Rehydrate_ShouldRestoreThePerSideFlagAndTheRest()
    {
        // Arrange
        var rest = TimeInMinutes.From(2m);

        // Act
        var entry = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [TwoByTen], perSide: true, rest: rest);

        // Assert
        entry.PerSide.Should().BeTrue();
        entry.Rest.Should().Be(rest);
    }
}
