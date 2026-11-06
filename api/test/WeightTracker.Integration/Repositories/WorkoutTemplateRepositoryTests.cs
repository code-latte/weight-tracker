using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Application.Common;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts;
using WeightTracker.Infra.Persistence;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Repositories;

/// <summary>
/// Covers persistence of the WorkoutTemplate aggregate across its three tables: inserts, the reconciling update
/// path and the cascading removal. The seeded template holds Sentadilla and Press Banca in a superseries, then Plancha.
/// </summary>
public class WorkoutTemplateRepositoryTests : IntegrationTest
{
    public WorkoutTemplateRepositoryTests(ApiFactory factory) : base(factory)
    { }

    private static readonly WorkoutName Name = WorkoutName.From("Full body B");

    private static readonly Id TemplateId = Id.From(SeedData.TrainerTemplateId);

    private static readonly Id TrainerId = Id.From(SeedData.TrainerId);

    private static readonly Id Sentadilla = Id.From(SeedData.SentadillaId);

    private static readonly Id PressBanca = Id.From(SeedData.PressBancaId);

    private static readonly Id Plancha = Id.From(SeedData.PlanchaId);

    private static readonly Id CurlBiceps = Id.From(SeedData.CurlBicepsId);

    private static WorkoutExercise EntryFor(WorkoutTemplate template, Id exerciseId)
        => template.Exercises.Single(entry => entry.ExerciseId == exerciseId);

    private Task<WorkoutTemplate?> Read(Id templateId)
        => Factory.WithScope(services =>
            services.GetRequiredService<IWorkoutTemplateRepository>().GetById(templateId, CancellationToken.None));

    private Task<bool> Write(Func<IWorkoutTemplateRepository, Task> action)
        => Factory.WithScope(async services =>
        {
            await action(services.GetRequiredService<IWorkoutTemplateRepository>());
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

            return true;
        });

    private Task<int> RowCounts()
        => Factory.WithScope(async services =>
        {
            var context = services.GetRequiredService<WeightTrackerDbContext>();

            return await context.WorkoutTemplates.CountAsync()
                + await context.WorkoutTemplateExercises.CountAsync()
                + await context.WorkoutTemplateExerciseSets.CountAsync();
        });

    [Fact]
    public async Task Save_ShouldPersistANewAggregateAcrossItsThreeTables()
    {
        // Arrange
        var template = WorkoutTemplate.Create(TrainerId, Name, WorkoutDescription.From("Pull focus"));
        template.AddExercise(CurlBiceps, Effort.MID, [new SetPrescription(3, new SetTarget.RepsRange(8, 12))], perSide: true, rest: TimeInMinutes.From(1.5m));
        template.AddExercise(Plancha, Effort.LOW, [new SetPrescription(3, new SetTarget.Minutes(TimeInMinutes.From(1m)))]);
        template.CreateSuperSeries(template.Exercises.Select(entry => entry.Id));

        await Write(repository => repository.Save(template, CancellationToken.None));

        // Act
        var reloaded = await Read(template.Id);

        // Assert
        reloaded!.TrainerId.Should().Be(TrainerId);
        reloaded.Name.Should().Be(Name);
        reloaded.Description.Should().Be(WorkoutDescription.From("Pull focus"));
        reloaded.IsNew.Should().BeFalse();
        reloaded.Exercises.Zip(template.Exercises).Should().AllSatisfy(pair =>
        {
            var (stored, source) = pair;
            stored.Id.Should().Be(source.Id);
            stored.ExerciseId.Should().Be(source.ExerciseId);
            stored.Effort.Should().Be(source.Effort);
            stored.Sets.Should().Equal(source.Sets);
            stored.PerSide.Should().Be(source.PerSide);
            stored.Rest.Should().Be(source.Rest);
            stored.SuperSeriesId.Should().Be(source.SuperSeriesId);
        });
    }

    [Fact]
    public async Task Save_ShouldPersistUpdatedDetailsOfAnExistingAggregate()
    {
        // Arrange
        var stored = await Read(TemplateId);
        stored!.UpdateDetails(Name, null);

        await Write(repository => repository.Save(stored, CancellationToken.None));

        // Act
        var reloaded = await Read(TemplateId);

        // Assert
        reloaded!.Name.Should().Be(Name);
        reloaded.Description.Should().BeNull();
        reloaded.TrainerId.Should().Be(TrainerId);
        reloaded.Exercises.Select(entry => entry.ExerciseId).Should().Equal(Sentadilla, PressBanca, Plancha);
    }

