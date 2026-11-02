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

    private static Workout AWorkoutIn(WorkoutStatus status, Id? trainerId = null)
        => Workout.Rehydrate(Id.New(), Id.New(), trainerId, Name, null, WorkoutDate, status, []);

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
    [InlineData(WorkoutStatus.STARTED, WorkoutStatus.FINISHED)]
    public void ChangeStatus_ShouldAcceptTheBusinessTransitions(WorkoutStatus from, WorkoutStatus to)
    {
        // Arrange
        var workout = AWorkoutIn(from);

        // Act
        workout.ChangeStatus(to);

        // Assert
        workout.Status.Should().Be(to);
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.FINISHED)]
    [InlineData(WorkoutStatus.PUBLISHED, WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.PUBLISHED, WorkoutStatus.FINISHED)]
    [InlineData(WorkoutStatus.STARTED, WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.STARTED, WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.STARTED, WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.FINISHED, WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.FINISHED, WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.FINISHED, WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.FINISHED, WorkoutStatus.FINISHED)]
    public void ChangeStatus_ShouldRejectEveryOtherTransitionAndKeepTheStatus(WorkoutStatus from, WorkoutStatus to)
    {
        // Arrange
        var workout = AWorkoutIn(from);

        // Act
        var act = () => workout.ChangeStatus(to);

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
        var act = () => workout.ChangeStatus((WorkoutStatus)99);

        // Assert
        act.Should().Throw<InvalidWorkoutStatusException>();
        workout.Status.Should().Be(WorkoutStatus.DRAFT);
    }

    [Theory]
    [InlineData(WorkoutStatus.FINISHED, WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.FINISHED)]
    [InlineData(WorkoutStatus.STARTED, WorkoutStatus.STARTED)]
    public void OverrideStatus_ShouldSkipTheLifecycle(WorkoutStatus from, WorkoutStatus to)
    {
        // Arrange
        var workout = AWorkoutIn(from);

        // Act
        workout.OverrideStatus(to);

        // Assert
        workout.Status.Should().Be(to);
    }

    [Fact]
    public void OverrideStatus_ShouldRejectAnUndefinedTarget()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.PUBLISHED);

        // Act
        var act = () => workout.OverrideStatus((WorkoutStatus)99);

        // Assert
        act.Should().Throw<InvalidWorkoutStatusException>();
        workout.Status.Should().Be(WorkoutStatus.PUBLISHED);
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT, false)]
    [InlineData(WorkoutStatus.PUBLISHED, false)]
    [InlineData(WorkoutStatus.STARTED, true)]
    [InlineData(WorkoutStatus.FINISHED, true)]
    public void IsLocked_ShouldBeTrueOnlyOnceTheWorkoutHasStarted(WorkoutStatus status, bool expected)
    {
        // Arrange
        var workout = AWorkoutIn(status);

        // Act
        var locked = workout.IsLocked;

        // Assert
        locked.Should().Be(expected);
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
    [InlineData(WorkoutStatus.FINISHED)]
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
    [InlineData(WorkoutStatus.STARTED, WorkoutStatus.FINISHED, true)]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.STARTED, false)]
    [InlineData(WorkoutStatus.STARTED, WorkoutStatus.PUBLISHED, false)]
    [InlineData(WorkoutStatus.FINISHED, WorkoutStatus.DRAFT, false)]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.DRAFT, false)]
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
}
