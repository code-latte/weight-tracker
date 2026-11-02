using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Application.Common;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Repositories;

/// <summary>
/// Covers persistence of the Workout aggregate across its three tables, including the
/// mutation path that no endpoint reaches yet.
/// </summary>
public class WorkoutRepositoryTests : IntegrationTest
{
    public WorkoutRepositoryTests(ApiFactory factory) : base(factory)
    { }

    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private static readonly Id PressBanca = Id.From(SeedData.PressBancaId);

    private static readonly Id Sentadilla = Id.From(SeedData.SentadillaId);

    private static readonly Id Plancha = Id.From(SeedData.PlanchaId);

    private static WorkoutExercise EntryFor(Workout workout, Id exerciseId)
        => workout.Exercises.Single(entry => entry.ExerciseId == exerciseId);

    private Task<Workout?> Read(Id workoutId)
        => Factory.WithScope(services =>
            services.GetRequiredService<IWorkoutRepository>().GetById(workoutId, CancellationToken.None));

    private Task<bool> Write(Func<IWorkoutRepository, Task> action)
        => Factory.WithScope(async services =>
        {
            await action(services.GetRequiredService<IWorkoutRepository>());
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

            return true;
        });

    [Fact]
    public async Task Save_ShouldPersistANewAggregateAcrossItsThreeTables()
    {
        // Arrange
        var workout = Workout.Create(
            Id.From(SeedData.UserId), Id.From(SeedData.TrainerId), Name, null, new DateTime(2026, 11, 2));
        workout.AddExercise(PressBanca, Effort.HIGH,
        [
            new SetPrescription(4, new SetTarget.Reps(10)),
            new SetPrescription(1, new SetTarget.MaxReps()),
        ]);

        await Write(repository => repository.Save(workout, CancellationToken.None));

        // Act
        var stored = await Read(workout.Id);

        // Assert
        stored.Should().NotBeNull();
        stored!.UserId.Should().Be(Id.From(SeedData.UserId));
        stored.TrainerId.Should().Be(Id.From(SeedData.TrainerId));
        stored.WorkoutDate.Should().Be(new DateTime(2026, 11, 2));
        stored.IsNew.Should().BeFalse();
        EntryFor(stored, PressBanca).Sets.Should().Equal(
            new SetPrescription(4, new SetTarget.Reps(10)),
            new SetPrescription(1, new SetTarget.MaxReps()));
    }

    [Fact]
    public async Task Save_ShouldPersistAWorkoutWithoutATrainer()
    {
        // Arrange
        var workout = Workout.Create(Id.From(SeedData.UserId), null, Name, null, new DateTime(2026, 11, 3));
        workout.AddExercise(Sentadilla, Effort.MID, [new SetPrescription(5, new SetTarget.Duration(30))]);

        await Write(repository => repository.Save(workout, CancellationToken.None));

        // Act
        var stored = await Read(workout.Id);

        // Assert
        stored!.TrainerId.Should().BeNull();
        EntryFor(stored, Sentadilla).Sets.Single().target.Should().Be(new SetTarget.Duration(30));
    }

    [Fact]
    public async Task Save_ShouldPersistBothBoundsOfARepsRange()
    {
        // Arrange
        var workout = Workout.Create(Id.From(SeedData.UserId), null, Name, null, new DateTime(2026, 11, 3));
        workout.AddExercise(Sentadilla, Effort.MID, [new SetPrescription(3, new SetTarget.RepsRange(8, 12))]);

        await Write(repository => repository.Save(workout, CancellationToken.None));

        // Act
        var stored = await Read(workout.Id);

        // Assert
        EntryFor(stored!, Sentadilla).Sets.Single().Should().Be(new SetPrescription(3, new SetTarget.RepsRange(8, 12)));
    }

    [Fact]
    public async Task Save_ShouldPersistUpdatedDetailsAndAssignmentOfAnExistingAggregate()
    {
        // Arrange
        var stored = await Read(Id.From(SeedData.OwnedWorkoutId));
        stored!.UpdateDetails(WorkoutName.From("Push day"), WorkoutDescription.From("Heavy presses"), new DateTime(2026, 11, 5));
        stored.Reassign(Id.From(SeedData.TrainerId), Id.From(SeedData.AdminId));

        await Write(repository => repository.Save(stored, CancellationToken.None));

        // Act
        var reloaded = await Read(Id.From(SeedData.OwnedWorkoutId));

        // Assert
        reloaded!.Name.Should().Be(WorkoutName.From("Push day"));
        reloaded.Description.Should().Be(WorkoutDescription.From("Heavy presses"));
        reloaded.WorkoutDate.Should().Be(new DateTime(2026, 11, 5));
        reloaded.UserId.Should().Be(Id.From(SeedData.TrainerId));
        reloaded.TrainerId.Should().Be(Id.From(SeedData.AdminId));
        reloaded.Exercises.Should().ContainSingle().Which.ExerciseId.Should().Be(PressBanca);
    }

