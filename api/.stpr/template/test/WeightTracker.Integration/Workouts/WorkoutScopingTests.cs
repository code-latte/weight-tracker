using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Covers the role-aware scoping rule of <c>GET /workouts</c> against the seeded dataset:
/// workouts owned by the user on their own, workouts owned by the user and prescribed by the
/// trainer, and one owned by the trainer themselves.
/// </summary>
public class WorkoutScopingTests : WorkoutEndpointTest
{
    public WorkoutScopingTests(ApiFactory factory) : base(factory)
    { }

    private async Task<IReadOnlyList<Guid>> WorkoutIdsVisibleTo(string email, string route = "/workouts")
    {
        var client = await AuthenticatedClientFor(email);

        var response = await client.GetAsync(route);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        return payload.EnumerateArray()
            .Select(workout => workout.GetProperty("id").GetGuid())
            .ToList();
    }

    private async Task<JsonElement> WorkoutVisibleTo(string email, Guid workoutId)
    {
        var client = await AuthenticatedClientFor(email);

        var payload = await client.GetFromJsonAsync<JsonElement>("/workouts", JsonOptions);

        return payload.EnumerateArray().Single(workout => workout.GetProperty("id").GetGuid() == workoutId);
    }

    [Fact]
    public async Task GetAll_ShouldCarryTheReviewOfAReviewedWorkout()
    {
        // Arrange
        await GivenReviewed(SeedData.TrainedWorkoutId);

        // Act
        var workout = await WorkoutVisibleTo(SeedCredentials.Trainer, SeedData.TrainedWorkoutId);

        // Assert
        workout.GetProperty("status").GetString().Should().Be("REVIEWED");
        workout.GetProperty("reviewedBy").GetGuid().Should().Be(SeedData.AdminId);
        workout.GetProperty("reviewedAt").ValueKind.Should().Be(JsonValueKind.String);
        workout.GetProperty("completedAt").ValueKind.Should().Be(JsonValueKind.String);
    }

