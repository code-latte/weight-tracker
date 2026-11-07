using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Covers <c>PUT /workouts/{workoutId}/exercises/{exerciseId}/set-records/order</c>, including every branch of the
/// recording rule reachable with the seeded users.
/// </summary>
public class SetRecordReorderEndpointTests : SetRecordEndpointTest
{
    public SetRecordReorderEndpointTests(ApiFactory factory) : base(factory)
    { }

    [Fact]
    public async Task Put_ShouldRejectAnonymousCallers()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));

        // Act
        var response = await Client.PutAsJsonAsync(SetRecordOrderRoute(StartedWorkoutId, StartedExerciseId), Order(ids[1], ids[0]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldRejectAGarbageToken()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.PutAsJsonAsync(SetRecordOrderRoute(StartedWorkoutId, StartedExerciseId), Order(ids[1], ids[0]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldRejectATokenWhoseUserNoLongerExists()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.User);
        await Factory.RemoveUser(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordOrderRoute(StartedWorkoutId, StartedExerciseId), Order(ids[1], ids[0]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldStoreTheNewOrderForTheAthleteAndKeepTheValues()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 62.5m), (7, 65));
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordOrderRoute(StartedWorkoutId, StartedExerciseId), Order(ids[2], ids[0], ids[1]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        SetRecordsIn(body, StartedExerciseId).Select(record => record.Id).Should().Equal(ids[2], ids[0], ids[1]);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => (record.Id, record.Reps, record.Weight))
            .Should().Equal((ids[2], 7, 65m), (ids[0], 10, 60m), (ids[1], 8, 62.5m));
    }

    [Fact]
    public async Task Put_ShouldStoreTheNewOrderForAnAdmin()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordOrderRoute(StartedWorkoutId, StartedExerciseId), Order(ids[1], ids[0]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => record.Id).Should().Equal(ids[1], ids[0]);
    }

    [Fact]
    public async Task Put_ShouldForbidThePrescribingTrainerAndChangeNothing()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordOrderRoute(StartedWorkoutId, StartedExerciseId), Order(ids[1], ids[0]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => (record.Id, record.Reps, record.Weight))
            .Should().Equal((ids[0], 10, 60m), (ids[1], 8, 60m));
    }

    [Fact]
    public async Task Put_ShouldForbidATrainerOnAWorkoutTheyOwn()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.TrainerOwnWorkoutId, "STARTED");
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordOrderRoute(SeedData.TrainerOwnWorkoutId, StartedExerciseId), Order(Guid.NewGuid(), Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Put_ShouldForbidRatherThanRevealTheStatusToAUserWhoDoesNotOwnTheWorkout()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordOrderRoute(SeedData.TrainerOwnWorkoutId, StartedExerciseId), Order(Guid.NewGuid(), Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Put_ShouldReturnNotFoundForAnUnknownWorkout()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordOrderRoute(Guid.NewGuid(), StartedExerciseId), Order(ids[1], ids[0]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_ShouldReturnNotFoundForAnExerciseNotInTheWorkout()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordOrderRoute(StartedWorkoutId, SeedData.SentadillaId), Order(ids[1], ids[0]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => (record.Id, record.Reps, record.Weight))
            .Should().Equal((ids[0], 10, 60m), (ids[1], 8, 60m));
    }

    [Fact]
    public async Task Put_ShouldConflictOnAReviewedWorkoutEvenForAnAdmin()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        await GivenReviewed(StartedWorkoutId);
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordOrderRoute(StartedWorkoutId, StartedExerciseId), Order(ids[1], ids[0]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => (record.Id, record.Reps, record.Weight))
            .Should().Equal((ids[0], 10, 60m), (ids[1], 8, 60m));
    }

    [Fact]
    public async Task Put_ShouldConflictOnAMissingSetRecordAndKeepTheOrder()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordOrderRoute(StartedWorkoutId, StartedExerciseId), Order(ids[1]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => (record.Id, record.Reps, record.Weight))
            .Should().Equal((ids[0], 10, 60m), (ids[1], 8, 60m));
    }

    [Fact]
    public async Task Put_ShouldConflictOnARepeatedSetRecordAndKeepTheOrder()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordOrderRoute(StartedWorkoutId, StartedExerciseId), Order(ids[1], ids[1]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => (record.Id, record.Reps, record.Weight))
            .Should().Equal((ids[0], 10, 60m), (ids[1], 8, 60m));
    }

    [Fact]
    public async Task Put_ShouldConflictOnASetRecordOfAnotherExerciseAndKeepTheOrder()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordOrderRoute(StartedWorkoutId, StartedExerciseId), Order(ids[1], Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => (record.Id, record.Reps, record.Weight))
            .Should().Equal((ids[0], 10, 60m), (ids[1], 8, 60m));
    }

    [Fact]
    public async Task Put_ShouldRejectAMissingList()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordOrderRoute(StartedWorkoutId, StartedExerciseId), new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => (record.Id, record.Reps, record.Weight))
            .Should().Equal((ids[0], 10, 60m), (ids[1], 8, 60m));
    }

    [Fact]
    public async Task Put_ShouldLetTheAthleteReorderTheSetsOfACompletedWorkoutAndMoveTheResultsTime()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        await GivenWorkoutStatus(StartedWorkoutId, "COMPLETED");
        var before = await ResultsUpdatedAtOf(StartedWorkoutId);
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordOrderRoute(StartedWorkoutId, StartedExerciseId), Order(ids[1], ids[0]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => record.Id).Should().Equal(ids[1], ids[0]);
        (await ResultsUpdatedAtOf(StartedWorkoutId)).Should().BeAfter(before!.Value);
    }
}
