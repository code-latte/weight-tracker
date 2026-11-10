using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts;
using WeightTracker.Infra.Exceptions;
using WeightTracker.Infra.Mappers;
using Xunit;

namespace WeightTracker.Unit.Infra.Mappers;

public class WorkoutMapperTests
{
    private static readonly DateTime WorkoutDate = new(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc);
    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private static readonly Id SentadillaId = Id.New();

    private static readonly Id PlanchaId = Id.New();

    private static Workout AFullyLoadedWorkout(Id? trainerId = null)
        => Workout.Rehydrate(
            Id.New(), Id.New(), trainerId, Name, null, WorkoutDate, WorkoutStatus.DRAFT,
            [
                WorkoutExercise.Rehydrate(Id.New(), SentadillaId, Effort.MIDHIGH,
                [
                    new SetPrescription(5, new SetTarget.Reps(5)),
                    new SetPrescription(2, new SetTarget.MaxReps()),
                ]),
                WorkoutExercise.Rehydrate(Id.New(), PlanchaId, Effort.LOW, [new SetPrescription(3, new SetTarget.Duration(40))]),
            ]);

    private static Workout AWorkoutWithOneSet(SetTarget target)
        => Workout.Rehydrate(
            Id.New(), Id.New(), null, Name, null, WorkoutDate, WorkoutStatus.DRAFT,
            [WorkoutExercise.Rehydrate(Id.New(), SentadillaId, Effort.MID, [new SetPrescription(4, target)])]);

    private static Workout RoundTrip(Workout workout)
        => WorkoutMapper.MapToDomain(WorkoutMapper.MapToEntity(workout));

