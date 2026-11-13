using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Workouts;
using WeightTracker.Infra.Entities;
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
    private static SetResult Reps(int value) => new SetResult.Reps(RepCount.From(value));

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

    private Task<bool> ExistsByMesocycleWeek(Id mesocycleId, WeekNumber weekNumber)
        => Factory.WithScope(services =>
            services.GetRequiredService<IWorkoutRepository>()
                .ExistsByMesocycleWeek(mesocycleId, weekNumber, CancellationToken.None));

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
        stored!.ChangeStatus(WorkoutStatus.PUBLISHED, RecordedAt);

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

    [Fact]
    public async Task GetById_ShouldRestoreTheSeededPlacement()
    {
        // Arrange
        var workoutId = Id.From(SeedData.TrainedWorkoutId);

        // Act
        var workout = await Read(workoutId);

        // Assert
        workout!.MesocycleId.Should().Be(Id.From(SeedData.MesocycleId));
        workout.MesocycleWeekNumber.Should().Be(WeekNumber.From(1));
    }

    [Fact]
    public async Task Save_ShouldPersistANewWorkoutPlacedInAMesocycleWeek()
    {
        // Arrange
        var workout = Workout.Create(
            Id.From(SeedData.UserId), Id.From(SeedData.TrainerId), Name, null, new DateTime(2026, 10, 8));
        workout.LinkToMesocycle(Id.From(SeedData.MesocycleId), WeekNumber.From(2));

        await Write(repository => repository.Save(workout, CancellationToken.None));

        // Act
        var reloaded = await Read(workout.Id);

        // Assert
        reloaded!.MesocycleId.Should().Be(Id.From(SeedData.MesocycleId));
        reloaded.MesocycleWeekNumber.Should().Be(WeekNumber.From(2));
    }

    [Fact]
    public async Task Save_ShouldPersistANewPlacementOfAStoredStandaloneWorkout()
    {
        // Arrange
        var stored = await Read(Id.From(SeedData.PrescribedDraftWorkoutId));
        stored!.LinkToMesocycle(Id.From(SeedData.MesocycleId), WeekNumber.From(1));

        await Write(repository => repository.Save(stored, CancellationToken.None));

        // Act
        var reloaded = await Read(Id.From(SeedData.PrescribedDraftWorkoutId));

        // Assert
        reloaded!.MesocycleId.Should().Be(Id.From(SeedData.MesocycleId));
        reloaded.MesocycleWeekNumber.Should().Be(WeekNumber.From(1));
    }

    [Fact]
    public async Task Save_ShouldPersistAnUnlinkOfAStoredWorkout()
    {
        // Arrange
        var stored = await Read(Id.From(SeedData.TrainedWorkoutId));
        stored!.UnlinkFromMesocycle();

        await Write(repository => repository.Save(stored, CancellationToken.None));

        // Act
        var reloaded = await Read(Id.From(SeedData.TrainedWorkoutId));

        // Assert
        reloaded!.MesocycleId.Should().BeNull();
        reloaded.MesocycleWeekNumber.Should().BeNull();
    }

    [Fact]
    public async Task Save_ShouldKeepThePlacementWhenOtherDetailsChange()
    {
        // Arrange
        var stored = await Read(Id.From(SeedData.TrainedWorkoutId));
        stored!.UpdateDetails(WorkoutName.From("Renamed"), null, stored.WorkoutDate);

        await Write(repository => repository.Save(stored, CancellationToken.None));

        // Act
        var reloaded = await Read(Id.From(SeedData.TrainedWorkoutId));

        // Assert
        reloaded!.Name.Should().Be(WorkoutName.From("Renamed"));
        reloaded.MesocycleId.Should().Be(Id.From(SeedData.MesocycleId));
        reloaded.MesocycleWeekNumber.Should().Be(WeekNumber.From(1));
    }

    [Fact]
    public async Task Save_ShouldRejectAPlacementInAMesocycleThatDoesNotExist()
    {
        // Arrange
        var workout = Workout.Create(Id.From(SeedData.UserId), null, Name, null, new DateTime(2026, 10, 8));
        workout.LinkToMesocycle(Id.New(), WeekNumber.From(1));

        // Act
        var act = () => Write(repository => repository.Save(workout, CancellationToken.None));

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
        (await Read(workout.Id)).Should().BeNull();
    }

    [Fact]
    public async Task SavingAMesocycle_ShouldLeaveItsPlacedWorkoutsUnchanged()
    {
        // Arrange
        await Factory.WithScope(async services =>
        {
            var mesocycles = services.GetRequiredService<IMesocycleRepository>();
            var mesocycle = await mesocycles.GetById(Id.From(SeedData.MesocycleId), CancellationToken.None);
            mesocycle!.RedefineWeek(WeekNumber.From(1), TrainingWeekType.RECOVERY, MesocycleObjective.From("Easy week"));
            await mesocycles.Save(mesocycle, CancellationToken.None);
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

            return true;
        });

        // Act
        var workout = await Read(Id.From(SeedData.TrainedWorkoutId));

        // Assert
        workout!.MesocycleId.Should().Be(Id.From(SeedData.MesocycleId));
        workout.MesocycleWeekNumber.Should().Be(WeekNumber.From(1));
    }

    [Fact]
    public async Task GetAllByMesocycleId_ShouldReturnTheWorkoutsOfEveryWeekOfThatMesocycleOnly()
    {
        // Arrange
        var mesocycleId = Id.From(SeedData.MesocycleId);

        // Act
        var workouts = (await Factory.WithScope(services =>
            services.GetRequiredService<IWorkoutRepository>().GetAllByMesocycleId(mesocycleId, CancellationToken.None)))
            .ToList();

        // Assert
        workouts.Select(workout => workout.Id.Value)
            .Should().BeEquivalentTo([SeedData.TrainedWorkoutId, SeedData.PrescribedStartedWorkoutId]);
        workouts.Select(workout => workout.MesocycleWeekNumber!.Value).Should().BeEquivalentTo([1, 2]);
        workouts.Single(workout => workout.Id.Value == SeedData.TrainedWorkoutId).Exercises.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAllByMesocycleId_ShouldReturnNothingForAMesocycleWithoutWorkouts()
    {
        // Arrange
        var unknown = Id.New();

        // Act
        var workouts = await Factory.WithScope(services =>
            services.GetRequiredService<IWorkoutRepository>().GetAllByMesocycleId(unknown, CancellationToken.None));

        // Assert
        workouts.Should().BeEmpty();
    }

    [Fact]
    public async Task ExistsByMesocycleWeek_ShouldFindTheWorkoutPlacedInASeededWeek()
    {
        // Arrange
        var mesocycleId = Id.From(SeedData.MesocycleId);

        // Act
        var exists = await ExistsByMesocycleWeek(mesocycleId, WeekNumber.From(1));

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByMesocycleWeek_ShouldIgnoreAWeekWithoutWorkouts()
    {
        // Arrange
        var mesocycleId = Id.From(SeedData.MesocycleId);

        // Act
        var exists = await ExistsByMesocycleWeek(mesocycleId, WeekNumber.From(4));

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsByMesocycleWeek_ShouldIgnoreTheSameWeekOfAnotherMesocycle()
    {
        // Arrange
        var other = Mesocycle.Create(
            Id.From(SeedData.TrainerId), Id.From(SeedData.UserId), MesocycleName.From("Strength block"),
            MesocycleObjective.From("Get stronger"), new DateTime(2026, 11, 2), DurationInWeeks.From(4));
        await Factory.WithScope(async services =>
        {
            await services.GetRequiredService<IMesocycleRepository>().Save(other, CancellationToken.None);
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

            return true;
        });

        // Act
        var exists = await ExistsByMesocycleWeek(other.Id, WeekNumber.From(1));

        // Assert
        exists.Should().BeFalse();
    }

    private static readonly DateTime RecordedAt = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);

    private static Workout AStartedWorkoutWith(params Id[] exerciseIds)
    {
        var workout = Workout.Create(Id.From(SeedData.UserId), null, Name, null, new DateTime(2026, 11, 7));

        foreach (var exerciseId in exerciseIds)
            workout.AddExercise(exerciseId, Effort.MID, [new SetPrescription(3, new SetTarget.Reps(10))]);

        workout.ChangeStatus(WorkoutStatus.PUBLISHED, RecordedAt);
        workout.ChangeStatus(WorkoutStatus.STARTED, RecordedAt);

        return workout;
    }

    [Fact]
    public async Task Save_ShouldPersistTheSetRecordsOfANewAggregate()
    {
        // Arrange
        var workout = AStartedWorkoutWith(PressBanca, Sentadilla);
        workout.AddSetRecord(PressBanca, Reps(8), Weight.From(62.5m), RecordedAt);
        workout.AddSetRecord(PressBanca, Reps(6), Weight.From(65), RecordedAt.AddMinutes(3));
        workout.AddSetRecord(Sentadilla, Reps(12), Weight.From(0), RecordedAt.AddMinutes(9));

        await Write(repository => repository.Save(workout, CancellationToken.None));

        // Act
        var stored = await Read(workout.Id);

        // Assert
        foreach (var exerciseId in new[] { PressBanca, Sentadilla })
        {
            var expected = EntryFor(workout, exerciseId).SetRecords;
            var actual = EntryFor(stored!, exerciseId).SetRecords;
            actual.Select(setRecord => setRecord.Id).Should().Equal(expected.Select(setRecord => setRecord.Id));
            actual.Select(setRecord => setRecord.Result).Should().Equal(expected.Select(setRecord => setRecord.Result));
            actual.Select(setRecord => setRecord.Weight).Should().Equal(expected.Select(setRecord => setRecord.Weight));
            actual.Select(setRecord => setRecord.RegisteredAtUtc).Should().Equal(expected.Select(setRecord => setRecord.RegisteredAtUtc));
        }
    }

    [Fact]
    public async Task Save_ShouldAddASetRecordToAStoredAggregateAndKeepTheEarlierOnes()
    {
        // Arrange
        var workout = AStartedWorkoutWith(PressBanca);
        workout.AddSetRecord(PressBanca, Reps(8), Weight.From(60), RecordedAt);
        await Write(repository => repository.Save(workout, CancellationToken.None));
        var earlier = EntryFor(workout, PressBanca).SetRecords.Single();

        var reloaded = await Read(workout.Id);
        reloaded!.AddSetRecord(PressBanca, Reps(7), Weight.From(60), RecordedAt.AddMinutes(2));
        await Write(repository => repository.Save(reloaded, CancellationToken.None));

        // Act
        var stored = await Read(workout.Id);

        // Assert
        var setRecords = EntryFor(stored!, PressBanca).SetRecords;
        setRecords.Should().HaveCount(2);
        setRecords[0].Id.Should().Be(earlier.Id);
        setRecords[0].Result.Should().Be(Reps(8));
        setRecords[1].Result.Should().Be(Reps(7));
    }

    [Fact]
    public async Task Save_ShouldAppendSetRecordsAddedLaterWhateverTheirRegistrationTime()
    {
        // Arrange
        var workout = AStartedWorkoutWith(PressBanca);
        workout.AddSetRecord(PressBanca, Reps(8), Weight.From(60), RecordedAt.AddMinutes(5));
        workout.AddSetRecord(PressBanca, Reps(7), Weight.From(60), RecordedAt.AddMinutes(5));
        await Write(repository => repository.Save(workout, CancellationToken.None));

        var reloaded = await Read(workout.Id);
        reloaded!.AddSetRecord(PressBanca, Reps(10), Weight.From(55), RecordedAt);
        reloaded.AddSetRecord(PressBanca, Reps(6), Weight.From(60), RecordedAt.AddMinutes(5));
        await Write(repository => repository.Save(reloaded, CancellationToken.None));

        // Act
        var stored = await Read(workout.Id);

        // Assert
        EntryFor(stored!, PressBanca).SetRecords.Select(setRecord => setRecord.Result).Should().Equal(Reps(8), Reps(7), Reps(10), Reps(6));
    }

    [Fact]
    public async Task Save_ShouldDeleteTheSetRecordsOfARemovedExercise()
    {
        // Arrange
        var workout = AStartedWorkoutWith(PressBanca, Sentadilla);
        workout.AddSetRecord(PressBanca, Reps(8), Weight.From(60), RecordedAt);
        workout.AddSetRecord(Sentadilla, Reps(5), Weight.From(100), RecordedAt);
        await Write(repository => repository.Save(workout, CancellationToken.None));
        var removedEntryId = EntryFor(workout, PressBanca).Id.Value;
        var keptEntryId = EntryFor(workout, Sentadilla).Id.Value;

        var reloaded = await Read(workout.Id);
        reloaded!.RemoveExercise(PressBanca);
        await Write(repository => repository.Save(reloaded, CancellationToken.None));

        // Act
        var removedRows = await Factory.CountRows<SetRecordEntity>(row => row.WorkoutExerciseId == removedEntryId);

        // Assert
        removedRows.Should().Be(0);
        (await Factory.CountRows<SetRecordEntity>(row => row.WorkoutExerciseId == keptEntryId)).Should().Be(1);
    }

    [Fact]
    public async Task GetAllByUserId_ShouldRestoreTheSetRecordsAlongsideTheSets()
    {
        // Arrange
        var workout = AStartedWorkoutWith(PressBanca);
        workout.AddSetRecord(PressBanca, Reps(8), Weight.From(60), RecordedAt);
        workout.AddSetRecord(PressBanca, Reps(7), Weight.From(60), RecordedAt.AddMinutes(2));
        await Write(repository => repository.Save(workout, CancellationToken.None));

        // Act
        var workouts = await Factory.WithScope(services =>
            services.GetRequiredService<IWorkoutRepository>().GetAllByUserId(Id.From(SeedData.UserId), CancellationToken.None));

        // Assert
        var entry = EntryFor(workouts.Single(item => item.Id == workout.Id), PressBanca);
        entry.Sets.Should().Equal(new SetPrescription(3, new SetTarget.Reps(10)));
        entry.SetRecords.Select(setRecord => setRecord.Result).Should().Equal(Reps(8), Reps(7));
    }

    [Fact]
    public async Task Save_ShouldPersistAnEditedSetRecordAndKeepItsRegistrationTime()
    {
        // Arrange
        var workout = AStartedWorkoutWith(PressBanca);
        workout.AddSetRecord(PressBanca, Reps(8), Weight.From(60), RecordedAt);
        workout.AddSetRecord(PressBanca, Reps(7), Weight.From(60), RecordedAt.AddMinutes(2));
        await Write(repository => repository.Save(workout, CancellationToken.None));
        var edited = EntryFor(workout, PressBanca).SetRecords[0];

        var reloaded = await Read(workout.Id);
        reloaded!.UpdateSetRecord(PressBanca, edited.Id, Reps(10), Weight.From(62.5m), RecordedAt);
        await Write(repository => repository.Save(reloaded, CancellationToken.None));

        // Act
        var stored = await Read(workout.Id);

        // Assert
        var setRecords = EntryFor(stored!, PressBanca).SetRecords;
        setRecords[0].Id.Should().Be(edited.Id);
        setRecords[0].Result.Should().Be(Reps(10));
        setRecords[0].Weight.Should().Be(Weight.From(62.5m));
        setRecords[0].RegisteredAtUtc.Should().Be(RecordedAt);
        setRecords[1].Result.Should().Be(Reps(7));
    }

    [Fact]
    public async Task Save_ShouldDeleteOnlyTheRemovedSetRecord()
    {
        // Arrange
        var workout = AStartedWorkoutWith(PressBanca);
        workout.AddSetRecord(PressBanca, Reps(8), Weight.From(60), RecordedAt);
        workout.AddSetRecord(PressBanca, Reps(7), Weight.From(60), RecordedAt.AddMinutes(2));
        workout.AddSetRecord(PressBanca, Reps(6), Weight.From(60), RecordedAt.AddMinutes(4));
        await Write(repository => repository.Save(workout, CancellationToken.None));
        var setRecordIds = EntryFor(workout, PressBanca).SetRecords.Select(setRecord => setRecord.Id).ToList();

        var reloaded = await Read(workout.Id);
        reloaded!.RemoveSetRecord(PressBanca, setRecordIds[1], RecordedAt);
        await Write(repository => repository.Save(reloaded, CancellationToken.None));

        // Act
        var stored = await Read(workout.Id);

        // Assert
        EntryFor(stored!, PressBanca).SetRecords.Select(setRecord => setRecord.Id).Should().Equal(setRecordIds[0], setRecordIds[2]);
        (await Factory.CountRows<SetRecordEntity>(row => row.Id == setRecordIds[1].Value)).Should().Be(0);
    }

    [Fact]
    public async Task Save_ShouldPersistTheNewOrderOfReorderedSetRecords()
    {
        // Arrange
        var workout = AStartedWorkoutWith(PressBanca);
        workout.AddSetRecord(PressBanca, Reps(8), Weight.From(60), RecordedAt);
        workout.AddSetRecord(PressBanca, Reps(7), Weight.From(62.5m), RecordedAt.AddMinutes(2));
        workout.AddSetRecord(PressBanca, Reps(6), Weight.From(65), RecordedAt.AddMinutes(4));
        await Write(repository => repository.Save(workout, CancellationToken.None));
        var setRecordIds = EntryFor(workout, PressBanca).SetRecords.Select(setRecord => setRecord.Id).ToList();

        var reloaded = await Read(workout.Id);
        reloaded!.ReorderSetRecords(PressBanca, [setRecordIds[2], setRecordIds[0], setRecordIds[1]], RecordedAt);
        await Write(repository => repository.Save(reloaded, CancellationToken.None));

        // Act
        var stored = await Read(workout.Id);

        // Assert
        var setRecords = EntryFor(stored!, PressBanca).SetRecords;
        setRecords.Select(setRecord => setRecord.Id).Should().Equal(setRecordIds[2], setRecordIds[0], setRecordIds[1]);
        setRecords.Select(setRecord => setRecord.Result).Should().Equal(Reps(6), Reps(8), Reps(7));
        setRecords.Select(setRecord => setRecord.Weight).Should().Equal(Weight.From(65), Weight.From(60), Weight.From(62.5m));
    }

    private static readonly DateTime ReviewedAt = new(2026, 11, 8, 9, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime CompletedAt = new(2026, 11, 7, 11, 0, 0, DateTimeKind.Utc);

    /// <summary>A workout prescribed by the seeded trainer, completed at <see cref="CompletedAt"/> with one set record registered at <see cref="RecordedAt"/>.</summary>
    private static Workout ACompletedPrescribedWorkout()
    {
        var workout = Workout.Create(Id.From(SeedData.UserId), Id.From(SeedData.TrainerId), Name, null, new DateTime(2026, 11, 7));
        workout.AddExercise(PressBanca, Effort.MID, [new SetPrescription(3, new SetTarget.Reps(10))]);
        workout.ChangeStatus(WorkoutStatus.PUBLISHED, RecordedAt);
        workout.ChangeStatus(WorkoutStatus.STARTED, RecordedAt);
        workout.AddSetRecord(PressBanca, Reps(8), Weight.From(60), RecordedAt);
        workout.ChangeStatus(WorkoutStatus.COMPLETED, CompletedAt);

        return workout;
    }

    [Fact]
    public async Task Save_ShouldPersistTheCompletionTimeOfAStoredAggregate()
    {
        // Arrange
        var workout = AStartedWorkoutWith(PressBanca);
        await Write(repository => repository.Save(workout, CancellationToken.None));

        var reloaded = await Read(workout.Id);
        reloaded!.ChangeStatus(WorkoutStatus.COMPLETED, CompletedAt);
        await Write(repository => repository.Save(reloaded, CancellationToken.None));

        // Act
        var stored = await Read(workout.Id);

        // Assert
        stored!.Status.Should().Be(WorkoutStatus.COMPLETED);
        stored.CompletedAt.Should().Be(CompletedAt);
    }

    [Fact]
    public async Task Save_ShouldPersistTheStartTimeOfAStoredAggregate()
    {
        // Arrange
        var workout = AStartedWorkoutWith(PressBanca);
        await Write(repository => repository.Save(workout, CancellationToken.None));

        var reloaded = await Read(workout.Id);
        reloaded!.OverrideStatus(WorkoutStatus.STARTED, CompletedAt);
        await Write(repository => repository.Save(reloaded, CancellationToken.None));

        // Act
        var stored = await Read(workout.Id);

        // Assert
        stored!.Status.Should().Be(WorkoutStatus.STARTED);
        stored.StartedAt.Should().Be(CompletedAt);
    }

    [Fact]
    public async Task Save_ShouldPersistTheReviewAndTheResultsTime()
    {
        // Arrange
        var workout = ACompletedPrescribedWorkout();
        await Write(repository => repository.Save(workout, CancellationToken.None));

        var reloaded = await Read(workout.Id);
        reloaded!.MarkReviewed(Id.From(SeedData.TrainerId), ReviewedAt, reloaded.ResultsUpdatedAt);
        await Write(repository => repository.Save(reloaded, CancellationToken.None));

        // Act
        var stored = await Read(workout.Id);

        // Assert
        stored!.Status.Should().Be(WorkoutStatus.REVIEWED);
        stored.ReviewedAt.Should().Be(ReviewedAt);
        stored.ReviewedBy.Should().Be(Id.From(SeedData.TrainerId));
        stored.ResultsUpdatedAt.Should().Be(RecordedAt);
        stored.CompletedAt.Should().Be(CompletedAt);
    }

    [Fact]
    public async Task Save_ShouldKeepTheResultsTimeEqualToTheOneHeldInMemory()
    {
        // Arrange
        var workout = AStartedWorkoutWith(PressBanca);
        workout.AddSetRecord(PressBanca, Reps(8), Weight.From(60), RecordedAt.AddTicks(1234567));
        await Write(repository => repository.Save(workout, CancellationToken.None));

        // Act
        var stored = await Read(workout.Id);

        // Assert
        stored!.ResultsUpdatedAt.Should().Be(workout.ResultsUpdatedAt);
    }

    [Fact]
    public async Task SaveChanges_ShouldRefuseAWorkoutThatChangedAfterItWasRead()
    {
        // Arrange
        var workout = AStartedWorkoutWith(PressBanca);
        await Write(repository => repository.Save(workout, CancellationToken.None));

        using var slowScope = Factory.Services.CreateScope();
        var slowRepository = slowScope.ServiceProvider.GetRequiredService<IWorkoutRepository>();
        var slowCopy = await slowRepository.GetById(workout.Id, CancellationToken.None);

        var fastCopy = await Read(workout.Id);
        fastCopy!.AddSetRecord(PressBanca, Reps(8), Weight.From(60), RecordedAt);
        await Write(repository => repository.Save(fastCopy, CancellationToken.None));

        slowCopy!.AddSetRecord(PressBanca, Reps(6), Weight.From(65), RecordedAt.AddMinutes(1));
        await slowRepository.Save(slowCopy, CancellationToken.None);

        // Act
        var act = () => slowScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutConcurrencyConflictException>();
        var stored = await Read(workout.Id);
        EntryFor(stored!, PressBanca).SetRecords.Select(setRecord => setRecord.Result).Should().Equal(Reps(8));
    }

    [Fact]
    public async Task SaveChanges_ShouldRefuseAReviewWhenTheResultsChangedConcurrently()
    {
        // Arrange
        var workout = ACompletedPrescribedWorkout();
        await Write(repository => repository.Save(workout, CancellationToken.None));

        using var reviewScope = Factory.Services.CreateScope();
        var reviewRepository = reviewScope.ServiceProvider.GetRequiredService<IWorkoutRepository>();
        var reviewed = await reviewRepository.GetById(workout.Id, CancellationToken.None);

        var corrected = await Read(workout.Id);
        var setRecordId = EntryFor(corrected!, PressBanca).SetRecords.Single().Id;
        corrected!.UpdateSetRecord(PressBanca, setRecordId, Reps(10), Weight.From(60), RecordedAt.AddMinutes(30));
        await Write(repository => repository.Save(corrected, CancellationToken.None));

        // The reviewer still holds the results it read, so the aggregate alone cannot notice the correction
        reviewed!.MarkReviewed(Id.From(SeedData.TrainerId), ReviewedAt, RecordedAt);
        await reviewRepository.Save(reviewed, CancellationToken.None);

        // Act
        var act = () => reviewScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutConcurrencyConflictException>();
        var stored = await Read(workout.Id);
        stored!.Status.Should().Be(WorkoutStatus.COMPLETED);
        EntryFor(stored, PressBanca).SetRecords.Single().Result.Should().Be(Reps(10));
    }

    private static Workout ADraftWorkoutWith(params Id[] exerciseIds)
    {
        var workout = Workout.Create(
            Id.From(SeedData.UserId), Id.From(SeedData.TrainerId), Name, null, new DateTime(2026, 11, 12));
        foreach (var exerciseId in exerciseIds)
            workout.AddExercise(exerciseId, Effort.MID, [new SetPrescription(3, new SetTarget.Reps(10))]);

        return workout;
    }

    [Fact]
    public async Task Delete_ShouldRemoveTheWorkoutAcrossItsTables()
    {
        // Arrange
        var workout = ADraftWorkoutWith(PressBanca, Sentadilla, Plancha);
        workout.CreateSuperSeries([EntryFor(workout, PressBanca).Id, EntryFor(workout, Sentadilla).Id]);
        await Write(repository => repository.Save(workout, CancellationToken.None));
        var entryIds = workout.Exercises.Select(entry => entry.Id.Value).ToList();
        var stored = await Read(workout.Id);

        // Act
        await Write(repository => repository.Delete(stored!, CancellationToken.None));

        // Assert
        (await Read(workout.Id)).Should().BeNull();
        (await Factory.CountRows<WorkoutExerciseEntity>(row => row.WorkoutId == workout.Id.Value)).Should().Be(0);
        (await Factory.CountRows<WorkoutExerciseSetEntity>(row => entryIds.Contains(row.WorkoutExerciseId)))
            .Should().Be(0);
    }

    [Fact]
    public async Task Delete_ShouldLeaveOtherWorkoutsUntouched()
    {
        // Arrange
        var deleted = ADraftWorkoutWith(PressBanca);
        var kept = ADraftWorkoutWith(PressBanca);
        await Write(repository => repository.Save(deleted, CancellationToken.None));
        await Write(repository => repository.Save(kept, CancellationToken.None));
        var stored = await Read(deleted.Id);

        // Act
        await Write(repository => repository.Delete(stored!, CancellationToken.None));

        // Assert
        var survivor = await Read(kept.Id);
        survivor.Should().NotBeNull();
        EntryFor(survivor!, PressBanca).Sets.Should().Equal(new SetPrescription(3, new SetTarget.Reps(10)));
    }

    [Fact]
    public async Task SaveChanges_ShouldRefuseDeletingAWorkoutThatChangedAfterItWasRead()
    {
        // Arrange
        var workout = ADraftWorkoutWith(PressBanca);
        await Write(repository => repository.Save(workout, CancellationToken.None));

        using var deleteScope = Factory.Services.CreateScope();
        var deleteRepository = deleteScope.ServiceProvider.GetRequiredService<IWorkoutRepository>();
        var toDelete = await deleteRepository.GetById(workout.Id, CancellationToken.None);

        var published = await Read(workout.Id);
        published!.ChangeStatus(WorkoutStatus.PUBLISHED, new DateTime(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc));
        await Write(repository => repository.Save(published, CancellationToken.None));

        await deleteRepository.Delete(toDelete!, CancellationToken.None);

        // Act
        var act = () => deleteScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutConcurrencyConflictException>();
        var stored = await Read(workout.Id);
        stored.Should().NotBeNull();
        stored!.Status.Should().Be(WorkoutStatus.PUBLISHED);
    }
}
