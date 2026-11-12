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
    public async Task Post_ShouldConflictOnRepsForATimeBasedExercise()
    {
        // Arrange
        await GivenWorkoutStatus(TimedWorkoutId, "STARTED");
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(SetRecordsRoute(TimedWorkoutId, TimedExerciseId), SetRecordBody(1, 0));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await SetRecordsOf(TimedWorkoutId, TimedExerciseId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Post_ShouldConflictOnADurationForARepBasedExercise()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(Route, DurationBody(40));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Post_ShouldRecordASetWithoutWeight()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(Route, SetRecordBody(8));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Should().ContainSingle().Subject;
        stored.Type.Should().Be("Reps");
        stored.Reps.Should().Be(8);
        stored.DurationSeconds.Should().BeNull();
        stored.Weight.Should().BeNull();
    }

    [Fact]
    public async Task Post_ShouldRecordADurationForATimeBasedExercise()
    {
        // Arrange
        await GivenWorkoutStatus(TimedWorkoutId, "STARTED");
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(SetRecordsRoute(TimedWorkoutId, TimedExerciseId), DurationBody(30));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        SetRecordsIn(body, TimedExerciseId).Should().ContainSingle().Which.DurationSeconds.Should().Be(30);
        var stored = (await SetRecordsOf(TimedWorkoutId, TimedExerciseId)).Should().ContainSingle().Subject;
        stored.Type.Should().Be("Duration");
        stored.DurationSeconds.Should().Be(30);
        stored.Reps.Should().BeNull();
        stored.Weight.Should().BeNull();
    }

    [Fact]
    public async Task Post_ShouldRecordAWeightedDurationForATimeBasedExercise()
    {
        // Arrange
        await GivenWorkoutStatus(TimedWorkoutId, "STARTED");
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(SetRecordsRoute(TimedWorkoutId, TimedExerciseId), DurationBody(45, 10m));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = (await SetRecordsOf(TimedWorkoutId, TimedExerciseId)).Should().ContainSingle().Subject;
        stored.DurationSeconds.Should().Be(45);
        stored.Weight.Should().Be(10m);
    }

    [Fact]
    public async Task Post_ShouldRecordThePrescribedRepsForAnEmptyBody()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(Route, new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Should().ContainSingle().Subject;
        stored.Type.Should().Be("Reps");
        stored.Reps.Should().Be(10);
        stored.Weight.Should().BeNull();
    }

    [Fact]
    public async Task Post_ShouldRecordThePrescribedSecondsForAnEmptyBody()
    {
        // Arrange
        await GivenWorkoutStatus(TimedWorkoutId, "STARTED");
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(SetRecordsRoute(TimedWorkoutId, TimedExerciseId), new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = (await SetRecordsOf(TimedWorkoutId, TimedExerciseId)).Should().ContainSingle().Subject;
        stored.Type.Should().Be("Duration");
        stored.DurationSeconds.Should().Be(TimedExercisePrescribedSeconds);
        stored.Reps.Should().BeNull();
        stored.Weight.Should().BeNull();
    }

    [Fact]
    public async Task Post_ShouldRejectAnEmptyBodyForARepsRangeExercise()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var toRange = new
        {
            exerciseId = StartedExerciseId,
            effort = "MID",
            sets = new[] { new { count = 3, targetType = "RepsRange", target = 8, maxTarget = 12 } },
        };
        (await admin.PutAsJsonAsync($"/workouts/{StartedWorkoutId}/exercises/{StartedExerciseId}", toRange)).StatusCode
            .Should().Be(HttpStatusCode.OK, "the exercise must be prescribed as a range first");
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(Route, new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Post_ShouldRejectAnEmptyBodyBeyondThePrescribedSets()
    {
        // Arrange
        await GivenRecordedSets((10, 60), (10, 60), (10, 60));
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(Route, new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Should().HaveCount(3);
    }

    [Fact]
    public async Task Post_ShouldRejectBothRepsAndDuration()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(Route, new { reps = 8, durationSeconds = 40 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Post_ShouldRejectAnInvalidDuration()
    {
        // Arrange
        await GivenWorkoutStatus(TimedWorkoutId, "STARTED");
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(SetRecordsRoute(TimedWorkoutId, TimedExerciseId), DurationBody(0));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await SetRecordsOf(TimedWorkoutId, TimedExerciseId)).Should().BeEmpty();
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