    [Fact]
    public async Task GetAll_ShouldCarryTheStartDateOfAStartedWorkout()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "STARTED");
        var startedAt = await StartedAtOf(SeedData.TrainedWorkoutId);

        // Act
        var workout = await WorkoutVisibleTo(SeedCredentials.User, SeedData.TrainedWorkoutId);

        // Assert
        workout.GetProperty("status").GetString().Should().Be("STARTED");
        startedAt.Should().NotBeNull();
        workout.GetProperty("startedAt").GetDateTime().Should().Be(startedAt!.Value);
    }

    [Fact]
    public async Task GetAll_ShouldCarryTheCompletionDateOfACompletedWorkout()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "COMPLETED");
        var completedAt = await CompletedAtOf(SeedData.TrainedWorkoutId);

        // Act
        var workout = await WorkoutVisibleTo(SeedCredentials.User, SeedData.TrainedWorkoutId);

        // Assert
        workout.GetProperty("status").GetString().Should().Be("COMPLETED");
        completedAt.Should().NotBeNull();
        workout.GetProperty("completedAt").GetDateTime().Should().Be(completedAt!.Value);
    }

    [Fact]
    public async Task GetAll_ShouldCarryEmptyReviewAndResultsForAWorkoutThatNeverHadThem()
    {
        // Arrange & Act
        var workout = await WorkoutVisibleTo(SeedCredentials.Trainer, SeedData.TrainedWorkoutId);

        // Assert
        workout.GetProperty("reviewedAt").ValueKind.Should().Be(JsonValueKind.Null);
        workout.GetProperty("reviewedBy").ValueKind.Should().Be(JsonValueKind.Null);
        workout.GetProperty("resultsUpdatedAt").ValueKind.Should().Be(JsonValueKind.Null);
        workout.GetProperty("startedAt").ValueKind.Should().Be(JsonValueKind.Null);
        workout.GetProperty("completedAt").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetAll_ShouldRejectATokenWhoseUserNoLongerExists()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);
        await Factory.RemoveUser(SeedCredentials.User);

        // Act
        var response = await client.GetAsync("/workouts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task User_ShouldOnlySeeTheirOwnWorkouts()
    {
        // Arrange & Act
        var visible = await WorkoutIdsVisibleTo(SeedCredentials.User);

        // Assert
        visible.Should().BeEquivalentTo(new[]
        {
            SeedData.OwnedWorkoutId,
            SeedData.TrainedWorkoutId,
            SeedData.PrescribedPairWorkoutId,
            SeedData.PrescribedSuperSeriesWorkoutId,
            SeedData.SelfCreatedPairWorkoutId,
            SeedData.SelfCreatedSuperSeriesWorkoutId,
            SeedData.PrescribedStartedWorkoutId,
        });
    }

    [Fact]
    public async Task User_ShouldNotSeeAPrescribedDraft()
    {
        // Arrange & Act
        var visible = await WorkoutIdsVisibleTo(SeedCredentials.User);

        // Assert
        visible.Should().NotContain(SeedData.PrescribedDraftWorkoutId);
    }

    [Fact]
    public async Task User_ShouldNotSeeAWorkoutOwnedBySomebodyElse()
    {
        // Arrange & Act
        var visible = await WorkoutIdsVisibleTo(SeedCredentials.User);

        // Assert
        visible.Should().NotContain(SeedData.TrainerOwnWorkoutId);
    }

    [Fact]
    public async Task Trainer_ShouldSeeOwnedAndPrescribedWorkouts()
    {
        // Arrange & Act
        var visible = await WorkoutIdsVisibleTo(SeedCredentials.Trainer);

        // Assert
        visible.Should().BeEquivalentTo(new[]
        {
            SeedData.TrainedWorkoutId,
            SeedData.TrainerOwnWorkoutId,
            SeedData.PrescribedPairWorkoutId,
            SeedData.PrescribedSuperSeriesWorkoutId,
            SeedData.PrescribedDraftWorkoutId,
            SeedData.PrescribedStartedWorkoutId,
        });
    }

    [Fact]
    public async Task Trainer_ShouldNotSeeAWorkoutTheyNeitherOwnNorPrescribed()
    {
        // Arrange & Act
        var visible = await WorkoutIdsVisibleTo(SeedCredentials.Trainer);

        // Assert
        visible.Should().NotContain(new[]
        {
            SeedData.OwnedWorkoutId,
            SeedData.SelfCreatedPairWorkoutId,
            SeedData.SelfCreatedSuperSeriesWorkoutId,
        });
    }

    [Fact]
    public async Task Admin_ShouldSeeEveryWorkout()
    {
        // Arrange & Act
        var visible = await WorkoutIdsVisibleTo(SeedCredentials.Admin);

        // Assert
        visible.Should().BeEquivalentTo(new[]
        {
            SeedData.OwnedWorkoutId,
            SeedData.TrainedWorkoutId,
            SeedData.TrainerOwnWorkoutId,
            SeedData.PrescribedPairWorkoutId,
            SeedData.PrescribedSuperSeriesWorkoutId,
            SeedData.SelfCreatedPairWorkoutId,
            SeedData.SelfCreatedSuperSeriesWorkoutId,
            SeedData.PrescribedDraftWorkoutId,
            SeedData.PrescribedStartedWorkoutId,
        });
    }

    [Fact]
    public async Task Standalone_ShouldLeaveOutTheWorkoutsPlacedInAMesocycle()
    {
        // Arrange & Act
        var visible = await WorkoutIdsVisibleTo(SeedCredentials.Admin, "/workouts?standalone=true");

        // Assert
        visible.Should().BeEquivalentTo(new[]
        {
            SeedData.OwnedWorkoutId,
            SeedData.TrainerOwnWorkoutId,
            SeedData.PrescribedPairWorkoutId,
            SeedData.PrescribedSuperSeriesWorkoutId,
            SeedData.SelfCreatedPairWorkoutId,
            SeedData.SelfCreatedSuperSeriesWorkoutId,
            SeedData.PrescribedDraftWorkoutId,
        });
    }

    [Fact]
    public async Task Standalone_ShouldNotWidenWhatATrainerSees()
    {
        // Arrange
        var all = await WorkoutIdsVisibleTo(SeedCredentials.Trainer);

        // Act
        var standalone = await WorkoutIdsVisibleTo(SeedCredentials.Trainer, "/workouts?standalone=true");

        // Assert
        standalone.Should().BeSubsetOf(all);
        standalone.Should().NotContain(new[] { SeedData.TrainedWorkoutId, SeedData.PrescribedStartedWorkoutId });
    }

    [Fact]
    public async Task User_ShouldSeeAPrescribedDraftOnceItIsPublished()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.PrescribedDraftWorkoutId, "PUBLISHED");

        // Act
        var visible = await WorkoutIdsVisibleTo(SeedCredentials.User);

        // Assert
        visible.Should().Contain(SeedData.PrescribedDraftWorkoutId);
    }

    [Fact]
    public async Task User_ShouldNoLongerSeeAPrescribedWorkoutThatGoesBackToDraft()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "DRAFT");

        // Act
        var visible = await WorkoutIdsVisibleTo(SeedCredentials.User);

        // Assert
        visible.Should().NotContain(SeedData.TrainedWorkoutId);
        visible.Should().Contain(SeedData.PrescribedPairWorkoutId);
    }

    [Fact]
    public async Task User_ShouldAlwaysSeeTheirSelfPlannedDrafts()
    {
        // Arrange & Act
        var visible = await WorkoutIdsVisibleTo(SeedCredentials.User);

        // Assert
        visible.Should().Contain(new[] { SeedData.OwnedWorkoutId, SeedData.SelfCreatedPairWorkoutId });
    }

    [Fact]
    public async Task Trainer_ShouldSeeTheStatusOfEachWorkout()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var payload = await client.GetFromJsonAsync<JsonElement>("/workouts", JsonOptions);

        // Assert
        var statuses = payload.EnumerateArray().ToDictionary(
            workout => workout.GetProperty("id").GetGuid(),
            workout => workout.GetProperty("status").GetString());
        statuses[SeedData.PrescribedDraftWorkoutId].Should().Be("DRAFT");
        statuses[SeedData.TrainedWorkoutId].Should().Be("PUBLISHED");
        statuses[SeedData.PrescribedStartedWorkoutId].Should().Be("STARTED");
    }

    [Fact]
    public async Task User_ShouldSeeThemselvesAsTheAthleteAndTheirTrainer()
    {
        // Arrange & Act
        var workout = await WorkoutVisibleTo(SeedCredentials.User, SeedData.TrainedWorkoutId);

        // Assert
        workout.GetProperty("athlete").GetProperty("id").GetGuid().Should().Be(SeedData.UserId);
        workout.GetProperty("trainer").GetProperty("id").GetGuid().Should().Be(SeedData.TrainerId);
    }

    [Fact]
    public async Task Trainer_ShouldStillSeeTheAthleteOnceTheRelationshipHasEnded()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        (await trainer.PutAsJsonAsync($"/trainer-athletes/{SeedData.TrainerId}/end", new { athleteId = SeedData.UserId }))
            .EnsureSuccessStatusCode();

        // Act
        var workout = await WorkoutVisibleTo(SeedCredentials.Trainer, SeedData.TrainedWorkoutId);

        // Assert
        workout.GetProperty("athlete").GetProperty("id").GetGuid().Should().Be(SeedData.UserId);
        workout.GetProperty("trainer").GetProperty("id").GetGuid().Should().Be(SeedData.TrainerId);
    }

    [Fact]
    public async Task Admin_ShouldStillSeeADeactivatedAthlete()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        (await admin.PutAsync($"/users/{SeedData.UserId}/deactivate", null)).EnsureSuccessStatusCode();

        // Act
        var workout = await WorkoutVisibleTo(SeedCredentials.Admin, SeedData.OwnedWorkoutId);

        // Assert
        workout.GetProperty("athlete").GetProperty("id").GetGuid().Should().Be(SeedData.UserId);
        workout.GetProperty("athlete").GetProperty("email").GetString().Should().Be(SeedData.UserEmail);
    }
}
