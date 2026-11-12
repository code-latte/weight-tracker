using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Covers <c>GET /workouts/{id}</c>: the auth matrix, the response shape and the role-aware scope,
/// against the seeded dataset.
/// </summary>
public class WorkoutGetByIdEndpointTests : WorkoutEndpointTest
{
    public WorkoutGetByIdEndpointTests(ApiFactory factory) : base(factory)
    { }

    private async Task<HttpResponseMessage> Get(string email, Guid workoutId)
    {
        var client = await AuthenticatedClientFor(email);

        return await client.GetAsync($"/workouts/{workoutId}");
    }

    [Fact]
    public async Task Get_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await Client.GetAsync($"/workouts/{SeedData.OwnedWorkoutId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.GetAsync($"/workouts/{SeedData.OwnedWorkoutId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFoundForAnUnknownWorkout()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.Admin, Guid.NewGuid());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ShouldNotMatchAMalformedId()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.GetAsync("/workouts/not-a-guid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ShouldReturnTheWorkoutWithItsEntriesSetsAndCatalogData()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.Admin, SeedData.TrainedWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("id").GetGuid().Should().Be(SeedData.TrainedWorkoutId);
        body.GetProperty("name").GetString().Should().Be(SeedData.TrainedWorkoutName);
        body.GetProperty("description").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("workoutDate").GetString().Should().Be($"{SeedData.TrainedWorkoutDate:yyyy-MM-dd}");
        body.GetProperty("userId").GetGuid().Should().Be(SeedData.UserId);
        body.GetProperty("trainerId").GetGuid().Should().Be(SeedData.TrainerId);
        var entries = body.GetProperty("exercises").EnumerateArray().ToList();
        entries.Select(entry => entry.GetProperty("exerciseId").GetGuid())
            .Should().Equal(SeedData.SentadillaId, SeedData.PlanchaId);
        entries[0].GetProperty("id").GetGuid().Should().Be(SeedData.TrainedSentadillaEntryId);
        entries[0].GetProperty("effort").GetString().Should().Be("MIDHIGH");
        entries[0].GetProperty("sets").GetArrayLength().Should().BeGreaterThan(0);
        entries[0].TryGetProperty("superSeriesId", out var superSeriesId).Should().BeTrue();
        superSeriesId.ValueKind.Should().Be(JsonValueKind.Null);
        entries[1].GetProperty("sets")[0].GetProperty("target").GetProperty("type").GetString().Should().Be("Duration");
        foreach (var entry in entries)
        {
            entry.TryGetProperty("name", out _).Should().BeFalse();
            entry.GetProperty("translations").GetArrayLength().Should().BeGreaterThan(0);
            entry.TryGetProperty("videoUrl", out _).Should().BeTrue();
            entry.GetProperty("muscle").GetString().Should().NotBeNullOrEmpty();
            entry.GetProperty("bodyPart").GetString().Should().NotBeNullOrEmpty();
            entry.GetProperty("type").GetString().Should().NotBeNullOrEmpty();
        }
    }

    private static IEnumerable<(string?, string?)> TranslationsOf(JsonElement entry)
        => entry.GetProperty("translations").EnumerateArray()
            .Select(translation => (translation.GetProperty("languageCode").GetString(), translation.GetProperty("name").GetString()));

    [Fact]
    public async Task Get_ShouldDescribeTheCatalogExerciseOfEachEntry()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.User, SeedData.OwnedWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var entry = body.GetProperty("exercises").EnumerateArray().Single();
        entry.GetProperty("exerciseId").GetGuid().Should().Be(SeedData.PressBancaId);
        TranslationsOf(entry).Should().BeEquivalentTo(new (string?, string?)[] { ("es", "Press de banca"), ("en", "Barbell Bench Press - Medium Grip") });
        entry.GetProperty("muscle").GetString().Should().Be("CHEST");
        entry.GetProperty("bodyPart").GetString().Should().Be("CHEST");
        entry.GetProperty("type").GetString().Should().Be("STRENGTH");
        entry.TryGetProperty("name", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Get_ShouldDescribeAnEntryAsTheWorkoutListDoes()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);
        var listed = (await client.GetFromJsonAsync<JsonElement>("/workouts", JsonOptions)).EnumerateArray()
            .Single(workout => workout.GetProperty("id").GetGuid() == SeedData.OwnedWorkoutId)
            .GetProperty("exercises").EnumerateArray().Single();

        // Act
        var response = await client.GetAsync($"/workouts/{SeedData.OwnedWorkoutId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entry = (await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("exercises").EnumerateArray().Single();
        TranslationsOf(entry).Should().Equal(TranslationsOf(listed));
        foreach (var property in new[] { "muscle", "bodyPart", "type" })
            entry.GetProperty(property).GetString().Should().Be(listed.GetProperty(property).GetString());
    }

    private async Task<Guid> GivenACustomExerciseInTheOwnedWorkout(HttpClient user)
    {
        var created = await user.PostAsJsonAsync(
            "/exercises", new { languageCode = "es", name = "Remo con banda", muscle = "MIDDLE_BACK", type = "STRENGTH" });
        created.EnsureSuccessStatusCode();
        var exerciseId = (await created.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("id").GetGuid();

        var added = await user.PostAsJsonAsync($"/workouts/{SeedData.OwnedWorkoutId}/exercises", new
        {
            exerciseId,
            effort = "MID",
            sets = new[] { new { count = 3, targetType = "Reps", target = 10 } },
        });
        added.EnsureSuccessStatusCode();

        return exerciseId;
    }

    private static void ShouldDescribeTheRowingCustomExercise(JsonElement body, Guid exerciseId)
    {
        var entry = body.GetProperty("exercises").EnumerateArray()
            .Single(candidate => candidate.GetProperty("exerciseId").GetGuid() == exerciseId);
        TranslationsOf(entry).Should().Equal(("es", "Remo con banda"));
        entry.GetProperty("muscle").GetString().Should().Be("MIDDLE_BACK");
        entry.GetProperty("bodyPart").GetString().Should().Be("BACK");
        entry.GetProperty("type").GetString().Should().Be("STRENGTH");
    }

    [Fact]
    public async Task Get_ShouldDescribeACustomExercise()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var exerciseId = await GivenACustomExerciseInTheOwnedWorkout(user);

        // Act
        var response = await user.GetAsync($"/workouts/{SeedData.OwnedWorkoutId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ShouldDescribeTheRowingCustomExercise(await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions), exerciseId);
    }

    [Fact]
    public async Task Get_ShouldStillDescribeAnArchivedCustomExercise()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var exerciseId = await GivenACustomExerciseInTheOwnedWorkout(user);
        (await user.DeleteAsync($"/exercises/{exerciseId}")).EnsureSuccessStatusCode();

        // Act
        var response = await user.GetAsync($"/workouts/{SeedData.OwnedWorkoutId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ShouldDescribeTheRowingCustomExercise(await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions), exerciseId);
    }

    [Fact]
    public async Task Get_ShouldEmbedTheMesocycleOfAPlacedWorkoutWithItsWeeks()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.User, SeedData.TrainedWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("mesocycleId").GetGuid().Should().Be(SeedData.MesocycleId);
        body.GetProperty("mesocycleWeekNumber").GetInt32().Should().Be(1);
        var mesocycle = body.GetProperty("mesocycle");
        mesocycle.GetProperty("id").GetGuid().Should().Be(SeedData.MesocycleId);
        mesocycle.GetProperty("name").GetString().Should().Be(SeedData.MesocycleName);
        mesocycle.GetProperty("objective").GetString().Should().Be("Build muscle mass before the strength block");
        mesocycle.GetProperty("startDate").GetString().Should().Be("2026-10-01");
        mesocycle.GetProperty("durationInWeeks").GetInt32().Should().Be(SeedData.MesocycleDurationInWeeks);
        mesocycle.GetProperty("weeks").EnumerateArray()
            .Select(week => (week.GetProperty("weekNumber").GetInt32(), week.GetProperty("trainingWeekType").GetString(), week.GetProperty("objective").GetString()))
            .Should().Equal(
                (1, "ADAPTATION", "Learn the technique with moderate loads"),
                (2, "ACCUMULATION", "Raise the weekly volume"),
                (4, "DELOAD", "Recover before the next block"));
        mesocycle.EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo("id", "name", "objective", "startDate", "durationInWeeks", "weeks");
    }

    [Fact]
    public async Task Get_ShouldCarryANullMesocycleForAStandaloneWorkout()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.User, SeedData.OwnedWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.TryGetProperty("mesocycle", out var mesocycle).Should().BeTrue();
        mesocycle.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Theory]
    [InlineData(SeedCredentials.Admin)]
    [InlineData(SeedCredentials.Trainer)]
    [InlineData(SeedCredentials.User)]
    public async Task Get_ShouldEmbedTheMesocycleForEveryCallerWhoSeesTheWorkout(string email)
    {
        // Arrange & Act
        var response = await Get(email, SeedData.TrainedWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("mesocycle").GetProperty("id").GetGuid().Should().Be(SeedData.MesocycleId);
        body.GetProperty("mesocycle").GetProperty("weeks").GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task Get_ShouldNotDiscloseTheExercisesOrTheMesocycleOfARefusedWorkout()
    {
        // Arrange
        // A prescribed draft is hidden from its athlete
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "DRAFT");

        // Act
        var response = await Get(SeedCredentials.User, SeedData.TrainedWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var raw = await response.Content.ReadAsStringAsync();
        foreach (var property in new[] { "\"exercises\"", "\"mesocycle\"", "\"translations\"", "\"weeks\"" })
            raw.Should().NotContain(property);
        raw.Should().NotContain(SeedData.MesocycleName);
    }

    [Fact]
    public async Task Get_ShouldReturnTheNameAndTheDescription()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.User, SeedData.OwnedWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("name").GetString().Should().Be(SeedData.OwnedWorkoutName);
        body.GetProperty("description").GetString().Should().Be(SeedData.OwnedWorkoutDescription);
    }

    [Fact]
    public async Task Get_ShouldCarryTheSharedSuperSeriesIdOfGroupedEntries()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.Admin, SeedData.PrescribedSuperSeriesWorkoutId);

