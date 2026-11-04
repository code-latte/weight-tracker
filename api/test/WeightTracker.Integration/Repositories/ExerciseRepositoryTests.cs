using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Application.Common;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Repositories;

/// <summary>
/// Covers loading the system catalog and the custom exercises with their translations, and saving custom exercises.
/// </summary>
public class ExerciseRepositoryTests : IntegrationTest
{
    public ExerciseRepositoryTests(ApiFactory factory) : base(factory)
    { }

    private Task<Exercise?> Load(Id id)
        => Factory.WithScope(services => services.GetRequiredService<IExerciseRepository>().GetById(id, CancellationToken.None));

    [Fact]
    public async Task GetById_ShouldLoadTheSeededMuscleFreeExerciseDbIdAndTranslations()
    {
        // Arrange
        var id = Id.From(SeedData.SentadillaId);

        // Act
        var exercise = await Load(id);

        // Assert
        exercise!.Muscle.Should().Be(Muscles.QUADRICEPS);
        exercise.BodyPart.Should().Be(BodyParts.LEGS);
        exercise.FreeExerciseDbId!.Value.Should().Be(SeedData.SentadillaFreeExerciseDbId);
        exercise.Translations.Select(translation => (translation.Id.Value, translation.LanguageCode.Value, translation.Name.Value))
            .Should().BeEquivalentTo([
                (SeedData.SentadillaEsTranslationId, "es", "Sentadilla con barra"),
                (SeedData.SentadillaEnTranslationId, "en", "Barbell Squat"),
            ]);
    }

    [Fact]
    public async Task GetAllByIds_ShouldLoadTheTranslationsOfEveryExercise()
    {
        // Arrange
        var requested = new[] { Id.From(SeedData.PressBancaId), Id.From(SeedData.PlanchaId) };

        // Act
        var exercises = await Factory.WithScope(services =>
            services.GetRequiredService<IExerciseRepository>().GetAllByIds(requested, CancellationToken.None));

        // Assert
        exercises.Should().OnlyContain(exercise => exercise.Translations.Count == 2);
    }

    [Fact]
    public async Task GetAllByIds_ShouldReturnOnlyTheRequestedExercises()
    {
        // Arrange
        var requested = new[] { Id.From(SeedData.PressBancaId), Id.From(SeedData.PlanchaId) };

        // Act
        var exercises = await Factory.WithScope(services =>
            services.GetRequiredService<IExerciseRepository>().GetAllByIds(requested, CancellationToken.None));

        // Assert
        exercises.Select(exercise => exercise.Id).Should().BeEquivalentTo(requested);
    }

    [Fact]
    public async Task GetAllByIds_ShouldIgnoreUnknownIds()
    {
        // Arrange
        var requested = new[] { Id.From(SeedData.SentadillaId), Id.New() };

        // Act
        var exercises = await Factory.WithScope(services =>
            services.GetRequiredService<IExerciseRepository>().GetAllByIds(requested, CancellationToken.None));

        // Assert
        exercises.Should().ContainSingle().Which.Id.Should().Be(Id.From(SeedData.SentadillaId));
    }

    [Fact]
    public async Task GetAllByIds_ShouldReturnNothingForNoIds()
    {
        // Arrange
        var requested = Array.Empty<Id>();

        // Act
        var exercises = await Factory.WithScope(services =>
            services.GetRequiredService<IExerciseRepository>().GetAllByIds(requested, CancellationToken.None));

        // Assert
        exercises.Should().BeEmpty();
    }

    private Task<bool> Write(Func<IExerciseRepository, Task> action)
        => Factory.WithScope(async services =>
        {
            await action(services.GetRequiredService<IExerciseRepository>());
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

            return true;
        });

    private static readonly Id Trainer = Id.From(SeedData.TrainerId);

    private static readonly Id User = Id.From(SeedData.UserId);

    private static Exercise ACustomExercise(Id ownerId, string name = "Remo con banda")
        => Exercise.Create(
            ownerId,
            ExerciseTranslation.Create(LanguageCode.From("es"), ExerciseName.From(name)),
            Muscles.MIDDLE_BACK,
            ExerciseTypes.STRENGTH,
            Url.From("https://videos.example.com/remo"));

