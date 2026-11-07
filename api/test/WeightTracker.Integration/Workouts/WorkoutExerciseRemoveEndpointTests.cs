using System.Net;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Covers <c>DELETE /workouts/{workoutId}/exercises/{exerciseId}</c>, including every branch of the
/// workout edit rule reachable with the seeded users. A second trainer on a prescribed workout is covered by unit tests.
/// </summary>
public class WorkoutExerciseRemoveEndpointTests : WorkoutExerciseEndpointTest
{
    public WorkoutExerciseRemoveEndpointTests(ApiFactory factory) : base(factory)
    { }

    [Fact]
    public async Task Delete_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await Client.DeleteAsync(ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.PlanchaId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.DeleteAsync(ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.PlanchaId));

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
        var response = await client.DeleteAsync(ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.PlanchaId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_ShouldRemoveTheEntryAndItsStoredSetsForThePrescribingTrainer()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "DRAFT");
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);
        (await StoredSetRowIds(SeedData.TrainedPlanchaEntryId)).Should().NotBeEmpty();

        // Act
        var response = await client.DeleteAsync(ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.PlanchaId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        ExerciseIds(await EntriesOf(SeedData.TrainedWorkoutId)).Should().Equal(SeedData.SentadillaId);
        (await StoredEntryCount(SeedData.TrainedPlanchaEntryId)).Should().Be(0);
        (await StoredSetRowIds(SeedData.TrainedPlanchaEntryId)).Should().BeEmpty();
        (await StoredSetRowIds(SeedData.TrainedSentadillaEntryId)).Should().NotBeEmpty();
    }

    [Fact]
    public async Task Delete_ShouldRemoveTheEntryForTheOwnerOfASelfCreatedWorkout()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.DeleteAsync(ExerciseRoute(SeedData.OwnedWorkoutId, SeedData.PressBancaId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await EntriesOf(SeedData.OwnedWorkoutId)).Should().BeEmpty();
        (await StoredSetRowIds(SeedData.OwnedPressBancaEntryId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_ShouldRemoveTheEntryForAnAdmin()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.DeleteAsync(ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.PlanchaId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_ShouldForbidTheOwnerOfAPrescribedWorkoutAndKeepTheEntryAndItsSets()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.DeleteAsync(ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.PlanchaId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ExerciseIds(await EntriesOf(SeedData.TrainedWorkoutId)).Should().Equal(SeedData.SentadillaId, SeedData.PlanchaId);
        (await StoredSetRowIds(SeedData.TrainedPlanchaEntryId)).Should().NotBeEmpty();
    }

    [Fact]
    public async Task Delete_ShouldForbidATrainerOnASelfCreatedWorkoutTheyDoNotOwnAndKeepTheEntry()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.DeleteAsync(ExerciseRoute(SeedData.OwnedWorkoutId, SeedData.PressBancaId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ExerciseIds(await EntriesOf(SeedData.OwnedWorkoutId)).Should().Equal(SeedData.PressBancaId);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFoundForAnUnknownWorkout()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.DeleteAsync(ExerciseRoute(Guid.NewGuid(), SeedData.PlanchaId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFoundForAnExerciseNotInTheWorkout()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "DRAFT");
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.DeleteAsync(ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.CurlBicepsId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ExerciseIds(await EntriesOf(SeedData.TrainedWorkoutId)).Should().Equal(SeedData.SentadillaId, SeedData.PlanchaId);
    }

    [Fact]
    public async Task Delete_ShouldNotAcceptTheWorkoutEntryIdInPlaceOfTheCatalogExercise()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "DRAFT");
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.DeleteAsync(ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.TrainedPlanchaEntryId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ExerciseIds(await EntriesOf(SeedData.TrainedWorkoutId)).Should().Equal(SeedData.SentadillaId, SeedData.PlanchaId);
    }

    [Fact]
    public async Task Delete_ShouldUngroupTheLastRemainingMemberOfASuperSeries()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.DeleteAsync(ExerciseRoute(SeedData.SelfCreatedSuperSeriesWorkoutId, SeedData.PlanchaId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var entries = await EntriesOf(SeedData.SelfCreatedSuperSeriesWorkoutId);
        ExerciseIds(entries).Should().Equal(SeedData.PressBancaId);
        entries[0].GetProperty("superSeriesId").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
    }

    [Fact]
    public async Task Delete_ShouldRejectThePrescribingTrainerOnAStartedWorkoutWithConflict()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.DeleteAsync(ExerciseRoute(SeedData.PrescribedStartedWorkoutId, SeedData.PressBancaId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ExerciseIds(await EntriesOf(SeedData.PrescribedStartedWorkoutId)).Should().Equal(SeedData.PressBancaId);
    }

    [Fact]
    public async Task Delete_ShouldRejectTheOwnerOfAFinishedWorkoutWithoutTrainerWithConflict()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.OwnedWorkoutId, "COMPLETED");
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.DeleteAsync(ExerciseRoute(SeedData.OwnedWorkoutId, SeedData.PressBancaId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ExerciseIds(await EntriesOf(SeedData.OwnedWorkoutId)).Should().Equal(SeedData.PressBancaId);
    }

    [Fact]
    public async Task Delete_ShouldRejectACallerWhoMayNotEditAStartedWorkoutWithForbidden()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.DeleteAsync(ExerciseRoute(SeedData.PrescribedStartedWorkoutId, SeedData.PressBancaId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_ShouldAllowAnAdminOnAStartedWorkout()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.DeleteAsync(ExerciseRoute(SeedData.PrescribedStartedWorkoutId, SeedData.PressBancaId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await EntriesOf(SeedData.PrescribedStartedWorkoutId)).Should().BeEmpty();
    }
}
