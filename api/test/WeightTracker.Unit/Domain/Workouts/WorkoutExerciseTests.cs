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

    private static readonly DateTime TenOClock = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);

    private static SetResult Reps(int value) => new SetResult.Reps(RepCount.From(value));

    [Fact]
    public void SetRecordCreate_ShouldMarkTheSetRecordAsNewWithAFreshIdAndKeepItsValues()
    {
        // Arrange
        var reps = Reps(8);
        var weight = Weight.From(62.5m);

        // Act
        var setRecord = SetRecord.Create(reps, weight, TenOClock);

        // Assert
        setRecord.IsNew.Should().BeTrue();
        setRecord.Id.Should().NotBeNull();
        setRecord.Result.Should().Be(reps);
        setRecord.Weight.Should().Be(weight);
        setRecord.RegisteredAtUtc.Should().Be(TenOClock);
    }

    [Fact]
    public void SetRecordCreate_ShouldAcceptADurationWithoutWeight()
    {
        // Arrange
        var seconds = new SetResult.Duration(DurationInSeconds.From(45));

        // Act
        var setRecord = SetRecord.Create(seconds, null, TenOClock);

        // Assert
        setRecord.Result.Should().Be(seconds);
        setRecord.Weight.Should().BeNull();
    }

    [Fact]
    public void SetRecordRehydrate_ShouldRestoreTheStoredValuesWithoutMarkingItAsNew()
    {
        // Arrange
        var id = Id.New();

        // Act
        var setRecord = SetRecord.Rehydrate(id, Reps(12), Weight.From(0), TenOClock);

        // Assert
        setRecord.IsNew.Should().BeFalse();
        setRecord.Id.Should().Be(id);
        setRecord.Result.Should().Be(Reps(12));
        setRecord.Weight.Should().Be(Weight.From(0));
        setRecord.RegisteredAtUtc.Should().Be(TenOClock);
    }

    [Fact]
    public void Create_ShouldStartWithoutSetRecords()
    {
        // Arrange
        var exerciseId = Id.New();

        // Act
        var entry = WorkoutExercise.Create(exerciseId, Effort.MID, [TwoByTen]);

        // Assert
        entry.SetRecords.Should().BeEmpty();
    }

    [Fact]
    public void Rehydrate_ShouldRestoreStoredSetRecordsInTheGivenOrder()
    {
        // Arrange
        var atTenOFive = SetRecord.Rehydrate(Id.New(), Reps(8), Weight.From(60), TenOClock.AddMinutes(5));
        var atTenOOne = SetRecord.Rehydrate(Id.New(), Reps(10), Weight.From(55), TenOClock.AddMinutes(1));
        var atTenOThree = SetRecord.Rehydrate(Id.New(), Reps(9), Weight.From(57.5m), TenOClock.AddMinutes(3));

        // Act
        var entry = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [TwoByTen], setRecords: [atTenOFive, atTenOOne, atTenOThree]);

        // Assert
        entry.SetRecords.Should().Equal(atTenOFive, atTenOOne, atTenOThree);
    }

    [Fact]
    public void Rehydrate_ShouldRestoreStoredSetRecordsWhateverThePrescriptions()
    {
        // Arrange
        var stored = SetRecord.Rehydrate(Id.New(), Reps(8), Weight.From(20), TenOClock);
        var duration = new SetPrescription(3, new SetTarget.Duration(40));

        // Act
        var entry = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [duration], setRecords: [stored]);

        // Assert
        entry.SetRecords.Should().Equal(stored);
    }

    [Fact]
    public void SetRecords_ShouldNotExposeTheInternalCollectionForMutation()
    {
        // Arrange
        var stored = SetRecord.Rehydrate(Id.New(), Reps(8), Weight.From(20), TenOClock);
        var entry = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [TwoByTen], setRecords: [stored]);

        // Act
        var snapshot = entry.SetRecords;

        // Assert
        snapshot.Should().NotBeSameAs(entry.SetRecords);
        snapshot.Should().BeAssignableTo<IReadOnlyList<SetRecord>>();
    }

    private static SetRecord Record(SetResult result, int minute)
        => SetRecord.Rehydrate(Id.New(), result, Weight.From(20), TenOClock.AddMinutes(minute));

    [Fact]
    public void SetsInTarget_ShouldBeZeroOfZeroWithoutSetRecords()
    {
        // Arrange
        var entry = WorkoutExercise.Create(Id.New(), Effort.MID, [TwoByTen]);

        // Act
        var sets = entry.SetsInTarget();

        // Assert
        sets.Should().Be((0, 0));
    }

    [Fact]
    public void SetsInTarget_ShouldCountTheSetsThatMeetTheirTarget()
    {
        // Arrange
        var entry = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [new SetPrescription(3, new SetTarget.Reps(10))],
            setRecords: [Record(Reps(10), 0), Record(Reps(9), 1), Record(Reps(12), 2)]);

        // Act
        var sets = entry.SetsInTarget();

        // Assert
        sets.Should().Be((2, 3));
    }

    [Fact]
    public void SetsInTarget_ShouldNotEvaluateRecordsBeyondThePrescribedSets()
    {
        // Arrange
        var entry = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [new SetPrescription(3, new SetTarget.Reps(10))],
            setRecords: [Record(Reps(10), 0), Record(Reps(10), 1), Record(Reps(10), 2), Record(Reps(10), 3)]);

        // Act
        var sets = entry.SetsInTarget();

        // Assert
        sets.Should().Be((3, 3));
    }

    [Fact]
    public void SetsInTarget_ShouldNotEvaluatePrescribedSetsWithoutARecord()
    {
        // Arrange
        var entry = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [new SetPrescription(4, new SetTarget.Reps(10))],
            setRecords: [Record(Reps(10), 0), Record(Reps(5), 1)]);

        // Act
        var sets = entry.SetsInTarget();

        // Assert
        sets.Should().Be((1, 2));
    }

    [Fact]
    public void SetsInTarget_ShouldMatchEachRecordToThePrescriptionAtItsPosition()
    {
        // Arrange
        var entry = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID,
            [new SetPrescription(1, new SetTarget.Reps(10)), new SetPrescription(2, new SetTarget.RepsRange(6, 8)), new SetPrescription(1, new SetTarget.MaxReps())],
            setRecords: [Record(Reps(10), 0), Record(Reps(6), 1), Record(Reps(5), 2), Record(Reps(2), 3)]);

        // Act
        var sets = entry.SetsInTarget();

        // Assert
        sets.Should().Be((3, 4));
    }
}
