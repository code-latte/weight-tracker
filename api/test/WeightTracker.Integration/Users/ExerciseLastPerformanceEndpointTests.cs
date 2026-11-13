using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using WeightTracker.Integration.Workouts;
using Xunit;

namespace WeightTracker.Integration.Users;

/// <summary>
/// Covers <c>GET /users/me/exercises/{exerciseId}/last-performance</c>. Every seeded workout of the user is unfinished
/// and has no set records, so each finished session is arranged through the API: the admin starts the workout, the
/// user records the sets, and the admin completes it. The seeded prescribed pair workout (2026-10-07, Press Banca at
/// effort HIGH) and self-created superseries workout (2026-10-13, Press Banca at effort MID) give two sessions of one
/// exercise on different days.
/// </summary>
public class ExerciseLastPerformanceEndpointTests : SetRecordEndpointTest
{
    public ExerciseLastPerformanceEndpointTests(ApiFactory factory) : base(factory)
    { }

    private static string LastPerformanceRoute(Guid exerciseId) => $"/users/me/exercises/{exerciseId}/last-performance";

    /// <summary>Starts the workout, records the given sets on the exercise as its athlete, and completes it.</summary>
    private async Task GivenFinishedSession(Guid workoutId, Guid exerciseId, params object[] sets)
    {
        await GivenWorkoutStatus(workoutId, "STARTED");
        await GivenRecorded(workoutId, exerciseId, sets);
        await GivenWorkoutStatus(workoutId, "COMPLETED");
    }

    private async Task GivenRecorded(Guid workoutId, Guid exerciseId, params object[] sets)
    {
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        foreach (var set in sets)
        {
            var response = await user.PostAsJsonAsync(SetRecordsRoute(workoutId, exerciseId), set);
            response.StatusCode.Should().Be(HttpStatusCode.OK, "the set record must be arranged first");
        }
    }

    /// <summary>Creates another user with the user role through <c>POST /users</c> as the admin and returns their email.</summary>
    private async Task<string> GivenAnotherUser()
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var email = $"{Guid.NewGuid():N}@weighttracker.test";

        var response = await admin.PostAsJsonAsync("/users", new
        {
            email,
            password = SeedCredentials.Password,
            role = (int)Role.USER,
            name = "Extra",
            surname = "User",
            dateBirth = "1990-01-01",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created, "the other user must be arranged first");

        return email;
    }

