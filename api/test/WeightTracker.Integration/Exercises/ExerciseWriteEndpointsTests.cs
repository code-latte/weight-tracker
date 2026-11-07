using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Exercises;

/// <summary>
/// Covers <c>POST /exercises</c>, <c>PUT /exercises/{id}</c> and <c>DELETE /exercises/{id}</c>: every route
/// rejects a missing, invalid or orphaned token, and the rest of the matrix follows ownership, visibility and
/// the protection of the system catalog.
/// </summary>
public class ExerciseWriteEndpointsTests : ExerciseEndpointTest
{
    public ExerciseWriteEndpointsTests(ApiFactory factory) : base(factory)
    { }

    private static HttpRequestMessage RequestFor(string method, Guid id)
        => method switch
        {
            "POST" => new HttpRequestMessage(HttpMethod.Post, "/exercises") { Content = JsonContent.Create(ExerciseBody()) },
            "PUT" => new HttpRequestMessage(HttpMethod.Put, $"/exercises/{id}") { Content = JsonContent.Create(ExerciseBody()) },
            _ => new HttpRequestMessage(HttpMethod.Delete, $"/exercises/{id}"),
        };

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task Write_ShouldRejectAnonymousCallers(string method)
    {
        // Arrange
        var request = RequestFor(method, SeedData.SentadillaId);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task Write_ShouldRejectAGarbageToken(string method)
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");
        var request = RequestFor(method, SeedData.SentadillaId);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task Write_ShouldRejectATokenWhoseUserNoLongerExists(string method)
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);
        await Factory.RemoveUser(SeedCredentials.Trainer);
        var request = RequestFor(method, SeedData.SentadillaId);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_ShouldLetATrainerCreateAnExerciseWithASingleTranslation()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync("/exercises", ExerciseBody(videoUrl: "https://videos.example.com/remo"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        response.Headers.Location!.ToString().Should().EndWith($"/exercises/{created.GetProperty("id").GetGuid()}");
        created.GetProperty("ownerId").GetGuid().Should().Be(SeedData.TrainerId);
        created.GetProperty("freeExerciseDbId").ValueKind.Should().Be(JsonValueKind.Null);
        created.GetProperty("archivedAtUtc").ValueKind.Should().Be(JsonValueKind.Null);
        created.GetProperty("muscle").GetString().Should().Be("MIDDLE_BACK");
        created.GetProperty("bodyPart").GetString().Should().Be("BACK");
        created.GetProperty("videoUrl").GetString().Should().Be("https://videos.example.com/remo");
        ExerciseJson.Translations(created).Should().Equal(("es", "Remo con banda"));
        IdsOf(await ExercisesSeenBy(trainer)).Should().Contain(created.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Post_ShouldLetAnAthleteCreateAnExercise()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.PostAsJsonAsync("/exercises", ExerciseBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("ownerId").GetGuid().Should().Be(SeedData.UserId);
    }

    [Fact]
    public async Task Post_ShouldCreateOnlyTheSentLanguage()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync("/exercises", ExerciseBody(name: "Band row", languageCode: "en"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        ExerciseJson.Translations(await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).Should().Equal(("en", "Band row"));
    }

    [Fact]
    public async Task Post_ShouldNotRequireANameThatDiffersFromASystemExercise()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync("/exercises", ExerciseBody(name: "Sentadilla con barra"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Post_ShouldRefuseAnAdminAndCreateNothing()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var before = IdsOf(await ExercisesSeenBy(admin));

        // Act
        var response = await admin.PostAsJsonAsync("/exercises", ExerciseBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        IdsOf(await ExercisesSeenBy(admin)).Should().BeEquivalentTo(before);
    }

    [Fact]
    public async Task Post_ShouldRejectAMissingName()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var body = new { languageCode = "es", muscle = "MIDDLE_BACK", type = "STRENGTH" };

        // Act
        var response = await trainer.PostAsJsonAsync("/exercises", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("WINGS", "STRENGTH", "es", "https://videos.example.com/remo")]
    [InlineData("MIDDLE_BACK", "YOGA", "es", "https://videos.example.com/remo")]
    [InlineData("MIDDLE_BACK", "STRENGTH", "spanish", "https://videos.example.com/remo")]
    [InlineData("MIDDLE_BACK", "STRENGTH", "es", "not a url")]
    public async Task Post_ShouldRejectInvalidInputAndCreateNothing(string muscle, string type, string languageCode, string videoUrl)
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var before = IdsOf(await ExercisesSeenBy(trainer));

        // Act
        var response = await trainer.PostAsJsonAsync("/exercises", ExerciseBody(muscle: muscle, type: type, languageCode: languageCode, videoUrl: videoUrl));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        IdsOf(await ExercisesSeenBy(trainer)).Should().BeEquivalentTo(before);
    }

    [Fact]
    public async Task Put_ShouldLetTheOwnerRenameTheSameLanguage()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var id = await CreateExercise(trainer);

        // Act
        var response = await trainer.PutAsJsonAsync($"/exercises/{id}", ExerciseBody(name: "Remo con goma", muscle: "LATS", type: "CARDIO"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        ExerciseJson.Translations(updated).Should().Equal(("es", "Remo con goma"));
        updated.GetProperty("muscle").GetString().Should().Be("LATS");
        updated.GetProperty("type").GetString().Should().Be("CARDIO");
        updated.GetProperty("ownerId").GetGuid().Should().Be(SeedData.TrainerId);
    }

    [Fact]
    public async Task Put_ShouldAddAnotherLanguageAndKeepTheExistingOne()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var id = await CreateExercise(trainer);

        // Act
        var response = await trainer.PutAsJsonAsync($"/exercises/{id}", ExerciseBody(name: "Band row", languageCode: "en"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ExerciseJson.Translations(await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions))
            .Should().BeEquivalentTo([("es", "Remo con banda"), ("en", "Band row")]);
    }

    [Fact]
    public async Task Put_ShouldLetAnAdminEditACustomExerciseWithoutChangingItsOwner()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var id = await CreateExercise(trainer);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PutAsJsonAsync($"/exercises/{id}", ExerciseBody(name: "Remo editado"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("ownerId").GetGuid().Should().Be(SeedData.TrainerId);
    }

    [Fact]
    public async Task Put_ShouldForbidAnAthleteWhoSeesTheirTrainersExerciseThroughAWorkout()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var id = await CreateExercise(trainer);
        await AddToPublishedWorkout(trainer, SeedData.TrainedWorkoutId, id);
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.PutAsJsonAsync($"/exercises/{id}", ExerciseBody(name: "Hackeado"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ExerciseJson.Translations(await trainer.GetFromJsonAsync<JsonElement>($"/exercises/{id}", JsonOptions)).Should().Equal(("es", "Remo con banda"));
    }

    [Fact]
    public async Task Put_ShouldHideAnInvisibleExerciseAsNotFound()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var id = await CreateExercise(user);
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync($"/exercises/{id}", ExerciseBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_ShouldReportAnUnknownExerciseAsNotFound()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync($"/exercises/{Id.New().Value}", ExerciseBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_ShouldForbidAnAdminOnASystemExerciseAndLeaveItUnchanged()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PutAsJsonAsync($"/exercises/{SeedData.SentadillaId}", ExerciseBody(name: "Otra cosa", muscle: "GLUTES"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var stored = await Client.GetFromJsonAsync<JsonElement>($"/exercises/{SeedData.SentadillaId}", JsonOptions);
        stored.GetProperty("muscle").GetString().Should().Be("QUADRICEPS");
        ExerciseJson.Translations(stored).Should().BeEquivalentTo([("es", "Sentadilla con barra"), ("en", "Barbell Squat")]);
    }

    [Fact]
    public async Task Put_ShouldRejectAnArchivedExercise()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var id = await CreateExercise(user);
        (await user.DeleteAsync($"/exercises/{id}")).EnsureSuccessStatusCode();

        // Act
        var response = await user.PutAsJsonAsync($"/exercises/{id}", ExerciseBody(name: "Otro"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Put_ShouldRejectInvalidInputAndKeepTheExercise()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var id = await CreateExercise(trainer);

        // Act
        var response = await trainer.PutAsJsonAsync($"/exercises/{id}", ExerciseBody(muscle: "WINGS"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await trainer.GetFromJsonAsync<JsonElement>($"/exercises/{id}", JsonOptions)).GetProperty("muscle").GetString().Should().Be("MIDDLE_BACK");
    }

    [Fact]
    public async Task Delete_ShouldLetTheOwnerArchiveTheExerciseAndKeepItReadable()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var id = await CreateExercise(user);

        // Act
        var response = await user.DeleteAsync($"/exercises/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var stored = await user.GetFromJsonAsync<JsonElement>($"/exercises/{id}", JsonOptions);
        stored.GetProperty("archivedAtUtc").ValueKind.Should().Be(JsonValueKind.String);
    }

    [Fact]
    public async Task Delete_ShouldKeepAnArchivedExerciseInTheWorkoutsThatUseIt()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var id = await CreateExercise(trainer);
        await AddToPublishedWorkout(trainer, SeedData.TrainedWorkoutId, id);

        // Act
        var response = await trainer.DeleteAsync($"/exercises/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var workouts = await trainer.GetFromJsonAsync<JsonElement>("/workouts", JsonOptions);
        var entry = workouts.EnumerateArray().Single(workout => workout.GetProperty("id").GetGuid() == SeedData.TrainedWorkoutId)
            .GetProperty("exercises").EnumerateArray().Single(item => item.GetProperty("exerciseId").GetGuid() == id);
        ExerciseJson.Translations(entry).Should().Equal(("es", "Remo con banda"));
    }

    [Fact]
    public async Task Delete_ShouldRejectArchivingTwice()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var id = await CreateExercise(user);
        (await user.DeleteAsync($"/exercises/{id}")).EnsureSuccessStatusCode();

        // Act
        var response = await user.DeleteAsync($"/exercises/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_ShouldLetAnAdminArchiveACustomExercise()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var id = await CreateExercise(user);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.DeleteAsync($"/exercises/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_ShouldForbidAnAdminOnASystemExercise()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.DeleteAsync($"/exercises/{SeedData.CurlBicepsId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Client.GetFromJsonAsync<JsonElement>($"/exercises/{SeedData.CurlBicepsId}", JsonOptions))
            .GetProperty("archivedAtUtc").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Delete_ShouldForbidAnAthleteWhoSeesTheirTrainersExerciseThroughAWorkout()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var id = await CreateExercise(trainer);
        await AddToPublishedWorkout(trainer, SeedData.TrainedWorkoutId, id);
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.DeleteAsync($"/exercises/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await trainer.GetFromJsonAsync<JsonElement>($"/exercises/{id}", JsonOptions))
            .GetProperty("archivedAtUtc").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Delete_ShouldHideAnInvisibleExerciseAsNotFound()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var id = await CreateExercise(user);
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.DeleteAsync($"/exercises/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldReportAnUnknownExerciseAsNotFound()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.DeleteAsync($"/exercises/{Id.New().Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
