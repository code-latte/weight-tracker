using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Covers <c>GET /workouts/{workoutId}/observations</c>: the auth matrix, the visibility rule, the empty answer and the
/// shape of the response. What the observations say is covered by <see cref="WorkoutObservationsGenerationTests"/>.
/// Workouts are arranged through the API with <see cref="WorkoutStatsEndpointTest.GivenWorkout"/>.
/// </summary>
public class WorkoutObservationsEndpointTests : WorkoutStatsEndpointTest
{
    public WorkoutObservationsEndpointTests(ApiFactory factory) : base(factory)
    { }

    private async Task<HttpResponseMessage> Get(string email, Guid workoutId)
    {
        var client = await AuthenticatedClientFor(email);

        return await client.GetAsync($"/workouts/{workoutId}/observations");
    }

    private static object[] RecordsAt(decimal weight) => [Rec(10, weight), Rec(10, weight), Rec(10, weight)];

    /// <summary>A completed prescribed workout holding a weight record, so it has a highlight, plus an exercise done above its range.</summary>
    private async Task<Guid> GivenWorkoutWithHighlights()
    {
        await GivenWorkout("2026-11-01", "COMPLETED", User, Trainer,
            new Entry(Press, [Reps(3, 10)], RecordsAt(100m)),
            new Entry(Squat, [Reps(3, 10)], RecordsAt(100m)));

        return await GivenWorkout("2026-11-03", "COMPLETED", User, Trainer,
            new Entry(Press, [Reps(3, 10)], RecordsAt(120m)),
            new Entry(Squat, [RepsRange(3, 8, 9)], RecordsAt(100m)));
    }

    [Fact]
    public async Task Get_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await Client.GetAsync($"/workouts/{SeedData.OwnedWorkoutId}/observations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.GetAsync($"/workouts/{SeedData.OwnedWorkoutId}/observations");

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
    public async Task Get_ShouldReturnNotFoundForAMalformedWorkoutId()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.GetAsync("/workouts/not-a-guid/observations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ShouldRefuseAUserTheWorkoutOfAnotherUser()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.User, SeedData.TrainerOwnWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_ShouldRefuseATrainerAWorkoutThatIsNeitherTheirsNorPrescribedByThem()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.Trainer, SeedData.OwnedWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_ShouldRefuseAnAthleteAPrescribedDraftTheirTrainerHasNotPublished()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.User, SeedData.PrescribedDraftWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(SeedCredentials.User)]
    [InlineData(SeedCredentials.Trainer)]
    [InlineData(SeedCredentials.Admin)]
    public async Task Get_ShouldAllowTheAthleteThePrescribingTrainerAndAnAdmin(string email)
    {
        // Arrange
        var workoutId = await GivenWorkoutWithHighlights();

        // Act
        var response = await Get(email, workoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_ShouldReturnEmptyListsForAVisibleWorkoutThatIsNotCompleted()
    {
        // Arrange
        // Act
        var response = await Get(SeedCredentials.User, SeedData.OwnedWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("observations").GetArrayLength().Should().Be(0);
        body.GetProperty("highlights").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Get_ShouldReturnEveryObservationWithItsShapeAndHighlightsThatReferToThem()
    {
        // Arrange
        var workoutId = await GivenWorkoutWithHighlights();

        // Act
        var response = await Get(SeedCredentials.Trainer, workoutId);

        // Assert
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var observations = body.GetProperty("observations").EnumerateArray().ToList();
        observations.Should().NotBeEmpty();

        foreach (var observation in observations)
        {
            observation.GetProperty("id").GetGuid().Should().NotBeEmpty();
            observation.GetProperty("code").GetString().Should().NotBeNullOrEmpty();
            observation.GetProperty("severity").GetString().Should().BeOneOf("INFO", "WARNING", "SUCCESS");
            observation.TryGetProperty("exerciseId", out _).Should().BeTrue();
            observation.TryGetProperty("priority", out _).Should().BeTrue();
            observation.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Object);
        }

        var record = observations.Single(observation => observation.GetProperty("code").GetString() == "WEIGHT_PERSONAL_RECORD");
        record.GetProperty("exerciseId").GetGuid().Should().Be(Press);
        record.GetProperty("severity").GetString().Should().Be("SUCCESS");
        record.GetProperty("priority").GetString().Should().Be("MEDIUM");
        record.GetProperty("data").GetProperty("previousMaxKg").GetDecimal().Should().Be(100m);

        var highlights = body.GetProperty("highlights").EnumerateArray().ToList();
        highlights.Should().NotBeEmpty().And.HaveCountLessThanOrEqualTo(3);
        highlights.Select(highlight => highlight.GetProperty("rank").GetInt32()).Should().Equal(Enumerable.Range(1, highlights.Count));

        var ids = observations.Select(observation => observation.GetProperty("id").GetGuid()).ToList();
        highlights.Select(highlight => highlight.GetProperty("observationId").GetGuid()).Should().BeSubsetOf(ids);
    }

    [Fact]
    public async Task Get_ShouldListWorkoutLevelObservationsFirstThenByExercisePositionInTheWorkout()
    {
        // Arrange
        await GivenWorkout("2026-11-01", "COMPLETED", User, Trainer,
            new Entry(Press, [Reps(3, 10)], RecordsAt(100m)),
            new Entry(Squat, [Reps(3, 10)], RecordsAt(100m)));
        var workoutId = await GivenWorkout("2026-11-03", "COMPLETED", User, Trainer,
            new Entry(Press, [Reps(3, 10)], [Rec(10, 120m), Rec(6, 120m)]),
            new Entry(Squat, [Reps(3, 10)], RecordsAt(110m)));

        // Act
        var response = await Get(SeedCredentials.Admin, workoutId);

        // Assert
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var order = body.GetProperty("observations").EnumerateArray()
            .Select(observation => (
                observation.GetProperty("code").GetString(),
                observation.GetProperty("exerciseId").ValueKind == JsonValueKind.Null ? (Guid?)null : observation.GetProperty("exerciseId").GetGuid()))
            .ToList();

        order[0].Item2.Should().BeNull();
        order.Select(entry => entry.Item2).Where(exercise => exercise is not null).Distinct()
            .Should().Equal((Guid?)Press, (Guid?)Squat);
    }
}