    [Fact]
    public void RoundTrip_ShouldPreserveIdentityAndOwnership()
    {
        // Arrange
        var trainerId = Id.New();
        var original = AFullyLoadedWorkout(trainerId);

        // Act
        var restored = RoundTrip(original);

        // Assert
        restored.Id.Should().Be(original.Id);
        restored.UserId.Should().Be(original.UserId);
        restored.TrainerId.Should().Be(trainerId);
        restored.WorkoutDate.Should().Be(original.WorkoutDate);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveANullTrainer()
    {
        // Arrange
        var workout = AFullyLoadedWorkout();

        // Act
        var restored = RoundTrip(workout);

        // Assert
        restored.TrainerId.Should().BeNull();
    }

    [Fact]
    public void RoundTrip_ShouldPreserveEachExerciseIdentityReferenceAndEffort()
    {
        // Arrange
        var original = AFullyLoadedWorkout();

        // Act
        var restored = RoundTrip(original);

        // Assert
        restored.Exercises.Select(entry => (entry.Id, entry.ExerciseId, entry.Effort)).Should().Equal(
            original.Exercises.Select(entry => (entry.Id, entry.ExerciseId, entry.Effort)));
    }

    [Fact]
    public void RoundTrip_ShouldPreserveSetOrdering()
    {
        // Arrange & Act
        var restored = RoundTrip(AFullyLoadedWorkout());

        // Assert
        restored.Exercises[0].Sets.Should().Equal(
            new SetPrescription(5, new SetTarget.Reps(5)),
            new SetPrescription(2, new SetTarget.MaxReps()));
    }

    [Fact]
    public void RoundTrip_ShouldPreserveExerciseOrdering()
    {
        // Arrange & Act
        var restored = RoundTrip(AFullyLoadedWorkout());

        // Assert
        restored.Exercises.Select(entry => entry.ExerciseId).Should().Equal(SentadillaId, PlanchaId);
    }

    [Theory]
    [MemberData(nameof(EverySetTargetVariant))]
    public void RoundTrip_ShouldPreserveEverySetTargetVariant(SetTarget target)
    {
        // Arrange
        var workout = AWorkoutWithOneSet(target);

        // Act
        var restored = RoundTrip(workout);

        // Assert
        restored.Exercises.Single().Sets.Single().target.Should().Be(target);
    }

    public static TheoryData<SetTarget> EverySetTargetVariant() =>
    [
        new SetTarget.Reps(10),
        new SetTarget.Duration(40),
        new SetTarget.MaxReps(),
        new SetTarget.RepsRange(8, 12),
    ];

    public static TheoryData<SetTarget> EveryNonRangeSetTarget() =>
    [
        new SetTarget.Reps(10),
        new SetTarget.Duration(40),
        new SetTarget.MaxReps(),
    ];

    [Fact]
    public void MapToEntity_ShouldUseTheEntryIdAsRowIdAndStoreTheEffort()
    {
        // Arrange
        var workout = AFullyLoadedWorkout();

        // Act
        var entity = WorkoutMapper.MapToEntity(workout);

        // Assert
        entity.WorkoutExercises.Select(item => item.Id).Should().Equal(workout.Exercises.Select(entry => entry.Id.Value));
        entity.WorkoutExercises.Select(item => item.Effort).Should().Equal((int)Effort.MIDHIGH, (int)Effort.LOW);
    }

    [Fact]
    public void MapToEntity_ShouldLeaveMaxRepsWithoutAValue()
    {
        // Arrange
        var workout = AWorkoutWithOneSet(new SetTarget.MaxReps());

        // Act
        var set = WorkoutMapper.MapToEntity(workout).WorkoutExercises.Single().Sets.Single();

        // Assert
        set.TargetType.Should().Be("MaxReps");
        set.TargetValue.Should().BeNull();
    }

    [Fact]
    public void MapToEntity_ShouldStoreBothBoundsOfARepsRange()
    {
        // Arrange
        var workout = AWorkoutWithOneSet(new SetTarget.RepsRange(8, 12));

        // Act
        var set = WorkoutMapper.MapToEntity(workout).WorkoutExercises.Single().Sets.Single();

        // Assert
        set.TargetType.Should().Be("RepsRange");
        set.TargetValue.Should().Be(8);
        set.TargetMaxValue.Should().Be(12);
    }

    [Theory]
    [MemberData(nameof(EveryNonRangeSetTarget))]
    public void MapToEntity_ShouldLeaveTheMaxEmptyForEveryNonRangeTarget(SetTarget target)
    {
        // Arrange
        var workout = AWorkoutWithOneSet(target);

        // Act
        var set = WorkoutMapper.MapToEntity(workout).WorkoutExercises.Single().Sets.Single();

        // Assert
        set.TargetMaxValue.Should().BeNull();
    }

    [Fact]
    public void MapToEntity_ShouldNumberPositionsFromZero()
    {
        // Arrange & Act
        var entity = WorkoutMapper.MapToEntity(AFullyLoadedWorkout());

        // Assert
        entity.WorkoutExercises.Select(item => item.Position).Should().ContainInOrder(0, 1);
        entity.WorkoutExercises.First().Sets.Select(set => set.Position).Should().ContainInOrder(0, 1);
    }

    [Fact]
    public void MapToDomain_ShouldHonourStoredPositionsRatherThanRowOrder()
    {
        // Arrange
        var entity = WorkoutMapper.MapToEntity(AFullyLoadedWorkout());
        entity.WorkoutExercises.Reverse();
        foreach (var workoutExercise in entity.WorkoutExercises)
            workoutExercise.Sets.Reverse();

        // Act
        var restored = WorkoutMapper.MapToDomain(entity);

        // Assert
        restored.Exercises.Select(entry => entry.ExerciseId).Should().Equal(SentadillaId, PlanchaId);
        restored.Exercises[0].Sets.Should().Equal(
            new SetPrescription(5, new SetTarget.Reps(5)),
            new SetPrescription(2, new SetTarget.MaxReps()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void MapToDomain_ShouldRejectAnUnknownStoredEffort(int effort)
    {
        // Arrange
        var entity = WorkoutMapper.MapToEntity(AFullyLoadedWorkout());
        entity.WorkoutExercises.First().Effort = effort;

        // Act
        var act = () => WorkoutMapper.MapToDomain(entity);

        // Assert
        act.Should().Throw<PersistenceMappingException>();
    }

    [Fact]
    public void MapToDomain_ShouldRejectTheSameExerciseStoredTwice()
    {
        // Arrange
        var entity = WorkoutMapper.MapToEntity(AFullyLoadedWorkout());
        entity.WorkoutExercises.Last().ExerciseId = entity.WorkoutExercises.First().ExerciseId;

        // Act
        var act = () => WorkoutMapper.MapToDomain(entity);

        // Assert
        act.Should().Throw<PersistenceMappingException>();
    }

    [Fact]
    public void MapToDomain_ShouldRejectAnUnknownTargetType()
    {
        // Arrange
        var entity = WorkoutMapper.MapToEntity(AFullyLoadedWorkout());
        entity.WorkoutExercises.First().Sets.First().TargetType = "Furlongs";

        // Act
        var act = () => WorkoutMapper.MapToDomain(entity);

        // Assert
        act.Should().Throw<PersistenceMappingException>();
    }

    [Fact]
    public void MapToDomain_ShouldRejectARepsTargetWithoutAValue()
    {
        // Arrange
        var entity = WorkoutMapper.MapToEntity(AFullyLoadedWorkout());
        var set = entity.WorkoutExercises.First().Sets.First(item => item.TargetType == "Reps");
        set.TargetValue = null;

        // Act
        var act = () => WorkoutMapper.MapToDomain(entity);

        // Assert
        act.Should().Throw<PersistenceMappingException>();
    }

    [Fact]
    public void MapToDomain_ShouldRejectARepsRangeTargetWithoutAMax()
    {
        // Arrange
        var entity = WorkoutMapper.MapToEntity(AWorkoutWithOneSet(new SetTarget.RepsRange(8, 12)));
        entity.WorkoutExercises.Single().Sets.Single().TargetMaxValue = null;

        // Act
        var act = () => WorkoutMapper.MapToDomain(entity);

        // Assert
        act.Should().Throw<PersistenceMappingException>();
    }

    [Fact]
    public void RoundTrip_ShouldMarkTheRestoredWorkoutAsNotNew()
    {
        // Arrange
        var workout = AFullyLoadedWorkout();

        // Act
        var restored = RoundTrip(workout);

        // Assert
        restored.IsNew.Should().BeFalse();
    }

    private static Workout AWorkoutWithASuperSeries()
    {
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);
        workout.AddExercise(SentadillaId, Effort.MID, [new SetPrescription(3, new SetTarget.Reps(8))]);
        workout.AddExercise(PlanchaId, Effort.LOW, [new SetPrescription(3, new SetTarget.Duration(40))]);
        workout.AddExercise(Id.New(), Effort.HIGH, [new SetPrescription(4, new SetTarget.MaxReps())]);
        workout.CreateSuperSeries(workout.Exercises.Take(2).Select(entry => entry.Id));

        return WorkoutMapper.MapToDomain(WorkoutMapper.MapToEntity(workout));
    }

    [Fact]
    public void RoundTrip_ShouldPreserveTheSuperSeriesOfEachExercise()
    {
        // Arrange
        var original = AWorkoutWithASuperSeries();

        // Act
        var restored = RoundTrip(original);

        // Assert
        restored.Exercises.Select(entry => entry.SuperSeriesId)
            .Should().Equal(original.Exercises.Select(entry => entry.SuperSeriesId));
        restored.Exercises.Count(entry => entry.SuperSeriesId is not null).Should().Be(2);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveExercisesWithoutASuperSeries()
    {
        // Arrange
        var original = AFullyLoadedWorkout();

        // Act
        var restored = RoundTrip(original);

        // Assert
        restored.Exercises.Should().OnlyContain(entry => entry.SuperSeriesId == null);
    }

    [Fact]
    public void MapToDomain_ShouldRejectAStoredSuperSeriesWithASingleMember()
    {
        // Arrange
        var entity = WorkoutMapper.MapToEntity(AFullyLoadedWorkout());
        entity.WorkoutExercises.First().SuperSeriesId = 1;

        // Act
        var act = () => WorkoutMapper.MapToDomain(entity);

        // Assert
        act.Should().Throw<PersistenceMappingException>();
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.COMPLETED)]
    public void RoundTrip_ShouldPreserveEveryStatus(WorkoutStatus status)
    {
        // Arrange
        var original = Workout.Rehydrate(Id.New(), Id.New(), null, Name, null, WorkoutDate, status, []);

        // Act
        var restored = RoundTrip(original);

        // Assert
        restored.Status.Should().Be(status);
    }

    [Fact]
    public void MapToEntity_ShouldStoreTheStatusAsItsIntegerValue()
    {
        // Arrange
        var workout = Workout.Rehydrate(Id.New(), Id.New(), null, Name, null, WorkoutDate, WorkoutStatus.STARTED, []);

        // Act
        var entity = WorkoutMapper.MapToEntity(workout);

        // Assert
        entity.Status.Should().Be((int)WorkoutStatus.STARTED);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void MapToDomain_ShouldRejectAnUnknownStoredStatus(int status)
    {
        // Arrange
        var entity = WorkoutMapper.MapToEntity(AFullyLoadedWorkout());
        entity.Status = status;

        // Act
        var act = () => WorkoutMapper.MapToDomain(entity);

        // Assert
        act.Should().Throw<PersistenceMappingException>();
    }

    [Fact]
    public void RoundTrip_ShouldPreserveTheNameAndTheDescription()
    {
        // Arrange
        var description = WorkoutDescription.From("Focus on tempo");
        var original = Workout.Rehydrate(Id.New(), Id.New(), null, Name, description, WorkoutDate, WorkoutStatus.DRAFT, []);

        // Act
        var restored = RoundTrip(original);

        // Assert
        restored.Name.Should().Be(Name);
        restored.Description.Should().Be(description);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveAMissingDescription()
    {
        // Arrange
        var original = Workout.Rehydrate(Id.New(), Id.New(), null, Name, null, WorkoutDate, WorkoutStatus.DRAFT, []);

        // Act
        var restored = RoundTrip(original);

        // Assert
        restored.Description.Should().BeNull();
    }

    [Fact]
    public void MapToDomain_ShouldRejectABlankStoredName()
    {
        // Arrange
        var entity = WorkoutMapper.MapToEntity(AFullyLoadedWorkout());
        entity.Name = "   ";

        // Act
        var act = () => WorkoutMapper.MapToDomain(entity);

        // Assert
        act.Should().Throw<PersistenceMappingException>();
    }

    [Fact]
    public void RoundTrip_ShouldPreserveAMinutesTarget()
    {
        // Arrange
        var original = AWorkoutWithOneSet(new SetTarget.Minutes(TimeInMinutes.From(1.25m)));

        // Act
        var restored = RoundTrip(original);

        // Assert
        restored.Exercises.Single().Sets.Single().target.Should().Be(new SetTarget.Minutes(TimeInMinutes.From(1.25m)));
    }

    [Fact]
    public void MapToEntity_ShouldStoreMinutesOnlyInTheMinutesColumn()
    {
        // Arrange
        var workout = AWorkoutWithOneSet(new SetTarget.Minutes(TimeInMinutes.From(30m)));

        // Act
        var set = WorkoutMapper.MapToEntity(workout).WorkoutExercises.Single().Sets.Single();

        // Assert
        set.TargetType.Should().Be(SetTargetTypes.Minutes);
        set.TargetMinutes.Should().Be(30m);
        set.TargetValue.Should().BeNull();
        set.TargetMaxValue.Should().BeNull();
    }

    [Fact]
    public void MapToEntity_ShouldLeaveTheMinutesColumnEmptyForARepsTarget()
    {
        // Arrange
        var workout = AWorkoutWithOneSet(new SetTarget.Reps(8));

        // Act
        var set = WorkoutMapper.MapToEntity(workout).WorkoutExercises.Single().Sets.Single();

        // Assert
        set.TargetMinutes.Should().BeNull();
    }

    [Fact]
    public void RoundTrip_ShouldPreserveThePerSideFlagAndTheRest()
    {
        // Arrange
        var rest = TimeInMinutes.From(1.5m);
        var original = Workout.Rehydrate(
            Id.New(), Id.New(), null, Name, null, WorkoutDate, WorkoutStatus.DRAFT,
            [WorkoutExercise.Rehydrate(Id.New(), PlanchaId, Effort.MID, [new SetPrescription(1, new SetTarget.Reps(8))], perSide: true, rest: rest)]);

        // Act
        var restored = RoundTrip(original);

        // Assert
        var entry = restored.Exercises.Single();
        entry.PerSide.Should().BeTrue();
        entry.Rest.Should().Be(rest);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveNoRest()
    {
        // Arrange
        var original = AFullyLoadedWorkout();

        // Act
        var restored = RoundTrip(original);

        // Assert
        restored.Exercises.Should().OnlyContain(entry => entry.PerSide == false && entry.Rest == null);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveTheMesocyclePlacement()
    {
        // Arrange
        var mesocycleId = Id.New();
        var original = Workout.Rehydrate(Id.New(), Id.New(), null, Name, null, WorkoutDate, WorkoutStatus.STARTED, [],
            mesocycleId, WeekNumber.From(2));

        // Act
        var restored = RoundTrip(original);

        // Assert
        restored.MesocycleId.Should().Be(mesocycleId);
        restored.MesocycleWeekNumber.Should().Be(WeekNumber.From(2));
    }

    [Fact]
    public void RoundTrip_ShouldPreserveAStandaloneWorkout()
    {
        // Arrange
        var original = Workout.Create(Id.New(), null, Name, null, WorkoutDate);

        // Act
        var restored = RoundTrip(original);

        // Assert
        restored.MesocycleId.Should().BeNull();
        restored.MesocycleWeekNumber.Should().BeNull();
    }

    [Fact]
    public void MapToDomain_ShouldRejectAHalfSetStoredPlacement()
    {
        // Arrange
        var entity = WorkoutMapper.MapToEntity(Workout.Create(Id.New(), null, Name, null, WorkoutDate));
        entity.MesocycleId = Id.New().Value;

        // Act
        var act = () => WorkoutMapper.MapToDomain(entity);

        // Assert
        act.Should().Throw<PersistenceMappingException>();
    }

    private static readonly DateTime RecordedAt = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);

    private static Workout AStartedWorkoutWithSetRecords()
    {
        var workout = Workout.Rehydrate(
            Id.New(), Id.New(), null, Name, null, WorkoutDate, WorkoutStatus.STARTED,
            [
                WorkoutExercise.Rehydrate(Id.New(), SentadillaId, Effort.MIDHIGH, [new SetPrescription(3, new SetTarget.RepsRange(8, 12))]),
                WorkoutExercise.Rehydrate(Id.New(), PlanchaId, Effort.LOW, [new SetPrescription(2, new SetTarget.MaxReps())]),
            ]);
        workout.AddSetRecord(SentadillaId, RepCount.From(8), Weight.From(80), RecordedAt.AddMinutes(4));
        workout.AddSetRecord(SentadillaId, RepCount.From(12), Weight.From(72.5m), RecordedAt);
        workout.AddSetRecord(PlanchaId, RepCount.From(15), Weight.From(0), RecordedAt.AddMinutes(9));

        return workout;
    }

    [Fact]
    public void RoundTrip_ShouldPreserveTheSetRecordsOfEachExercise()
    {
        // Arrange
        var original = AStartedWorkoutWithSetRecords();

        // Act
        var restored = RoundTrip(original);

        // Assert
        restored.Exercises.Zip(original.Exercises).Should().AllSatisfy(pair =>
        {
            var (copy, source) = pair;
            copy.SetRecords.Select(setRecord => setRecord.Id).Should().Equal(source.SetRecords.Select(setRecord => setRecord.Id));
            copy.SetRecords.Select(setRecord => setRecord.Reps).Should().Equal(source.SetRecords.Select(setRecord => setRecord.Reps));
            copy.SetRecords.Select(setRecord => setRecord.Weight).Should().Equal(source.SetRecords.Select(setRecord => setRecord.Weight));
            copy.SetRecords.Select(setRecord => setRecord.RegisteredAtUtc).Should().Equal(source.SetRecords.Select(setRecord => setRecord.RegisteredAtUtc));
            copy.SetRecords.Should().AllSatisfy(setRecord => setRecord.IsNew.Should().BeFalse());
        });
    }

    [Fact]
    public void MapToEntity_ShouldKeepTheSetRecordIdAndStoreItsValues()
    {
        // Arrange
        var workout = AStartedWorkoutWithSetRecords();
        var source = workout.Exercises[1].SetRecords.Single();

        // Act
        var entity = WorkoutMapper.MapToEntity(workout);

        // Assert
        var row = entity.WorkoutExercises[1].SetRecords.Should().ContainSingle().Subject;
        row.Id.Should().Be(source.Id.Value);
        row.WorkoutExerciseId.Should().Be(workout.Exercises[1].Id.Value);
        row.Reps.Should().Be(15);
        row.Weight.Should().Be(0);
        row.RegisterDatetimeUtc.Should().Be(RecordedAt.AddMinutes(9));
    }

    [Fact]
    public void MapToEntity_ShouldNumberSetRecordPositionsFromZeroInTheirOrder()
    {
        // Arrange
        var workout = AStartedWorkoutWithSetRecords();

        // Act
        var entity = WorkoutMapper.MapToEntity(workout);

        // Assert
        var rows = entity.WorkoutExercises[0].SetRecords;
        rows.Select(row => row.Position).Should().Equal(0, 1);
        rows.Select(row => row.RegisterDatetimeUtc).Should().Equal(RecordedAt.AddMinutes(4), RecordedAt);
    }

    [Fact]
    public void MapToDomain_ShouldHonourStoredSetRecordPositionsRatherThanRowOrder()
    {
        // Arrange
        var entity = WorkoutMapper.MapToEntity(AStartedWorkoutWithSetRecords());
        var expected = entity.WorkoutExercises[0].SetRecords.Select(row => row.Id).ToList();
        entity.WorkoutExercises[0].SetRecords.Reverse();

        // Act
        var restored = WorkoutMapper.MapToDomain(entity);

        // Assert
        restored.Exercises[0].SetRecords.Select(setRecord => setRecord.Id.Value).Should().Equal(expected);
    }

    [Fact]
    public void RoundTrip_ShouldKeepTheOrderAddedForSetRecordsWithTheSameRegistrationTime()
    {
        // Arrange
        var workout = Workout.Rehydrate(
            Id.New(), Id.New(), null, Name, null, WorkoutDate, WorkoutStatus.STARTED,
            [WorkoutExercise.Rehydrate(Id.New(), SentadillaId, Effort.MID, [new SetPrescription(3, new SetTarget.Reps(10))])]);
        foreach (var reps in new[] { 10, 9, 8, 7, 6 })
            workout.AddSetRecord(SentadillaId, RepCount.From(reps), Weight.From(60), RecordedAt);

        // Act
        var restored = RoundTrip(workout);

        // Assert
        restored.Exercises[0].SetRecords.Select(setRecord => setRecord.Reps.Value).Should().Equal(10, 9, 8, 7, 6);
    }

    [Fact]
    public void RoundTrip_ShouldKeepReorderedAndEditedSetRecordsInTheirOrder()
    {
        // Arrange
        var workout = AStartedWorkoutWithSetRecords();
        var setRecordIds = workout.Exercises[0].SetRecords.Select(setRecord => setRecord.Id).ToList();
        workout.ReorderSetRecords(SentadillaId, [setRecordIds[1], setRecordIds[0]], RecordedAt);
        workout.UpdateSetRecord(SentadillaId, setRecordIds[0], RepCount.From(9), Weight.From(82.5m), RecordedAt);

        // Act
        var restored = RoundTrip(workout);

        // Assert
        var setRecords = restored.Exercises[0].SetRecords;
        setRecords.Select(setRecord => setRecord.Id).Should().Equal(setRecordIds[1], setRecordIds[0]);
        setRecords[1].Reps.Value.Should().Be(9);
        setRecords[1].Weight.Value.Should().Be(82.5m);
        setRecords[1].RegisteredAtUtc.Should().Be(RecordedAt.AddMinutes(4));
    }

    [Fact]
    public void MapToDomain_ShouldRejectAnInvalidStoredWeight()
    {
        // Arrange
        var entity = WorkoutMapper.MapToEntity(AStartedWorkoutWithSetRecords());
        entity.WorkoutExercises[0].SetRecords[0].Weight = -1;

        // Act
        var act = () => WorkoutMapper.MapToDomain(entity);

        // Assert
        act.Should().Throw<PersistenceMappingException>();
    }

    [Fact]
    public void MapToDomain_ShouldRejectAnInvalidStoredRepCount()
    {
        // Arrange
        var entity = WorkoutMapper.MapToEntity(AStartedWorkoutWithSetRecords());
        entity.WorkoutExercises[0].SetRecords[0].Reps = 0;

        // Act
        var act = () => WorkoutMapper.MapToDomain(entity);

        // Assert
        act.Should().Throw<PersistenceMappingException>();
    }

    [Fact]
    public void RoundTrip_ShouldPreserveTheReviewAndTheResultsTime()
    {
        // Arrange
        var trainerId = Id.New();
        var reviewedAt = RecordedAt.AddHours(2);
        var original = Workout.Rehydrate(Id.New(), Id.New(), trainerId, Name, null, WorkoutDate, WorkoutStatus.REVIEWED, [],
            reviewedAt: reviewedAt, reviewedBy: trainerId, resultsUpdatedAt: RecordedAt);

        // Act
        var restored = RoundTrip(original);

        // Assert
        restored.Status.Should().Be(WorkoutStatus.REVIEWED);
        restored.ReviewedAt.Should().Be(reviewedAt);
        restored.ReviewedBy.Should().Be(trainerId);
        restored.ResultsUpdatedAt.Should().Be(RecordedAt);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveAWorkoutThatWasNeverReviewed()
    {
        // Arrange
        var original = AStartedWorkoutWithSetRecords();

        // Act
        var restored = RoundTrip(original);

        // Assert
        restored.ReviewedAt.Should().BeNull();
        restored.ReviewedBy.Should().BeNull();
        restored.ResultsUpdatedAt.Should().Be(original.ResultsUpdatedAt);
    }

    [Fact]
    public void MapToDomain_ShouldRejectAReviewedRowWithoutItsReviewer()
    {
        // Arrange
        var entity = WorkoutMapper.MapToEntity(AStartedWorkoutWithSetRecords());
        entity.Status = (int)WorkoutStatus.REVIEWED;
        entity.ReviewedAtUtc = RecordedAt;

        // Act
        var act = () => WorkoutMapper.MapToDomain(entity);

        // Assert
        act.Should().Throw<PersistenceMappingException>();
    }

    [Fact]
    public void RoundTrip_ShouldPreserveTheCompletionTime()
    {
        // Arrange
        var original = Workout.Rehydrate(Id.New(), Id.New(), null, Name, null, WorkoutDate, WorkoutStatus.COMPLETED, [],
            completedAt: RecordedAt);

        // Act
        var restored = RoundTrip(original);

        // Assert
        restored.CompletedAt.Should().Be(RecordedAt);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveACompletedWorkoutWithoutCompletionTime()
    {
        // Arrange
        var original = Workout.Rehydrate(Id.New(), Id.New(), null, Name, null, WorkoutDate, WorkoutStatus.COMPLETED, []);

        // Act
        var restored = RoundTrip(original);

        // Assert
        restored.Status.Should().Be(WorkoutStatus.COMPLETED);
        restored.CompletedAt.Should().BeNull();
    }
}