    private static async Task<JsonElement> Body(HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

    private static async Task ShouldBeEmpty(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
    }

    private static IEnumerable<(int? Reps, int? DurationSeconds, decimal? Weight)> SetsIn(JsonElement body)
        => body.GetProperty("sets").EnumerateArray().Select(set => (
            set.GetProperty("reps") is { ValueKind: not JsonValueKind.Null } reps ? reps.GetInt32() : (int?)null,
            set.GetProperty("durationSeconds") is { ValueKind: not JsonValueKind.Null } seconds ? seconds.GetInt32() : (int?)null,
            set.GetProperty("weight") is { ValueKind: not JsonValueKind.Null } weight ? weight.GetDecimal() : (decimal?)null));

    [Fact]
    public async Task Get_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await Client.GetAsync(LastPerformanceRoute(SeedData.PressBancaId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_ShouldRejectAnInvalidToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.GetAsync(LastPerformanceRoute(SeedData.PressBancaId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_ShouldNotFindAnUnknownExercise()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.GetAsync(LastPerformanceRoute(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ShouldNotFindACustomExerciseTheCallerMayNotSee()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var created = await trainer.PostAsJsonAsync("/exercises", new { languageCode = "es", name = "Remo con banda", muscle = "MIDDLE_BACK", type = "STRENGTH" });
        created.StatusCode.Should().Be(HttpStatusCode.Created, "the custom exercise must be arranged first");
        var exerciseId = (await Body(created)).GetProperty("id").GetGuid();
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.GetAsync(LastPerformanceRoute(exerciseId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ShouldNotMatchAMalformedId()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.GetAsync("/users/me/exercises/not-a-guid/last-performance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ShouldAnswerNoContentWhenTheExerciseWasNeverDone()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.GetAsync(LastPerformanceRoute(SeedData.PressBancaId));

        // Assert
        await ShouldBeEmpty(response);
    }

    [Fact]
    public async Task Get_ShouldAnswerNoContentWhenTheExerciseWasOnlyDoneInAStartedWorkout()
    {
        // Arrange
        await GivenRecorded(StartedWorkoutId, StartedExerciseId, SetRecordBody(10, 60));
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.GetAsync(LastPerformanceRoute(StartedExerciseId));

        // Assert
        await ShouldBeEmpty(response);
    }

    [Fact]
    public async Task Get_ShouldReturnTheDateEffortAndSetsOfTheLatestFinishedWorkout()
    {
        // Arrange
        await GivenFinishedSession(SeedData.PrescribedPairWorkoutId, SeedData.PressBancaId, SetRecordBody(10, 60));
        await GivenFinishedSession(SeedData.SelfCreatedSuperSeriesWorkoutId, SeedData.PressBancaId,
            SetRecordBody(8, 40), SetRecordBody(7, 42.5m), SetRecordBody(6, 45));
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.GetAsync(LastPerformanceRoute(SeedData.PressBancaId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await Body(response);
        body.EnumerateObject().Select(property => property.Name).Should().Equal("workoutDate", "effort", "sets");
        body.GetProperty("workoutDate").GetString().Should().Be("2026-10-13");
        body.GetProperty("effort").GetString().Should().Be("MID");
        SetsIn(body).Should().Equal((8, null, 40m), (7, null, 42.5m), (6, null, 45m));
        body.GetProperty("sets")[0].EnumerateObject().Select(property => property.Name)
            .Should().Equal("reps", "durationSeconds", "weight");
    }

    [Fact]
    public async Task Get_ShouldCountAReviewedWorkout()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.PrescribedPairWorkoutId, "STARTED");
        await GivenRecorded(SeedData.PrescribedPairWorkoutId, SeedData.PressBancaId, SetRecordBody(10, 60));
        await GivenReviewed(SeedData.PrescribedPairWorkoutId);
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.GetAsync(LastPerformanceRoute(SeedData.PressBancaId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await Body(response);
        body.GetProperty("workoutDate").GetString().Should().Be("2026-10-07");
        body.GetProperty("effort").GetString().Should().Be("HIGH");
        SetsIn(body).Should().Equal((10, null, 60m));
    }

    [Fact]
    public async Task Get_ShouldIgnoreTheRecordsOfAnotherUser()
    {
        // Arrange
        await GivenFinishedSession(SeedData.PrescribedPairWorkoutId, SeedData.PressBancaId, SetRecordBody(10, 60));
        var otherUser = await AuthenticatedClientFor(await GivenAnotherUser());

        // Act
        var response = await otherUser.GetAsync(LastPerformanceRoute(SeedData.PressBancaId));

        // Assert
        await ShouldBeEmpty(response);
    }

    [Fact]
    public async Task Get_ShouldNotGiveATrainerTheRecordsOfTheirAthlete()
    {
        // Arrange
        await GivenFinishedSession(SeedData.PrescribedPairWorkoutId, SeedData.PressBancaId, SetRecordBody(10, 60));
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.GetAsync(LastPerformanceRoute(SeedData.PressBancaId));

        // Assert
        await ShouldBeEmpty(response);
    }

    [Fact]
    public async Task Get_ShouldReturnAMissingWeightAsNullAndAZeroWeightAsZero()
    {
        // Arrange
        await GivenFinishedSession(SeedData.PrescribedPairWorkoutId, SeedData.PressBancaId, SetRecordBody(10), SetRecordBody(12, 0));
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.GetAsync(LastPerformanceRoute(SeedData.PressBancaId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SetsIn(await Body(response)).Should().Equal((10, null, null), (12, null, 0m));
    }

    [Fact]
    public async Task Get_ShouldReturnATimedSetWithItsSecondsAndNoReps()
    {
        // Arrange
        await GivenFinishedSession(TimedWorkoutId, TimedExerciseId, DurationBody(45, 10));
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.GetAsync(LastPerformanceRoute(TimedExerciseId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await Body(response);
        body.GetProperty("effort").GetString().Should().Be("LOW");
        SetsIn(body).Should().Equal((null, 45, 10m));
    }
}