    [Fact]
    public async Task Save_ShouldReplaceThePrescriptionsOfAnExistingAggregate()
    {
        // Arrange
        var stored = await Read(Id.From(SeedData.OwnedWorkoutId));
        stored!.RemoveExercise(PressBanca);
        stored.AddExercise(Sentadilla, Effort.LOW, [new SetPrescription(3, new SetTarget.Reps(12))]);

        await Write(repository => repository.Save(stored, CancellationToken.None));

        // Act
        var reloaded = await Read(Id.From(SeedData.OwnedWorkoutId));

        // Assert
        var entry = reloaded!.Exercises.Should().ContainSingle().Subject;
        entry.ExerciseId.Should().Be(Sentadilla);
        entry.Sets.Single().Should().Be(new SetPrescription(3, new SetTarget.Reps(12)));
    }

    [Fact]
    public async Task Save_ShouldPersistTheEffortOfEachExercise()
    {
        // Arrange
        var workout = Workout.Create(Id.From(SeedData.UserId), null, Name, null, new DateTime(2026, 11, 4));
        workout.AddExercise(PressBanca, Effort.MIDLOW, [new SetPrescription(4, new SetTarget.Reps(10))]);
        workout.AddExercise(Sentadilla, Effort.HIGH, [new SetPrescription(5, new SetTarget.Reps(5))]);

        await Write(repository => repository.Save(workout, CancellationToken.None));

        // Act
        var stored = await Read(workout.Id);

        // Assert
        stored!.Exercises.Select(entry => (entry.ExerciseId, entry.Effort)).Should().Equal(
            (PressBanca, Effort.MIDLOW),
            (Sentadilla, Effort.HIGH));
    }

    [Fact]
    public async Task Save_ShouldPersistANewWorkoutAsDraft()
    {
        // Arrange
        var workout = Workout.Create(Id.From(SeedData.UserId), null, Name, null, new DateTime(2026, 11, 5));

        await Write(repository => repository.Save(workout, CancellationToken.None));

        // Act
        var stored = await Read(workout.Id);

        // Assert
        stored!.Status.Should().Be(WorkoutStatus.DRAFT);
    }

    [Fact]
    public async Task Save_ShouldPersistAStatusChangeOfAStoredAggregate()
    {
        // Arrange
        var stored = await Read(Id.From(SeedData.OwnedWorkoutId));
        stored!.ChangeStatus(WorkoutStatus.PUBLISHED);

        await Write(repository => repository.Save(stored, CancellationToken.None));

        // Act
        var reloaded = await Read(Id.From(SeedData.OwnedWorkoutId));

        // Assert
        reloaded!.Status.Should().Be(WorkoutStatus.PUBLISHED);
    }

    [Fact]
    public async Task Save_ShouldKeepTheRowIdOfASurvivingExercise()
    {
        // Arrange
        var stored = await Read(Id.From(SeedData.OwnedWorkoutId));
        stored!.AddExercise(Sentadilla, Effort.MID, [new SetPrescription(2, new SetTarget.Reps(20))]);

        await Write(repository => repository.Save(stored, CancellationToken.None));

        // Act
        var reloaded = await Read(Id.From(SeedData.OwnedWorkoutId));

        // Assert
        EntryFor(reloaded!, PressBanca).Id.Should().Be(Id.From(SeedData.OwnedPressBancaEntryId));
        EntryFor(reloaded!, Sentadilla).Id.Should().Be(EntryFor(stored, Sentadilla).Id);
    }

    [Fact]
    public async Task Save_ShouldAllowRemovingAndReaddingTheSameExerciseInOneSave()
    {
        // Arrange
        var stored = await Read(Id.From(SeedData.OwnedWorkoutId));
        stored!.RemoveExercise(PressBanca);
        stored.AddExercise(PressBanca, Effort.LOW, [new SetPrescription(2, new SetTarget.Reps(15))]);

        await Write(repository => repository.Save(stored, CancellationToken.None));

        // Act
        var reloaded = await Read(Id.From(SeedData.OwnedWorkoutId));

        // Assert
        var entry = reloaded!.Exercises.Should().ContainSingle().Subject;
        entry.Id.Should().NotBe(Id.From(SeedData.OwnedPressBancaEntryId));
        entry.Effort.Should().Be(Effort.LOW);
        entry.Sets.Should().Equal(new SetPrescription(2, new SetTarget.Reps(15)));
    }