        // Assert
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var superSeriesByEntry = body.GetProperty("exercises").EnumerateArray().ToDictionary(
            entry => entry.GetProperty("id").GetGuid(),
            entry => entry.GetProperty("superSeriesId"));
        superSeriesByEntry[SeedData.PrescribedSuperSeriesPressBancaEntryId].GetInt32().Should().Be(SeedData.SeededSuperSeries);
        superSeriesByEntry[SeedData.PrescribedSuperSeriesSentadillaEntryId].GetInt32().Should().Be(SeedData.SeededSuperSeries);
        superSeriesByEntry[SeedData.PrescribedSuperSeriesPlanchaEntryId].ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Get_ShouldReturnAnEmptyExercisesListForAWorkoutWithoutExercises()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var created = await user.PostAsJsonAsync("/workouts", new { name = "Leg day", workoutDate = "2026-10-20" });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("id").GetGuid();

        // Act
        var response = await user.GetAsync($"/workouts/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("exercises").GetArrayLength().Should().Be(0);
    }

    [Theory]
    [InlineData(SeedCredentials.Admin, "00000000-0000-0000-0000-000000000021", HttpStatusCode.OK)]
    [InlineData(SeedCredentials.Admin, "00000000-0000-0000-0000-000000000023", HttpStatusCode.OK)]
    [InlineData(SeedCredentials.Trainer, "00000000-0000-0000-0000-000000000023", HttpStatusCode.OK)]
    [InlineData(SeedCredentials.Trainer, "00000000-0000-0000-0000-000000000022", HttpStatusCode.OK)]
    [InlineData(SeedCredentials.Trainer, "00000000-0000-0000-0000-000000000021", HttpStatusCode.Forbidden)]
    [InlineData(SeedCredentials.Trainer, "00000000-0000-0000-0000-000000000026", HttpStatusCode.Forbidden)]
    [InlineData(SeedCredentials.User, "00000000-0000-0000-0000-000000000021", HttpStatusCode.OK)]
    [InlineData(SeedCredentials.User, "00000000-0000-0000-0000-000000000022", HttpStatusCode.OK)]
    [InlineData(SeedCredentials.User, "00000000-0000-0000-0000-000000000023", HttpStatusCode.Forbidden)]
    public async Task Get_ShouldApplyTheRoleScope(string email, string workoutId, HttpStatusCode expected)
    {
        // Arrange & Act
        var response = await Get(email, Guid.Parse(workoutId));

        // Assert
        response.StatusCode.Should().Be(expected);
    }

