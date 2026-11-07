using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Persistence;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Exercises;

/// <summary>
/// Both read routes are public, so a missing token is a valid request; the invalid-token cases live in
/// <see cref="ExerciseOptionalCallerTests"/>. Custom exercises are created through the API in Arrange.
/// </summary>
public class ExerciseReadEndpointsTests : ExerciseEndpointTest
{
    public ExerciseReadEndpointsTests(ApiFactory factory) : base(factory)
    { }

    [Fact]
    public async Task GetAll_ShouldReturnEverySeededExerciseToAnAnonymousCaller()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/exercises");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        payload.EnumerateArray().Select(exercise => exercise.GetProperty("id").GetGuid())
            .Should().BeEquivalentTo(
                [SeedData.PressBancaId, SeedData.SentadillaId, SeedData.PlanchaId, SeedData.CurlBicepsId,
                    SeedData.CorrerEnCintaId, SeedData.EstiramientoIsquiosId]);
    }

    [Fact]
    public async Task GetAll_ShouldReturnTranslationsMuscleBodyPartAndTypeOfEveryExercise()
    {
        // Arrange & Act
        var payload = await Client.GetFromJsonAsync<JsonElement>("/exercises", JsonOptions);

        // Assert
        payload.EnumerateArray().Should().OnlyContain(exercise =>
            exercise.GetProperty("translations").GetArrayLength() == 2
            && exercise.GetProperty("muscle").ValueKind == JsonValueKind.String
            && exercise.GetProperty("bodyPart").ValueKind == JsonValueKind.String
            && exercise.GetProperty("type").ValueKind == JsonValueKind.String);
    }

    [Fact]
    public async Task GetById_ShouldReturnTheExerciseToAnAnonymousCaller()
    {
        // Arrange & Act
        var response = await Client.GetAsync($"/exercises/{SeedData.SentadillaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        payload.GetProperty("id").GetGuid().Should().Be(SeedData.SentadillaId);
        payload.GetProperty("freeExerciseDbId").GetString().Should().Be("Barbell_Squat");
        payload.GetProperty("videoUrl").ValueKind.Should().Be(JsonValueKind.Null);
        payload.GetProperty("muscle").GetString().Should().Be("QUADRICEPS");
        payload.GetProperty("bodyPart").GetString().Should().Be("LEGS");
        payload.GetProperty("type").GetString().Should().Be("STRENGTH");
        ExerciseJson.Translations(payload).Should().BeEquivalentTo([("es", "Sentadilla con barra"), ("en", "Barbell Squat")]);
        payload.TryGetProperty("name", out _).Should().BeFalse();
        payload.GetProperty("ownerId").ValueKind.Should().Be(JsonValueKind.Null);
        payload.GetProperty("archivedAtUtc").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetById_ShouldReturnACardioExerciseWithItsMuscleAndBodyPart()
    {
        // Arrange & Act
        var payload = await Client.GetFromJsonAsync<JsonElement>($"/exercises/{SeedData.CorrerEnCintaId}", JsonOptions);

        // Assert
        payload.GetProperty("type").GetString().Should().Be("CARDIO");
        payload.GetProperty("muscle").GetString().Should().Be("QUADRICEPS");
        payload.GetProperty("bodyPart").GetString().Should().Be("LEGS");
        ExerciseJson.Translations(payload).Should().BeEquivalentTo([("es", "Correr en cinta"), ("en", "Running, Treadmill")]);
    }

    [Fact]
    public async Task GetById_ShouldReturnAStretchingExercise()
    {
        // Arrange & Act
        var payload = await Client.GetFromJsonAsync<JsonElement>($"/exercises/{SeedData.EstiramientoIsquiosId}", JsonOptions);

        // Assert
        payload.GetProperty("type").GetString().Should().Be("STRETCHING");
        payload.GetProperty("muscle").GetString().Should().Be("HAMSTRINGS");
        payload.GetProperty("bodyPart").GetString().Should().Be("LEGS");
    }

    [Fact]
    public async Task GetById_ShouldReturnAnEmptyTranslationListForAnExerciseWithoutNames()
    {
        // Arrange
        // The catalog is maintained in the database, so the row is stored the same way
        var id = Id.New().Value;
        await Factory.WithScope(async services =>
        {
            var context = services.GetRequiredService<WeightTrackerDbContext>();
            context.Exercises.Add(new ExerciseEntity { Id = id, Muscle = (int)Muscles.NECK, Type = (int)ExerciseTypes.STRETCHING });
            await context.SaveChangesAsync();

            return true;
        });

        // Act
        var payload = await Client.GetFromJsonAsync<JsonElement>($"/exercises/{id}", JsonOptions);

        // Assert
        payload.GetProperty("translations").GetArrayLength().Should().Be(0);
        payload.GetProperty("bodyPart").GetString().Should().Be("NECK");
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFoundForAnUnknownExercise()
    {
        // Arrange & Act
        var response = await Client.GetAsync($"/exercises/{Id.New().Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_ShouldRejectAnEmptyIdentifier()
    {
        // Arrange & Act
        var response = await Client.GetAsync($"/exercises/{Guid.Empty}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOnlySystemExercisesToAnAnonymousCaller()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var custom = await CreateExercise(trainer);

        // Act
        var exercises = await ExercisesSeenBy(Client);

        // Assert
        IdsOf(exercises).Should().NotContain(custom);
        exercises.EnumerateArray().Should().OnlyContain(exercise => exercise.GetProperty("ownerId").ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public async Task GetById_ShouldHideACustomExerciseFromAnAnonymousCaller()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var custom = await CreateExercise(user);

        // Act
        var response = await Client.GetAsync($"/exercises/{custom}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_ShouldIncludeTheOwnCustomExerciseOfAnAuthenticatedCaller()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var custom = await CreateExercise(user);

        // Act
        var exercises = await ExercisesSeenBy(user);

        // Assert
        IdsOf(exercises).Should().Contain(custom).And.Contain(SeedData.SentadillaId);
        exercises.EnumerateArray().Single(exercise => exercise.GetProperty("id").GetGuid() == custom)
            .GetProperty("ownerId").GetGuid().Should().Be(SeedData.UserId);
    }

    [Fact]
    public async Task GetAll_ShouldIncludeTheTrainersExerciseForAnAthleteWhoseVisibleWorkoutUsesIt()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var custom = await CreateExercise(trainer);
        await AddToPublishedWorkout(trainer, SeedData.TrainedWorkoutId, custom);
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var exercises = await ExercisesSeenBy(user);

        // Assert
        IdsOf(exercises).Should().Contain(custom);
        (await user.GetAsync($"/exercises/{custom}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAll_ShouldExcludeAnExerciseUsedOnlyInAPrescribedDraftFromTheAthlete()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var custom = await CreateExercise(trainer);
        await AddToWorkout(trainer, SeedData.PrescribedDraftWorkoutId, custom);
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var exercises = await ExercisesSeenBy(user);

        // Assert
        IdsOf(exercises).Should().NotContain(custom);
        (await user.GetAsync($"/exercises/{custom}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_ShouldExcludeAnotherUsersExerciseThatIsInNoVisibleWorkoutFromATrainer()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var custom = await CreateExercise(user);
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var exercises = await ExercisesSeenBy(trainer);

        // Assert
        IdsOf(exercises).Should().NotContain(custom);
        (await trainer.GetAsync($"/exercises/{custom}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_ShouldKeepAnArchivedOwnExerciseWithItsArchiveDate()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var custom = await CreateExercise(user);
        (await user.DeleteAsync($"/exercises/{custom}")).EnsureSuccessStatusCode();

        // Act
        var exercises = await ExercisesSeenBy(user);

        // Assert
        var archived = exercises.EnumerateArray().Single(exercise => exercise.GetProperty("id").GetGuid() == custom);
        archived.GetProperty("archivedAtUtc").ValueKind.Should().Be(JsonValueKind.String);
    }

    [Fact]
    public async Task GetAll_ShouldReturnEveryExerciseToAnAdmin()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var usersExercise = await CreateExercise(user);
        var trainersExercise = await CreateExercise(trainer);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var exercises = await ExercisesSeenBy(admin);

        // Assert
        IdsOf(exercises).Should().Contain([usersExercise, trainersExercise, SeedData.SentadillaId]);
        (await admin.GetAsync($"/exercises/{usersExercise}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