    [Fact]
    public async Task Save_ShouldPersistTheSuperSeriesOfANewAggregate()
    {
        // Arrange
        var workout = Workout.Create(Id.From(SeedData.UserId), null, Name, null, new DateTime(2026, 11, 5));
        workout.AddExercise(PressBanca, Effort.MID, [new SetPrescription(4, new SetTarget.Reps(10))]);
        workout.AddExercise(Sentadilla, Effort.MID, [new SetPrescription(4, new SetTarget.Reps(8))]);
        workout.AddExercise(Plancha, Effort.LOW, [new SetPrescription(3, new SetTarget.Duration(40))]);
        var superSeriesId = workout.CreateSuperSeries([EntryFor(workout, PressBanca).Id, EntryFor(workout, Sentadilla).Id]);

        await Write(repository => repository.Save(workout, CancellationToken.None));

        // Act
        var stored = await Read(workout.Id);

        // Assert
        EntryFor(stored!, PressBanca).SuperSeriesId.Should().Be(superSeriesId);
        EntryFor(stored!, Sentadilla).SuperSeriesId.Should().Be(superSeriesId);
        EntryFor(stored!, Plancha).SuperSeriesId.Should().BeNull();
    }

    [Fact]
    public async Task Save_ShouldPersistASuperSeriesCreatedOnAStoredAggregate()
    {
        // Arrange
        var workoutId = Id.From(SeedData.OwnedWorkoutId);
        var withTwoExercises = await Read(workoutId);
        withTwoExercises!.AddExercise(Sentadilla, Effort.MID, [new SetPrescription(4, new SetTarget.Reps(8))]);
        await Write(repository => repository.Save(withTwoExercises, CancellationToken.None));

        var stored = await Read(workoutId);
        var superSeriesId = stored!.CreateSuperSeries([EntryFor(stored, PressBanca).Id, EntryFor(stored, Sentadilla).Id]);
        await Write(repository => repository.Save(stored, CancellationToken.None));

        // Act
        var reloaded = await Read(workoutId);

        // Assert
        EntryFor(reloaded!, PressBanca).SuperSeriesId.Should().Be(superSeriesId);
        EntryFor(reloaded!, Sentadilla).SuperSeriesId.Should().Be(superSeriesId);
        EntryFor(reloaded!, PressBanca).Id.Should().Be(Id.From(SeedData.OwnedPressBancaEntryId));
    }

    [Fact]
    public async Task Save_ShouldPersistTheBreakingOfAStoredSuperSeries()
    {
        // Arrange
        var workoutId = Id.From(SeedData.OwnedWorkoutId);
        var stored = await Read(workoutId);
        stored!.AddExercise(Sentadilla, Effort.MID, [new SetPrescription(4, new SetTarget.Reps(8))]);
        var superSeriesId = stored.CreateSuperSeries([EntryFor(stored, PressBanca).Id, EntryFor(stored, Sentadilla).Id]);
        await Write(repository => repository.Save(stored, CancellationToken.None));

        var grouped = await Read(workoutId);
        grouped!.BreakSuperSeries(superSeriesId);
        await Write(repository => repository.Save(grouped, CancellationToken.None));

        // Act
        var reloaded = await Read(workoutId);

        // Assert
        reloaded!.Exercises.Select(entry => entry.ExerciseId).Should().BeEquivalentTo([PressBanca, Sentadilla]);
        reloaded.Exercises.Should().OnlyContain(entry => entry.SuperSeriesId == null);
    }

    [Fact]
    public async Task Save_ShouldUngroupTheRemainingExerciseWhenOneOfAPairIsRemoved()
    {
        // Arrange
        var workoutId = Id.From(SeedData.OwnedWorkoutId);
        var stored = await Read(workoutId);
        stored!.AddExercise(Sentadilla, Effort.MID, [new SetPrescription(4, new SetTarget.Reps(8))]);
        stored.CreateSuperSeries([EntryFor(stored, PressBanca).Id, EntryFor(stored, Sentadilla).Id]);
        await Write(repository => repository.Save(stored, CancellationToken.None));

        var grouped = await Read(workoutId);
        grouped!.RemoveExercise(Sentadilla);
        await Write(repository => repository.Save(grouped, CancellationToken.None));

        // Act
        var reloaded = await Read(workoutId);

        // Assert
        var entry = reloaded!.Exercises.Should().ContainSingle().Subject;
        entry.ExerciseId.Should().Be(PressBanca);
        entry.SuperSeriesId.Should().BeNull();
    }