    [Fact]
    public async Task Get_ShouldNotDiscloseTheContentOfAForbiddenWorkout()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.User, SeedData.TrainerOwnWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain(SeedData.TrainerOwnPressBancaEntryId.ToString());
        raw.Should().NotContain(SeedData.PressBancaId.ToString());
    }

    [Theory]
    [InlineData(SeedCredentials.Admin, "00000000-0000-0000-0000-000000000028", HttpStatusCode.OK)]
    [InlineData(SeedCredentials.Trainer, "00000000-0000-0000-0000-000000000028", HttpStatusCode.OK)]
    [InlineData(SeedCredentials.User, "00000000-0000-0000-0000-000000000028", HttpStatusCode.Forbidden)]
    [InlineData(SeedCredentials.User, "00000000-0000-0000-0000-000000000029", HttpStatusCode.OK)]
    [InlineData(SeedCredentials.User, "00000000-0000-0000-0000-000000000026", HttpStatusCode.OK)]
    [InlineData(SeedCredentials.Trainer, "00000000-0000-0000-0000-000000000029", HttpStatusCode.OK)]
    public async Task Get_ShouldApplyTheDraftRule(string email, string workoutId, HttpStatusCode expected)
    {
        // Arrange & Act
        var response = await Get(email, Guid.Parse(workoutId));

        // Assert
        response.StatusCode.Should().Be(expected);
    }

    [Fact]
    public async Task Get_ShouldNotDiscloseThePrescribedDraftToItsAthlete()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.User, SeedData.PrescribedDraftWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain(SeedData.PrescribedDraftPressBancaEntryId.ToString());
        raw.Should().NotContain(SeedData.PressBancaId.ToString());
    }

    [Theory]
    [InlineData("PUBLISHED")]
    [InlineData("STARTED")]
    [InlineData("COMPLETED")]
    public async Task Get_ShouldShowAPrescribedWorkoutToItsAthleteOnceItIsNoLongerADraft(string status)
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.PrescribedDraftWorkoutId, status);

        // Act
        var response = await Get(SeedCredentials.User, SeedData.PrescribedDraftWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("status").GetString().Should().Be(status);
    }

    [Fact]
    public async Task Get_ShouldShowAReviewedWorkoutToItsAthleteWithItsReview()
    {
        // Arrange
        await GivenReviewed(SeedData.PrescribedDraftWorkoutId);

        // Act
        var response = await Get(SeedCredentials.User, SeedData.PrescribedDraftWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("status").GetString().Should().Be("REVIEWED");
        body.GetProperty("reviewedBy").GetGuid().Should().Be(SeedData.AdminId);
        body.GetProperty("reviewedAt").ValueKind.Should().Be(JsonValueKind.String);
        body.GetProperty("completedAt").ValueKind.Should().Be(JsonValueKind.String);
    }

    [Fact]
    public async Task Get_ShouldReturnEmptyReviewAndResultsForAWorkoutThatNeverHadThem()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.User, SeedData.OwnedWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("reviewedAt").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("reviewedBy").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("resultsUpdatedAt").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("completedAt").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Get_ShouldHideAPrescribedWorkoutAgainWhenAnAdminSetsItBackToDraft()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "DRAFT");

        // Act
        var response = await Get(SeedCredentials.User, SeedData.TrainedWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(SeedCredentials.Admin)]
    [InlineData(SeedCredentials.Trainer)]
    public async Task Get_ShouldCarryTheStatusOfThePrescribedDraft(string email)
    {
        // Arrange & Act
        var response = await Get(email, SeedData.PrescribedDraftWorkoutId);

        // Assert
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("status").GetString().Should().Be("DRAFT");
    }

    private static readonly string[] PublicProfileProperties = ["id", "email", "name", "surname", "role"];

    [Fact]
    public async Task Get_ShouldCarryThePublicProfilesOfTheAthleteAndTrainerOfAPrescribedWorkout()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.Admin, SeedData.TrainedWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var athlete = body.GetProperty("athlete");
        var trainer = body.GetProperty("trainer");
        athlete.GetProperty("id").GetGuid().Should().Be(body.GetProperty("userId").GetGuid());
        athlete.GetProperty("email").GetString().Should().Be(SeedData.UserEmail);
        trainer.GetProperty("id").GetGuid().Should().Be(body.GetProperty("trainerId").GetGuid());
        trainer.GetProperty("email").GetString().Should().Be(SeedData.TrainerEmail);
        athlete.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(PublicProfileProperties);
        trainer.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(PublicProfileProperties);
    }

    [Fact]
    public async Task Get_ShouldCarryTheAthleteAndANullTrainerOfASelfPlannedWorkout()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.User, SeedData.OwnedWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("athlete").GetProperty("id").GetGuid().Should().Be(SeedData.UserId);
        body.TryGetProperty("trainer", out var trainer).Should().BeTrue();
        trainer.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Update_ShouldKeepItsResponseWithoutAthleteTrainerMesocycleOrCatalogData()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "DRAFT");
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync($"/workouts/{SeedData.TrainedWorkoutId}", new
        {
            name = "Renamed day",
            workoutDate = "2026-10-03",
            userId = SeedData.UserId,
            trainerId = SeedData.TrainerId,
            mesocycleId = SeedData.MesocycleId,
            mesocycleWeekNumber = 1,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.TryGetProperty("athlete", out _).Should().BeFalse();
        body.TryGetProperty("trainer", out _).Should().BeFalse();
        body.TryGetProperty("mesocycle", out _).Should().BeFalse();
        foreach (var entry in body.GetProperty("exercises").EnumerateArray())
        {
            entry.TryGetProperty("translations", out _).Should().BeFalse();
            entry.TryGetProperty("muscle", out _).Should().BeFalse();
            entry.TryGetProperty("bodyPart", out _).Should().BeFalse();
            entry.TryGetProperty("type", out _).Should().BeFalse();
        }
    }

    [Fact]
    public async Task Get_ShouldListTheSetRecordsOfEachExerciseInOrder()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.SelfCreatedPairWorkoutId, "STARTED");
        var athlete = await AuthenticatedClientFor(SeedCredentials.User);
        var route = $"/workouts/{SeedData.SelfCreatedPairWorkoutId}/exercises/{SeedData.SentadillaId}/set-records";
        (await athlete.PostAsJsonAsync(route, new { reps = 6, weight = 100m })).EnsureSuccessStatusCode();
        (await athlete.PostAsJsonAsync(route, new { reps = 5, weight = 102.5m })).EnsureSuccessStatusCode();

        // Act
        var response = await Get(SeedCredentials.Admin, SeedData.SelfCreatedPairWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var exercises = (await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("exercises").EnumerateArray().ToList();
        var sentadilla = exercises.Single(entry => entry.GetProperty("exerciseId").GetGuid() == SeedData.SentadillaId);
        var setRecords = sentadilla.GetProperty("setRecords").EnumerateArray().ToList();
        setRecords.Select(record => record.GetProperty("reps").GetInt32()).Should().Equal(6, 5);
        setRecords.Select(record => record.GetProperty("weight").GetDecimal()).Should().Equal(100m, 102.5m);
        setRecords.Should().AllSatisfy(record =>
        {
            record.GetProperty("id").GetGuid().Should().NotBeEmpty();
            record.GetProperty("type").GetString().Should().Be("Reps");
            record.GetProperty("durationSeconds").ValueKind.Should().Be(JsonValueKind.Null);
            record.GetProperty("registeredAtUtc").GetDateTime().Kind.Should().Be(DateTimeKind.Utc);
        });
        var plancha = exercises.Single(entry => entry.GetProperty("exerciseId").GetGuid() == SeedData.PlanchaId);
        plancha.GetProperty("setRecords").EnumerateArray().Should().BeEmpty();
    }
}
