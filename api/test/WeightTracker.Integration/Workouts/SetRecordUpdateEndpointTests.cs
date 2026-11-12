using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Covers <c>PUT /workouts/{workoutId}/exercises/{exerciseId}/set-records/{setRecordId}</c>, including every branch of the
/// recording rule reachable with the seeded users.
/// </summary>
public class SetRecordUpdateEndpointTests : SetRecordEndpointTest
{
    public SetRecordUpdateEndpointTests(ApiFactory factory) : base(factory)
    { }

    [Fact]
    public async Task Put_ShouldRejectAnonymousCallers()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));

        // Act
        var response = await Client.PutAsJsonAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, ids[1]), SetRecordBody(9, 62.5m));

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
        var response = await Client.PutAsJsonAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, ids[1]), SetRecordBody(9, 62.5m));

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
        var response = await client.PutAsJsonAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, ids[1]), SetRecordBody(9, 62.5m));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldCorrectRepsAndWeightForTheAthleteAndKeepIdTimeAndPosition()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60), (7, 60));
        var before = await SetRecordsOf(StartedWorkoutId, StartedExerciseId);
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, ids[1]), SetRecordBody(9, 62.5m));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        SetRecordsIn(body, StartedExerciseId)[1].Reps.Should().Be(9);
        var stored = await SetRecordsOf(StartedWorkoutId, StartedExerciseId);
        stored.Select(record => record.Id).Should().Equal(ids);
        stored[1].Reps.Should().Be(9);
        stored[1].Weight.Should().Be(62.5m);
        stored[1].RegisteredAtUtc.Should().Be(before[1].RegisteredAtUtc);
        stored[0].Should().Be(before[0]);
        stored[2].Should().Be(before[2]);
    }

    [Fact]
    public async Task Put_ShouldCorrectTheSetForAnAdmin()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, ids[1]), SetRecordBody(9, 62.5m));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId))[1].Reps.Should().Be(9);
    }

    [Fact]
    public async Task Put_ShouldForbidThePrescribingTrainerAndChangeNothing()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, ids[1]), SetRecordBody(9, 62.5m));

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
        var response = await client.PutAsJsonAsync(SetRecordRoute(SeedData.TrainerOwnWorkoutId, StartedExerciseId, Guid.NewGuid()), SetRecordBody(9, 62.5m));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Put_ShouldForbidRatherThanRevealTheStatusToAUserWhoDoesNotOwnTheWorkout()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordRoute(SeedData.TrainerOwnWorkoutId, StartedExerciseId, Guid.NewGuid()), SetRecordBody(9, 62.5m));

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
        var response = await client.PutAsJsonAsync(SetRecordRoute(Guid.NewGuid(), StartedExerciseId, ids[1]), SetRecordBody(9, 62.5m));

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
        var response = await client.PutAsJsonAsync(SetRecordRoute(StartedWorkoutId, SeedData.SentadillaId, ids[1]), SetRecordBody(9, 62.5m));

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
        var response = await client.PutAsJsonAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, ids[1]), SetRecordBody(9, 62.5m));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => (record.Id, record.Reps, record.Weight))
            .Should().Equal((ids[0], 10, 60m), (ids[1], 8, 60m));
    }

    [Fact]
    public async Task Put_ShouldReturnNotFoundForAnUnknownSetRecord()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, Guid.NewGuid()), SetRecordBody(9, 62.5m));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => (record.Id, record.Reps, record.Weight))
            .Should().Equal((ids[0], 10, 60m), (ids[1], 8, 60m));
    }

    [Fact]
    public async Task Put_ShouldRejectAnEmptyBodyAndKeepThePreviousValues()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, ids[1]), new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => (record.Id, record.Reps, record.Weight))
            .Should().Equal((ids[0], 10, 60m), (ids[1], 8, 60m));
    }

    [Fact]
    public async Task Put_ShouldRejectBothRepsAndDurationAndKeepThePreviousValues()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, ids[1]), new { reps = 9, durationSeconds = 40 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => (record.Id, record.Reps, record.Weight))
            .Should().Equal((ids[0], 10, 60m), (ids[1], 8, 60m));
    }

    [Fact]
    public async Task Put_ShouldClearTheWeightWhenItIsOmitted()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, ids[1]), SetRecordBody(9));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await SetRecordsOf(StartedWorkoutId, StartedExerciseId);
        stored[1].Reps.Should().Be(9);
        stored[1].Weight.Should().BeNull();
        stored[0].Weight.Should().Be(60m);
    }

    [Fact]
    public async Task Put_ShouldCorrectADurationForATimeBasedExerciseAndKeepIdTimeAndPosition()
    {
        // Arrange
        var ids = await GivenRecordedTimedSets((40, 10m), (40, 10m));
        var before = await SetRecordsOf(TimedWorkoutId, TimedExerciseId);
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordRoute(TimedWorkoutId, TimedExerciseId, ids[1]), DurationBody(35));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await SetRecordsOf(TimedWorkoutId, TimedExerciseId);
        stored.Select(record => record.Id).Should().Equal(ids);
        stored[1].Type.Should().Be("Duration");
        stored[1].DurationSeconds.Should().Be(35);
        stored[1].Reps.Should().BeNull();
        stored[1].Weight.Should().BeNull();
        stored[1].RegisteredAtUtc.Should().Be(before[1].RegisteredAtUtc);
        stored[0].Should().Be(before[0]);
    }

    [Fact]
    public async Task Put_ShouldConflictOnADurationForARepBasedExerciseAndKeepThePreviousValues()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, ids[1]), DurationBody(40));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => (record.Id, record.Reps, record.Weight))
            .Should().Equal((ids[0], 10, 60m), (ids[1], 8, 60m));
    }

    [Fact]
    public async Task Put_ShouldConflictOnRepsForATimeBasedExerciseAndKeepThePreviousValues()
    {
        // Arrange
        var ids = await GivenRecordedTimedSets((40, null));
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordRoute(TimedWorkoutId, TimedExerciseId, ids[0]), SetRecordBody(10));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await SetRecordsOf(TimedWorkoutId, TimedExerciseId)).Single().DurationSeconds.Should().Be(40);
    }

    [Fact]
    public async Task Put_ShouldRejectAMissingResult()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, ids[1]), new { weight = 62.5m });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => (record.Id, record.Reps, record.Weight))
            .Should().Equal((ids[0], 10, 60m), (ids[1], 8, 60m));
    }

    [Theory]
    [InlineData(0, 60)]
    [InlineData(8, -5)]
    public async Task Put_ShouldRejectInvalidRepsOrWeightAndKeepThePreviousValues(int reps, decimal weight)
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, ids[1]), SetRecordBody(reps, weight));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => (record.Id, record.Reps, record.Weight))
            .Should().Equal((ids[0], 10, 60m), (ids[1], 8, 60m));
    }

    [Fact]
    public async Task Put_ShouldLetTheAthleteCorrectASetOfACompletedWorkoutAndMoveTheResultsTime()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60), (8, 60));
        await GivenWorkoutStatus(StartedWorkoutId, "COMPLETED");
        var before = await ResultsUpdatedAtOf(StartedWorkoutId);
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, ids[1]), SetRecordBody(9, 62.5m));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => (record.Id, record.Reps, record.Weight))
            .Should().Equal((ids[0], 10, 60m), (ids[1], 9, 62.5m));
        (await ResultsUpdatedAtOf(StartedWorkoutId)).Should().BeAfter(before!.Value);
    }

    [Fact]
    public async Task Put_ShouldKeepTheResultsTimeWhenAReviewedWorkoutRefusesTheCorrection()
    {
        // Arrange
        var ids = await GivenRecordedSets((10, 60));
        await GivenReviewed(StartedWorkoutId);
        var before = await ResultsUpdatedAtOf(StartedWorkoutId);
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(SetRecordRoute(StartedWorkoutId, StartedExerciseId, ids[0]), SetRecordBody(9, 62.5m));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ResultsUpdatedAtOf(StartedWorkoutId)).Should().Be(before);
    }
}
