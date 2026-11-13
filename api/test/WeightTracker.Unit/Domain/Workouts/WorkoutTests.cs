using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Domain.Workouts;

public class WorkoutTests
{
    private static readonly DateTime WorkoutDate = new(2026, 10, 1, 18, 30, 0, DateTimeKind.Utc);
    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private static readonly SetPrescription FourByTen = new(4, new SetTarget.Reps(10));

    [Fact]
    public void Create_ShouldRejectATrainerWhoIsTheOwner()
    {
        // Arrange
        var userId = Id.New();

        // Act
        var act = () => Workout.Create(userId, userId, Name, null, WorkoutDate);

        // Assert
        act.Should().Throw<TrainerCannotBeWorkoutOwnerException>();
    }

    [Fact]
    public void Create_ShouldAcceptAWorkoutWithoutTrainer()
    {
        // Arrange
        var userId = Id.New();

        // Act
        var workout = Workout.Create(userId, null, Name, null, WorkoutDate);

        // Assert
        workout.UserId.Should().Be(userId);
        workout.TrainerId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldAcceptATrainerDifferentFromTheOwner()
    {
        // Arrange
        var userId = Id.New();
        var trainerId = Id.New();

        // Act
        var workout = Workout.Create(userId, trainerId, Name, null, WorkoutDate);

        // Assert
        workout.UserId.Should().Be(userId);
        workout.TrainerId.Should().Be(trainerId);
    }

    [Fact]
    public void Create_ShouldMarkTheWorkoutAsNew()
    {
        // Arrange
        var userId = Id.New();

        // Act
        var workout = Workout.Create(userId, null, Name, null, WorkoutDate);

        // Assert
        workout.IsNew.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldTruncateTheWorkoutDateToTheDay()
    {
        // Arrange & Act
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);

        // Assert
        workout.WorkoutDate.Should().Be(WorkoutDate.Date);
    }

    [Fact]
    public void Create_ShouldKeepTheNameAndTheDescription()
    {
        // Arrange
        var description = WorkoutDescription.From("Focus on tempo");

        // Act
        var workout = Workout.Create(Id.New(), null, Name, description, WorkoutDate);

        // Assert
        workout.Name.Should().Be(Name);
        workout.Description.Should().Be(description);
    }

    [Fact]
    public void AddExercise_ShouldRegisterTheExerciseWithItsEffortAndSets()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);
        var exerciseId = Id.New();
        var twoByEight = new SetPrescription(2, new SetTarget.Reps(8));

        // Act
        workout.AddExercise(exerciseId, Effort.HIGH, [FourByTen, twoByEight]);

