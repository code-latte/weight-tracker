using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Domain.Workouts;

public class WorkoutTemplateTests
{
    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private static readonly SetPrescription FourByTen = new(4, new SetTarget.Reps(10));

    [Fact]
    public void Create_ShouldKeepTheTrainerNameAndDescription()
    {
        // Arrange
        var trainerId = Id.New();
        var description = WorkoutDescription.From("Focus on tempo");

        // Act
        var template = WorkoutTemplate.Create(trainerId, Name, description);

        // Assert
        template.TrainerId.Should().Be(trainerId);
        template.Name.Should().Be(Name);
        template.Description.Should().Be(description);
    }

    [Fact]
    public void Create_ShouldStartEmptyAndMarkTheTemplateAsNew()
    {
        // Arrange & Act
        var template = WorkoutTemplate.Create(Id.New(), Name, null);

        // Assert
        template.IsNew.Should().BeTrue();
        template.Exercises.Should().BeEmpty();
    }

    [Fact]
    public void AddExercise_ShouldRegisterTheExerciseWithItsEffortAndSets()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, null);
        var exerciseId = Id.New();
        var twoByEight = new SetPrescription(2, new SetTarget.Reps(8));

        // Act
        template.AddExercise(exerciseId, Effort.HIGH, [FourByTen, twoByEight]);

        // Assert
        var entry = template.Exercises.Should().ContainSingle().Subject;
        entry.ExerciseId.Should().Be(exerciseId);
        entry.Effort.Should().Be(Effort.HIGH);
        entry.Sets.Should().ContainInOrder(FourByTen, twoByEight);
        entry.IsNew.Should().BeTrue();
    }

    [Fact]
    public void AddExercise_ShouldAppendInInsertionOrder()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, null);
        var first = Id.New();
        var second = Id.New();
        template.AddExercise(first, Effort.MID, [FourByTen]);

        // Act
        template.AddExercise(second, Effort.LOW, [new SetPrescription(5, new SetTarget.Reps(5))]);

        // Assert
        template.Exercises.Select(entry => entry.ExerciseId).Should().Equal(first, second);
    }

    [Fact]
    public void AddExercise_ShouldRejectAnExerciseAlreadyInTheTemplate()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, null);
        var exerciseId = Id.New();
        template.AddExercise(exerciseId, Effort.MID, [FourByTen]);

        // Act
        var act = () => template.AddExercise(exerciseId, Effort.HIGH, [new SetPrescription(3, new SetTarget.MaxReps())]);

        // Assert
        act.Should().Throw<ExerciseAlreadyInWorkoutException>();
        var entry = template.Exercises.Should().ContainSingle().Subject;
        entry.Effort.Should().Be(Effort.MID);
        entry.Sets.Should().Equal(FourByTen);
    }

    [Fact]
    public void AddExercise_ShouldRejectAnUndefinedEffort()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, null);

        // Act
        var act = () => template.AddExercise(Id.New(), (Effort)0, [FourByTen]);

        // Assert
        act.Should().Throw<InvalidEffortException>();
        template.Exercises.Should().BeEmpty();
    }

    [Fact]
    public void RemoveExercise_ShouldDropTheExercise()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, null);
        var exerciseId = Id.New();
        template.AddExercise(exerciseId, Effort.MID, [FourByTen]);

        // Act
        template.RemoveExercise(exerciseId);

        // Assert
        template.Exercises.Should().BeEmpty();
    }

    [Fact]
    public void RemoveExercise_ShouldRejectAnExerciseNotInTheTemplate()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, null);

        // Act
        var act = () => template.RemoveExercise(Id.New());

        // Assert
        act.Should().Throw<ExerciseNotInWorkoutException>();
    }

    [Fact]
    public void RemoveExercise_ShouldNotLeaveItsSetsBehindWhenTheExerciseIsAddedAgain()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, null);
        var exerciseId = Id.New();
        template.AddExercise(exerciseId, Effort.MID, [FourByTen, ThreeByTen]);
        template.RemoveExercise(exerciseId);

        // Act
        template.AddExercise(exerciseId, Effort.LOW, [ThreeByTen]);

        // Assert
        template.Exercises.Should().ContainSingle()
            .Which.Sets.Should().Equal(ThreeByTen);
    }

    [Fact]
    public void UpdateExercise_ShouldReplaceTheEffortAndEverySet()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, null);
        var exerciseId = Id.New();
        template.AddExercise(exerciseId, Effort.MID, [FourByTen, ThreeByTen]);
        var twoByTwelve = new SetPrescription(2, new SetTarget.Reps(12));
        var oneMax = new SetPrescription(1, new SetTarget.MaxReps());

        // Act
        template.UpdateExercise(exerciseId, Effort.HIGH, [twoByTwelve, oneMax]);

        // Assert
        var entry = template.Exercises.Single();
        entry.Effort.Should().Be(Effort.HIGH);
        entry.Sets.Should().Equal(twoByTwelve, oneMax);
    }

    [Fact]
    public void UpdateExercise_ShouldKeepTheEntryIdPositionAndSuperSeries()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, null);
        var first = Id.New();
        var second = Id.New();
        var third = Id.New();
        template.AddExercise(first, Effort.MID, [FourByTen]);
        template.AddExercise(second, Effort.MID, [FourByTen]);
        template.AddExercise(third, Effort.MID, [FourByTen]);
        var secondEntryId = EntryOf(template, second);
        var superSeriesId = template.CreateSuperSeries([EntryOf(template, first), secondEntryId]);

        // Act
        template.UpdateExercise(second, Effort.LOW, [new SetPrescription(2, new SetTarget.Reps(8)), new SetPrescription(2, new SetTarget.Reps(6))]);

        // Assert
        template.Exercises.Select(entry => entry.ExerciseId).Should().Equal(first, second, third);
        template.Exercises[1].Id.Should().Be(secondEntryId);
        template.Exercises[1].SuperSeriesId.Should().Be(superSeriesId);
    }

    [Fact]
    public void UpdateExercise_ShouldRejectAnExerciseNotInTheTemplate()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, null);

        // Act
        var act = () => template.UpdateExercise(Id.New(), Effort.MID, [FourByTen]);

        // Assert
        act.Should().Throw<ExerciseNotInWorkoutException>();
    }

    [Fact]
    public void UpdateExercise_ShouldRejectANewTotalOfSetsInsideASuperSeriesAndChangeNothing()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, null);
        var first = Id.New();
        var second = Id.New();
        template.AddExercise(first, Effort.MID, [FourByTen]);
        template.AddExercise(second, Effort.MID, [FourByTen]);
        template.CreateSuperSeries([EntryOf(template, first), EntryOf(template, second)]);

        // Act
        var act = () => template.UpdateExercise(first, Effort.HIGH, [ThreeByTen]);

        // Assert
        act.Should().Throw<SuperSeriesSetCountMismatchException>();
        var entry = template.Exercises.Single(item => item.ExerciseId == first);
        entry.Effort.Should().Be(Effort.MID);
        entry.Sets.Should().Equal(FourByTen);
        entry.SuperSeriesId.Should().NotBeNull();
    }

    [Fact]
    public void UpdateExercise_ShouldAcceptADifferentPrescriptionWithTheSameTotalInsideASuperSeries()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, null);
        var first = Id.New();
        var second = Id.New();
        template.AddExercise(first, Effort.MID, [FourByTen]);
        template.AddExercise(second, Effort.MID, [FourByTen]);
        var superSeriesId = template.CreateSuperSeries([EntryOf(template, first), EntryOf(template, second)]);
        var twoPlusTwo = new[] { new SetPrescription(2, new SetTarget.Reps(12)), new SetPrescription(2, new SetTarget.Duration(30)) };

        // Act
        template.UpdateExercise(first, Effort.HIGH, twoPlusTwo);

        // Assert
        var entry = template.Exercises.Single(item => item.ExerciseId == first);
        entry.Sets.Should().Equal(twoPlusTwo);
        entry.SuperSeriesId.Should().Be(superSeriesId);
    }

    [Fact]
    public void UpdateExercise_ShouldRejectAnUndefinedEffortAndChangeNothing()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, null);
        var exerciseId = Id.New();
        template.AddExercise(exerciseId, Effort.MID, [FourByTen]);

        // Act
        var act = () => template.UpdateExercise(exerciseId, (Effort)9, [ThreeByTen]);

        // Assert
        act.Should().Throw<InvalidEffortException>();
        var entry = template.Exercises.Single();
        entry.Effort.Should().Be(Effort.MID);
        entry.Sets.Should().Equal(FourByTen);
    }

    [Fact]
    public void UpdateExercise_ShouldRejectEmptySetsAndChangeNothing()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, null);
        var exerciseId = Id.New();
        template.AddExercise(exerciseId, Effort.MID, [FourByTen]);

        // Act
        var act = () => template.UpdateExercise(exerciseId, Effort.HIGH, []);

        // Assert
        act.Should().Throw<WorkoutExerciseRequiresSetsException>();
        var entry = template.Exercises.Single();
        entry.Effort.Should().Be(Effort.MID);
        entry.Sets.Should().Equal(FourByTen);
    }

    [Fact]
    public void Rehydrate_ShouldNotMarkTheTemplateAsNew()
    {
        // Arrange & Act
        var template = WorkoutTemplate.Rehydrate(Id.New(), Id.New(), Name, null, []);

        // Assert
        template.IsNew.Should().BeFalse();
    }

    [Fact]
    public void Rehydrate_ShouldRestoreTheTrainerAndOrderedExercises()
    {
        // Arrange
        var id = Id.New();
        var trainerId = Id.New();
        var sentadilla = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MIDHIGH, [new SetPrescription(5, new SetTarget.Reps(5))]);
        var plancha = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.LOW, [new SetPrescription(3, new SetTarget.Duration(40))]);

        // Act
        var template = WorkoutTemplate.Rehydrate(id, trainerId, Name, null, [sentadilla, plancha]);

        // Assert
        template.Id.Should().Be(id);
        template.TrainerId.Should().Be(trainerId);
        template.Exercises.Should().Equal(sentadilla, plancha);
        template.Exercises[1].Sets.Single().target.Should().Be(new SetTarget.Duration(40));
    }

    [Fact]
    public void Rehydrate_ShouldRejectTheSameExerciseTwice()
    {
        // Arrange
        var exerciseId = Id.New();
        var first = WorkoutExercise.Rehydrate(Id.New(), exerciseId, Effort.MID, [FourByTen]);
        var second = WorkoutExercise.Rehydrate(Id.New(), exerciseId, Effort.HIGH, [FourByTen]);

        // Act
        var act = () => WorkoutTemplate.Rehydrate(Id.New(), Id.New(), Name, null, [first, second]);

        // Assert
        act.Should().Throw<ExerciseAlreadyInWorkoutException>();
    }

    [Fact]
    public void Exercises_ShouldNotExposeTheInternalCollectionForMutation()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, null);
        template.AddExercise(Id.New(), Effort.MID, [FourByTen]);

        var snapshot = template.Exercises;

        // Act
        template.AddExercise(Id.New(), Effort.LOW, [new SetPrescription(2, new SetTarget.Reps(8))]);

        // Assert
        snapshot.Should().HaveCount(1);
        template.Exercises.Should().HaveCount(2);
    }

    private static readonly SetPrescription ThreeByTen = new(3, new SetTarget.Reps(10));

    /// <param name="entryIds">Identifiers of the template exercise entries, not of the catalog exercises.</param>
    private static WorkoutTemplate TemplateWithExercises(out Id[] entryIds, int count = 3)
    {
        var template = WorkoutTemplate.Create(Id.New(), Name, null);

        for (var index = 0; index < count; index++)
            template.AddExercise(Id.New(), Effort.MID, [FourByTen]);

        entryIds = template.Exercises.Select(entry => entry.Id).ToArray();

        return template;
    }

    private static Id EntryOf(WorkoutTemplate template, Id exerciseId)
        => template.Exercises.Single(entry => entry.ExerciseId == exerciseId).Id;

    private static Id ExerciseOf(WorkoutTemplate template, Id entryId)
        => template.Exercises.Single(entry => entry.Id == entryId).ExerciseId;

    private static SuperSeriesId? SuperSeriesOf(WorkoutTemplate template, Id entryId)
        => template.Exercises.Single(entry => entry.Id == entryId).SuperSeriesId;

    [Fact]
    public void CreateSuperSeries_ShouldGroupTheGivenExercisesAndLeaveTheOthersAlone()
    {
        // Arrange
        var template = TemplateWithExercises(out var ids);

        // Act
        var superSeriesId = template.CreateSuperSeries([ids[0], ids[1]]);

        // Assert
        SuperSeriesOf(template, ids[0]).Should().Be(superSeriesId);
        SuperSeriesOf(template, ids[1]).Should().Be(superSeriesId);
        SuperSeriesOf(template, ids[2]).Should().BeNull();
    }

    [Fact]
    public void CreateSuperSeries_ShouldAssignADifferentIdToEachSuperSeriesOfTheTemplate()
    {
        // Arrange
        var template = TemplateWithExercises(out var ids, 4);
        var first = template.CreateSuperSeries([ids[0], ids[1]]);

        // Act
        var second = template.CreateSuperSeries([ids[2], ids[3]]);

        // Assert
        second.Should().NotBe(first);
    }

    [Fact]
    public void CreateSuperSeries_ShouldAllowTheSameIdInDifferentTemplates()
    {
        // Arrange
        var firstTemplate = TemplateWithExercises(out var firstIds, 2);
        var secondTemplate = TemplateWithExercises(out var secondIds, 2);

        // Act
        var firstSuperSeries = firstTemplate.CreateSuperSeries(firstIds);
        var secondSuperSeries = secondTemplate.CreateSuperSeries(secondIds);

        // Assert
        secondSuperSeries.Should().Be(firstSuperSeries);
    }

    [Fact]
    public void CreateSuperSeries_ShouldAcceptDifferentPrescriptionsWithTheSameTotalOfSets()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, null);
        var first = Id.New();
        var second = Id.New();
        template.AddExercise(first, Effort.MID, [ThreeByTen]);
        template.AddExercise(second, Effort.MID, [new SetPrescription(2, new SetTarget.Reps(8)), new SetPrescription(1, new SetTarget.MaxReps())]);
        var firstEntry = EntryOf(template, first);
        var secondEntry = EntryOf(template, second);

        // Act
        var superSeriesId = template.CreateSuperSeries([firstEntry, secondEntry]);

        // Assert
        SuperSeriesOf(template, firstEntry).Should().Be(superSeriesId);
        SuperSeriesOf(template, secondEntry).Should().Be(superSeriesId);
    }

    [Fact]
    public void CreateSuperSeries_ShouldRejectExercisesWithDifferentNumberOfSetsAndGroupNothing()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, null);
        var first = Id.New();
        var second = Id.New();
        template.AddExercise(first, Effort.MID, [ThreeByTen]);
        template.AddExercise(second, Effort.MID, [FourByTen]);
        var firstEntry = EntryOf(template, first);
        var secondEntry = EntryOf(template, second);

        // Act
        var act = () => template.CreateSuperSeries([firstEntry, secondEntry]);

        // Assert
        act.Should().Throw<SuperSeriesSetCountMismatchException>();
        SuperSeriesOf(template, firstEntry).Should().BeNull();
        SuperSeriesOf(template, secondEntry).Should().BeNull();
    }

    [Fact]
    public void CreateSuperSeries_ShouldRejectASingleExercise()
    {
        // Arrange
        var template = TemplateWithExercises(out var ids);

        // Act
        var act = () => template.CreateSuperSeries([ids[0]]);

        // Assert
        act.Should().Throw<SuperSeriesRequiresTwoExercisesException>();
    }

    [Fact]
    public void CreateSuperSeries_ShouldRejectTheSameExerciseTwice()
    {
        // Arrange
        var template = TemplateWithExercises(out var ids);

        // Act
        var act = () => template.CreateSuperSeries([ids[0], ids[0]]);

        // Assert
        act.Should().Throw<DuplicateExerciseInSuperSeriesException>();
    }

    [Fact]
    public void CreateSuperSeries_ShouldRejectAnExerciseNotInTheTemplateAndGroupNothing()
    {
        // Arrange
        var template = TemplateWithExercises(out var ids);

        // Act
        var act = () => template.CreateSuperSeries([ids[0], Id.New()]);

        // Assert
        act.Should().Throw<WorkoutExerciseNotInWorkoutException>();
        SuperSeriesOf(template, ids[0]).Should().BeNull();
    }

    [Fact]
    public void CreateSuperSeries_ShouldRejectCatalogExerciseIdsAndGroupNothing()
    {
        // Arrange
        var template = TemplateWithExercises(out var ids);
        var catalogIds = new[] { ExerciseOf(template, ids[0]), ExerciseOf(template, ids[1]) };

        // Act
        var act = () => template.CreateSuperSeries(catalogIds);

        // Assert
        act.Should().Throw<WorkoutExerciseNotInWorkoutException>();
        template.Exercises.Should().OnlyContain(entry => entry.SuperSeriesId == null);
    }

    [Fact]
    public void CreateSuperSeries_ShouldRejectAnExerciseAlreadyInASuperSeries()
    {
        // Arrange
        var template = TemplateWithExercises(out var ids);
        var existing = template.CreateSuperSeries([ids[0], ids[1]]);

        // Act
        var act = () => template.CreateSuperSeries([ids[1], ids[2]]);

        // Assert
        act.Should().Throw<ExerciseAlreadyInSuperSeriesException>();
        SuperSeriesOf(template, ids[1]).Should().Be(existing);
        SuperSeriesOf(template, ids[2]).Should().BeNull();
    }

    [Fact]
    public void BreakSuperSeries_ShouldUngroupTheExercisesAndKeepThemInTheTemplate()
    {
        // Arrange
        var template = TemplateWithExercises(out var ids);
        var superSeriesId = template.CreateSuperSeries([ids[0], ids[1]]);

        // Act
        template.BreakSuperSeries(superSeriesId);

        // Assert
        template.Exercises.Select(entry => entry.Id).Should().Equal(ids);
        template.Exercises.Should().OnlyContain(entry => entry.SuperSeriesId == null);
    }

    [Fact]
    public void BreakSuperSeries_ShouldLeaveOtherSuperSeriesUntouched()
    {
        // Arrange
        var template = TemplateWithExercises(out var ids, 4);
        var first = template.CreateSuperSeries([ids[0], ids[1]]);
        var second = template.CreateSuperSeries([ids[2], ids[3]]);

        // Act
        template.BreakSuperSeries(first);

        // Assert
        SuperSeriesOf(template, ids[0]).Should().BeNull();
        SuperSeriesOf(template, ids[2]).Should().Be(second);
        SuperSeriesOf(template, ids[3]).Should().Be(second);
    }

    [Fact]
    public void BreakSuperSeries_ShouldRejectAnUnknownSuperSeries()
    {
        // Arrange
        var template = TemplateWithExercises(out _);

        // Act
        var act = () => template.BreakSuperSeries(SuperSeriesId.From(1));

        // Assert
        act.Should().Throw<SuperSeriesNotFoundException>();
    }

    [Fact]
    public void RemoveExercise_ShouldUngroupTheRemainingExerciseOfAPair()
    {
        // Arrange
        var template = TemplateWithExercises(out var ids, 2);
        template.CreateSuperSeries(ids);
        var removed = ExerciseOf(template, ids[0]);

        // Act
        template.RemoveExercise(removed);

        // Assert
        template.Exercises.Should().ContainSingle().Which.SuperSeriesId.Should().BeNull();
    }

    [Fact]
    public void RemoveExercise_ShouldKeepTheRestOfALargerSuperSeriesGrouped()
    {
        // Arrange
        var template = TemplateWithExercises(out var ids);
        var superSeriesId = template.CreateSuperSeries(ids);
        var removed = ExerciseOf(template, ids[0]);

        // Act
        template.RemoveExercise(removed);

        // Assert
        SuperSeriesOf(template, ids[1]).Should().Be(superSeriesId);
        SuperSeriesOf(template, ids[2]).Should().Be(superSeriesId);
    }

    [Fact]
    public void Rehydrate_ShouldRestoreStoredSuperSeries()
    {
        // Arrange
        var superSeriesId = SuperSeriesId.From(1);
        var first = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [FourByTen], superSeriesId);
        var second = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [FourByTen], superSeriesId);

        // Act
        var template = WorkoutTemplate.Rehydrate(Id.New(), Id.New(), Name, null, [first, second]);

        // Assert
        template.Exercises.Should().OnlyContain(entry => entry.SuperSeriesId == superSeriesId);
    }

    [Fact]
    public void Rehydrate_ShouldRejectASuperSeriesWithASingleMember()
    {
        // Arrange
        var alone = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [FourByTen], SuperSeriesId.From(1));

        // Act
        var act = () => WorkoutTemplate.Rehydrate(Id.New(), Id.New(), Name, null, [alone]);

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
        var act = () => WorkoutTemplate.Rehydrate(Id.New(), Id.New(), Name, null, [first, second]);

        // Assert
        act.Should().Throw<SuperSeriesSetCountMismatchException>();
    }

    [Fact]
    public void ReorderExercises_ShouldPutTheExercisesInTheGivenOrder()
    {
        // Arrange
        var template = TemplateWithExercises(out var ids);
        var exercises = ids.Select(entryId => ExerciseOf(template, entryId)).ToArray();

        // Act
        template.ReorderExercises([exercises[2], exercises[0], exercises[1]]);

        // Assert
        template.Exercises.Select(entry => entry.ExerciseId).Should().Equal(exercises[2], exercises[0], exercises[1]);
    }

    [Fact]
    public void ReorderExercises_ShouldKeepTheEntryIdEffortSetsAndSuperSeries()
    {
        // Arrange
        var template = TemplateWithExercises(out var ids);
        var superSeriesId = template.CreateSuperSeries([ids[0], ids[1]]);
        var exercises = ids.Select(entryId => ExerciseOf(template, entryId)).ToArray();
        var before = template.Exercises.ToDictionary(entry => entry.Id);

        // Act
        template.ReorderExercises([exercises[1], exercises[2], exercises[0]]);

        // Assert
        template.Exercises.Select(entry => entry.Id).Should().Equal(ids[1], ids[2], ids[0]);
        template.Exercises.Should().AllSatisfy(entry =>
        {
            entry.Effort.Should().Be(before[entry.Id].Effort);
            entry.Sets.Should().Equal(before[entry.Id].Sets);
        });
        SuperSeriesOf(template, ids[0]).Should().Be(superSeriesId);
        SuperSeriesOf(template, ids[1]).Should().Be(superSeriesId);
        SuperSeriesOf(template, ids[2]).Should().BeNull();
    }

    [Fact]
    public void ReorderExercises_ShouldAcceptAnEmptyOrderOnATemplateWithoutExercises()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, null);

        // Act
        template.ReorderExercises([]);

        // Assert
        template.Exercises.Should().BeEmpty();
    }

    [Fact]
    public void ReorderExercises_ShouldRejectAMissingExerciseAndKeepTheOrder()
    {
        // Arrange
        var template = TemplateWithExercises(out var ids);
        var exercises = ids.Select(entryId => ExerciseOf(template, entryId)).ToArray();

        // Act
        var act = () => template.ReorderExercises([exercises[1], exercises[0]]);

        // Assert
        act.Should().Throw<WorkoutExerciseOrderMismatchException>();
        template.Exercises.Select(entry => entry.ExerciseId).Should().Equal(exercises);
    }

    [Fact]
    public void ReorderExercises_ShouldRejectAnExtraExerciseAndKeepTheOrder()
    {
        // Arrange
        var template = TemplateWithExercises(out var ids);
        var exercises = ids.Select(entryId => ExerciseOf(template, entryId)).ToArray();

        // Act
        var act = () => template.ReorderExercises([exercises[2], exercises[1], exercises[0], Id.New()]);

        // Assert
        act.Should().Throw<WorkoutExerciseOrderMismatchException>();
        template.Exercises.Select(entry => entry.ExerciseId).Should().Equal(exercises);
    }

    [Fact]
    public void ReorderExercises_ShouldRejectAnExerciseNotInTheTemplateAndKeepTheOrder()
    {
        // Arrange
        var template = TemplateWithExercises(out var ids);
        var exercises = ids.Select(entryId => ExerciseOf(template, entryId)).ToArray();

        // Act
        var act = () => template.ReorderExercises([exercises[2], exercises[1], Id.New()]);

        // Assert
        act.Should().Throw<WorkoutExerciseOrderMismatchException>();
        template.Exercises.Select(entry => entry.ExerciseId).Should().Equal(exercises);
    }

    [Fact]
    public void ReorderExercises_ShouldRejectARepeatedExerciseAndKeepTheOrder()
    {
        // Arrange
        var template = TemplateWithExercises(out var ids);
        var exercises = ids.Select(entryId => ExerciseOf(template, entryId)).ToArray();

        // Act
        var act = () => template.ReorderExercises([exercises[2], exercises[2], exercises[0]]);

        // Assert
        act.Should().Throw<WorkoutExerciseOrderMismatchException>();
        template.Exercises.Select(entry => entry.ExerciseId).Should().Equal(exercises);
    }

    [Fact]
    public void ReorderExercises_ShouldRejectTheTemplateEntryIdsInPlaceOfTheCatalogExercises()
    {
        // Arrange
        var template = TemplateWithExercises(out var ids);
        var exercises = ids.Select(entryId => ExerciseOf(template, entryId)).ToArray();

        // Act
        var act = () => template.ReorderExercises([ids[2], ids[1], ids[0]]);

        // Assert
        act.Should().Throw<WorkoutExerciseOrderMismatchException>();
        template.Exercises.Select(entry => entry.ExerciseId).Should().Equal(exercises);
    }

    [Fact]
    public void AddExercise_ShouldKeepThePerSideFlagAndTheRest()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, null);
        var exerciseId = Id.New();
        var rest = TimeInMinutes.From(1.25m);

        // Act
        template.AddExercise(exerciseId, Effort.MID, [ThreeByTen], perSide: true, rest: rest);

        // Assert
        var entry = template.Exercises.Single();
        entry.PerSide.Should().BeTrue();
        entry.Rest.Should().Be(rest);
    }

    [Fact]
    public void UpdateExercise_ShouldReplaceThePerSideFlagAndTheRest()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, null);
        var exerciseId = Id.New();
        template.AddExercise(exerciseId, Effort.MID, [ThreeByTen]);
        var rest = TimeInMinutes.From(1.5m);

        // Act
        template.UpdateExercise(exerciseId, Effort.MID, [ThreeByTen], perSide: true, rest: rest);

        // Assert
        var entry = template.Exercises.Single();
        entry.PerSide.Should().BeTrue();
        entry.Rest.Should().Be(rest);
    }

    [Fact]
    public void UpdateExercise_ShouldClearThePerSideFlagAndTheRestWhenOmitted()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, null);
        var exerciseId = Id.New();
        template.AddExercise(exerciseId, Effort.MID, [ThreeByTen], perSide: true, rest: TimeInMinutes.From(2m));

        // Act
        template.UpdateExercise(exerciseId, Effort.MID, [ThreeByTen]);

        // Assert
        var entry = template.Exercises.Single();
        entry.PerSide.Should().BeFalse();
        entry.Rest.Should().BeNull();
    }

    [Fact]
    public void UpdateExercise_ShouldKeepThePerSideFlagAndTheRestWhenTheSuperSeriesRejectsTheUpdate()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, null);
        var first = Id.New();
        var second = Id.New();
        var rest = TimeInMinutes.From(2m);
        template.AddExercise(first, Effort.MID, [FourByTen], perSide: true, rest: rest);
        template.AddExercise(second, Effort.MID, [FourByTen]);
        template.CreateSuperSeries([EntryOf(template, first), EntryOf(template, second)]);

        // Act
        var act = () => template.UpdateExercise(first, Effort.HIGH, [ThreeByTen], perSide: false, rest: null);

        // Assert
        act.Should().Throw<SuperSeriesSetCountMismatchException>();
        var entry = template.Exercises.Single(item => item.ExerciseId == first);
        entry.PerSide.Should().BeTrue();
        entry.Rest.Should().Be(rest);
    }

    [Fact]
    public void Rehydrate_ShouldRestoreTheNameAndTheDescription()
    {
        // Arrange
        var description = WorkoutDescription.From("Focus on tempo");

        // Act
        var template = WorkoutTemplate.Rehydrate(Id.New(), Id.New(), Name, description, []);

        // Assert
        template.Name.Should().Be(Name);
        template.Description.Should().Be(description);
    }

    [Fact]
    public void UpdateDetails_ShouldReplaceNameAndDescription()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, WorkoutDescription.From("Focus on tempo"));
        var newName = WorkoutName.From("Push day");
        var newDescription = WorkoutDescription.From("Heavy presses");

        // Act
        template.UpdateDetails(newName, newDescription);

        // Assert
        template.Name.Should().Be(newName);
        template.Description.Should().Be(newDescription);
    }

    [Fact]
    public void UpdateDetails_ShouldClearTheDescription()
    {
        // Arrange
        var template = WorkoutTemplate.Create(Id.New(), Name, WorkoutDescription.From("Focus on tempo"));

        // Act
        template.UpdateDetails(Name, null);

        // Assert
        template.Description.Should().BeNull();
    }

    [Fact]
    public void UpdateDetails_ShouldKeepTheTrainerAndTheExercises()
    {
        // Arrange
        var trainerId = Id.New();
        var exerciseId = Id.New();
        var template = WorkoutTemplate.Rehydrate(
            Id.New(), trainerId, Name, null,
            [WorkoutExercise.Rehydrate(Id.New(), exerciseId, Effort.MID, [FourByTen])]);

        // Act
        template.UpdateDetails(WorkoutName.From("Push day"), null);

        // Assert
        template.TrainerId.Should().Be(trainerId);
        template.Exercises.Should().ContainSingle().Which.ExerciseId.Should().Be(exerciseId);
    }

    private static readonly DateTime WorkoutDate = new(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc);

    private static Workout AWorkoutWithASuperSeries(Id? trainerId, WorkoutStatus status = WorkoutStatus.DRAFT)
    {
        var superSeries = SuperSeriesId.From(1);

        return Workout.Rehydrate(
            Id.New(), Id.New(), trainerId, Name, WorkoutDescription.From("Focus on tempo"), WorkoutDate, status,
            [
                WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.HIGH, [FourByTen], superSeries, perSide: true, rest: TimeInMinutes.From(1.5m)),
                WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [ThreeByTen, new SetPrescription(1, new SetTarget.MaxReps())], superSeries),
                WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.LOW, [new SetPrescription(3, new SetTarget.Duration(40))]),
            ]);
    }

    [Fact]
    public void CreateFromWorkout_ShouldCopyTheNameDescriptionAndEveryExerciseInOrder()
    {
        // Arrange
        var workout = AWorkoutWithASuperSeries(Id.New());

        // Act
        var template = WorkoutTemplate.CreateFromWorkout(workout);

        // Assert
        template.Name.Should().Be(workout.Name);
        template.Description.Should().Be(workout.Description);
        template.Exercises.Should().HaveCount(3);
        template.Exercises.Zip(workout.Exercises).Should().AllSatisfy(pair =>
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
    public void CreateFromWorkout_ShouldGiveEveryCopiedEntryANewIdentifier()
    {
        // Arrange
        var workout = AWorkoutWithASuperSeries(Id.New());

        // Act
        var template = WorkoutTemplate.CreateFromWorkout(workout);

        // Assert
        template.Exercises.Select(entry => entry.Id).Should().NotIntersectWith(workout.Exercises.Select(entry => entry.Id));
        template.Exercises.Should().OnlyContain(entry => entry.IsNew);
    }

    [Fact]
    public void CreateFromWorkout_ShouldCreateANewTemplateOwnedByTheWorkoutTrainer()
    {
        // Arrange
        var trainerId = Id.New();
        var workout = AWorkoutWithASuperSeries(trainerId);

        // Act
        var template = WorkoutTemplate.CreateFromWorkout(workout);

        // Assert
        template.TrainerId.Should().Be(trainerId);
        template.IsNew.Should().BeTrue();
        template.Id.Should().NotBe(workout.Id);
    }

    [Fact]
    public void CreateFromWorkout_ShouldRejectAWorkoutWithoutTrainer()
    {
        // Arrange
        var workout = AWorkoutWithASuperSeries(null);

        // Act
        var act = () => WorkoutTemplate.CreateFromWorkout(workout);

        // Assert
        act.Should().Throw<WorkoutWithoutTrainerException>();
    }

    [Fact]
    public void CreateFromWorkout_ShouldAcceptAWorkoutWithoutExercises()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), Id.New(), Name, null, WorkoutDate);

        // Act
        var template = WorkoutTemplate.CreateFromWorkout(workout);

        // Assert
        template.Exercises.Should().BeEmpty();
        template.Description.Should().BeNull();
    }

    [Fact]
    public void CreateFromWorkout_ShouldAcceptAStartedWorkout()
    {
        // Arrange
        var workout = AWorkoutWithASuperSeries(Id.New(), WorkoutStatus.STARTED);

        // Act
        var template = WorkoutTemplate.CreateFromWorkout(workout);

        // Assert
        template.Exercises.Should().HaveCount(3);
        workout.Status.Should().Be(WorkoutStatus.STARTED);
    }

    [Fact]
    public void CreateFromWorkout_ShouldNotFollowLaterChangesToTheWorkout()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), Id.New(), Name, null, WorkoutDate);
        workout.AddExercise(Id.New(), Effort.MID, [FourByTen]);
        workout.AddExercise(Id.New(), Effort.MID, [FourByTen]);
        workout.CreateSuperSeries(workout.Exercises.Select(entry => entry.Id));
        var template = WorkoutTemplate.CreateFromWorkout(workout);
        var removed = workout.Exercises[0].ExerciseId;

        // Act
        workout.RemoveExercise(removed);

        // Assert
        template.Exercises.Select(entry => entry.ExerciseId).Should().Contain(removed);
        template.Exercises.Should().OnlyContain(entry => entry.SuperSeriesId != null);
    }
}