    [Fact]
    public async Task Save_ShouldPersistANewCustomExerciseWithItsOwnerAndTranslation()
    {
        // Arrange
        var exercise = ACustomExercise(Trainer);
        await Write(repository => repository.Save(exercise, CancellationToken.None));

        // Act
        var stored = await Load(exercise.Id);

        // Assert
        stored!.OwnerId.Should().Be(Trainer);
        stored.IsSystem.Should().BeFalse();
        stored.FreeExerciseDbId.Should().BeNull();
        stored.ArchivedAtUtc.Should().BeNull();
        stored.Muscle.Should().Be(Muscles.MIDDLE_BACK);
        stored.VideoUrl!.Value.Should().Be("https://videos.example.com/remo");
        stored.Translations.Select(translation => (translation.LanguageCode.Value, translation.Name.Value))
            .Should().Equal(("es", "Remo con banda"));
    }

    [Fact]
    public async Task Save_ShouldReplaceATranslationAndAddALanguageOfAStoredExercise()
    {
        // Arrange
        var exercise = ACustomExercise(Trainer);
        await Write(repository => repository.Save(exercise, CancellationToken.None));
        var stored = await Load(exercise.Id);
        stored!.UpdateDetails(ExerciseTranslation.Create(LanguageCode.From("es"), ExerciseName.From("Remo con goma")), Muscles.LATS, ExerciseTypes.CARDIO, null);
        stored.UpdateDetails(ExerciseTranslation.Create(LanguageCode.From("en"), ExerciseName.From("Band row")), Muscles.LATS, ExerciseTypes.CARDIO, null);
        await Write(repository => repository.Save(stored, CancellationToken.None));

        // Act
        var reloaded = await Load(exercise.Id);

        // Assert
        reloaded!.Muscle.Should().Be(Muscles.LATS);
        reloaded.Type.Should().Be(ExerciseTypes.CARDIO);
        reloaded.VideoUrl.Should().BeNull();
        reloaded.Translations.Select(translation => (translation.LanguageCode.Value, translation.Name.Value))
            .Should().Equal(("en", "Band row"), ("es", "Remo con goma"));
    }

    [Fact]
    public async Task Save_ShouldPersistTheArchiveDateOfAStoredExercise()
    {
        // Arrange
        var exercise = ACustomExercise(User);
        await Write(repository => repository.Save(exercise, CancellationToken.None));
        var stored = await Load(exercise.Id);
        var archivedAt = new DateTime(2026, 11, 3, 9, 30, 0, DateTimeKind.Utc);
        stored!.Archive(archivedAt);
        await Write(repository => repository.Save(stored, CancellationToken.None));

        // Act
        var reloaded = await Load(exercise.Id);

        // Assert
        reloaded!.ArchivedAtUtc.Should().Be(archivedAt);
        reloaded.OwnerId.Should().Be(User);
    }

    [Fact]
    public async Task GetSystemAndOwnedBy_ShouldReturnOnlySystemExercisesWithoutAnOwner()
    {
        // Arrange
        var custom = ACustomExercise(Trainer);
        await Write(repository => repository.Save(custom, CancellationToken.None));

        // Act
        var exercises = await Factory.WithScope(services =>
            services.GetRequiredService<IExerciseRepository>().GetSystemAndOwnedBy(null, CancellationToken.None));

        // Assert
        exercises.Should().OnlyContain(exercise => exercise.IsSystem);
        exercises.Select(exercise => exercise.Id).Should().NotContain(custom.Id);
    }

    [Fact]
    public async Task GetSystemAndOwnedBy_ShouldAddOnlyTheExercisesOfThatOwner()
    {
        // Arrange
        var trainersExercise = ACustomExercise(Trainer);
        var usersExercise = ACustomExercise(User);
        await Write(async repository =>
        {
            await repository.Save(trainersExercise, CancellationToken.None);
            await repository.Save(usersExercise, CancellationToken.None);
        });

        // Act
        var exercises = await Factory.WithScope(services =>
            services.GetRequiredService<IExerciseRepository>().GetSystemAndOwnedBy(Trainer, CancellationToken.None));

        // Assert
        var ids = exercises.Select(exercise => exercise.Id).ToList();
        ids.Should().Contain(trainersExercise.Id);
        ids.Should().NotContain(usersExercise.Id);
        exercises.Where(exercise => exercise.IsSystem).Should().NotBeEmpty();
    }
}
