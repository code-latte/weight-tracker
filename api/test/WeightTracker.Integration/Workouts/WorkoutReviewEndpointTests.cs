using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Covers <c>PUT /workouts/{workoutId}/review</c>. The seeded started workout is prescribed by the trainer to the user,
/// who completes it through the status route before it is reviewed. <c>OwnedWorkoutId</c> has no trainer, and
/// <c>TrainerOwnWorkoutId</c> belongs to the trainer, so the user is a stranger to it.
/// </summary>
public class WorkoutReviewEndpointTests : WorkoutEndpointTest
{
    public WorkoutReviewEndpointTests(ApiFactory factory) : base(factory)
    { }

    private static readonly Guid PrescribedWorkoutId = SeedData.PrescribedStartedWorkoutId;

    private static string ReviewRoute(Guid workoutId) => $"/workouts/{workoutId}/review";

    private static Task<HttpResponseMessage> PutReview(HttpClient client, Guid workoutId, object? resultsUpdatedAt)
        => client.PutAsJsonAsync(ReviewRoute(workoutId), new { resultsUpdatedAt });

    /// <summary>The workout as an admin reads it, so a rejected call is checked independently of the caller.</summary>
    private async Task<JsonElement> StoredWorkout(Guid workoutId)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        return await admin.GetFromJsonAsync<JsonElement>($"/workouts/{workoutId}", JsonOptions);
    }

    /// <summary>The athlete records one set on the prescribed workout and completes it through the status route.</summary>
    private async Task GivenCompletedByTheAthlete()
    {
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        (await user.PostAsJsonAsync($"/workouts/{PrescribedWorkoutId}/exercises/{SeedData.PressBancaId}/set-records", new { reps = 8, weight = 60 }))
            .StatusCode.Should().Be(HttpStatusCode.OK, "the set record must be arranged first");
        (await user.PutAsJsonAsync($"/workouts/{PrescribedWorkoutId}/status", new { status = "COMPLETED" }))
            .StatusCode.Should().Be(HttpStatusCode.OK, "the workout must be completed first");
    }

    [Fact]
    public async Task Put_ShouldLetThePrescribingTrainerReviewACompletedWorkout()
    {
        // Arrange
        await GivenCompletedByTheAthlete();
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var resultsUpdatedAt = await ResultsUpdatedAtOf(PrescribedWorkoutId);
        var completedAt = await CompletedAtOf(PrescribedWorkoutId);

        // Act
        var response = await PutReview(trainer, PrescribedWorkoutId, resultsUpdatedAt);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var returned = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        returned.GetProperty("status").GetString().Should().Be("REVIEWED");
        returned.GetProperty("reviewedBy").GetGuid().Should().Be(SeedData.TrainerId);
        var stored = await StoredWorkout(PrescribedWorkoutId);
        stored.GetProperty("status").GetString().Should().Be("REVIEWED");
        stored.GetProperty("reviewedBy").GetGuid().Should().Be(SeedData.TrainerId);
        stored.GetProperty("reviewedAt").GetDateTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        completedAt.Should().NotBeNull();
        stored.GetProperty("completedAt").GetDateTime().Should().Be(completedAt!.Value);
    }

    [Fact]
    public async Task Put_ShouldLetAnAdminReviewACompletedPrescribedWorkout()
    {
        // Arrange
        await GivenCompletedByTheAthlete();
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var resultsUpdatedAt = await ResultsUpdatedAtOf(PrescribedWorkoutId);

        // Act
        var response = await PutReview(admin, PrescribedWorkoutId, resultsUpdatedAt);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await StoredWorkout(PrescribedWorkoutId);
        stored.GetProperty("status").GetString().Should().Be("REVIEWED");
        stored.GetProperty("reviewedBy").GetGuid().Should().Be(SeedData.AdminId);
    }

    [Fact]
    public async Task Put_ShouldReviewACompletedWorkoutWithoutResultsWhenNoneAreSent()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "COMPLETED");
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await PutReview(trainer, SeedData.TrainedWorkoutId, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await StoredWorkout(SeedData.TrainedWorkoutId)).GetProperty("status").GetString().Should().Be("REVIEWED");
    }

    [Fact]
    public async Task Put_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await PutReview(Client, PrescribedWorkoutId, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await PutReview(Client, PrescribedWorkoutId, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldRejectATokenWhoseUserNoLongerExistsEvenAfterTheyReviewed()
    {
        // Arrange
        await GivenReviewed(SeedData.TrainedWorkoutId);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        await Factory.RemoveUser(SeedCredentials.Admin);

        // Act
        var response = await PutReview(admin, PrescribedWorkoutId, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldForbidTheAthleteAndKeepTheWorkoutCompleted()
    {
        // Arrange
        await GivenCompletedByTheAthlete();
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var resultsUpdatedAt = await ResultsUpdatedAtOf(PrescribedWorkoutId);

        // Act
        var response = await PutReview(user, PrescribedWorkoutId, resultsUpdatedAt);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await StoredWorkout(PrescribedWorkoutId)).GetProperty("status").GetString().Should().Be("COMPLETED");
    }

    [Fact]
    public async Task Put_ShouldForbidATrainerWhoDidNotPrescribeTheWorkout()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.OwnedWorkoutId, "COMPLETED");
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await PutReview(trainer, SeedData.OwnedWorkoutId, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await StoredWorkout(SeedData.OwnedWorkoutId)).GetProperty("status").GetString().Should().Be("COMPLETED");
    }

    [Fact]
    public async Task Put_ShouldForbidAStrangerWithoutRevealingTheStatus()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.TrainerOwnWorkoutId, "STARTED");
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await PutReview(user, SeedData.TrainerOwnWorkoutId, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Put_ShouldReturnNotFoundForAnUnknownWorkout()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await PutReview(trainer, Guid.NewGuid(), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_ShouldConflictOnAWorkoutThatIsNotCompleted()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await PutReview(trainer, PrescribedWorkoutId, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StoredWorkout(PrescribedWorkoutId)).GetProperty("status").GetString().Should().Be("STARTED");
    }

    [Fact]
    public async Task Put_ShouldConflictOnAnAlreadyReviewedWorkoutAndKeepTheFirstReview()
    {
        // Arrange
        await GivenReviewed(SeedData.TrainedWorkoutId);
        var before = await StoredWorkout(SeedData.TrainedWorkoutId);
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await PutReview(trainer, SeedData.TrainedWorkoutId, await ResultsUpdatedAtOf(SeedData.TrainedWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var stored = await StoredWorkout(SeedData.TrainedWorkoutId);
        stored.GetProperty("reviewedBy").GetGuid().Should().Be(SeedData.AdminId);
        stored.GetProperty("reviewedAt").GetDateTime().Should().Be(before.GetProperty("reviewedAt").GetDateTime());
    }

    [Fact]
    public async Task Put_ShouldConflictOnAWorkoutWithoutTrainerEvenForAnAdmin()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.OwnedWorkoutId, "COMPLETED");
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await PutReview(admin, SeedData.OwnedWorkoutId, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StoredWorkout(SeedData.OwnedWorkoutId)).GetProperty("status").GetString().Should().Be("COMPLETED");
    }

    [Fact]
    public async Task Put_ShouldConflictWhenTheResultsChangedAfterTheTrainerReadThem()
    {
        // Arrange
        await GivenCompletedByTheAthlete();
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var read = await ResultsUpdatedAtOf(PrescribedWorkoutId);
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var setRecordId = (await StoredWorkout(PrescribedWorkoutId)).GetProperty("exercises").EnumerateArray()
            .Single(entry => entry.GetProperty("exerciseId").GetGuid() == SeedData.PressBancaId)
            .GetProperty("setRecords")[0].GetProperty("id").GetGuid();
        (await user.PutAsJsonAsync($"/workouts/{PrescribedWorkoutId}/exercises/{SeedData.PressBancaId}/set-records/{setRecordId}", new { reps = 10, weight = 60 }))
            .StatusCode.Should().Be(HttpStatusCode.OK, "the athlete must correct the set first");

        // Act
        var response = await PutReview(trainer, PrescribedWorkoutId, read);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StoredWorkout(PrescribedWorkoutId)).GetProperty("status").GetString().Should().Be("COMPLETED");
    }

    [Fact]
    public async Task Put_ShouldRejectAMalformedTimestamp()
    {
        // Arrange
        await GivenCompletedByTheAthlete();
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await PutReview(trainer, PrescribedWorkoutId, "yesterday");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await StoredWorkout(PrescribedWorkoutId)).GetProperty("status").GetString().Should().Be("COMPLETED");
    }

    [Fact]
    public async Task Put_ShouldRejectAMissingBody()
    {
        // Arrange
        await GivenCompletedByTheAthlete();
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsync(ReviewRoute(PrescribedWorkoutId), new StringContent(string.Empty, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await StoredWorkout(PrescribedWorkoutId)).GetProperty("status").GetString().Should().Be("COMPLETED");
    }
}