        // Assert
        var entry = workout.Exercises.Should().ContainSingle().Subject;
        entry.ExerciseId.Should().Be(exerciseId);
        entry.Effort.Should().Be(Effort.HIGH);
        entry.Sets.Should().ContainInOrder(FourByTen, twoByEight);
        entry.IsNew.Should().BeTrue();
    }

    [Fact]
    public void AddExercise_ShouldAppendInInsertionOrder()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);
        var first = Id.New();
        var second = Id.New();
        workout.AddExercise(first, Effort.MID, [FourByTen]);

        // Act
        workout.AddExercise(second, Effort.LOW, [new SetPrescription(5, new SetTarget.Reps(5))]);

        // Assert
        workout.Exercises.Select(entry => entry.ExerciseId).Should().Equal(first, second);
    }

    [Fact]
    public void AddExercise_ShouldRejectAnExerciseAlreadyInTheWorkout()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);
        var exerciseId = Id.New();
        workout.AddExercise(exerciseId, Effort.MID, [FourByTen]);

        // Act
        var act = () => workout.AddExercise(exerciseId, Effort.HIGH, [new SetPrescription(3, new SetTarget.MaxReps())]);

        // Assert
        act.Should().Throw<ExerciseAlreadyInWorkoutException>();
        var entry = workout.Exercises.Should().ContainSingle().Subject;
        entry.Effort.Should().Be(Effort.MID);
        entry.Sets.Should().Equal(FourByTen);
    }

    [Fact]
    public void AddExercise_ShouldRejectAnUndefinedEffort()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);

        // Act
        var act = () => workout.AddExercise(Id.New(), (Effort)0, [FourByTen]);

        // Assert
        act.Should().Throw<InvalidEffortException>();
        workout.Exercises.Should().BeEmpty();
    }

    [Fact]
    public void RemoveExercise_ShouldDropTheExercise()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);
        var exerciseId = Id.New();
        workout.AddExercise(exerciseId, Effort.MID, [FourByTen]);

        // Act
        workout.RemoveExercise(exerciseId);

        // Assert
        workout.Exercises.Should().BeEmpty();
    }

    [Fact]
    public void RemoveExercise_ShouldRejectAnExerciseNotInTheWorkout()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);

        // Act
        var act = () => workout.RemoveExercise(Id.New());

        // Assert
        act.Should().Throw<ExerciseNotInWorkoutException>();
    }

    [Fact]
    public void RemoveExercise_ShouldNotLeaveItsSetsBehindWhenTheExerciseIsAddedAgain()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);
        var exerciseId = Id.New();
        workout.AddExercise(exerciseId, Effort.MID, [FourByTen, ThreeByTen]);
        workout.RemoveExercise(exerciseId);

        // Act
        workout.AddExercise(exerciseId, Effort.LOW, [ThreeByTen]);

        // Assert
        workout.Exercises.Should().ContainSingle()
            .Which.Sets.Should().Equal(ThreeByTen);
    }

    [Fact]
    public void UpdateExercise_ShouldReplaceTheEffortAndEverySet()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);
        var exerciseId = Id.New();
        workout.AddExercise(exerciseId, Effort.MID, [FourByTen, ThreeByTen]);
        var twoByTwelve = new SetPrescription(2, new SetTarget.Reps(12));
        var oneMax = new SetPrescription(1, new SetTarget.MaxReps());

        // Act
        workout.UpdateExercise(exerciseId, Effort.HIGH, [twoByTwelve, oneMax]);

        // Assert
        var entry = workout.Exercises.Single();
        entry.Effort.Should().Be(Effort.HIGH);
        entry.Sets.Should().Equal(twoByTwelve, oneMax);
    }

    [Fact]
    public void UpdateExercise_ShouldKeepTheEntryIdPositionAndSuperSeries()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);
        var first = Id.New();
        var second = Id.New();
        var third = Id.New();
        workout.AddExercise(first, Effort.MID, [FourByTen]);
        workout.AddExercise(second, Effort.MID, [FourByTen]);
        workout.AddExercise(third, Effort.MID, [FourByTen]);
        var secondEntryId = EntryOf(workout, second);
        var superSeriesId = workout.CreateSuperSeries([EntryOf(workout, first), secondEntryId]);

        // Act
        workout.UpdateExercise(second, Effort.LOW, [new SetPrescription(2, new SetTarget.Reps(8)), new SetPrescription(2, new SetTarget.Reps(6))]);

        // Assert
        workout.Exercises.Select(entry => entry.ExerciseId).Should().Equal(first, second, third);
        workout.Exercises[1].Id.Should().Be(secondEntryId);
        workout.Exercises[1].SuperSeriesId.Should().Be(superSeriesId);
    }

    [Fact]
    public void UpdateExercise_ShouldRejectAnExerciseNotInTheWorkout()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);

        // Act
        var act = () => workout.UpdateExercise(Id.New(), Effort.MID, [FourByTen]);

        // Assert
        act.Should().Throw<ExerciseNotInWorkoutException>();
    }

    [Fact]
    public void UpdateExercise_ShouldRejectANewTotalOfSetsInsideASuperSeriesAndChangeNothing()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);
        var first = Id.New();
        var second = Id.New();
        workout.AddExercise(first, Effort.MID, [FourByTen]);
        workout.AddExercise(second, Effort.MID, [FourByTen]);
        workout.CreateSuperSeries([EntryOf(workout, first), EntryOf(workout, second)]);

        // Act
        var act = () => workout.UpdateExercise(first, Effort.HIGH, [ThreeByTen]);

        // Assert
        act.Should().Throw<SuperSeriesSetCountMismatchException>();
        var entry = workout.Exercises.Single(item => item.ExerciseId == first);
        entry.Effort.Should().Be(Effort.MID);
        entry.Sets.Should().Equal(FourByTen);
        entry.SuperSeriesId.Should().NotBeNull();
    }

    [Fact]
    public void UpdateExercise_ShouldAcceptADifferentPrescriptionWithTheSameTotalInsideASuperSeries()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);
        var first = Id.New();
        var second = Id.New();
        workout.AddExercise(first, Effort.MID, [FourByTen]);
        workout.AddExercise(second, Effort.MID, [FourByTen]);
        var superSeriesId = workout.CreateSuperSeries([EntryOf(workout, first), EntryOf(workout, second)]);
        var twoPlusTwo = new[] { new SetPrescription(2, new SetTarget.Reps(12)), new SetPrescription(2, new SetTarget.Duration(30)) };

        // Act
        workout.UpdateExercise(first, Effort.HIGH, twoPlusTwo);

        // Assert
        var entry = workout.Exercises.Single(item => item.ExerciseId == first);
        entry.Sets.Should().Equal(twoPlusTwo);
        entry.SuperSeriesId.Should().Be(superSeriesId);
    }

    [Fact]
    public void UpdateExercise_ShouldRejectAnUndefinedEffortAndChangeNothing()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);
        var exerciseId = Id.New();
        workout.AddExercise(exerciseId, Effort.MID, [FourByTen]);

        // Act
        var act = () => workout.UpdateExercise(exerciseId, (Effort)9, [ThreeByTen]);

        // Assert
        act.Should().Throw<InvalidEffortException>();
        var entry = workout.Exercises.Single();
        entry.Effort.Should().Be(Effort.MID);
        entry.Sets.Should().Equal(FourByTen);
    }

    [Fact]
    public void UpdateExercise_ShouldRejectEmptySetsAndChangeNothing()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);
        var exerciseId = Id.New();
        workout.AddExercise(exerciseId, Effort.MID, [FourByTen]);

        // Act
        var act = () => workout.UpdateExercise(exerciseId, Effort.HIGH, []);

        // Assert
        act.Should().Throw<WorkoutExerciseRequiresSetsException>();
        var entry = workout.Exercises.Single();
        entry.Effort.Should().Be(Effort.MID);
        entry.Sets.Should().Equal(FourByTen);
    }

    [Fact]
    public void Rehydrate_ShouldNotMarkTheWorkoutAsNew()
    {
        // Arrange & Act
        var workout = Workout.Rehydrate(Id.New(), Id.New(), null, Name, null, WorkoutDate, WorkoutStatus.DRAFT, []);

        // Assert
        workout.IsNew.Should().BeFalse();
    }

    [Fact]
    public void Rehydrate_ShouldRestoreOwnershipAndOrderedExercises()
    {
        // Arrange
        var id = Id.New();
        var userId = Id.New();
        var trainerId = Id.New();
        var sentadilla = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MIDHIGH, [new SetPrescription(5, new SetTarget.Reps(5))]);
        var plancha = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.LOW, [new SetPrescription(3, new SetTarget.Duration(40))]);

        // Act
        var workout = Workout.Rehydrate(id, userId, trainerId, Name, null, WorkoutDate, WorkoutStatus.DRAFT, [sentadilla, plancha]);

        // Assert
        workout.Id.Should().Be(id);
        workout.UserId.Should().Be(userId);
        workout.TrainerId.Should().Be(trainerId);
        workout.WorkoutDate.Should().Be(WorkoutDate.Date);
        workout.Exercises.Should().Equal(sentadilla, plancha);
        workout.Exercises[1].Sets.Single().target.Should().Be(new SetTarget.Duration(40));
    }

    [Fact]
    public void Rehydrate_ShouldRejectTheSameExerciseTwice()
    {
        // Arrange
        var exerciseId = Id.New();
        var first = WorkoutExercise.Rehydrate(Id.New(), exerciseId, Effort.MID, [FourByTen]);
        var second = WorkoutExercise.Rehydrate(Id.New(), exerciseId, Effort.HIGH, [FourByTen]);

        // Act
        var act = () => Workout.Rehydrate(Id.New(), Id.New(), null, Name, null, WorkoutDate, WorkoutStatus.DRAFT, [first, second]);

        // Assert
        act.Should().Throw<ExerciseAlreadyInWorkoutException>();
    }

    [Fact]
    public void Exercises_ShouldNotExposeTheInternalCollectionForMutation()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);
        workout.AddExercise(Id.New(), Effort.MID, [FourByTen]);

        var snapshot = workout.Exercises;

        // Act
        workout.AddExercise(Id.New(), Effort.LOW, [new SetPrescription(2, new SetTarget.Reps(8))]);

        // Assert
        snapshot.Should().HaveCount(1);
        workout.Exercises.Should().HaveCount(2);
    }

    private static readonly SetPrescription ThreeByTen = new(3, new SetTarget.Reps(10));

    /// <param name="entryIds">Identifiers of the workout exercise entries, not of the catalog exercises.</param>
    private static Workout WorkoutWithExercises(out Id[] entryIds, int count = 3)
    {
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);

        for (var index = 0; index < count; index++)
            workout.AddExercise(Id.New(), Effort.MID, [FourByTen]);

        entryIds = workout.Exercises.Select(entry => entry.Id).ToArray();

        return workout;
    }

    private static Id EntryOf(Workout workout, Id exerciseId)
        => workout.Exercises.Single(entry => entry.ExerciseId == exerciseId).Id;

    private static Id ExerciseOf(Workout workout, Id entryId)
        => workout.Exercises.Single(entry => entry.Id == entryId).ExerciseId;

    private static SuperSeriesId? SuperSeriesOf(Workout workout, Id entryId)
        => workout.Exercises.Single(entry => entry.Id == entryId).SuperSeriesId;

    [Fact]
    public void CreateSuperSeries_ShouldGroupTheGivenExercisesAndLeaveTheOthersAlone()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids);

        // Act
        var superSeriesId = workout.CreateSuperSeries([ids[0], ids[1]]);

        // Assert
        SuperSeriesOf(workout, ids[0]).Should().Be(superSeriesId);
        SuperSeriesOf(workout, ids[1]).Should().Be(superSeriesId);
        SuperSeriesOf(workout, ids[2]).Should().BeNull();
    }

    [Fact]
    public void CreateSuperSeries_ShouldAssignADifferentIdToEachSuperSeriesOfTheWorkout()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids, 4);
        var first = workout.CreateSuperSeries([ids[0], ids[1]]);

        // Act
        var second = workout.CreateSuperSeries([ids[2], ids[3]]);

        // Assert
        second.Should().NotBe(first);
    }

    [Fact]
    public void CreateSuperSeries_ShouldAllowTheSameIdInDifferentWorkouts()
    {
        // Arrange
        var firstWorkout = WorkoutWithExercises(out var firstIds, 2);
        var secondWorkout = WorkoutWithExercises(out var secondIds, 2);

        // Act
        var firstSuperSeries = firstWorkout.CreateSuperSeries(firstIds);
        var secondSuperSeries = secondWorkout.CreateSuperSeries(secondIds);

        // Assert
        secondSuperSeries.Should().Be(firstSuperSeries);
    }

    [Fact]
    public void CreateSuperSeries_ShouldAcceptDifferentPrescriptionsWithTheSameTotalOfSets()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);
        var first = Id.New();
        var second = Id.New();
        workout.AddExercise(first, Effort.MID, [ThreeByTen]);
        workout.AddExercise(second, Effort.MID, [new SetPrescription(2, new SetTarget.Reps(8)), new SetPrescription(1, new SetTarget.MaxReps())]);
        var firstEntry = EntryOf(workout, first);
        var secondEntry = EntryOf(workout, second);

        // Act
        var superSeriesId = workout.CreateSuperSeries([firstEntry, secondEntry]);

        // Assert
        SuperSeriesOf(workout, firstEntry).Should().Be(superSeriesId);
        SuperSeriesOf(workout, secondEntry).Should().Be(superSeriesId);
    }

    [Fact]
    public void CreateSuperSeries_ShouldRejectExercisesWithDifferentNumberOfSetsAndGroupNothing()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);
        var first = Id.New();
        var second = Id.New();
        workout.AddExercise(first, Effort.MID, [ThreeByTen]);
        workout.AddExercise(second, Effort.MID, [FourByTen]);
        var firstEntry = EntryOf(workout, first);
        var secondEntry = EntryOf(workout, second);

        // Act
        var act = () => workout.CreateSuperSeries([firstEntry, secondEntry]);

        // Assert
        act.Should().Throw<SuperSeriesSetCountMismatchException>();
        SuperSeriesOf(workout, firstEntry).Should().BeNull();
        SuperSeriesOf(workout, secondEntry).Should().BeNull();
    }

    [Fact]
    public void CreateSuperSeries_ShouldRejectASingleExercise()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids);

        // Act
        var act = () => workout.CreateSuperSeries([ids[0]]);

        // Assert
        act.Should().Throw<SuperSeriesRequiresTwoExercisesException>();
    }

    [Fact]
    public void CreateSuperSeries_ShouldRejectTheSameExerciseTwice()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids);

        // Act
        var act = () => workout.CreateSuperSeries([ids[0], ids[0]]);

        // Assert
        act.Should().Throw<DuplicateExerciseInSuperSeriesException>();
    }

    [Fact]
    public void CreateSuperSeries_ShouldRejectAnExerciseNotInTheWorkoutAndGroupNothing()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids);

        // Act
        var act = () => workout.CreateSuperSeries([ids[0], Id.New()]);

        // Assert
        act.Should().Throw<WorkoutExerciseNotInWorkoutException>();
        SuperSeriesOf(workout, ids[0]).Should().BeNull();
    }

    [Fact]
    public void CreateSuperSeries_ShouldRejectCatalogExerciseIdsAndGroupNothing()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids);
        var catalogIds = new[] { ExerciseOf(workout, ids[0]), ExerciseOf(workout, ids[1]) };

        // Act
        var act = () => workout.CreateSuperSeries(catalogIds);

        // Assert
        act.Should().Throw<WorkoutExerciseNotInWorkoutException>();
        workout.Exercises.Should().OnlyContain(entry => entry.SuperSeriesId == null);
    }

    [Fact]
    public void CreateSuperSeries_ShouldRejectAnExerciseAlreadyInASuperSeries()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids);
        var existing = workout.CreateSuperSeries([ids[0], ids[1]]);

        // Act
        var act = () => workout.CreateSuperSeries([ids[1], ids[2]]);

        // Assert
        act.Should().Throw<ExerciseAlreadyInSuperSeriesException>();
        SuperSeriesOf(workout, ids[1]).Should().Be(existing);
        SuperSeriesOf(workout, ids[2]).Should().BeNull();
    }

    [Fact]
    public void BreakSuperSeries_ShouldUngroupTheExercisesAndKeepThemInTheWorkout()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids);
        var superSeriesId = workout.CreateSuperSeries([ids[0], ids[1]]);

        // Act
        workout.BreakSuperSeries(superSeriesId);

        // Assert
        workout.Exercises.Select(entry => entry.Id).Should().Equal(ids);
        workout.Exercises.Should().OnlyContain(entry => entry.SuperSeriesId == null);
    }

    [Fact]
    public void BreakSuperSeries_ShouldLeaveOtherSuperSeriesUntouched()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids, 4);
        var first = workout.CreateSuperSeries([ids[0], ids[1]]);
        var second = workout.CreateSuperSeries([ids[2], ids[3]]);

        // Act
        workout.BreakSuperSeries(first);

        // Assert
        SuperSeriesOf(workout, ids[0]).Should().BeNull();
        SuperSeriesOf(workout, ids[2]).Should().Be(second);
        SuperSeriesOf(workout, ids[3]).Should().Be(second);
    }

    [Fact]
    public void BreakSuperSeries_ShouldRejectAnUnknownSuperSeries()
    {
        // Arrange
        var workout = WorkoutWithExercises(out _);

        // Act
        var act = () => workout.BreakSuperSeries(SuperSeriesId.From(1));

        // Assert
        act.Should().Throw<SuperSeriesNotFoundException>();
    }

    [Fact]
    public void RemoveExercise_ShouldUngroupTheRemainingExerciseOfAPair()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids, 2);
        workout.CreateSuperSeries(ids);
        var removed = ExerciseOf(workout, ids[0]);

        // Act
        workout.RemoveExercise(removed);

        // Assert
        workout.Exercises.Should().ContainSingle().Which.SuperSeriesId.Should().BeNull();
    }

    [Fact]
    public void RemoveExercise_ShouldKeepTheRestOfALargerSuperSeriesGrouped()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids);
        var superSeriesId = workout.CreateSuperSeries(ids);
        var removed = ExerciseOf(workout, ids[0]);

        // Act
        workout.RemoveExercise(removed);

        // Assert
        SuperSeriesOf(workout, ids[1]).Should().Be(superSeriesId);
        SuperSeriesOf(workout, ids[2]).Should().Be(superSeriesId);
    }

    [Fact]
    public void Rehydrate_ShouldRestoreStoredSuperSeries()
    {
        // Arrange
        var superSeriesId = SuperSeriesId.From(1);
        var first = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [FourByTen], superSeriesId);
        var second = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [FourByTen], superSeriesId);

        // Act
        var workout = Workout.Rehydrate(Id.New(), Id.New(), null, Name, null, WorkoutDate, WorkoutStatus.DRAFT, [first, second]);

        // Assert
        workout.Exercises.Should().OnlyContain(entry => entry.SuperSeriesId == superSeriesId);
    }

    [Fact]
    public void Rehydrate_ShouldRejectASuperSeriesWithASingleMember()
    {
        // Arrange
        var alone = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [FourByTen], SuperSeriesId.From(1));

        // Act
        var act = () => Workout.Rehydrate(Id.New(), Id.New(), null, Name, null, WorkoutDate, WorkoutStatus.DRAFT, [alone]);

        // Assert
        act.Should().Throw<SuperSeriesRequiresTwoExercisesException>();
    }

    [Fact]
    public void Rehydrate_ShouldRejectASuperSeriesWithDifferentNumberOfSets()
    {
        // Arrange
        var superSeriesId = SuperSeriesId.From(1);
        var first = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [ThreeByTen], superSeriesId);
        var second = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [FourByTen], superSeriesId);

        // Act
        var act = () => Workout.Rehydrate(Id.New(), Id.New(), null, Name, null, WorkoutDate, WorkoutStatus.DRAFT, [first, second]);

        // Assert
        act.Should().Throw<SuperSeriesSetCountMismatchException>();
    }

    [Fact]
    public void EnsureCanBeDeleted_ShouldAcceptADraft()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.DRAFT);

        // Act
        var act = () => workout.EnsureCanBeDeleted();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.COMPLETED)]
    [InlineData(WorkoutStatus.REVIEWED)]
    public void EnsureCanBeDeleted_ShouldRejectAWorkoutThatIsNoLongerADraft(WorkoutStatus status)
    {
        // Arrange
        var workout = AWorkoutIn(status);

        // Act
        var act = () => workout.EnsureCanBeDeleted();

        // Assert
        act.Should().Throw<WorkoutDeletionLockedException>();
    }

    private static Workout AWorkoutIn(WorkoutStatus status, Id? trainerId = null, DateTime? completedAt = null, DateTime? startedAt = null)
        => Workout.Rehydrate(Id.New(), Id.New(), trainerId, Name, null, WorkoutDate, status, [],
            reviewedAt: ReviewedAtFor(status), reviewedBy: ReviewerFor(status), completedAt: completedAt, startedAt: startedAt);

    // A reviewed workout always carries its review metadata, so the factories add it for that status only
    private static DateTime? ReviewedAtFor(WorkoutStatus status) => status == WorkoutStatus.REVIEWED ? WorkoutDate : null;

    private static Id? ReviewerFor(WorkoutStatus status) => status == WorkoutStatus.REVIEWED ? Id.New() : null;

    [Fact]
    public void Create_ShouldStartAsDraft()
    {
        // Arrange & Act
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);

        // Assert
        workout.Status.Should().Be(WorkoutStatus.DRAFT);
    }

    [Fact]
    public void Rehydrate_ShouldRestoreTheStoredStatus()
    {
        // Arrange & Act
        var workout = AWorkoutIn(WorkoutStatus.STARTED);

        // Assert
        workout.Status.Should().Be(WorkoutStatus.STARTED);
    }

    [Fact]
    public void Rehydrate_ShouldRejectAnUndefinedStatus()
    {
        // Arrange & Act
        var act = () => AWorkoutIn((WorkoutStatus)99);

        // Assert
        act.Should().Throw<InvalidWorkoutStatusException>();
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.PUBLISHED, WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.PUBLISHED, WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.STARTED, WorkoutStatus.COMPLETED)]
    public void ChangeStatus_ShouldAcceptTheBusinessTransitions(WorkoutStatus from, WorkoutStatus to)
    {
        // Arrange
        var workout = AWorkoutIn(from);

        // Act
        workout.ChangeStatus(to, ChangedAt);

        // Assert
        workout.Status.Should().Be(to);
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.COMPLETED)]
    [InlineData(WorkoutStatus.PUBLISHED, WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.PUBLISHED, WorkoutStatus.COMPLETED)]
    [InlineData(WorkoutStatus.STARTED, WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.STARTED, WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.STARTED, WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.COMPLETED, WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.COMPLETED, WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.COMPLETED, WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.COMPLETED, WorkoutStatus.COMPLETED)]
    [InlineData(WorkoutStatus.COMPLETED, WorkoutStatus.REVIEWED)]
    [InlineData(WorkoutStatus.REVIEWED, WorkoutStatus.COMPLETED)]
    [InlineData(WorkoutStatus.REVIEWED, WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.REVIEWED, WorkoutStatus.REVIEWED)]
    public void ChangeStatus_ShouldRejectEveryOtherTransitionAndKeepTheStatus(WorkoutStatus from, WorkoutStatus to)
    {
        // Arrange
        var workout = AWorkoutIn(from);

        // Act
        var act = () => workout.ChangeStatus(to, ChangedAt);

        // Assert
        act.Should().Throw<InvalidWorkoutStatusTransitionException>();
        workout.Status.Should().Be(from);
    }

    [Fact]
    public void ChangeStatus_ShouldRejectAnUndefinedTarget()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.DRAFT);

        // Act
        var act = () => workout.ChangeStatus((WorkoutStatus)99, ChangedAt);

        // Assert
        act.Should().Throw<InvalidWorkoutStatusException>();
        workout.Status.Should().Be(WorkoutStatus.DRAFT);
    }

    [Theory]
    [InlineData(WorkoutStatus.COMPLETED, WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.COMPLETED)]
    [InlineData(WorkoutStatus.STARTED, WorkoutStatus.STARTED)]
    public void OverrideStatus_ShouldSkipTheLifecycle(WorkoutStatus from, WorkoutStatus to)
    {
        // Arrange
        var workout = AWorkoutIn(from);

        // Act
        workout.OverrideStatus(to, ChangedAt);

        // Assert
        workout.Status.Should().Be(to);
    }

    [Theory]
    [InlineData(WorkoutStatus.COMPLETED, WorkoutStatus.REVIEWED)]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.REVIEWED)]
    [InlineData(WorkoutStatus.REVIEWED, WorkoutStatus.COMPLETED)]
    [InlineData(WorkoutStatus.REVIEWED, WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.REVIEWED, WorkoutStatus.REVIEWED)]
    public void OverrideStatus_ShouldNeverSetOrLeaveReviewed(WorkoutStatus from, WorkoutStatus to)
    {
        // Arrange
        var workout = AWorkoutIn(from, Id.New());

        // Act
        var act = () => workout.OverrideStatus(to, ChangedAt);

        // Assert
        act.Should().Throw<InvalidWorkoutStatusTransitionException>();
        workout.Status.Should().Be(from);
    }

    [Fact]
    public void OverrideStatus_ShouldRejectAnUndefinedTarget()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.PUBLISHED);

        // Act
        var act = () => workout.OverrideStatus((WorkoutStatus)99, ChangedAt);

        // Assert
        act.Should().Throw<InvalidWorkoutStatusException>();
        workout.Status.Should().Be(WorkoutStatus.PUBLISHED);
    }

    [Fact]
    public void ChangeStatus_ShouldRaiseTheCompletedEventOnCompletion()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.STARTED);

        // Act
        workout.ChangeStatus(WorkoutStatus.COMPLETED, ChangedAt);

        // Assert
        workout.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(new WorkoutCompletedEvent(workout.Id, ChangedAt));
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.COMPLETED)]
    [InlineData(WorkoutStatus.COMPLETED, WorkoutStatus.COMPLETED)]
    public void OverrideStatus_ShouldRaiseTheCompletedEventEveryTimeItCompletes(WorkoutStatus from, WorkoutStatus to)
    {
        // Arrange
        var workout = AWorkoutIn(from);

        // Act
        workout.OverrideStatus(to, ChangedAt);

        // Assert
        workout.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(new WorkoutCompletedEvent(workout.Id, ChangedAt));
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.PUBLISHED, WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.PUBLISHED, WorkoutStatus.STARTED)]
    public void ChangeStatus_ShouldNotRaiseTheCompletedEventForOtherTargets(WorkoutStatus from, WorkoutStatus to)
    {
        // Arrange
        var workout = AWorkoutIn(from);

        // Act
        workout.ChangeStatus(to, ChangedAt);

        // Assert
        workout.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void OverrideStatus_ShouldNotRaiseTheCompletedEventWhenReopening()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.COMPLETED);

        // Act
        workout.OverrideStatus(WorkoutStatus.STARTED, ChangedAt);

        // Assert
        workout.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void MarkReviewed_ShouldNotRaiseTheCompletedEvent()
    {
        // Arrange
        var trainerId = Id.New();
        var workout = ACompletedWorkout(trainerId);

        // Act
        workout.MarkReviewed(trainerId, ReviewedAt, null);

        // Assert
        workout.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Rehydrate_ShouldNotRaiseTheCompletedEventForACompletedWorkout()
    {
        // Arrange
        // Act
        var workout = AWorkoutIn(WorkoutStatus.COMPLETED);

        // Assert
        workout.DomainEvents.Should().BeEmpty();
    }

    private static readonly DateTime ChangedAt = new(2026, 11, 10, 7, 45, 0, DateTimeKind.Utc);

    [Fact]
    public void ChangeStatus_ShouldRecordWhenTheWorkoutIsCompleted()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.STARTED);

        // Act
        workout.ChangeStatus(WorkoutStatus.COMPLETED, ChangedAt);

        // Assert
        workout.CompletedAt.Should().Be(ChangedAt);
    }

    [Fact]
    public void ChangeStatus_ShouldTruncateTheCompletionTimeToTheStoredPrecision()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.STARTED);

        // Act
        workout.ChangeStatus(WorkoutStatus.COMPLETED, ChangedAt.AddTicks(7));

        // Assert
        workout.CompletedAt.Should().Be(ChangedAt);
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.PUBLISHED, WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.PUBLISHED, WorkoutStatus.STARTED)]
    public void ChangeStatus_ShouldKeepTheCompletionDateWhenNotCompleting(WorkoutStatus from, WorkoutStatus to)
    {
        // Arrange
        var workout = AWorkoutIn(from, completedAt: WorkoutDate);

        // Act
        workout.ChangeStatus(to, ChangedAt);

        // Assert
        workout.CompletedAt.Should().Be(WorkoutDate);
    }

    [Fact]
    public void ChangeStatus_ShouldKeepTheCompletionDateWhenTheMoveIsRefused()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.COMPLETED, completedAt: WorkoutDate);

        // Act
        var act = () => workout.ChangeStatus(WorkoutStatus.STARTED, ChangedAt);

        // Assert
        act.Should().Throw<InvalidWorkoutStatusTransitionException>();
        workout.CompletedAt.Should().Be(WorkoutDate);
    }

    [Fact]
    public void OverrideStatus_ShouldRecordWhenTheWorkoutIsCompleted()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.DRAFT);

        // Act
        workout.OverrideStatus(WorkoutStatus.COMPLETED, ChangedAt);

        // Assert
        workout.CompletedAt.Should().Be(ChangedAt);
    }

    [Fact]
    public void OverrideStatus_ShouldKeepTheCompletionDateWhenLeavingCompleted()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.COMPLETED, completedAt: WorkoutDate);

        // Act
        workout.OverrideStatus(WorkoutStatus.DRAFT, ChangedAt);

        // Assert
        workout.Status.Should().Be(WorkoutStatus.DRAFT);
        workout.CompletedAt.Should().Be(WorkoutDate);
    }

    [Fact]
    public void OverrideStatus_ShouldOverwriteTheCompletionDateWhenCompletingAgain()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.COMPLETED, completedAt: WorkoutDate);

        // Act
        workout.OverrideStatus(WorkoutStatus.COMPLETED, ChangedAt);

        // Assert
        workout.CompletedAt.Should().Be(ChangedAt);
    }

    [Fact]
    public void OverrideStatus_ShouldKeepTheCompletionDateWhenRefused()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.REVIEWED, Id.New(), completedAt: WorkoutDate);

        // Act
        var act = () => workout.OverrideStatus(WorkoutStatus.COMPLETED, ChangedAt);

        // Assert
        act.Should().Throw<InvalidWorkoutStatusTransitionException>();
        workout.CompletedAt.Should().Be(WorkoutDate);
    }

    [Fact]
    public void ChangeStatus_ShouldRecordWhenTheWorkoutIsStarted()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.PUBLISHED);

        // Act
        workout.ChangeStatus(WorkoutStatus.STARTED, ChangedAt);

        // Assert
        workout.StartedAt.Should().Be(ChangedAt);
    }

    [Fact]
    public void ChangeStatus_ShouldTruncateTheStartTimeToTheStoredPrecision()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.PUBLISHED);

        // Act
        workout.ChangeStatus(WorkoutStatus.STARTED, ChangedAt.AddTicks(7));

        // Assert
        workout.StartedAt.Should().Be(ChangedAt);
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.PUBLISHED, WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.STARTED, WorkoutStatus.COMPLETED)]
    public void ChangeStatus_ShouldKeepTheStartDateWhenNotStarting(WorkoutStatus from, WorkoutStatus to)
    {
        // Arrange
        var workout = AWorkoutIn(from, startedAt: WorkoutDate);

        // Act
        workout.ChangeStatus(to, ChangedAt);

        // Assert
        workout.StartedAt.Should().Be(WorkoutDate);
    }

    [Fact]
    public void ChangeStatus_ShouldKeepTheStartDateWhenTheMoveIsRefused()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.COMPLETED, startedAt: WorkoutDate);

        // Act
        var act = () => workout.ChangeStatus(WorkoutStatus.STARTED, ChangedAt);

        // Assert
        act.Should().Throw<InvalidWorkoutStatusTransitionException>();
        workout.StartedAt.Should().Be(WorkoutDate);
    }

    [Fact]
    public void OverrideStatus_ShouldRecordWhenTheWorkoutIsStarted()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.DRAFT);

        // Act
        workout.OverrideStatus(WorkoutStatus.STARTED, ChangedAt);

        // Assert
        workout.StartedAt.Should().Be(ChangedAt);
    }

    [Fact]
    public void OverrideStatus_ShouldKeepTheStartDateWhenLeavingStarted()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.STARTED, startedAt: WorkoutDate);

        // Act
        workout.OverrideStatus(WorkoutStatus.PUBLISHED, ChangedAt);

        // Assert
        workout.Status.Should().Be(WorkoutStatus.PUBLISHED);
        workout.StartedAt.Should().Be(WorkoutDate);
    }

    [Fact]
    public void OverrideStatus_ShouldOverwriteTheStartDateWhenStartingAgain()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.STARTED, startedAt: WorkoutDate);

        // Act
        workout.OverrideStatus(WorkoutStatus.STARTED, ChangedAt);

        // Assert
        workout.StartedAt.Should().Be(ChangedAt);
    }

    [Fact]
    public void OverrideStatus_ShouldKeepTheStartDateWhenRefused()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.REVIEWED, Id.New(), startedAt: WorkoutDate);

        // Act
        var act = () => workout.OverrideStatus(WorkoutStatus.STARTED, ChangedAt);

        // Assert
        act.Should().Throw<InvalidWorkoutStatusTransitionException>();
        workout.StartedAt.Should().Be(WorkoutDate);
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT, false)]
    [InlineData(WorkoutStatus.PUBLISHED, true)]
    [InlineData(WorkoutStatus.STARTED, true)]
    [InlineData(WorkoutStatus.COMPLETED, true)]
    [InlineData(WorkoutStatus.REVIEWED, true)]
    public void IsLocked_ShouldBeTrueForEveryStatusButDraft(WorkoutStatus status, bool expected)
    {
        // Arrange
        var workout = AWorkoutIn(status);

        // Act
        var locked = workout.IsLocked;

        // Assert
        locked.Should().Be(expected);
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT, false)]
    [InlineData(WorkoutStatus.PUBLISHED, false)]
    [InlineData(WorkoutStatus.STARTED, true)]
    [InlineData(WorkoutStatus.COMPLETED, true)]
    [InlineData(WorkoutStatus.REVIEWED, true)]
    public void IsExecuted_ShouldBeTrueOnceTheWorkoutHasStarted(WorkoutStatus status, bool expected)
    {
        // Arrange
        var workout = AWorkoutIn(status);

        // Act
        var executed = workout.IsExecuted;

        // Assert
        executed.Should().Be(expected);
    }

    [Fact]
    public void IsHiddenFrom_ShouldHideAPrescribedDraftFromItsAthlete()
    {
        // Arrange
        var athleteId = Id.New();
        var workout = Workout.Rehydrate(Id.New(), athleteId, Id.New(), Name, null, WorkoutDate, WorkoutStatus.DRAFT, []);

        // Act
        var hidden = workout.IsHiddenFrom(athleteId);

        // Assert
        hidden.Should().BeTrue();
    }

    [Fact]
    public void IsHiddenFrom_ShouldShowAPrescribedDraftToAnyoneElse()
    {
        // Arrange
        var trainerId = Id.New();
        var workout = Workout.Rehydrate(Id.New(), Id.New(), trainerId, Name, null, WorkoutDate, WorkoutStatus.DRAFT, []);

        // Act
        var hiddenFromTrainer = workout.IsHiddenFrom(trainerId);
        var hiddenFromStranger = workout.IsHiddenFrom(Id.New());

        // Assert
        hiddenFromTrainer.Should().BeFalse();
        hiddenFromStranger.Should().BeFalse();
    }

    [Fact]
    public void IsHiddenFrom_ShouldNeverHideASelfPlannedDraftFromItsAthlete()
    {
        // Arrange
        var athleteId = Id.New();
        var workout = Workout.Rehydrate(Id.New(), athleteId, null, Name, null, WorkoutDate, WorkoutStatus.DRAFT, []);

        // Act
        var hidden = workout.IsHiddenFrom(athleteId);

        // Assert
        hidden.Should().BeFalse();
    }

    [Theory]
    [InlineData(WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.COMPLETED)]
    public void IsHiddenFrom_ShouldShowAPrescribedWorkoutOnceItIsPublished(WorkoutStatus status)
    {
        // Arrange
        var athleteId = Id.New();
        var workout = Workout.Rehydrate(Id.New(), athleteId, Id.New(), Name, null, WorkoutDate, status, []);

        // Act
        var hidden = workout.IsHiddenFrom(athleteId);

        // Assert
        hidden.Should().BeFalse();
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.PUBLISHED, true)]
    [InlineData(WorkoutStatus.PUBLISHED, WorkoutStatus.DRAFT, true)]
    [InlineData(WorkoutStatus.PUBLISHED, WorkoutStatus.STARTED, true)]
    [InlineData(WorkoutStatus.STARTED, WorkoutStatus.COMPLETED, true)]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.STARTED, false)]
    [InlineData(WorkoutStatus.STARTED, WorkoutStatus.PUBLISHED, false)]
    [InlineData(WorkoutStatus.COMPLETED, WorkoutStatus.DRAFT, false)]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.DRAFT, false)]
    [InlineData(WorkoutStatus.COMPLETED, WorkoutStatus.REVIEWED, false)]
    [InlineData(WorkoutStatus.REVIEWED, WorkoutStatus.COMPLETED, false)]
    public void CanTransitionTo_ShouldMatchTheLifecycle(WorkoutStatus from, WorkoutStatus to, bool expected)
    {
        // Arrange
        var workout = AWorkoutIn(from);

        // Act
        var allowed = workout.CanTransitionTo(to);

        // Assert
        allowed.Should().Be(expected);
    }

    [Fact]
    public void Rehydrate_ShouldRestoreTheNameAndTheDescription()
    {
        // Arrange
        var description = WorkoutDescription.From("Focus on tempo");

        // Act
        var workout = Workout.Rehydrate(Id.New(), Id.New(), null, Name, description, WorkoutDate, WorkoutStatus.DRAFT, []);

        // Assert
        workout.Name.Should().Be(Name);
        workout.Description.Should().Be(description);
    }

    [Fact]
    public void UpdateDetails_ShouldReplaceNameDescriptionAndDay()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, WorkoutDescription.From("Focus on tempo"), WorkoutDate);
        var newName = WorkoutName.From("Push day");
        var newDescription = WorkoutDescription.From("Heavy presses");
        var newDate = new DateTime(2026, 11, 5, 7, 15, 0, DateTimeKind.Utc);

        // Act
        workout.UpdateDetails(newName, newDescription, newDate);

        // Assert
        workout.Name.Should().Be(newName);
        workout.Description.Should().Be(newDescription);
        workout.WorkoutDate.Should().Be(newDate.Date);
    }

    [Fact]
    public void UpdateDetails_ShouldClearTheDescription()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, WorkoutDescription.From("Focus on tempo"), WorkoutDate);

        // Act
        workout.UpdateDetails(Name, null, WorkoutDate);

        // Assert
        workout.Description.Should().BeNull();
    }

    [Fact]
    public void UpdateDetails_ShouldKeepStatusAndExercises()
    {
        // Arrange
        var exerciseId = Id.New();
        var workout = Workout.Rehydrate(
            Id.New(), Id.New(), null, Name, null, WorkoutDate, WorkoutStatus.PUBLISHED,
            [WorkoutExercise.Rehydrate(Id.New(), exerciseId, Effort.MID, [FourByTen])]);

        // Act
        workout.UpdateDetails(WorkoutName.From("Push day"), null, WorkoutDate);

        // Assert
        workout.Status.Should().Be(WorkoutStatus.PUBLISHED);
        workout.Exercises.Should().ContainSingle().Which.ExerciseId.Should().Be(exerciseId);
    }

    [Fact]
    public void Reassign_ShouldChangeTheOwnerAndTheTrainer()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);
        var newOwner = Id.New();
        var newTrainer = Id.New();

        // Act
        workout.Reassign(newOwner, newTrainer);

        // Assert
        workout.UserId.Should().Be(newOwner);
        workout.TrainerId.Should().Be(newTrainer);
    }

    [Fact]
    public void Reassign_ShouldClearTheTrainer()
    {
        // Arrange
        var ownerId = Id.New();
        var workout = Workout.Create(ownerId, Id.New(), Name, null, WorkoutDate);

        // Act
        workout.Reassign(ownerId, null);

        // Assert
        workout.UserId.Should().Be(ownerId);
        workout.TrainerId.Should().BeNull();
    }

    [Fact]
    public void Reassign_ShouldRejectATrainerWhoIsTheOwnerWithoutChangingTheWorkout()
    {
        // Arrange
        var ownerId = Id.New();
        var trainerId = Id.New();
        var workout = Workout.Create(ownerId, trainerId, Name, null, WorkoutDate);
        var newOwner = Id.New();

        // Act
        var act = () => workout.Reassign(newOwner, newOwner);

        // Assert
        act.Should().Throw<TrainerCannotBeWorkoutOwnerException>();
        workout.UserId.Should().Be(ownerId);
        workout.TrainerId.Should().Be(trainerId);
    }

    [Fact]
    public void ReorderExercises_ShouldPutTheExercisesInTheGivenOrder()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids);
        var exercises = ids.Select(entryId => ExerciseOf(workout, entryId)).ToArray();

        // Act
        workout.ReorderExercises([exercises[2], exercises[0], exercises[1]]);

        // Assert
        workout.Exercises.Select(entry => entry.ExerciseId).Should().Equal(exercises[2], exercises[0], exercises[1]);
    }

    [Fact]
    public void ReorderExercises_ShouldKeepTheEntryIdEffortSetsAndSuperSeries()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids);
        var superSeriesId = workout.CreateSuperSeries([ids[0], ids[1]]);
        var exercises = ids.Select(entryId => ExerciseOf(workout, entryId)).ToArray();
        var before = workout.Exercises.ToDictionary(entry => entry.Id);

        // Act
        workout.ReorderExercises([exercises[1], exercises[2], exercises[0]]);

        // Assert
        workout.Exercises.Select(entry => entry.Id).Should().Equal(ids[1], ids[2], ids[0]);
        workout.Exercises.Should().AllSatisfy(entry =>
        {
            entry.Effort.Should().Be(before[entry.Id].Effort);
            entry.Sets.Should().Equal(before[entry.Id].Sets);
        });
        SuperSeriesOf(workout, ids[0]).Should().Be(superSeriesId);
        SuperSeriesOf(workout, ids[1]).Should().Be(superSeriesId);
        SuperSeriesOf(workout, ids[2]).Should().BeNull();
    }

    [Fact]
    public void ReorderExercises_ShouldAcceptAnEmptyOrderOnAWorkoutWithoutExercises()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);

        // Act
        workout.ReorderExercises([]);

        // Assert
        workout.Exercises.Should().BeEmpty();
    }

    [Fact]
    public void ReorderExercises_ShouldRejectAMissingExerciseAndKeepTheOrder()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids);
        var exercises = ids.Select(entryId => ExerciseOf(workout, entryId)).ToArray();

        // Act
        var act = () => workout.ReorderExercises([exercises[1], exercises[0]]);

        // Assert
        act.Should().Throw<WorkoutExerciseOrderMismatchException>();
        workout.Exercises.Select(entry => entry.ExerciseId).Should().Equal(exercises);
    }

    [Fact]
    public void ReorderExercises_ShouldRejectAnExtraExerciseAndKeepTheOrder()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids);
        var exercises = ids.Select(entryId => ExerciseOf(workout, entryId)).ToArray();

        // Act
        var act = () => workout.ReorderExercises([exercises[2], exercises[1], exercises[0], Id.New()]);

        // Assert
        act.Should().Throw<WorkoutExerciseOrderMismatchException>();
        workout.Exercises.Select(entry => entry.ExerciseId).Should().Equal(exercises);
    }

    [Fact]
    public void ReorderExercises_ShouldRejectAnExerciseNotInTheWorkoutAndKeepTheOrder()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids);
        var exercises = ids.Select(entryId => ExerciseOf(workout, entryId)).ToArray();

        // Act
        var act = () => workout.ReorderExercises([exercises[2], exercises[1], Id.New()]);

        // Assert
        act.Should().Throw<WorkoutExerciseOrderMismatchException>();
        workout.Exercises.Select(entry => entry.ExerciseId).Should().Equal(exercises);
    }

    [Fact]
    public void ReorderExercises_ShouldRejectARepeatedExerciseAndKeepTheOrder()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids);
        var exercises = ids.Select(entryId => ExerciseOf(workout, entryId)).ToArray();

        // Act
        var act = () => workout.ReorderExercises([exercises[2], exercises[2], exercises[0]]);

        // Assert
        act.Should().Throw<WorkoutExerciseOrderMismatchException>();
        workout.Exercises.Select(entry => entry.ExerciseId).Should().Equal(exercises);
    }

    [Fact]
    public void ReorderExercises_ShouldRejectTheWorkoutEntryIdsInPlaceOfTheCatalogExercises()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids);
        var exercises = ids.Select(entryId => ExerciseOf(workout, entryId)).ToArray();

        // Act
        var act = () => workout.ReorderExercises([ids[2], ids[1], ids[0]]);

        // Assert
        act.Should().Throw<WorkoutExerciseOrderMismatchException>();
        workout.Exercises.Select(entry => entry.ExerciseId).Should().Equal(exercises);
    }

    [Fact]
    public void AddExercise_ShouldKeepThePerSideFlagAndTheRest()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);
        var exerciseId = Id.New();
        var rest = TimeInMinutes.From(1.25m);

        // Act
        workout.AddExercise(exerciseId, Effort.MID, [ThreeByTen], perSide: true, rest: rest);

        // Assert
        var entry = workout.Exercises.Single();
        entry.PerSide.Should().BeTrue();
        entry.Rest.Should().Be(rest);
    }

    [Fact]
    public void UpdateExercise_ShouldReplaceThePerSideFlagAndTheRest()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);
        var exerciseId = Id.New();
        workout.AddExercise(exerciseId, Effort.MID, [ThreeByTen]);
        var rest = TimeInMinutes.From(1.5m);

        // Act
        workout.UpdateExercise(exerciseId, Effort.MID, [ThreeByTen], perSide: true, rest: rest);

        // Assert
        var entry = workout.Exercises.Single();
        entry.PerSide.Should().BeTrue();
        entry.Rest.Should().Be(rest);
    }

    [Fact]
    public void UpdateExercise_ShouldClearThePerSideFlagAndTheRestWhenOmitted()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);
        var exerciseId = Id.New();
        workout.AddExercise(exerciseId, Effort.MID, [ThreeByTen], perSide: true, rest: TimeInMinutes.From(2m));

        // Act
        workout.UpdateExercise(exerciseId, Effort.MID, [ThreeByTen]);

        // Assert
        var entry = workout.Exercises.Single();
        entry.PerSide.Should().BeFalse();
        entry.Rest.Should().BeNull();
    }

    [Fact]
    public void UpdateExercise_ShouldKeepThePerSideFlagAndTheRestWhenTheSuperSeriesRejectsTheUpdate()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);
        var first = Id.New();
        var second = Id.New();
        var rest = TimeInMinutes.From(2m);
        workout.AddExercise(first, Effort.MID, [FourByTen], perSide: true, rest: rest);
        workout.AddExercise(second, Effort.MID, [FourByTen]);
        workout.CreateSuperSeries([EntryOf(workout, first), EntryOf(workout, second)]);

        // Act
        var act = () => workout.UpdateExercise(first, Effort.HIGH, [ThreeByTen], perSide: false, rest: null);

        // Assert
        act.Should().Throw<SuperSeriesSetCountMismatchException>();
        var entry = workout.Exercises.Single(item => item.ExerciseId == first);
        entry.PerSide.Should().BeTrue();
        entry.Rest.Should().Be(rest);
    }

    [Fact]
    public void Create_ShouldStartAsAStandaloneWorkout()
    {
        // Arrange & Act
        var workout = Workout.Create(Id.New(), null, Name, null, WorkoutDate);

        // Assert
        workout.MesocycleId.Should().BeNull();
        workout.MesocycleWeekNumber.Should().BeNull();
    }

    [Fact]
    public void LinkToMesocycle_ShouldPlaceADraftWorkoutInTheWeek()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.DRAFT);
        var mesocycleId = Id.New();

        // Act
        workout.LinkToMesocycle(mesocycleId, WeekNumber.From(2));

        // Assert
        workout.MesocycleId.Should().Be(mesocycleId);
        workout.MesocycleWeekNumber.Should().Be(WeekNumber.From(2));
    }

    [Fact]
    public void LinkToMesocycle_ShouldMoveAPublishedWorkoutToAnotherWeek()
    {
        // Arrange
        var mesocycleId = Id.New();
        var workout = APlacedWorkoutIn(WorkoutStatus.PUBLISHED, mesocycleId, WeekNumber.From(2));

        // Act
        workout.LinkToMesocycle(mesocycleId, WeekNumber.From(3));

        // Assert
        workout.MesocycleId.Should().Be(mesocycleId);
        workout.MesocycleWeekNumber.Should().Be(WeekNumber.From(3));
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.PUBLISHED)]
    public void LinkToMesocycle_ShouldMoveAWorkoutThatIsNotExecutedToAnotherMesocycle(WorkoutStatus status)
    {
        // Arrange
        var workout = APlacedWorkoutIn(status, Id.New(), WeekNumber.From(2));
        var otherMesocycleId = Id.New();

        // Act
        workout.LinkToMesocycle(otherMesocycleId, WeekNumber.From(1));

        // Assert
        workout.MesocycleId.Should().Be(otherMesocycleId);
        workout.MesocycleWeekNumber.Should().Be(WeekNumber.From(1));
    }

    [Theory]
    [InlineData(WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.COMPLETED)]
    [InlineData(WorkoutStatus.REVIEWED)]
    public void LinkToMesocycle_ShouldRejectMovingALockedWorkoutAndKeepItsPlacement(WorkoutStatus status)
    {
        // Arrange
        var mesocycleId = Id.New();
        var workout = APlacedWorkoutIn(status, mesocycleId, WeekNumber.From(2));

        // Act
        var act = () => workout.LinkToMesocycle(mesocycleId, WeekNumber.From(3));

        // Assert
        act.Should().Throw<WorkoutPlacementLockedException>();
        workout.MesocycleId.Should().Be(mesocycleId);
        workout.MesocycleWeekNumber.Should().Be(WeekNumber.From(2));
    }

    [Theory]
    [InlineData(WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.COMPLETED)]
    [InlineData(WorkoutStatus.REVIEWED)]
    public void LinkToMesocycle_ShouldRejectPlacingALockedStandaloneWorkout(WorkoutStatus status)
    {
        // Arrange
        var workout = AWorkoutIn(status);

        // Act
        var act = () => workout.LinkToMesocycle(Id.New(), WeekNumber.From(1));

        // Assert
        act.Should().Throw<WorkoutPlacementLockedException>();
        workout.MesocycleId.Should().BeNull();
        workout.MesocycleWeekNumber.Should().BeNull();
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.PUBLISHED)]
    public void UnlinkFromMesocycle_ShouldLeaveAPlannedWorkoutStandalone(WorkoutStatus status)
    {
        // Arrange
        var workout = APlacedWorkoutIn(status, Id.New(), WeekNumber.From(2));

        // Act
        workout.UnlinkFromMesocycle();

        // Assert
        workout.MesocycleId.Should().BeNull();
        workout.MesocycleWeekNumber.Should().BeNull();
    }

    [Theory]
    [InlineData(WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.COMPLETED)]
    [InlineData(WorkoutStatus.REVIEWED)]
    public void UnlinkFromMesocycle_ShouldRejectALockedWorkoutAndKeepItsPlacement(WorkoutStatus status)
    {
        // Arrange
        var mesocycleId = Id.New();
        var workout = APlacedWorkoutIn(status, mesocycleId, WeekNumber.From(2));

        // Act
        var act = () => workout.UnlinkFromMesocycle();

        // Assert
        act.Should().Throw<WorkoutPlacementLockedException>();
        workout.MesocycleId.Should().Be(mesocycleId);
        workout.MesocycleWeekNumber.Should().Be(WeekNumber.From(2));
    }

    [Fact]
    public void Rehydrate_ShouldRestoreTheStoredPlacement()
    {
        // Arrange
        var mesocycleId = Id.New();

        // Act
        var workout = APlacedWorkoutIn(WorkoutStatus.COMPLETED, mesocycleId, WeekNumber.From(4));

        // Assert
        workout.MesocycleId.Should().Be(mesocycleId);
        workout.MesocycleWeekNumber.Should().Be(WeekNumber.From(4));
    }

    [Fact]
    public void Rehydrate_ShouldRestoreAStandaloneWorkoutWithoutPlacement()
    {
        // Arrange & Act
        var workout = AWorkoutIn(WorkoutStatus.DRAFT);

        // Assert
        workout.MesocycleId.Should().BeNull();
        workout.MesocycleWeekNumber.Should().BeNull();
    }

    [Fact]
    public void Rehydrate_ShouldRejectAMesocycleWithoutWeekNumber()
    {
        // Arrange
        var mesocycleId = Id.New();

        // Act
        var act = () => Workout.Rehydrate(Id.New(), Id.New(), null, Name, null, WorkoutDate, WorkoutStatus.DRAFT, [], mesocycleId, null);

        // Assert
        act.Should().Throw<IncompleteWorkoutPlacementException>();
    }

    [Fact]
    public void Rehydrate_ShouldRejectAWeekNumberWithoutMesocycle()
    {
        // Arrange
        var weekNumber = WeekNumber.From(1);

        // Act
        var act = () => Workout.Rehydrate(Id.New(), Id.New(), null, Name, null, WorkoutDate, WorkoutStatus.DRAFT, [], null, weekNumber);

        // Assert
        act.Should().Throw<IncompleteWorkoutPlacementException>();
    }

    private static Workout APlacedWorkoutIn(WorkoutStatus status, Id mesocycleId, WeekNumber weekNumber)
        => Workout.Rehydrate(Id.New(), Id.New(), null, Name, null, WorkoutDate, status, [], mesocycleId, weekNumber,
            reviewedAt: ReviewedAtFor(status), reviewedBy: ReviewerFor(status));

    private static WorkoutTemplate ATemplateWithASuperSeries(Id trainerId)
    {
        var template = WorkoutTemplate.Create(trainerId, Name, WorkoutDescription.From("Focus on tempo"));
        template.AddExercise(Id.New(), Effort.HIGH, [FourByTen], perSide: true, rest: TimeInMinutes.From(1.5m));
        template.AddExercise(Id.New(), Effort.MID, [ThreeByTen, new SetPrescription(1, new SetTarget.MaxReps())]);
        template.AddExercise(Id.New(), Effort.LOW, [new SetPrescription(3, new SetTarget.Duration(40))]);
        template.CreateSuperSeries([template.Exercises[0].Id, template.Exercises[1].Id]);

        return template;
    }

    [Fact]
    public void CreateFromTemplate_ShouldCopyTheNameDescriptionAndEveryExerciseInOrder()
    {
        // Arrange
        var template = ATemplateWithASuperSeries(Id.New());

        // Act
        var workout = Workout.CreateFromTemplate(template, Id.New(), WorkoutDate);

        // Assert
        workout.Name.Should().Be(template.Name);
        workout.Description.Should().Be(template.Description);
        workout.Exercises.Should().HaveCount(3);
        workout.Exercises.Zip(template.Exercises).Should().AllSatisfy(pair =>
        {
            var (copy, source) = pair;
            copy.ExerciseId.Should().Be(source.ExerciseId);
            copy.Effort.Should().Be(source.Effort);
            copy.Sets.Should().Equal(source.Sets);
            copy.PerSide.Should().Be(source.PerSide);
            copy.Rest.Should().Be(source.Rest);
            copy.SuperSeriesId.Should().Be(source.SuperSeriesId);
        });
    }

    [Fact]
    public void CreateFromTemplate_ShouldGiveEveryCopiedEntryANewIdentifier()
    {
        // Arrange
        var template = ATemplateWithASuperSeries(Id.New());

        // Act
        var workout = Workout.CreateFromTemplate(template, Id.New(), WorkoutDate);

        // Assert
        workout.Exercises.Select(entry => entry.Id).Should().NotIntersectWith(template.Exercises.Select(entry => entry.Id));
        workout.Exercises.Should().OnlyContain(entry => entry.IsNew);
    }

    [Fact]
    public void CreateFromTemplate_ShouldPrescribeANewDraftFromTheTemplateTrainerToTheUser()
    {
        // Arrange
        var trainerId = Id.New();
        var userId = Id.New();
        var template = ATemplateWithASuperSeries(trainerId);

        // Act
        var workout = Workout.CreateFromTemplate(template, userId, WorkoutDate);

        // Assert
        workout.UserId.Should().Be(userId);
        workout.TrainerId.Should().Be(trainerId);
        workout.Status.Should().Be(WorkoutStatus.DRAFT);
        workout.IsNew.Should().BeTrue();
        workout.WorkoutDate.Should().Be(WorkoutDate.Date);
        workout.MesocycleId.Should().BeNull();
    }

    [Fact]
    public void CreateFromTemplate_ShouldRejectTheTemplateTrainerAsOwner()
    {
        // Arrange
        var trainerId = Id.New();
        var template = ATemplateWithASuperSeries(trainerId);

        // Act
        var act = () => Workout.CreateFromTemplate(template, trainerId, WorkoutDate);

        // Assert
        act.Should().Throw<TrainerCannotBeWorkoutOwnerException>();
    }

    [Fact]
    public void CreateFromTemplate_ShouldAcceptAnEmptyTemplate()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, null);

        // Act
        var workout = Workout.CreateFromTemplate(template, Id.New(), WorkoutDate);

        // Assert
        workout.Exercises.Should().BeEmpty();
    }

    [Fact]
    public void CreateFromTemplate_ShouldNotFollowLaterChangesToTheTemplate()
    {
        // Arrange
        var template = ATemplateWithASuperSeries(Id.New());
        var workout = Workout.CreateFromTemplate(template, Id.New(), WorkoutDate);
        var removed = template.Exercises[0].ExerciseId;

        // Act
        template.RemoveExercise(removed);

        // Assert
        workout.Exercises.Select(entry => entry.ExerciseId).Should().Contain(removed);
        workout.Exercises[0].SuperSeriesId.Should().NotBeNull();
        workout.Exercises[1].SuperSeriesId.Should().NotBeNull();
    }

    private static readonly DateTime RecordedAt = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);

    private static SetResult Reps(int value) => new SetResult.Reps(RepCount.From(value));

    private static SetResult Seconds(int value) => new SetResult.Duration(DurationInSeconds.From(value));

    private static Workout AStartedWorkoutWith(params (Id ExerciseId, SetPrescription[] Sets)[] entries)
        => AWorkoutWith(WorkoutStatus.STARTED, entries);

    private static Workout AWorkoutWith(WorkoutStatus status, params (Id ExerciseId, SetPrescription[] Sets)[] entries)
        => Workout.Rehydrate(Id.New(), Id.New(), null, Name, null, WorkoutDate, status,
            entries.Select(entry => WorkoutExercise.Rehydrate(Id.New(), entry.ExerciseId, Effort.MID, entry.Sets)).ToList(),
            reviewedAt: ReviewedAtFor(status), reviewedBy: ReviewerFor(status));

    private static WorkoutExercise EntryFor(Workout workout, Id exerciseId)
        => workout.Exercises.Single(entry => entry.ExerciseId == exerciseId);

    [Fact]
    public void AddSetRecord_ShouldRecordTheSetOnTheExerciseAndLeaveTheOthersAlone()
    {
        // Arrange
        var bench = Id.New();
        var row = Id.New();
        var workout = AStartedWorkoutWith((bench, [FourByTen]), (row, [ThreeByTen]));

        // Act
        workout.AddSetRecord(bench, Reps(8), Weight.From(62.5m), RecordedAt);

        // Assert
        var setRecord = EntryFor(workout, bench).SetRecords.Should().ContainSingle().Subject;
        setRecord.IsNew.Should().BeTrue();
        setRecord.Result.Should().Be(Reps(8));
        setRecord.Weight.Should().Be(Weight.From(62.5m));
        setRecord.RegisteredAtUtc.Should().Be(RecordedAt);
        EntryFor(workout, row).SetRecords.Should().BeEmpty();
    }

    [Fact]
    public void AddSetRecord_ShouldPutTheNewSetRecordLastWhateverItsRegistrationTime()
    {
        // Arrange
        var bench = Id.New();
        var workout = AStartedWorkoutWith((bench, [FourByTen]));
        workout.AddSetRecord(bench, Reps(8), Weight.From(60), RecordedAt.AddMinutes(5));
        workout.AddSetRecord(bench, Reps(10), Weight.From(55), RecordedAt.AddMinutes(1));

        // Act
        workout.AddSetRecord(bench, Reps(9), Weight.From(57.5m), RecordedAt.AddMinutes(3));

        // Assert
        EntryFor(workout, bench).SetRecords.Select(record => record.RegisteredAtUtc)
            .Should().Equal(RecordedAt.AddMinutes(5), RecordedAt.AddMinutes(1), RecordedAt.AddMinutes(3));
    }

    [Fact]
    public void AddSetRecord_ShouldKeepTheOrderAddedForTheSameRegistrationTime()
    {
        // Arrange
        var bench = Id.New();
        var workout = AStartedWorkoutWith((bench, [FourByTen]));
        workout.AddSetRecord(bench, Reps(10), Weight.From(60), RecordedAt);

        // Act
        workout.AddSetRecord(bench, Reps(7), Weight.From(60), RecordedAt);

        // Assert
        EntryFor(workout, bench).SetRecords.Select(record => record.Result).Should().Equal(Reps(10), Reps(7));
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.REVIEWED)]
    public void AddSetRecord_ShouldRejectAWorkoutThatIsNotStartedOrCompletedAndRecordNothing(WorkoutStatus status)
    {
        // Arrange
        var bench = Id.New();
        var workout = AWorkoutWith(status, (bench, [FourByTen]));

        // Act
        var act = () => workout.AddSetRecord(bench, Reps(8), Weight.From(60), RecordedAt);

        // Assert
        act.Should().Throw<WorkoutResultsLockedException>();
        EntryFor(workout, bench).SetRecords.Should().BeEmpty();
    }

    [Fact]
    public void AddSetRecord_ShouldRejectAnExerciseNotInTheWorkout()
    {
        // Arrange
        var bench = Id.New();
        var workout = AStartedWorkoutWith((bench, [FourByTen]));

        // Act
        var act = () => workout.AddSetRecord(Id.New(), Reps(8), Weight.From(60), RecordedAt);

        // Assert
        act.Should().Throw<ExerciseNotInWorkoutException>();
        EntryFor(workout, bench).SetRecords.Should().BeEmpty();
    }

    [Fact]
    public void AddSetRecord_ShouldAcceptADurationResultOnAnExerciseWithADurationPrescription()
    {
        // Arrange
        var plank = Id.New();
        var workout = AStartedWorkoutWith((plank, [new SetPrescription(3, new SetTarget.Duration(40))]));

        // Act
        workout.AddSetRecord(plank, Seconds(40), null, RecordedAt);

        // Assert
        var setRecord = EntryFor(workout, plank).SetRecords.Should().ContainSingle().Subject;
        setRecord.Result.Should().Be(Seconds(40));
        setRecord.Weight.Should().BeNull();
    }

    [Fact]
    public void AddSetRecord_ShouldAcceptAMixOfTimeBasedPrescriptions()
    {
        // Arrange
        var plank = Id.New();
        var oneByMinutes = new SetPrescription(1, new SetTarget.Minutes(TimeInMinutes.From(1.5m)));
        var workout = AStartedWorkoutWith((plank, [new SetPrescription(2, new SetTarget.Duration(40)), oneByMinutes]));

        // Act
        workout.AddSetRecord(plank, Seconds(60), null, RecordedAt);

        // Assert
        EntryFor(workout, plank).SetRecords.Should().ContainSingle().Which.Result.Should().Be(Seconds(60));
    }

    [Fact]
    public void AddSetRecord_ShouldRejectARepsResultOnATimeBasedExercise()
    {
        // Arrange
        var plank = Id.New();
        var workout = AStartedWorkoutWith((plank, [new SetPrescription(3, new SetTarget.Duration(40))]));

        // Act
        var act = () => workout.AddSetRecord(plank, Reps(10), null, RecordedAt);

        // Assert
        act.Should().Throw<SetRecordNotAllowedForTargetException>();
        EntryFor(workout, plank).SetRecords.Should().BeEmpty();
    }

    [Fact]
    public void AddSetRecord_ShouldRejectADurationResultOnARepBasedExercise()
    {
        // Arrange
        var bench = Id.New();
        var workout = AStartedWorkoutWith((bench, [ThreeByTen]));

        // Act
        var act = () => workout.AddSetRecord(bench, Seconds(40), null, RecordedAt);

        // Assert
        act.Should().Throw<SetRecordNotAllowedForTargetException>();
        EntryFor(workout, bench).SetRecords.Should().BeEmpty();
    }

    [Fact]
    public void AddSetRecord_ShouldAcceptASetWithoutWeight()
    {
        // Arrange
        var jumpingJacks = Id.New();
        var workout = AStartedWorkoutWith((jumpingJacks, [ThreeByTen]));

        // Act
        workout.AddSetRecord(jumpingJacks, Reps(10), null, RecordedAt);

        // Assert
        var setRecord = EntryFor(workout, jumpingJacks).SetRecords.Should().ContainSingle().Subject;
        setRecord.Result.Should().Be(Reps(10));
        setRecord.Weight.Should().BeNull();
    }

    [Fact]
    public void AddSetRecord_ShouldAcceptAWeightedTimedSet()
    {
        // Arrange
        var plank = Id.New();
        var workout = AStartedWorkoutWith((plank, [new SetPrescription(3, new SetTarget.Duration(45))]));

        // Act
        workout.AddSetRecord(plank, Seconds(45), Weight.From(10), RecordedAt);

        // Assert
        var setRecord = EntryFor(workout, plank).SetRecords.Should().ContainSingle().Subject;
        setRecord.Result.Should().Be(Seconds(45));
        setRecord.Weight.Should().Be(Weight.From(10));
    }

    [Fact]
    public void AddSetRecord_ShouldResolveAnEmptyResultFromAFixedRepsPrescription()
    {
        // Arrange
        var bench = Id.New();
        var workout = AStartedWorkoutWith((bench, [ThreeByTen]));

        // Act
        workout.AddSetRecord(bench, null, null, RecordedAt);

        // Assert
        var setRecord = EntryFor(workout, bench).SetRecords.Should().ContainSingle().Subject;
        setRecord.Result.Should().Be(Reps(10));
        setRecord.Weight.Should().BeNull();
    }

    [Fact]
    public void AddSetRecord_ShouldResolveAnEmptyResultFromADurationPrescription()
    {
        // Arrange
        var plank = Id.New();
        var workout = AStartedWorkoutWith((plank, [new SetPrescription(3, new SetTarget.Duration(45))]));

        // Act
        workout.AddSetRecord(plank, null, null, RecordedAt);

        // Assert
        EntryFor(workout, plank).SetRecords.Should().ContainSingle().Which.Result.Should().Be(Seconds(45));
    }

    [Theory]
    [InlineData(1.5, 90)]
    [InlineData(1.01, 61)]
    [InlineData(0.01, 1)]
    public void AddSetRecord_ShouldResolveAnEmptyResultFromAMinutesPrescriptionAsRoundedSeconds(decimal minutes, int seconds)
    {
        // Arrange
        var bike = Id.New();
        var workout = AStartedWorkoutWith((bike, [new SetPrescription(2, new SetTarget.Minutes(TimeInMinutes.From(minutes)))]));

        // Act
        workout.AddSetRecord(bike, null, null, RecordedAt);

        // Assert
        EntryFor(workout, bike).SetRecords.Should().ContainSingle().Which.Result.Should().Be(Seconds(seconds));
    }

    [Fact]
    public void AddSetRecord_ShouldResolveAnEmptyResultFromThePrescribedSetAtThePositionOfTheNextRecord()
    {
        // Arrange
        var bench = Id.New();
        var workout = AStartedWorkoutWith((bench, [new SetPrescription(2, new SetTarget.Reps(10)), new SetPrescription(1, new SetTarget.Reps(8))]));
        workout.AddSetRecord(bench, null, null, RecordedAt);
        workout.AddSetRecord(bench, null, null, RecordedAt.AddMinutes(2));

        // Act
        workout.AddSetRecord(bench, null, null, RecordedAt.AddMinutes(4));

        // Assert
        EntryFor(workout, bench).SetRecords.Select(record => record.Result).Should().Equal(Reps(10), Reps(10), Reps(8));
    }

    [Fact]
    public void AddSetRecord_ShouldFollowTheCountOfRecordsAfterARemovalWhenResolvingAnEmptyResult()
    {
        // Arrange
        var bench = Id.New();
        var workout = AStartedWorkoutWith((bench, [new SetPrescription(2, new SetTarget.Reps(10)), new SetPrescription(1, new SetTarget.Reps(8))]));
        workout.AddSetRecord(bench, null, null, RecordedAt);
        workout.AddSetRecord(bench, null, null, RecordedAt.AddMinutes(2));
        workout.RemoveSetRecord(bench, EntryFor(workout, bench).SetRecords[0].Id, RecordedAt.AddMinutes(3));

        // Act
        workout.AddSetRecord(bench, null, null, RecordedAt.AddMinutes(4));

        // Assert
        EntryFor(workout, bench).SetRecords.Select(record => record.Result).Should().Equal(Reps(10), Reps(10));
    }

    [Fact]
    public void AddSetRecord_ShouldRequireAResultForARepsRangePrescription()
    {
        // Arrange
        var squat = Id.New();
        var workout = AStartedWorkoutWith((squat, [new SetPrescription(3, new SetTarget.RepsRange(8, 12))]));

        // Act
        var act = () => workout.AddSetRecord(squat, null, null, RecordedAt);

        // Assert
        act.Should().Throw<SetResultRequiredException>();
        EntryFor(workout, squat).SetRecords.Should().BeEmpty();
    }

    [Fact]
    public void AddSetRecord_ShouldRequireAResultForAMaxRepsPrescription()
    {
        // Arrange
        var pullUps = Id.New();
        var workout = AStartedWorkoutWith((pullUps, [new SetPrescription(3, new SetTarget.MaxReps())]));

        // Act
        var act = () => workout.AddSetRecord(pullUps, null, null, RecordedAt);

        // Assert
        act.Should().Throw<SetResultRequiredException>();
        EntryFor(workout, pullUps).SetRecords.Should().BeEmpty();
    }

    [Fact]
    public void AddSetRecord_ShouldRequireAResultBeyondThePrescribedSets()
    {
        // Arrange
        var bench = Id.New();
        var workout = AStartedWorkoutWith((bench, [ThreeByTen]));
        workout.AddSetRecord(bench, null, null, RecordedAt);
        workout.AddSetRecord(bench, null, null, RecordedAt.AddMinutes(2));
        workout.AddSetRecord(bench, null, null, RecordedAt.AddMinutes(4));

        // Act
        var act = () => workout.AddSetRecord(bench, null, null, RecordedAt.AddMinutes(6));

        // Assert
        act.Should().Throw<SetResultRequiredException>();
        EntryFor(workout, bench).SetRecords.Should().HaveCount(3);
    }

    [Fact]
    public void AddSetRecord_ShouldRejectAnEmptyResultOnAMixedExerciseBeforeResolvingIt()
    {
        // Arrange
        var burpees = Id.New();
        var oneByMinutes = new SetPrescription(1, new SetTarget.Minutes(TimeInMinutes.From(1.5m)));
        var workout = AStartedWorkoutWith((burpees, [new SetPrescription(2, new SetTarget.Reps(10)), oneByMinutes]));

        // Act
        var act = () => workout.AddSetRecord(burpees, null, null, RecordedAt);

        // Assert
        act.Should().Throw<SetRecordNotAllowedForTargetException>();
        EntryFor(workout, burpees).SetRecords.Should().BeEmpty();
    }

    [Fact]
    public void AddSetRecord_ShouldRejectAnExerciseMixingRepsAndMinutes()
    {
        // Arrange
        var burpees = Id.New();
        var oneByMinutes = new SetPrescription(1, new SetTarget.Minutes(TimeInMinutes.From(1.5m)));
        var workout = AStartedWorkoutWith((burpees, [new SetPrescription(2, new SetTarget.Reps(10)), oneByMinutes]));

        // Act
        var act = () => workout.AddSetRecord(burpees, Reps(10), Weight.From(0), RecordedAt);

        // Assert
        act.Should().Throw<SetRecordNotAllowedForTargetException>();
        EntryFor(workout, burpees).SetRecords.Should().BeEmpty();
    }

    [Fact]
    public void AddSetRecord_ShouldAcceptAMixOfRepBasedPrescriptions()
    {
        // Arrange
        var squat = Id.New();
        var workout = AStartedWorkoutWith((squat,
        [
            new SetPrescription(1, new SetTarget.Reps(10)),
            new SetPrescription(2, new SetTarget.RepsRange(8, 12)),
            new SetPrescription(1, new SetTarget.MaxReps()),
        ]));

        // Act
        workout.AddSetRecord(squat, Reps(14), Weight.From(80), RecordedAt);

        // Assert
        EntryFor(workout, squat).SetRecords.Should().ContainSingle()
            .Which.Result.Should().Be(Reps(14));
    }

    [Fact]
    public void AddSetRecord_ShouldAcceptMoreSetRecordsThanPrescribedSets()
    {
        // Arrange
        var bench = Id.New();
        var workout = AStartedWorkoutWith((bench, [ThreeByTen]));
        workout.AddSetRecord(bench, Reps(10), Weight.From(60), RecordedAt);
        workout.AddSetRecord(bench, Reps(10), Weight.From(60), RecordedAt.AddMinutes(2));
        workout.AddSetRecord(bench, Reps(9), Weight.From(60), RecordedAt.AddMinutes(4));

        // Act
        workout.AddSetRecord(bench, Reps(8), Weight.From(60), RecordedAt.AddMinutes(6));

        // Assert
        EntryFor(workout, bench).SetRecords.Should().HaveCount(4);
    }

    [Fact]
    public void AddSetRecord_ShouldNotBeBlockedByTheLockOfAStartedWorkout()
    {
        // Arrange
        var bench = Id.New();
        var workout = AStartedWorkoutWith((bench, [FourByTen]));

        // Act
        workout.AddSetRecord(bench, Reps(8), Weight.From(60), RecordedAt);

        // Assert
        workout.IsLocked.Should().BeTrue();
        EntryFor(workout, bench).SetRecords.Should().ContainSingle();
    }

    [Fact]
    public void Rehydrate_ShouldRestoreTheStoredSetRecordsOfEachExercise()
    {
        // Arrange
        var stored = SetRecord.Rehydrate(Id.New(), Reps(8), Weight.From(60), RecordedAt);
        var bench = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [FourByTen], setRecords: [stored]);

        // Act
        var workout = Workout.Rehydrate(Id.New(), Id.New(), null, Name, null, WorkoutDate, WorkoutStatus.COMPLETED, [bench]);

        // Assert
        workout.Exercises.Single().SetRecords.Should().Equal(stored);
        workout.Exercises.Single().SetRecords.Single().IsNew.Should().BeFalse();
    }

    [Fact]
    public void CreateFromTemplate_ShouldStartWithoutSetRecords()
    {
        // Arrange
        var template = ATemplateWithASuperSeries(Id.New());

        // Act
        var workout = Workout.CreateFromTemplate(template, Id.New(), WorkoutDate);

        // Assert
        workout.Exercises.Should().AllSatisfy(entry => entry.SetRecords.Should().BeEmpty());
    }

    [Fact]
    public void UpdateExercise_ShouldRejectADurationTargetForAnExerciseWithSetRecordsAndChangeNothing()
    {
        // Arrange
        var bench = Id.New();
        var rest = TimeInMinutes.From(2);
        var stored = SetRecord.Rehydrate(Id.New(), Reps(8), Weight.From(60), RecordedAt);
        var entry = WorkoutExercise.Rehydrate(Id.New(), bench, Effort.MID, [ThreeByTen], perSide: true, rest: rest, setRecords: [stored]);
        var workout = Workout.Rehydrate(Id.New(), Id.New(), null, Name, null, WorkoutDate, WorkoutStatus.STARTED, [entry]);

        // Act
        var act = () => workout.UpdateExercise(bench, Effort.HIGH, [new SetPrescription(3, new SetTarget.Duration(40))]);

        // Assert
        act.Should().Throw<SetRecordNotAllowedForTargetException>();
        var unchanged = EntryFor(workout, bench);
        unchanged.Effort.Should().Be(Effort.MID);
        unchanged.Sets.Should().Equal(ThreeByTen);
        unchanged.PerSide.Should().BeTrue();
        unchanged.Rest.Should().Be(rest);
        unchanged.SetRecords.Should().Equal(stored);
    }

    [Fact]
    public void UpdateExercise_ShouldRejectAMinutesTargetForAnExerciseWithSetRecords()
    {
        // Arrange
        var bench = Id.New();
        var workout = AStartedWorkoutWith((bench, [ThreeByTen]));
        workout.AddSetRecord(bench, Reps(8), Weight.From(60), RecordedAt);
        var minutes = new SetPrescription(1, new SetTarget.Minutes(TimeInMinutes.From(1.5m)));

        // Act
        var act = () => workout.UpdateExercise(bench, Effort.MID, [ThreeByTen, minutes]);

        // Assert
        act.Should().Throw<SetRecordNotAllowedForTargetException>();
        EntryFor(workout, bench).Sets.Should().Equal(ThreeByTen);
    }

    [Fact]
    public void UpdateExercise_ShouldAcceptARepBasedChangeForAnExerciseWithSetRecordsAndKeepThem()
    {
        // Arrange
        var bench = Id.New();
        var workout = AStartedWorkoutWith((bench, [ThreeByTen]));
        workout.AddSetRecord(bench, Reps(8), Weight.From(60), RecordedAt);
        var range = new SetPrescription(3, new SetTarget.RepsRange(8, 12));

        // Act
        workout.UpdateExercise(bench, Effort.MID, [range]);

        // Assert
        var updated = EntryFor(workout, bench);
        updated.Sets.Should().Equal(range);
        updated.SetRecords.Should().ContainSingle();
    }

    [Fact]
    public void UpdateExercise_ShouldAcceptADurationTargetForAnExerciseWithoutSetRecords()
    {
        // Arrange
        var bench = Id.New();
        var workout = AStartedWorkoutWith((bench, [ThreeByTen]));
        var duration = new SetPrescription(3, new SetTarget.Duration(40));

        // Act
        workout.UpdateExercise(bench, Effort.MID, [duration]);

        // Assert
        EntryFor(workout, bench).Sets.Should().Equal(duration);
    }

    [Fact]
    public void UpdateExercise_ShouldRejectARepsTargetForATimeBasedExerciseWithSetRecordsAndChangeNothing()
    {
        // Arrange
        var plank = Id.New();
        var rest = TimeInMinutes.From(1);
        var threeByForty = new SetPrescription(3, new SetTarget.Duration(40));
        var stored = SetRecord.Rehydrate(Id.New(), Seconds(40), null, RecordedAt);
        var entry = WorkoutExercise.Rehydrate(Id.New(), plank, Effort.MID, [threeByForty], perSide: true, rest: rest, setRecords: [stored]);
        var workout = Workout.Rehydrate(Id.New(), Id.New(), null, Name, null, WorkoutDate, WorkoutStatus.STARTED, [entry]);

        // Act
        var act = () => workout.UpdateExercise(plank, Effort.HIGH, [ThreeByTen]);

        // Assert
        act.Should().Throw<SetRecordNotAllowedForTargetException>();
        var unchanged = EntryFor(workout, plank);
        unchanged.Effort.Should().Be(Effort.MID);
        unchanged.Sets.Should().Equal(threeByForty);
        unchanged.PerSide.Should().BeTrue();
        unchanged.Rest.Should().Be(rest);
        unchanged.SetRecords.Should().Equal(stored);
    }

    [Fact]
    public void UpdateExercise_ShouldRejectAMixOfKindsForATimeBasedExerciseWithSetRecords()
    {
        // Arrange
        var plank = Id.New();
        var threeByForty = new SetPrescription(3, new SetTarget.Duration(40));
        var workout = AStartedWorkoutWith((plank, [threeByForty]));
        workout.AddSetRecord(plank, Seconds(40), null, RecordedAt);

        // Act
        var act = () => workout.UpdateExercise(plank, Effort.MID, [threeByForty, ThreeByTen]);

        // Assert
        act.Should().Throw<SetRecordNotAllowedForTargetException>();
        EntryFor(workout, plank).Sets.Should().Equal(threeByForty);
    }

    [Fact]
    public void UpdateExercise_ShouldAcceptATimeBasedChangeForATimeBasedExerciseWithSetRecordsAndKeepThem()
    {
        // Arrange
        var plank = Id.New();
        var workout = AStartedWorkoutWith((plank, [new SetPrescription(3, new SetTarget.Duration(40))]));
        workout.AddSetRecord(plank, Seconds(40), null, RecordedAt);
        var oneMinute = new SetPrescription(3, new SetTarget.Minutes(TimeInMinutes.From(1)));

        // Act
        workout.UpdateExercise(plank, Effort.MID, [oneMinute]);

        // Assert
        var updated = EntryFor(workout, plank);
        updated.Sets.Should().Equal(oneMinute);
        updated.SetRecords.Should().ContainSingle();
    }

    private static SetRecord AStoredSetRecord(int reps, decimal weight, int minutesAfter = 0)
        => SetRecord.Rehydrate(Id.New(), Reps(reps), Weight.From(weight), RecordedAt.AddMinutes(minutesAfter));

    private static Workout AWorkoutWithSetRecords(WorkoutStatus status, Id exerciseId, params SetRecord[] setRecords)
        => Workout.Rehydrate(Id.New(), Id.New(), null, Name, null, WorkoutDate, status,
            [WorkoutExercise.Rehydrate(Id.New(), exerciseId, Effort.MID, [FourByTen], setRecords: setRecords)],
            reviewedAt: ReviewedAtFor(status), reviewedBy: ReviewerFor(status));

    [Fact]
    public void UpdateSetRecord_ShouldReplaceRepsAndWeightAndKeepIdTimeAndPosition()
    {
        // Arrange
        var bench = Id.New();
        var first = AStoredSetRecord(10, 60);
        var second = AStoredSetRecord(8, 60, 2);
        var third = AStoredSetRecord(7, 60, 4);
        var workout = AWorkoutWithSetRecords(WorkoutStatus.STARTED, bench, first, second, third);

        // Act
        workout.UpdateSetRecord(bench, second.Id, Reps(10), Weight.From(62.5m), RecordedAt);

        // Assert
        var setRecords = EntryFor(workout, bench).SetRecords;
        setRecords.Select(record => record.Id).Should().Equal(first.Id, second.Id, third.Id);
        setRecords[1].Result.Should().Be(Reps(10));
        setRecords[1].Weight.Should().Be(Weight.From(62.5m));
        setRecords[1].RegisteredAtUtc.Should().Be(RecordedAt.AddMinutes(2));
    }

    [Fact]
    public void UpdateSetRecord_ShouldClearTheWeightWhenNoneIsGiven()
    {
        // Arrange
        var bench = Id.New();
        var stored = AStoredSetRecord(8, 60);
        var workout = AWorkoutWithSetRecords(WorkoutStatus.STARTED, bench, stored);

        // Act
        workout.UpdateSetRecord(bench, stored.Id, Reps(8), null, RecordedAt);

        // Assert
        stored.Result.Should().Be(Reps(8));
        stored.Weight.Should().BeNull();
    }

    [Fact]
    public void UpdateSetRecord_ShouldCorrectADurationOnATimeBasedExercise()
    {
        // Arrange
        var plank = Id.New();
        var stored = SetRecord.Rehydrate(Id.New(), Seconds(45), Weight.From(10), RecordedAt);
        var entry = WorkoutExercise.Rehydrate(Id.New(), plank, Effort.MID, [new SetPrescription(3, new SetTarget.Duration(45))], setRecords: [stored]);
        var workout = Workout.Rehydrate(Id.New(), Id.New(), null, Name, null, WorkoutDate, WorkoutStatus.STARTED, [entry]);

        // Act
        workout.UpdateSetRecord(plank, stored.Id, Seconds(40), null, RecordedAt);

        // Assert
        stored.Result.Should().Be(Seconds(40));
        stored.Weight.Should().BeNull();
    }

    [Fact]
    public void UpdateSetRecord_ShouldRejectAResultOfTheOtherKindAndChangeNothing()
    {
        // Arrange
        var bench = Id.New();
        var stored = AStoredSetRecord(8, 60);
        var workout = AWorkoutWithSetRecords(WorkoutStatus.STARTED, bench, stored);

        // Act
        var act = () => workout.UpdateSetRecord(bench, stored.Id, Seconds(40), null, RecordedAt);

        // Assert
        act.Should().Throw<SetRecordNotAllowedForTargetException>();
        stored.Result.Should().Be(Reps(8));
        stored.Weight.Should().Be(Weight.From(60));
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.REVIEWED)]
    public void UpdateSetRecord_ShouldRejectAWorkoutThatIsNotStartedOrCompletedAndChangeNothing(WorkoutStatus status)
    {
        // Arrange
        var bench = Id.New();
        var stored = AStoredSetRecord(8, 60);
        var workout = AWorkoutWithSetRecords(status, bench, stored);

        // Act
        var act = () => workout.UpdateSetRecord(bench, stored.Id, Reps(10), Weight.From(70), RecordedAt);

        // Assert
        act.Should().Throw<WorkoutResultsLockedException>();
        stored.Result.Should().Be(Reps(8));
        stored.Weight.Should().Be(Weight.From(60));
    }

    [Fact]
    public void UpdateSetRecord_ShouldRejectAnExerciseNotInTheWorkout()
    {
        // Arrange
        var bench = Id.New();
        var stored = AStoredSetRecord(8, 60);
        var workout = AWorkoutWithSetRecords(WorkoutStatus.STARTED, bench, stored);

        // Act
        var act = () => workout.UpdateSetRecord(Id.New(), stored.Id, Reps(10), Weight.From(70), RecordedAt);

        // Assert
        act.Should().Throw<ExerciseNotInWorkoutException>();
        stored.Result.Should().Be(Reps(8));
    }

    [Fact]
    public void UpdateSetRecord_ShouldRejectASetRecordNotRecordedForTheExerciseAndChangeNothing()
    {
        // Arrange
        var bench = Id.New();
        var stored = AStoredSetRecord(8, 60);
        var workout = AWorkoutWithSetRecords(WorkoutStatus.STARTED, bench, stored);

        // Act
        var act = () => workout.UpdateSetRecord(bench, Id.New(), Reps(10), Weight.From(70), RecordedAt);

        // Assert
        act.Should().Throw<SetRecordNotFoundException>();
        stored.Result.Should().Be(Reps(8));
        stored.Weight.Should().Be(Weight.From(60));
    }

    [Fact]
    public void RemoveSetRecord_ShouldRemoveOnlyThatSetRecordAndKeepTheOrderOfTheRest()
    {
        // Arrange
        var bench = Id.New();
        var first = AStoredSetRecord(10, 60);
        var second = AStoredSetRecord(8, 60, 2);
        var third = AStoredSetRecord(7, 60, 4);
        var workout = AWorkoutWithSetRecords(WorkoutStatus.STARTED, bench, first, second, third);

        // Act
        workout.RemoveSetRecord(bench, second.Id, RecordedAt);

        // Assert
        EntryFor(workout, bench).SetRecords.Should().Equal(first, third);
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.REVIEWED)]
    public void RemoveSetRecord_ShouldRejectAWorkoutThatIsNotStartedOrCompletedAndRemoveNothing(WorkoutStatus status)
    {
        // Arrange
        var bench = Id.New();
        var stored = AStoredSetRecord(8, 60);
        var workout = AWorkoutWithSetRecords(status, bench, stored);

        // Act
        var act = () => workout.RemoveSetRecord(bench, stored.Id, RecordedAt);

        // Assert
        act.Should().Throw<WorkoutResultsLockedException>();
        EntryFor(workout, bench).SetRecords.Should().Equal(stored);
    }

    [Fact]
    public void RemoveSetRecord_ShouldRejectAnExerciseNotInTheWorkout()
    {
        // Arrange
        var bench = Id.New();
        var stored = AStoredSetRecord(8, 60);
        var workout = AWorkoutWithSetRecords(WorkoutStatus.STARTED, bench, stored);

        // Act
        var act = () => workout.RemoveSetRecord(Id.New(), stored.Id, RecordedAt);

        // Assert
        act.Should().Throw<ExerciseNotInWorkoutException>();
        EntryFor(workout, bench).SetRecords.Should().Equal(stored);
    }

    [Fact]
    public void RemoveSetRecord_ShouldRejectASetRecordNotRecordedForTheExercise()
    {
        // Arrange
        var bench = Id.New();
        var stored = AStoredSetRecord(8, 60);
        var workout = AWorkoutWithSetRecords(WorkoutStatus.STARTED, bench, stored);

        // Act
        var act = () => workout.RemoveSetRecord(bench, Id.New(), RecordedAt);

        // Assert
        act.Should().Throw<SetRecordNotFoundException>();
        EntryFor(workout, bench).SetRecords.Should().Equal(stored);
    }

    [Fact]
    public void ReorderSetRecords_ShouldPutTheSetRecordsInTheGivenOrderAndKeepTheirValues()
    {
        // Arrange
        var bench = Id.New();
        var first = AStoredSetRecord(10, 60);
        var second = AStoredSetRecord(8, 62.5m, 2);
        var third = AStoredSetRecord(7, 65, 4);
        var workout = AWorkoutWithSetRecords(WorkoutStatus.STARTED, bench, first, second, third);

        // Act
        workout.ReorderSetRecords(bench, [third.Id, second.Id, first.Id], RecordedAt);

        // Assert
        var setRecords = EntryFor(workout, bench).SetRecords;
        setRecords.Should().Equal(third, second, first);
        setRecords.Select(record => record.Result).Should().Equal(Reps(7), Reps(8), Reps(10));
        setRecords.Select(record => record.Weight).Should().Equal(Weight.From(65), Weight.From(62.5m), Weight.From(60));
    }

    [Fact]
    public void ReorderSetRecords_ShouldLeaveTheSetRecordsOfTheOtherExercisesAlone()
    {
        // Arrange
        var bench = Id.New();
        var row = Id.New();
        var benchFirst = AStoredSetRecord(10, 60);
        var benchSecond = AStoredSetRecord(8, 60, 2);
        var rowFirst = AStoredSetRecord(12, 40, 1);
        var rowSecond = AStoredSetRecord(10, 40, 3);
        var workout = Workout.Rehydrate(Id.New(), Id.New(), null, Name, null, WorkoutDate, WorkoutStatus.STARTED,
        [
            WorkoutExercise.Rehydrate(Id.New(), bench, Effort.MID, [FourByTen], setRecords: [benchFirst, benchSecond]),
            WorkoutExercise.Rehydrate(Id.New(), row, Effort.MID, [ThreeByTen], setRecords: [rowFirst, rowSecond]),
        ]);

        // Act
        workout.ReorderSetRecords(bench, [benchSecond.Id, benchFirst.Id], RecordedAt);

        // Assert
        EntryFor(workout, row).SetRecords.Should().Equal(rowFirst, rowSecond);
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.REVIEWED)]
    public void ReorderSetRecords_ShouldRejectAWorkoutThatIsNotStartedOrCompletedAndKeepTheOrder(WorkoutStatus status)
    {
        // Arrange
        var bench = Id.New();
        var first = AStoredSetRecord(10, 60);
        var second = AStoredSetRecord(8, 60, 2);
        var workout = AWorkoutWithSetRecords(status, bench, first, second);

        // Act
        var act = () => workout.ReorderSetRecords(bench, [second.Id, first.Id], RecordedAt);

        // Assert
        act.Should().Throw<WorkoutResultsLockedException>();
        EntryFor(workout, bench).SetRecords.Should().Equal(first, second);
    }

    [Fact]
    public void ReorderSetRecords_ShouldRejectAnExerciseNotInTheWorkout()
    {
        // Arrange
        var bench = Id.New();
        var stored = AStoredSetRecord(8, 60);
        var workout = AWorkoutWithSetRecords(WorkoutStatus.STARTED, bench, stored);

        // Act
        var act = () => workout.ReorderSetRecords(Id.New(), [stored.Id], RecordedAt);

        // Assert
        act.Should().Throw<ExerciseNotInWorkoutException>();
    }

    [Fact]
    public void ReorderSetRecords_ShouldRejectAMissingSetRecordAndKeepTheOrder()
    {
        // Arrange
        var bench = Id.New();
        var first = AStoredSetRecord(10, 60);
        var second = AStoredSetRecord(8, 60, 2);
        var third = AStoredSetRecord(7, 60, 4);
        var workout = AWorkoutWithSetRecords(WorkoutStatus.STARTED, bench, first, second, third);

        // Act
        var act = () => workout.ReorderSetRecords(bench, [third.Id, first.Id], RecordedAt);

        // Assert
        act.Should().Throw<SetRecordOrderMismatchException>();
        EntryFor(workout, bench).SetRecords.Should().Equal(first, second, third);
    }

    [Fact]
    public void ReorderSetRecords_ShouldRejectAnExtraSetRecordAndKeepTheOrder()
    {
        // Arrange
        var bench = Id.New();
        var first = AStoredSetRecord(10, 60);
        var second = AStoredSetRecord(8, 60, 2);
        var workout = AWorkoutWithSetRecords(WorkoutStatus.STARTED, bench, first, second);

        // Act
        var act = () => workout.ReorderSetRecords(bench, [second.Id, first.Id, Id.New()], RecordedAt);

        // Assert
        act.Should().Throw<SetRecordOrderMismatchException>();
        EntryFor(workout, bench).SetRecords.Should().Equal(first, second);
    }

    [Fact]
    public void ReorderSetRecords_ShouldRejectARepeatedSetRecordAndKeepTheOrder()
    {
        // Arrange
        var bench = Id.New();
        var first = AStoredSetRecord(10, 60);
        var second = AStoredSetRecord(8, 60, 2);
        var workout = AWorkoutWithSetRecords(WorkoutStatus.STARTED, bench, first, second);

        // Act
        var act = () => workout.ReorderSetRecords(bench, [second.Id, second.Id], RecordedAt);

        // Assert
        act.Should().Throw<SetRecordOrderMismatchException>();
        EntryFor(workout, bench).SetRecords.Should().Equal(first, second);
    }

    [Fact]
    public void ReorderSetRecords_ShouldRejectASetRecordOfAnotherExerciseAndKeepTheOrder()
    {
        // Arrange
        var bench = Id.New();
        var row = Id.New();
        var benchFirst = AStoredSetRecord(10, 60);
        var benchSecond = AStoredSetRecord(8, 60, 2);
        var rowRecord = AStoredSetRecord(12, 40, 1);
        var workout = Workout.Rehydrate(Id.New(), Id.New(), null, Name, null, WorkoutDate, WorkoutStatus.STARTED,
        [
            WorkoutExercise.Rehydrate(Id.New(), bench, Effort.MID, [FourByTen], setRecords: [benchFirst, benchSecond]),
            WorkoutExercise.Rehydrate(Id.New(), row, Effort.MID, [ThreeByTen], setRecords: [rowRecord]),
        ]);

        // Act
        var act = () => workout.ReorderSetRecords(bench, [benchSecond.Id, rowRecord.Id], RecordedAt);

        // Assert
        act.Should().Throw<SetRecordOrderMismatchException>();
        EntryFor(workout, bench).SetRecords.Should().Equal(benchFirst, benchSecond);
    }

    private static readonly DateTime ReviewedAt = new(2026, 11, 8, 9, 0, 0, DateTimeKind.Utc);

    /// <summary>A completed workout prescribed by <paramref name="trainerId"/>, whose results last changed at <paramref name="resultsUpdatedAt"/>.</summary>
    private static Workout ACompletedWorkout(Id? trainerId, DateTime? resultsUpdatedAt = null, DateTime? completedAt = null, DateTime? startedAt = null)
        => Workout.Rehydrate(Id.New(), Id.New(), trainerId, Name, null, WorkoutDate, WorkoutStatus.COMPLETED, [],
            resultsUpdatedAt: resultsUpdatedAt, completedAt: completedAt, startedAt: startedAt);

    [Fact]
    public void MarkReviewed_ShouldReviewACompletedPrescribedWorkoutAndRecordWhoAndWhen()
    {
        // Arrange
        var trainerId = Id.New();
        var workout = ACompletedWorkout(trainerId, RecordedAt);

        // Act
        workout.MarkReviewed(trainerId, ReviewedAt, RecordedAt);

        // Assert
        workout.Status.Should().Be(WorkoutStatus.REVIEWED);
        workout.ReviewedBy.Should().Be(trainerId);
        workout.ReviewedAt.Should().Be(ReviewedAt);
    }

    [Fact]
    public void MarkReviewed_ShouldAcceptAWorkoutWithoutResultsWhenNoneWereExpected()
    {
        // Arrange
        var trainerId = Id.New();
        var workout = ACompletedWorkout(trainerId);

        // Act
        workout.MarkReviewed(trainerId, ReviewedAt, null);

        // Assert
        workout.Status.Should().Be(WorkoutStatus.REVIEWED);
        workout.ResultsUpdatedAt.Should().BeNull();
    }

    [Fact]
    public void MarkReviewed_ShouldCompareTheExpectedTimeAtTheStoredPrecision()
    {
        // Arrange
        var trainerId = Id.New();
        var workout = ACompletedWorkout(trainerId, RecordedAt);

        // Act
        workout.MarkReviewed(trainerId, ReviewedAt, RecordedAt.AddTicks(7));

        // Assert
        workout.Status.Should().Be(WorkoutStatus.REVIEWED);
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.REVIEWED)]
    public void MarkReviewed_ShouldRejectAWorkoutThatIsNotCompletedAndChangeNothing(WorkoutStatus status)
    {
        // Arrange
        var trainerId = Id.New();
        var workout = AWorkoutIn(status, trainerId);
        var reviewedAt = workout.ReviewedAt;
        var reviewedBy = workout.ReviewedBy;

        // Act
        var act = () => workout.MarkReviewed(trainerId, ReviewedAt, null);

        // Assert
        act.Should().Throw<WorkoutReviewNotAllowedException>();
        workout.Status.Should().Be(status);
        workout.ReviewedAt.Should().Be(reviewedAt);
        workout.ReviewedBy.Should().Be(reviewedBy);
    }

    [Fact]
    public void MarkReviewed_ShouldRejectAWorkoutWithoutTrainerAndChangeNothing()
    {
        // Arrange
        var workout = ACompletedWorkout(null);

        // Act
        var act = () => workout.MarkReviewed(Id.New(), ReviewedAt, null);

        // Assert
        act.Should().Throw<WorkoutReviewNotAllowedException>();
        workout.Status.Should().Be(WorkoutStatus.COMPLETED);
        workout.ReviewedAt.Should().BeNull();
        workout.ReviewedBy.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MarkReviewed_ShouldRejectResultsThatChangedAfterTheyWereReadAndChangeNothing(bool readBeforeAnyResult)
    {
        // Arrange
        var trainerId = Id.New();
        var workout = ACompletedWorkout(trainerId, RecordedAt.AddMinutes(5));
        DateTime? expected = readBeforeAnyResult ? null : RecordedAt;

        // Act
        var act = () => workout.MarkReviewed(trainerId, ReviewedAt, expected);

        // Assert
        act.Should().Throw<WorkoutResultsChangedException>();
        workout.Status.Should().Be(WorkoutStatus.COMPLETED);
        workout.ReviewedAt.Should().BeNull();
        workout.ReviewedBy.Should().BeNull();
    }

    [Fact]
    public void MarkReviewed_ShouldRejectExpectedResultsWhenTheWorkoutHasNone()
    {
        // Arrange
        var trainerId = Id.New();
        var workout = ACompletedWorkout(trainerId);

        // Act
        var act = () => workout.MarkReviewed(trainerId, ReviewedAt, RecordedAt);

        // Assert
        act.Should().Throw<WorkoutResultsChangedException>();
        workout.Status.Should().Be(WorkoutStatus.COMPLETED);
    }

    [Fact]
    public void AddSetRecord_ShouldLetTheAthleteAddResultsToACompletedWorkout()
    {
        // Arrange
        var bench = Id.New();
        var workout = AWorkoutWith(WorkoutStatus.COMPLETED, (bench, [FourByTen]));

        // Act
        workout.AddSetRecord(bench, Reps(8), Weight.From(60), RecordedAt);

        // Assert
        EntryFor(workout, bench).SetRecords.Should().ContainSingle();
    }

    [Fact]
    public void UpdateSetRecord_ShouldCorrectAResultOfACompletedWorkout()
    {
        // Arrange
        var bench = Id.New();
        var stored = SetRecord.Rehydrate(Id.New(), Reps(8), Weight.From(60), RecordedAt);
        var workout = AWorkoutWithSetRecords(WorkoutStatus.COMPLETED, bench, stored);

        // Act
        workout.UpdateSetRecord(bench, stored.Id, Reps(10), Weight.From(60), RecordedAt.AddMinutes(30));

        // Assert
        EntryFor(workout, bench).SetRecords.Single().Result.Should().Be(Reps(10));
    }

    [Fact]
    public void AddSetRecord_ShouldStampTheResultsWithTheRegistrationTime()
    {
        // Arrange
        var bench = Id.New();
        var workout = AWorkoutWith(WorkoutStatus.STARTED, (bench, [FourByTen]));

        // Act
        workout.AddSetRecord(bench, Reps(8), Weight.From(60), RecordedAt);

        // Assert
        workout.ResultsUpdatedAt.Should().Be(RecordedAt);
    }

    [Fact]
    public void UpdateSetRecord_ShouldStampTheResultsWithTheCorrectionTime()
    {
        // Arrange
        var bench = Id.New();
        var stored = SetRecord.Rehydrate(Id.New(), Reps(8), Weight.From(60), RecordedAt);
        var workout = AWorkoutWithSetRecords(WorkoutStatus.COMPLETED, bench, stored);
        var correctedAt = RecordedAt.AddMinutes(30);

        // Act
        workout.UpdateSetRecord(bench, stored.Id, Reps(10), Weight.From(60), correctedAt);

        // Assert
        workout.ResultsUpdatedAt.Should().Be(correctedAt);
    }

    [Fact]
    public void RemoveSetRecord_ShouldStampTheResultsWithTheRemovalTime()
    {
        // Arrange
        var bench = Id.New();
        var stored = SetRecord.Rehydrate(Id.New(), Reps(8), Weight.From(60), RecordedAt);
        var workout = AWorkoutWithSetRecords(WorkoutStatus.COMPLETED, bench, stored);
        var removedAt = RecordedAt.AddMinutes(30);

        // Act
        workout.RemoveSetRecord(bench, stored.Id, removedAt);

        // Assert
        workout.ResultsUpdatedAt.Should().Be(removedAt);
    }

    [Fact]
    public void ReorderSetRecords_ShouldStampTheResultsWithTheReorderTime()
    {
        // Arrange
        var bench = Id.New();
        var first = SetRecord.Rehydrate(Id.New(), Reps(8), Weight.From(60), RecordedAt);
        var second = SetRecord.Rehydrate(Id.New(), Reps(6), Weight.From(65), RecordedAt.AddMinutes(2));
        var workout = AWorkoutWithSetRecords(WorkoutStatus.COMPLETED, bench, first, second);
        var reorderedAt = RecordedAt.AddMinutes(30);

        // Act
        workout.ReorderSetRecords(bench, [second.Id, first.Id], reorderedAt);

        // Assert
        workout.ResultsUpdatedAt.Should().Be(reorderedAt);
    }

    [Fact]
    public void AddSetRecord_ShouldTruncateTheResultsTimeToTheStoredPrecision()
    {
        // Arrange
        var bench = Id.New();
        var workout = AWorkoutWith(WorkoutStatus.STARTED, (bench, [FourByTen]));

        // Act
        workout.AddSetRecord(bench, Reps(8), Weight.From(60), RecordedAt.AddTicks(7));

        // Assert
        workout.ResultsUpdatedAt.Should().Be(RecordedAt);
    }

    [Fact]
    public void UpdateSetRecord_ShouldKeepTheResultsTimeWhenTheChangeIsRefused()
    {
        // Arrange
        var bench = Id.New();
        var stored = SetRecord.Rehydrate(Id.New(), Reps(8), Weight.From(60), RecordedAt);
        var workout = Workout.Rehydrate(Id.New(), Id.New(), Id.New(), Name, null, WorkoutDate, WorkoutStatus.REVIEWED,
            [WorkoutExercise.Rehydrate(Id.New(), bench, Effort.MID, [FourByTen], setRecords: [stored])],
            reviewedAt: ReviewedAt, reviewedBy: Id.New(), resultsUpdatedAt: RecordedAt);

        // Act
        var act = () => workout.UpdateSetRecord(bench, stored.Id, Reps(10), Weight.From(60), RecordedAt.AddMinutes(30));

        // Assert
        act.Should().Throw<WorkoutResultsLockedException>();
        workout.ResultsUpdatedAt.Should().Be(RecordedAt);
    }

    [Fact]
    public void RemoveSetRecord_ShouldKeepTheResultsTimeWhenTheSetRecordIsUnknown()
    {
        // Arrange
        var bench = Id.New();
        var stored = SetRecord.Rehydrate(Id.New(), Reps(8), Weight.From(60), RecordedAt);
        var workout = AWorkoutWithSetRecords(WorkoutStatus.STARTED, bench, stored);

        // Act
        var act = () => workout.RemoveSetRecord(bench, Id.New(), RecordedAt.AddMinutes(30));

        // Assert
        act.Should().Throw<SetRecordNotFoundException>();
        workout.ResultsUpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Rehydrate_ShouldRestoreTheReviewAndTheResultsTime()
    {
        // Arrange
        var reviewerId = Id.New();

        // Act
        var workout = Workout.Rehydrate(Id.New(), Id.New(), reviewerId, Name, null, WorkoutDate, WorkoutStatus.REVIEWED, [],
            reviewedAt: ReviewedAt, reviewedBy: reviewerId, resultsUpdatedAt: RecordedAt);

        // Assert
        workout.ReviewedAt.Should().Be(ReviewedAt);
        workout.ReviewedBy.Should().Be(reviewerId);
        workout.ResultsUpdatedAt.Should().Be(RecordedAt);
    }

    [Fact]
    public void Create_ShouldStartWithoutReviewNorResults()
    {
        // Arrange & Act
        var workout = Workout.Create(Id.New(), Id.New(), Name, null, WorkoutDate);

        // Assert
        workout.ReviewedAt.Should().BeNull();
        workout.ReviewedBy.Should().BeNull();
        workout.ResultsUpdatedAt.Should().BeNull();
        workout.CompletedAt.Should().BeNull();
        workout.StartedAt.Should().BeNull();
    }

    [Fact]
    public void CreateFromTemplate_ShouldStartWithoutCompletionDate()
    {
        // Arrange
        var trainerId = Id.New();
        var template = ATemplateWithASuperSeries(trainerId);

        // Act
        var workout = Workout.CreateFromTemplate(template, Id.New(), WorkoutDate);

        // Assert
        workout.CompletedAt.Should().BeNull();
        workout.StartedAt.Should().BeNull();
    }

    [Fact]
    public void MarkReviewed_ShouldKeepTheStartDate()
    {
        // Arrange
        var trainerId = Id.New();
        var workout = ACompletedWorkout(trainerId, startedAt: WorkoutDate);

        // Act
        workout.MarkReviewed(trainerId, ReviewedAt, null);

        // Assert
        workout.StartedAt.Should().Be(WorkoutDate);
    }

    [Fact]
    public void AddSetRecord_ShouldKeepTheStartDate()
    {
        // Arrange
        var bench = Id.New();
        var workout = Workout.Rehydrate(Id.New(), Id.New(), null, Name, null, WorkoutDate, WorkoutStatus.STARTED,
            [WorkoutExercise.Rehydrate(Id.New(), bench, Effort.MID, [FourByTen])],
            startedAt: WorkoutDate);

        // Act
        workout.AddSetRecord(bench, Reps(8), Weight.From(62.5m), RecordedAt);

        // Assert
        workout.StartedAt.Should().Be(WorkoutDate);
    }

    [Theory]
    [InlineData(WorkoutStatus.STARTED, false)]
    [InlineData(WorkoutStatus.STARTED, true)]
    [InlineData(WorkoutStatus.DRAFT, true)]
    public void Rehydrate_ShouldRestoreTheStartDateWhateverTheStatus(WorkoutStatus status, bool withStartedAt)
    {
        // Arrange
        DateTime? startedAt = withStartedAt ? WorkoutDate : null;

        // Act
        var workout = AWorkoutIn(status, startedAt: startedAt);

        // Assert
        workout.Status.Should().Be(status);
        workout.StartedAt.Should().Be(startedAt);
    }

    [Fact]
    public void MarkReviewed_ShouldKeepTheCompletionDate()
    {
        // Arrange
        var trainerId = Id.New();
        var workout = ACompletedWorkout(trainerId, completedAt: WorkoutDate);

        // Act
        workout.MarkReviewed(trainerId, ReviewedAt, null);

        // Assert
        workout.CompletedAt.Should().Be(WorkoutDate);
    }

    [Theory]
    [InlineData(WorkoutStatus.COMPLETED, false)]
    [InlineData(WorkoutStatus.COMPLETED, true)]
    [InlineData(WorkoutStatus.DRAFT, true)]
    public void Rehydrate_ShouldRestoreTheCompletionDateWhateverTheStatus(WorkoutStatus status, bool withCompletedAt)
    {
        // Arrange
        DateTime? completedAt = withCompletedAt ? WorkoutDate : null;

        // Act
        var workout = AWorkoutIn(status, completedAt: completedAt);

        // Assert
        workout.Status.Should().Be(status);
        workout.CompletedAt.Should().Be(completedAt);
    }

    [Theory]
    [InlineData(WorkoutStatus.REVIEWED, false, false)]
    [InlineData(WorkoutStatus.REVIEWED, true, false)]
    [InlineData(WorkoutStatus.REVIEWED, false, true)]
    [InlineData(WorkoutStatus.COMPLETED, true, true)]
    [InlineData(WorkoutStatus.COMPLETED, true, false)]
    public void Rehydrate_ShouldRejectReviewMetadataThatDoesNotMatchTheStatus(WorkoutStatus status, bool withReviewedAt, bool withReviewedBy)
    {
        // Arrange
        DateTime? reviewedAt = withReviewedAt ? ReviewedAt : null;
        Id? reviewedBy = withReviewedBy ? Id.New() : null;

        // Act
        var act = () => Workout.Rehydrate(Id.New(), Id.New(), Id.New(), Name, null, WorkoutDate, status, [],
            reviewedAt: reviewedAt, reviewedBy: reviewedBy);

        // Assert
        act.Should().Throw<InvalidWorkoutStatusException>();
    }
}
