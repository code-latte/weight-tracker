using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Covers <c>DELETE /workouts/{workoutId}</c>: the edit permission, the draft-only lock that holds for an admin too,
/// and what disappears with the workout. A second trainer on a prescribed workout is covered by unit tests.
/// </summary>
public class WorkoutDeleteEndpointTests : WorkoutExerciseEndpointTest
{
    public WorkoutDeleteEndpointTests(ApiFactory factory) : base(factory)
    { }

    private static string WorkoutRoute(Guid workoutId) => $"/workouts/{workoutId}";

    private async Task<IReadOnlyList<Guid>> WorkoutIdsVisibleTo(string email)
    {
        var client = await AuthenticatedClientFor(email);

        var payload = await client.GetFromJsonAsync<JsonElement>("/workouts", JsonOptions);

        return payload.EnumerateArray()
            .Select(workout => workout.GetProperty("id").GetGuid())
            .ToList();
    }

    /// <summary>The status of a workout as an admin reads it, <c>null</c> when the workout no longer exists.</summary>
    private async Task<string?> StatusOf(Guid workoutId)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        var response = await admin.GetAsync(WorkoutRoute(workoutId));
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        var workout = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        return workout.GetProperty("status").GetString();
    }

    [Fact]
    public async Task Delete_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await Client.DeleteAsync(WorkoutRoute(SeedData.PrescribedDraftWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await StatusOf(SeedData.PrescribedDraftWorkoutId)).Should().Be("DRAFT");
    }

    [Fact]
    public async Task Delete_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.DeleteAsync(WorkoutRoute(SeedData.PrescribedDraftWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_ShouldRejectATokenWhoseUserNoLongerExists()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);
        await Factory.RemoveUser(SeedCredentials.Trainer);

        // Act
        var response = await client.DeleteAsync(WorkoutRoute(SeedData.PrescribedDraftWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_ShouldRemoveADraftWithItsEntriesAndSetsForThePrescribingTrainer()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);
        (await StoredSetRowIds(SeedData.PrescribedDraftPressBancaEntryId)).Should().NotBeEmpty();

        // Act
        var response = await client.DeleteAsync(WorkoutRoute(SeedData.PrescribedDraftWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await StatusOf(SeedData.PrescribedDraftWorkoutId)).Should().BeNull();
        (await StoredEntryCount(SeedData.PrescribedDraftPressBancaEntryId)).Should().Be(0);
        (await StoredSetRowIds(SeedData.PrescribedDraftPressBancaEntryId)).Should().BeEmpty();
        var visibleToTrainer = await WorkoutIdsVisibleTo(SeedCredentials.Trainer);
        visibleToTrainer.Should().NotContain(SeedData.PrescribedDraftWorkoutId);
        visibleToTrainer.Should().Contain(SeedData.TrainedWorkoutId);
    }

    [Fact]
    public async Task Delete_ShouldRemoveADraftWithItsSuperSeriesForTheOwnerOfASelfCreatedWorkout()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.DeleteAsync(WorkoutRoute(SeedData.SelfCreatedSuperSeriesWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await StatusOf(SeedData.SelfCreatedSuperSeriesWorkoutId)).Should().BeNull();
        (await StoredEntryCount(SeedData.SelfCreatedSuperSeriesPressBancaEntryId)).Should().Be(0);
        (await StoredEntryCount(SeedData.SelfCreatedSuperSeriesPlanchaEntryId)).Should().Be(0);
        var visibleToUser = await WorkoutIdsVisibleTo(SeedCredentials.User);
        visibleToUser.Should().NotContain(SeedData.SelfCreatedSuperSeriesWorkoutId);
        visibleToUser.Should().Contain(SeedData.OwnedWorkoutId);
    }

    [Fact]
    public async Task Delete_ShouldRemoveADraftForAnAdmin()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.DeleteAsync(WorkoutRoute(SeedData.OwnedWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await StatusOf(SeedData.OwnedWorkoutId)).Should().BeNull();
        (await StoredEntryCount(SeedData.OwnedPressBancaEntryId)).Should().Be(0);
    }

    [Fact]
    public async Task Delete_ShouldForbidTheOwnerOfAPrescribedDraftAndKeepIt()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.DeleteAsync(WorkoutRoute(SeedData.PrescribedDraftWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await StatusOf(SeedData.PrescribedDraftWorkoutId)).Should().Be("DRAFT");
        (await StoredEntryCount(SeedData.PrescribedDraftPressBancaEntryId)).Should().Be(1);
    }

    [Fact]
    public async Task Delete_ShouldForbidATrainerOnASelfCreatedWorkoutTheyDoNotOwnAndKeepIt()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.DeleteAsync(WorkoutRoute(SeedData.OwnedWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await StatusOf(SeedData.OwnedWorkoutId)).Should().Be("DRAFT");
    }

    [Fact]
    public async Task Delete_ShouldForbidAUserOnASelfCreatedWorkoutTheyDoNotOwnAndKeepIt()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.DeleteAsync(WorkoutRoute(SeedData.TrainerOwnWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await StatusOf(SeedData.TrainerOwnWorkoutId)).Should().Be("DRAFT");
    }

    [Fact]
    public async Task Delete_ShouldForbidACallerWhoMayNotDeleteAPublishedWorkoutWithoutRevealingTheLock()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.DeleteAsync(WorkoutRoute(SeedData.TrainedWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await StatusOf(SeedData.TrainedWorkoutId)).Should().Be("PUBLISHED");
    }

    [Theory]
    [InlineData("PUBLISHED")]
    [InlineData("STARTED")]
    [InlineData("COMPLETED")]
    public async Task Delete_ShouldRefuseAnAdminOnAWorkoutThatIsNoLongerADraftAndKeepIt(string status)
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.PrescribedDraftWorkoutId, status);
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.DeleteAsync(WorkoutRoute(SeedData.PrescribedDraftWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StatusOf(SeedData.PrescribedDraftWorkoutId)).Should().Be(status);
    }

    [Fact]
    public async Task Delete_ShouldRefuseAnAdminOnAReviewedWorkoutAndKeepIt()
    {
        // Arrange
        await GivenReviewed(SeedData.TrainedWorkoutId);
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.DeleteAsync(WorkoutRoute(SeedData.TrainedWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StatusOf(SeedData.TrainedWorkoutId)).Should().Be("REVIEWED");
    }

    [Theory]
    [InlineData("PUBLISHED")]
    [InlineData("STARTED")]
    [InlineData("COMPLETED")]
    public async Task Delete_ShouldRefuseThePrescribingTrainerOnAWorkoutThatIsNoLongerADraftAndKeepIt(string status)
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.PrescribedDraftWorkoutId, status);
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.DeleteAsync(WorkoutRoute(SeedData.PrescribedDraftWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StatusOf(SeedData.PrescribedDraftWorkoutId)).Should().Be(status);
    }

    [Fact]
    public async Task Delete_ShouldRefuseThePrescribingTrainerOnAReviewedWorkoutAndKeepIt()
    {
        // Arrange
        await GivenReviewed(SeedData.TrainedWorkoutId);
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.DeleteAsync(WorkoutRoute(SeedData.TrainedWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StatusOf(SeedData.TrainedWorkoutId)).Should().Be("REVIEWED");
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFoundForAnUnknownWorkout()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.DeleteAsync(WorkoutRoute(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