    [Fact]
    public async Task Save_ShouldNotDuplicateTheWorkoutRowOnUpdate()
    {
        // Arrange
        var stored = await Read(Id.From(SeedData.OwnedWorkoutId));
        stored!.AddExercise(Sentadilla, Effort.MID, [new SetPrescription(2, new SetTarget.Reps(20))]);

        await Write(repository => repository.Save(stored, CancellationToken.None));

        // Act
        var all = await Factory.WithScope(services =>
            services.GetRequiredService<IWorkoutRepository>().GetAll(CancellationToken.None));

        // Assert
        all.Should().HaveCount(9);
        all.Count(workout => workout.Id == Id.From(SeedData.OwnedWorkoutId)).Should().Be(1);
    }

    [Fact]
    public async Task GetById_ShouldReturnNullForAnUnknownWorkout()
    {
        // Arrange
        var unknownId = Id.New();

        // Act
        var workout = await Read(unknownId);

        // Assert
        workout.Should().BeNull();
    }

    [Fact]
    public async Task GetAllByUserId_ShouldIgnoreWorkoutsWhereTheUserIsOnlyTheTrainer()
    {
        // Arrange & Act
        var owned = await Factory.WithScope(services => services
            .GetRequiredService<IWorkoutRepository>()
            .GetAllByUserId(Id.From(SeedData.TrainerId), CancellationToken.None));

        // Assert
        owned.Select(workout => workout.Id)
            .Should().BeEquivalentTo(new[] { Id.From(SeedData.TrainerOwnWorkoutId) });
    }

    [Fact]
    public async Task GetAllByUserOrTrainerId_ShouldIncludeBothRoles()
    {
        // Arrange & Act
        var visible = await Factory.WithScope(services => services
            .GetRequiredService<IWorkoutRepository>()
            .GetAllByUserOrTrainerId(Id.From(SeedData.TrainerId), CancellationToken.None));

        // Assert
        visible.Select(workout => workout.Id).Should().BeEquivalentTo(new[]
        {
            Id.From(SeedData.TrainedWorkoutId),
            Id.From(SeedData.TrainerOwnWorkoutId),
            Id.From(SeedData.PrescribedPairWorkoutId),
            Id.From(SeedData.PrescribedSuperSeriesWorkoutId),
            Id.From(SeedData.PrescribedDraftWorkoutId),
            Id.From(SeedData.PrescribedStartedWorkoutId),
        });
    }

    [Fact]
    public async Task Save_ShouldPersistAMinutesTargetPerSideAndRestOfANewAggregate()
    {
        // Arrange
        var workout = Workout.Create(Id.From(SeedData.UserId), null, Name, null, new DateTime(2026, 11, 3));
        workout.AddExercise(
            Plancha,
            Effort.MID,
            [new SetPrescription(1, new SetTarget.Minutes(TimeInMinutes.From(1.25m)))],
            perSide: true,
            rest: TimeInMinutes.From(0.75m));

        await Write(repository => repository.Save(workout, CancellationToken.None));

        // Act
        var stored = await Read(workout.Id);

        // Assert
        var entry = EntryFor(stored!, Plancha);
        entry.Sets.Single().Should().Be(new SetPrescription(1, new SetTarget.Minutes(TimeInMinutes.From(1.25m))));
        entry.PerSide.Should().BeTrue();
        entry.Rest.Should().Be(TimeInMinutes.From(0.75m));
    }

    [Fact]
    public async Task Save_ShouldPersistAnUpdatedPerSideFlagAndRestOfAnExistingAggregate()
    {
        // Arrange
        var stored = await Read(Id.From(SeedData.OwnedWorkoutId));
        stored!.UpdateExercise(
            PressBanca,
            Effort.MID,
            [new SetPrescription(3, new SetTarget.Reps(10))],
            perSide: true,
            rest: TimeInMinutes.From(2m));

        await Write(repository => repository.Save(stored, CancellationToken.None));

        // Act
        var reloaded = await Read(Id.From(SeedData.OwnedWorkoutId));

        // Assert
        var entry = EntryFor(reloaded!, PressBanca);
        entry.PerSide.Should().BeTrue();
        entry.Rest.Should().Be(TimeInMinutes.From(2m));
    }
}
