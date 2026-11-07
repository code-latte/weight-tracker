using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Covers <c>POST /workouts/{workoutId}/exercises/{exerciseId}/set-records</c>, including every branch of the
/// recording rule reachable with the seeded users.
/// </summary>
public class SetRecordAddEndpointTests : SetRecordEndpointTest
{
    public SetRecordAddEndpointTests(ApiFactory factory) : base(factory)
    { }

    private static readonly string Route = SetRecordsRoute(StartedWorkoutId, StartedExerciseId);

    private static readonly object EightAtSixtyTwoAndAHalf = SetRecordBody(8, 62.5m);

    [Fact]
    public async Task Post_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await Client.PostAsJsonAsync(Route, EightAtSixtyTwoAndAHalf);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.PostAsJsonAsync(Route, EightAtSixtyTwoAndAHalf);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_ShouldRejectATokenWhoseUserNoLongerExists()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);
        await Factory.RemoveUser(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(Route, EightAtSixtyTwoAndAHalf);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_ShouldRecordTheSetLastForTheAthleteAndReturnTheWorkout()
    {
        // Arrange
        var earlier = await GivenRecordedSets((10, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.User);
        var before = DateTime.UtcNow;

        // Act
        var response = await client.PostAsJsonAsync(Route, EightAtSixtyTwoAndAHalf);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("id").GetGuid().Should().Be(StartedWorkoutId);
        SetRecordsIn(body, StartedExerciseId).Should().HaveCount(2);
        var stored = await SetRecordsOf(StartedWorkoutId, StartedExerciseId);
        stored.Select(record => record.Id).First().Should().Be(earlier[0]);
        stored[1].Reps.Should().Be(8);
        stored[1].Weight.Should().Be(62.5m);
        stored[1].RegisteredAtUtc.Should().BeOnOrAfter(before.AddSeconds(-1)).And.BeOnOrBefore(DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task Post_ShouldRecordTheSetForAnAdminOnAWorkoutTheyDoNotOwn()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PostAsJsonAsync(Route, EightAtSixtyTwoAndAHalf);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Should().ContainSingle().Which.Reps.Should().Be(8);
    }

    [Fact]
    public async Task Post_ShouldForbidThePrescribingTrainerAndRecordNothing()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PostAsJsonAsync(Route, EightAtSixtyTwoAndAHalf);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Post_ShouldForbidATrainerOnAWorkoutTheyOwn()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.TrainerOwnWorkoutId, "STARTED");
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PostAsJsonAsync(SetRecordsRoute(SeedData.TrainerOwnWorkoutId, SeedData.PressBancaId), EightAtSixtyTwoAndAHalf);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SetRecordsOf(SeedData.TrainerOwnWorkoutId, SeedData.PressBancaId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Post_ShouldForbidAUserWhoDoesNotOwnTheWorkout()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.TrainerOwnWorkoutId, "STARTED");
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(SetRecordsRoute(SeedData.TrainerOwnWorkoutId, SeedData.PressBancaId), EightAtSixtyTwoAndAHalf);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SetRecordsOf(SeedData.TrainerOwnWorkoutId, SeedData.PressBancaId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Post_ShouldForbidRatherThanRevealTheStatusToACallerWhoMayNotRecord()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(SetRecordsRoute(SeedData.TrainerOwnWorkoutId, SeedData.PressBancaId), EightAtSixtyTwoAndAHalf);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_ShouldReturnNotFoundForAnUnknownWorkout()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(SetRecordsRoute(Guid.NewGuid(), StartedExerciseId), EightAtSixtyTwoAndAHalf);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_ShouldReturnNotFoundForAnExerciseNotInTheWorkout()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(SetRecordsRoute(StartedWorkoutId, SeedData.SentadillaId), EightAtSixtyTwoAndAHalf);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Post_ShouldConflictOnAReviewedWorkoutEvenForAnAdmin()
    {
        // Arrange
        await GivenReviewed(StartedWorkoutId);
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PostAsJsonAsync(Route, EightAtSixtyTwoAndAHalf);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Post_ShouldConflictOnAnExerciseThatIsNotRepBased()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.SelfCreatedPairWorkoutId, "STARTED");
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(SetRecordsRoute(SeedData.SelfCreatedPairWorkoutId, SeedData.PlanchaId), SetRecordBody(1, 0));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await SetRecordsOf(SeedData.SelfCreatedPairWorkoutId, SeedData.PlanchaId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Post_ShouldRejectAMissingWeight()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(Route, new { reps = 8 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Should().BeEmpty();
    }

    [Theory]
    [InlineData(0, 60)]
    [InlineData(8, -5)]
    [InlineData(8, 20.125)]
    public async Task Post_ShouldRejectInvalidRepsOrWeight(int reps, decimal weight)
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(Route, SetRecordBody(reps, weight));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Post_ShouldExposeTheResultsTimeOfTheWorkout()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);
        var before = DateTime.UtcNow.AddMinutes(-1);

        // Act
        var response = await client.PostAsJsonAsync(Route, EightAtSixtyTwoAndAHalf);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var returned = (await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("resultsUpdatedAt").GetDateTime();
        returned.Should().BeAfter(before);
        (await ResultsUpdatedAtOf(StartedWorkoutId)).Should().Be(returned);
    }

    [Fact]
    public async Task Post_ShouldLetTheAthleteAddASetToACompletedWorkout()
    {
        // Arrange
        await GivenWorkoutStatus(StartedWorkoutId, "COMPLETED");
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(Route, EightAtSixtyTwoAndAHalf);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Should().ContainSingle();
    }
}
