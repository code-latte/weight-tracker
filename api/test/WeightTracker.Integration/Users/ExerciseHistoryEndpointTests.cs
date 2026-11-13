using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using WeightTracker.Integration.Workouts;
using Xunit;

namespace WeightTracker.Integration.Users;

/// <summary>
/// Covers <c>GET /users/{userId}/exercises/{exerciseId}/history</c>: the auth matrix, the limit, who may read whose
/// history, the sessions that count and the numbers computed for each. Workouts are arranged through the API with
/// <see cref="WorkoutStatsEndpointTest.GivenWorkout"/>.
/// </summary>
public class ExerciseHistoryEndpointTests : WorkoutStatsEndpointTest
{
    public ExerciseHistoryEndpointTests(ApiFactory factory) : base(factory)
    { }

    private static string HistoryRoute(Guid userId, Guid exerciseId, string? query = null)
        => $"/users/{userId}/exercises/{exerciseId}/history{query}";

    private async Task<HttpResponseMessage> Get(string email, Guid userId, Guid exerciseId, string? query = null)
    {
        var client = await AuthenticatedClientFor(email);

        return await client.GetAsync(HistoryRoute(userId, exerciseId, query));
    }

    private async Task<IReadOnlyList<JsonElement>> SessionsOf(string email, Guid userId, Guid exerciseId, string? query = null)
    {
        var response = await Get(email, userId, exerciseId, query);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).EnumerateArray().ToList();
    }

    private static IEnumerable<Guid> WorkoutIds(IEnumerable<JsonElement> sessions)
        => sessions.Select(session => session.GetProperty("workoutId").GetGuid());

    private static decimal? Decimal(JsonElement session, string name)
        => session.GetProperty(name) is { ValueKind: not JsonValueKind.Null } value ? value.GetDecimal() : null;

    [Fact]
    public async Task Get_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await Client.GetAsync(HistoryRoute(User, Press));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_ShouldRejectAnInvalidToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.GetAsync(HistoryRoute(User, Press));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("51")]
    [InlineData("abc")]
    public async Task Get_ShouldRejectALimitThatIsNotAnIntegerFromOneToFifty(string limit)
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.User, User, Press, $"?limit={limit}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("50")]
    public async Task Get_ShouldAcceptTheLimitsOfTheRange(string limit)
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.User, User, Press, $"?limit={limit}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_ShouldRefuseAUserTheHistoryOfAnotherUser()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.User, Trainer, Press);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_ShouldNotFindAnUnknownUser()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.Admin, Guid.NewGuid(), Press);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ShouldNotFindAnUnknownExercise()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.User, User, Guid.NewGuid());

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
        var exerciseId = (await created.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("id").GetGuid();

        // Act
        var response = await Get(SeedCredentials.User, User, exerciseId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ShouldNotMatchAMalformedExerciseId()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.GetAsync($"/users/{User}/exercises/not-a-guid/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ShouldNotMatchAMalformedUserId()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.GetAsync($"/users/not-a-guid/exercises/{Press}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ShouldReturnAnEmptyListWhenTheExerciseWasNeverDone()
    {
        // Arrange
        await GivenWorkout("2026-11-03", "COMPLETED", User, null, new Entry(Squat, [Reps(1, 5)], [Rec(5, 100m)]));

        // Act
        var sessions = await SessionsOf(SeedCredentials.User, User, Press);

        // Assert
        sessions.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_ShouldReturnTheSessionsNewestFirst()
    {
        // Arrange
        var oldest = await GivenWorkout("2026-11-03", "COMPLETED", User, null, new Entry(Press, [Reps(1, 5)], [Rec(5, 50m)]));
        var newest = await GivenWorkout("2026-11-10", "COMPLETED", User, null, new Entry(Press, [Reps(1, 8)], [Rec(8, 60m)]));
        var middle = await GivenWorkout("2026-11-06", "COMPLETED", User, null, new Entry(Press, [Reps(1, 6)], [Rec(6, 55m)]));

        // Act
        var sessions = await SessionsOf(SeedCredentials.User, User, Press);

        // Assert
        WorkoutIds(sessions).Should().Equal(newest, middle, oldest);
        sessions.Select(session => session.GetProperty("workoutDate").GetString()).Should().Equal("2026-11-10", "2026-11-06", "2026-11-03");
    }

    [Fact]
    public async Task Get_ShouldKeepOnlyTheLatestSessionsUpToTheLimit()
    {
        // Arrange
        await GivenWorkout("2026-11-03", "COMPLETED", User, null, new Entry(Press, [Reps(1, 5)], [Rec(5, 50m)]));
        var middle = await GivenWorkout("2026-11-06", "COMPLETED", User, null, new Entry(Press, [Reps(1, 6)], [Rec(6, 55m)]));
        var newest = await GivenWorkout("2026-11-10", "COMPLETED", User, null, new Entry(Press, [Reps(1, 8)], [Rec(8, 60m)]));

        // Act
        var sessions = await SessionsOf(SeedCredentials.User, User, Press, "?limit=2");

        // Assert
        WorkoutIds(sessions).Should().Equal(newest, middle);
    }

    [Fact]
    public async Task Get_ShouldReturnTenSessionsWhenNoLimitIsGiven()
    {
        // Arrange
        for (var day = 1; day <= 11; day++)
            await GivenWorkout($"2026-10-{day:00}", "COMPLETED", User, null, new Entry(Press, [Reps(1, 5)], [Rec(5, 50m)]));

        // Act
        var sessions = await SessionsOf(SeedCredentials.User, User, Press);

        // Assert
        sessions.Should().HaveCount(10);
        sessions.Last().GetProperty("workoutDate").GetString().Should().Be("2026-10-02");
    }

    [Fact]
    public async Task Get_ShouldComputeTheMaxWeightVolumeAndEstimatedOneRepMaxOfASession()
    {
        // Arrange
        await GivenWorkout("2026-11-10", "COMPLETED", User, null,
            new Entry(Press, [Reps(3, 10)], [Rec(10, 60m), Rec(3, 70m), Rec(8)]));

        // Act
        var sessions = await SessionsOf(SeedCredentials.User, User, Press);

        // Assert
        var session = sessions.Single();
        Decimal(session, "maxWeight").Should().Be(70m);
        Decimal(session, "volume").Should().Be(810m);
        Decimal(session, "estimatedOneRepMax").Should().Be(80m);
    }

    [Fact]
    public async Task Get_ShouldReturnNullNumbersForASessionWithoutWeight()
    {
        // Arrange
        await GivenWorkout("2026-11-10", "COMPLETED", User, null, new Entry(Press, [Reps(2, 10)], [Rec(10), Rec(8)]));

        // Act
        var sessions = await SessionsOf(SeedCredentials.User, User, Press);

        // Assert
        var session = sessions.Single();
        Decimal(session, "maxWeight").Should().BeNull();
        Decimal(session, "volume").Should().BeNull();
        Decimal(session, "estimatedOneRepMax").Should().BeNull();
    }

    [Fact]
    public async Task Get_ShouldKeepTheMaxWeightButNotVolumeOrEstimateForATimedExercise()
    {
        // Arrange
        await GivenWorkout("2026-11-10", "COMPLETED", User, null, new Entry(Plank, [Sets(2, "Duration", 45)], [Timed(45, 10m), Timed(40, 15m)]));

        // Act
        var sessions = await SessionsOf(SeedCredentials.User, User, Plank);

        // Assert
        var session = sessions.Single();
        Decimal(session, "maxWeight").Should().Be(15m);
        Decimal(session, "volume").Should().BeNull();
        Decimal(session, "estimatedOneRepMax").Should().BeNull();
    }

    [Fact]
    public async Task Get_ShouldCountAReviewedWorkout()
    {
        // Arrange
        var reviewed = await GivenWorkout("2026-11-06", "REVIEWED", User, Trainer, new Entry(Press, [Reps(1, 8)], [Rec(8, 60m)]));

        // Act
        var sessions = await SessionsOf(SeedCredentials.User, User, Press);

        // Assert
        WorkoutIds(sessions).Should().Equal(reviewed);
    }

    [Fact]
    public async Task Get_ShouldIgnoreUnfinishedWorkoutsAndTheOnesWithoutRecordsForTheExercise()
    {
        // Arrange
        var finished = await GivenWorkout("2026-11-03", "COMPLETED", User, null, new Entry(Press, [Reps(1, 5)], [Rec(5, 50m)]));
        await GivenWorkout("2026-11-12", "STARTED", User, null, new Entry(Press, [Reps(1, 12)], [Rec(12, 80m)]));
        await GivenWorkout("2026-11-13", "DRAFT", User, null, new Entry(Press, [Reps(1, 12)], []));
        await GivenWorkout("2026-11-08", "COMPLETED", User, null, new Entry(Press, [Reps(1, 10)], []), new Entry(Squat, [Reps(1, 5)], [Rec(5, 100m)]));

        // Act
        var sessions = await SessionsOf(SeedCredentials.User, User, Press);

        // Assert
        WorkoutIds(sessions).Should().Equal(finished);
    }

    [Fact]
    public async Task Get_ShouldExcludeTheSessionsOfOtherUsers()
    {
        // Arrange
        var mine = await GivenWorkout("2026-11-03", "COMPLETED", User, null, new Entry(Press, [Reps(1, 5)], [Rec(5, 50m)]));
        await GivenWorkout("2026-11-09", "COMPLETED", Trainer, null, new Entry(Press, [Reps(1, 15)], [Rec(15, 100m)]));

        // Act
        var sessions = await SessionsOf(SeedCredentials.User, User, Press);

        // Assert
        WorkoutIds(sessions).Should().Equal(mine);
    }

    /// <summary>A session the trainer prescribed to the user and one the user created on their own. Returns both ids.</summary>
    private async Task<(Guid Prescribed, Guid Own)> GivenPrescribedAndOwnSessions()
    {
        var prescribed = await GivenWorkout("2026-11-03", "COMPLETED", User, Trainer, new Entry(Press, [Reps(1, 5)], [Rec(5, 50m)]));
        var own = await GivenWorkout("2026-11-08", "COMPLETED", User, null, new Entry(Press, [Reps(1, 9)], [Rec(9, 70m)]));

        return (prescribed, own);
    }

    [Fact]
    public async Task Get_ShouldGiveTheAthleteEverySessionWhoeverPrescribedIt()
    {
        // Arrange
        var (prescribed, own) = await GivenPrescribedAndOwnSessions();

        // Act
        var sessions = await SessionsOf(SeedCredentials.User, User, Press);

        // Assert
        WorkoutIds(sessions).Should().Equal(own, prescribed);
    }

    [Fact]
    public async Task Get_ShouldGiveATrainerOnlyTheSessionsTheyPrescribedWhenReadingAnAthlete()
    {
        // Arrange
        var (prescribed, _) = await GivenPrescribedAndOwnSessions();

        // Act
        var sessions = await SessionsOf(SeedCredentials.Trainer, User, Press);

        // Assert
        WorkoutIds(sessions).Should().Equal(prescribed);
    }

    [Fact]
    public async Task Get_ShouldGiveATrainerTheirOwnSessionsWhenReadingThemselves()
    {
        // Arrange
        var own = await GivenWorkout("2026-11-09", "COMPLETED", Trainer, null, new Entry(Press, [Reps(1, 15)], [Rec(15, 100m)]));
        await GivenPrescribedAndOwnSessions();

        // Act
        var sessions = await SessionsOf(SeedCredentials.Trainer, Trainer, Press);

        // Assert
        WorkoutIds(sessions).Should().Equal(own);
    }

    [Fact]
    public async Task Get_ShouldGiveAnAdminEverySessionOfAnyUser()
    {
        // Arrange
        var (prescribed, own) = await GivenPrescribedAndOwnSessions();

        // Act
        var sessions = await SessionsOf(SeedCredentials.Admin, User, Press);

        // Assert
        WorkoutIds(sessions).Should().Equal(own, prescribed);
    }
}
