using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Covers <c>DELETE /workouts/{workoutId}/exercises/{exerciseId}/set-records/{setRecordId}</c>, including every branch of the
/// recording rule reachable with the seeded users. The route has no body, so there is no structural 400 case.
/// </summary>
public class SetRecordRemoveEndpointTests : SetRecordEndpointTest
{
    public SetRecordRemoveEndpointTests(ApiFactory factory) : base(factory)
    { }

    [Fact]
    public async Task Delete_ShouldRejectAnonymousCallers()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));

        // Act
        var response = await Client.DeleteAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, ids[1]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_ShouldRejectAGarbageToken()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.DeleteAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, ids[1]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_ShouldRejectATokenWhoseUserNoLongerExists()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.User);
        await Factory.RemoveUser(SeedCredentials.User);

        // Act
        var response = await client.DeleteAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, ids[1]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_ShouldRemoveTheSetForTheAthleteAndKeepTheOrderOfTheRest()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60), (7, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.DeleteAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, ids[1]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        SetRecordsIn(body, StartedExerciseId).Select(record => record.Id).Should().Equal(ids[0], ids[2]);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => record.Id).Should().Equal(ids[0], ids[2]);
    }

    [Fact]
    public async Task Delete_ShouldRemoveTheSetForAnAdmin()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.DeleteAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, ids[1]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => record.Id).Should().Equal(ids[0]);
    }

    [Fact]
    public async Task Delete_ShouldForbidThePrescribingTrainerAndChangeNothing()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.DeleteAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, ids[1]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => (record.Id, record.Reps, record.Weight))
            .Should().Equal((ids[0], 10, 60m), (ids[1], 8, 60m));
    }

    [Fact]
    public async Task Delete_ShouldForbidATrainerOnAWorkoutTheyOwn()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.TrainerOwnWorkoutId, "STARTED");
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.DeleteAsync(SetRecordRoute(SeedData.TrainerOwnWorkoutId, StartedExerciseId, Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_ShouldForbidRatherThanRevealTheStatusToAUserWhoDoesNotOwnTheWorkout()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.DeleteAsync(SetRecordRoute(SeedData.TrainerOwnWorkoutId, StartedExerciseId, Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFoundForAnUnknownWorkout()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.DeleteAsync(SetRecordRoute(Guid.NewGuid(), StartedExerciseId, ids[1]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFoundForAnExerciseNotInTheWorkout()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.DeleteAsync(SetRecordRoute(StartedWorkoutId, SeedData.SentadillaId, ids[1]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => (record.Id, record.Reps, record.Weight))
            .Should().Equal((ids[0], 10, 60m), (ids[1], 8, 60m));
    }

    [Fact]
    public async Task Delete_ShouldConflictOnAReviewedWorkoutEvenForAnAdmin()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        await GivenReviewed(StartedWorkoutId);
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.DeleteAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, ids[1]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => (record.Id, record.Reps, record.Weight))
            .Should().Equal((ids[0], 10, 60m), (ids[1], 8, 60m));
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFoundForAnUnknownSetRecord()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.DeleteAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => (record.Id, record.Reps, record.Weight))
            .Should().Equal((ids[0], 10, 60m), (ids[1], 8, 60m));
    }

    [Fact]
    public async Task Delete_ShouldLetTheAthleteRemoveASetOfACompletedWorkoutAndMoveTheResultsTime()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        await GivenWorkoutStatus(StartedWorkoutId, "COMPLETED");
        var before = await ResultsUpdatedAtOf(StartedWorkoutId);
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.DeleteAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, ids[1]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => record.Id).Should().Equal(ids[0]);
        (await ResultsUpdatedAtOf(StartedWorkoutId)).Should().BeAfter(before!.Value);
    }
}