    [Fact]
    public async Task Save_ShouldKeepTheRowIdOfASurvivingExercise()
    {
        // Arrange
        var stored = await Read(TemplateId);
        stored!.AddExercise(CurlBiceps, Effort.MID, [new SetPrescription(2, new SetTarget.Reps(20))]);

        await Write(repository => repository.Save(stored, CancellationToken.None));

        // Act
        var reloaded = await Read(TemplateId);

        // Assert
        EntryFor(reloaded!, Plancha).Id.Should().Be(Id.From(SeedData.TrainerTemplatePlanchaEntryId));
        EntryFor(reloaded!, CurlBiceps).Id.Should().Be(EntryFor(stored, CurlBiceps).Id);
    }

    [Fact]
    public async Task Save_ShouldAllowRemovingAndReaddingTheSameExerciseInOneSave()
    {
        // Arrange
        var stored = await Read(TemplateId);
        stored!.RemoveExercise(Plancha);
        stored.AddExercise(Plancha, Effort.HIGH, [new SetPrescription(2, new SetTarget.Duration(60))]);

        await Write(repository => repository.Save(stored, CancellationToken.None));

        // Act
        var reloaded = await Read(TemplateId);

        // Assert
        var entry = EntryFor(reloaded!, Plancha);
        entry.Id.Should().NotBe(Id.From(SeedData.TrainerTemplatePlanchaEntryId));
        entry.Effort.Should().Be(Effort.HIGH);
        entry.Sets.Should().Equal(new SetPrescription(2, new SetTarget.Duration(60)));
    }

    [Fact]
    public async Task Save_ShouldPersistReplacedSetsAndANewOrder()
    {
        // Arrange
        var stored = await Read(TemplateId);
        stored!.UpdateExercise(Plancha, Effort.MID, [new SetPrescription(1, new SetTarget.MaxReps()), new SetPrescription(2, new SetTarget.Reps(5))]);
        stored.ReorderExercises([Plancha, Sentadilla, PressBanca]);

        await Write(repository => repository.Save(stored, CancellationToken.None));

        // Act
        var reloaded = await Read(TemplateId);

        // Assert
        reloaded!.Exercises.Select(entry => entry.ExerciseId).Should().Equal(Plancha, Sentadilla, PressBanca);
        EntryFor(reloaded, Plancha).Sets.Should().Equal(
            new SetPrescription(1, new SetTarget.MaxReps()), new SetPrescription(2, new SetTarget.Reps(5)));
    }

    [Fact]
    public async Task Save_ShouldUngroupTheRemainingExerciseWhenOneOfAPairIsRemoved()
    {
        // Arrange
        var stored = await Read(TemplateId);
        stored!.RemoveExercise(Sentadilla);

        await Write(repository => repository.Save(stored, CancellationToken.None));

        // Act
        var reloaded = await Read(TemplateId);

        // Assert
        reloaded!.Exercises.Select(entry => entry.ExerciseId).Should().Equal(PressBanca, Plancha);
        reloaded.Exercises.Should().OnlyContain(entry => entry.SuperSeriesId == null);
    }

    [Fact]
    public async Task Save_ShouldNotDuplicateTheTemplateRowOnUpdate()
    {
        // Arrange
        var stored = await Read(TemplateId);
        stored!.AddExercise(CurlBiceps, Effort.MID, [new SetPrescription(2, new SetTarget.Reps(20))]);

        await Write(repository => repository.Save(stored, CancellationToken.None));

        // Act
        var all = await Factory.WithScope(services =>
            services.GetRequiredService<IWorkoutTemplateRepository>().GetAll(CancellationToken.None));

        // Assert
        all.Should().ContainSingle().Which.Id.Should().Be(TemplateId);
    }

    [Fact]
    public async Task Remove_ShouldDeleteTheTemplateWithItsExercisesAndSets()
    {
        // Arrange
        var stored = await Read(TemplateId);

        // Act
        await Write(repository => repository.Remove(stored!, CancellationToken.None));

        // Assert
        (await Read(TemplateId)).Should().BeNull();
        (await RowCounts()).Should().Be(0);
    }

    [Fact]
    public async Task GetById_ShouldReturnNullForAnUnknownTemplate()
    {
        // Arrange
        var unknownId = Id.New();

        // Act
        var template = await Read(unknownId);

        // Assert
        template.Should().BeNull();
    }

    [Fact]
    public async Task GetAllByTrainerId_ShouldReturnOnlyThatTrainersTemplatesOrderedByName()
    {
        // Arrange
        var second = WorkoutTemplate.Create(TrainerId, WorkoutName.From("Arms"), null);
        var otherTrainers = WorkoutTemplate.Create(Id.From(SeedData.AdminId), WorkoutName.From("Not the trainer's"), null);
        await Write(async repository =>
        {
            await repository.Save(second, CancellationToken.None);
            await repository.Save(otherTrainers, CancellationToken.None);
        });

        // Act
        var templates = await Factory.WithScope(services =>
            services.GetRequiredService<IWorkoutTemplateRepository>().GetAllByTrainerId(TrainerId, CancellationToken.None));

        // Assert
        templates.Select(template => template.Id).Should().Equal(second.Id, TemplateId);
    }
}
