using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Covers <c>DELETE /workouts/{workoutId}/superseries/{superSeriesId}</c>, including every branch of the
/// workout edit rule reachable with the seeded users. A second trainer on a prescribed workout is covered by unit tests.
/// </summary>
public class SuperSeriesBreakEndpointTests : WorkoutEndpointTest
{
    public SuperSeriesBreakEndpointTests(ApiFactory factory) : base(factory)
    { }

    private static string Route(Guid workoutId, int superSeriesId = SeedData.SeededSuperSeries)
        => $"/workouts/{workoutId}/superseries/{superSeriesId}";

    /// <summary>Superseries of every entry of the workout, in order, as an admin reads them.</summary>
    private async Task<IReadOnlyList<(Guid Id, int? SuperSeriesId)>> EntriesOf(Guid workoutId)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var workouts = await admin.GetFromJsonAsync<JsonElement>("/workouts", JsonOptions);

        return workouts.EnumerateArray()
            .Single(workout => workout.GetProperty("id").GetGuid() == workoutId)
            .GetProperty("exercises").EnumerateArray()
            .Select(entry =>
            {
                var superSeriesId = entry.GetProperty("superSeriesId");
                return (entry.GetProperty("id").GetGuid(),
                    superSeriesId.ValueKind == JsonValueKind.Null ? (int?)null : superSeriesId.GetInt32());
            })
            .ToList();
    }

    [Fact]
    public async Task Delete_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await Client.DeleteAsync(Route(SeedData.PrescribedSuperSeriesWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.DeleteAsync(Route(SeedData.PrescribedSuperSeriesWorkoutId));

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
        var response = await client.DeleteAsync(Route(SeedData.PrescribedSuperSeriesWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_ShouldUngroupTheEntriesAndKeepThemInOrderForThePrescribingTrainer()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.PrescribedSuperSeriesWorkoutId, "DRAFT");
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.DeleteAsync(Route(SeedData.PrescribedSuperSeriesWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await EntriesOf(SeedData.PrescribedSuperSeriesWorkoutId)).Should().Equal(
            (SeedData.PrescribedSuperSeriesPressBancaEntryId, (int?)null),
            (SeedData.PrescribedSuperSeriesSentadillaEntryId, (int?)null),
            (SeedData.PrescribedSuperSeriesPlanchaEntryId, (int?)null));
    }

    [Fact]
    public async Task Delete_ShouldUngroupTheEntriesForTheOwnerOfASelfCreatedWorkout()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.DeleteAsync(Route(SeedData.SelfCreatedSuperSeriesWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await EntriesOf(SeedData.SelfCreatedSuperSeriesWorkoutId)).Should().OnlyContain(entry => entry.SuperSeriesId == null);
    }

    [Fact]
    public async Task Delete_ShouldUngroupTheEntriesForAnAdmin()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.DeleteAsync(Route(SeedData.PrescribedSuperSeriesWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await EntriesOf(SeedData.PrescribedSuperSeriesWorkoutId)).Should().OnlyContain(entry => entry.SuperSeriesId == null);
    }

    [Fact]
    public async Task Delete_ShouldForbidTheOwnerOfAPrescribedWorkoutAndKeepTheSuperSeries()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);
        var stored = await EntriesOf(SeedData.PrescribedSuperSeriesWorkoutId);

        // Act
        var response = await client.DeleteAsync(Route(SeedData.PrescribedSuperSeriesWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await EntriesOf(SeedData.PrescribedSuperSeriesWorkoutId)).Should().Equal(stored);
    }

    [Fact]
    public async Task Delete_ShouldForbidATrainerOnASelfCreatedWorkoutTheyDoNotOwnAndKeepTheSuperSeries()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var stored = await EntriesOf(SeedData.SelfCreatedSuperSeriesWorkoutId);

        // Act
        var response = await client.DeleteAsync(Route(SeedData.SelfCreatedSuperSeriesWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await EntriesOf(SeedData.SelfCreatedSuperSeriesWorkoutId)).Should().Equal(stored);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFoundForAnUnknownWorkout()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.DeleteAsync(Route(Id.New().Value));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFoundForAnUnknownSuperSeries()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.PrescribedSuperSeriesWorkoutId, "DRAFT");
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.DeleteAsync(Route(SeedData.PrescribedSuperSeriesWorkoutId, SeedData.SeededSuperSeries + 1));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldRejectANonPositiveSuperSeriesId()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.DeleteAsync(Route(SeedData.PrescribedSuperSeriesWorkoutId, 0));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_ShouldRejectThePrescribingTrainerOnAStartedWorkoutWithConflict()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.PrescribedSuperSeriesWorkoutId, "STARTED");
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.DeleteAsync(Route(SeedData.PrescribedSuperSeriesWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await EntriesOf(SeedData.PrescribedSuperSeriesWorkoutId)).Count(entry => entry.SuperSeriesId == SeedData.SeededSuperSeries).Should().Be(2);
    }

    [Fact]
    public async Task Delete_ShouldRejectTheOwnerOfAFinishedWorkoutWithoutTrainerWithConflict()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.SelfCreatedSuperSeriesWorkoutId, "COMPLETED");
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.DeleteAsync(Route(SeedData.SelfCreatedSuperSeriesWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await EntriesOf(SeedData.SelfCreatedSuperSeriesWorkoutId)).Should().OnlyContain(entry => entry.SuperSeriesId == SeedData.SeededSuperSeries);
    }

    [Fact]
    public async Task Delete_ShouldRejectACallerWhoMayNotEditAStartedWorkoutWithForbidden()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.PrescribedSuperSeriesWorkoutId, "STARTED");
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.DeleteAsync(Route(SeedData.PrescribedSuperSeriesWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_ShouldAllowAnAdminOnAStartedWorkout()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.PrescribedSuperSeriesWorkoutId, "STARTED");
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.DeleteAsync(Route(SeedData.PrescribedSuperSeriesWorkoutId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await EntriesOf(SeedData.PrescribedSuperSeriesWorkoutId)).Should().OnlyContain(entry => entry.SuperSeriesId == null);
    }
}
