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
/// Covers <c>POST /workouts/{workoutId}/superseries</c>, including every branch of the workout edit rule
/// reachable with the seeded users. A second trainer on a prescribed workout is covered by unit tests.
/// </summary>
public class SuperSeriesCreateEndpointTests : WorkoutEndpointTest
{
    private static readonly object PrescribedPairBody = new
    {
        exerciseIds = new[] { SeedData.PrescribedPairPressBancaEntryId, SeedData.PrescribedPairSentadillaEntryId },
    };

    private static readonly object SelfCreatedPairBody = new
    {
        exerciseIds = new[] { SeedData.SelfCreatedPairSentadillaEntryId, SeedData.SelfCreatedPairPlanchaEntryId },
    };

    public SuperSeriesCreateEndpointTests(ApiFactory factory) : base(factory)
    { }

    private static string Route(Guid workoutId) => $"/workouts/{workoutId}/superseries";

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
    public async Task Post_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await Client.PostAsJsonAsync(Route(SeedData.PrescribedPairWorkoutId), PrescribedPairBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.PostAsJsonAsync(Route(SeedData.PrescribedPairWorkoutId), PrescribedPairBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_ShouldRejectATokenWhoseUserNoLongerExists()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);
        await Factory.RemoveUser(SeedCredentials.Trainer);

        // Act
        var response = await client.PostAsJsonAsync(Route(SeedData.PrescribedPairWorkoutId), PrescribedPairBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_ShouldGroupTheEntriesForThePrescribingTrainer()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.PrescribedPairWorkoutId, "DRAFT");
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PostAsJsonAsync(Route(SeedData.PrescribedPairWorkoutId), PrescribedPairBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var superSeriesId = (await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("id").GetInt32();

        (await EntriesOf(SeedData.PrescribedPairWorkoutId)).Should().Equal(
            (SeedData.PrescribedPairPressBancaEntryId, superSeriesId),
            (SeedData.PrescribedPairSentadillaEntryId, superSeriesId));
    }

    [Fact]
    public async Task Post_ShouldGroupTheEntriesForTheOwnerOfASelfCreatedWorkout()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(Route(SeedData.SelfCreatedPairWorkoutId), SelfCreatedPairBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await EntriesOf(SeedData.SelfCreatedPairWorkoutId)).Should().OnlyContain(entry => entry.SuperSeriesId != null);
    }

    [Fact]
    public async Task Post_ShouldGroupTheEntriesForAnAdmin()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PostAsJsonAsync(Route(SeedData.PrescribedPairWorkoutId), PrescribedPairBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await EntriesOf(SeedData.PrescribedPairWorkoutId)).Should().OnlyContain(entry => entry.SuperSeriesId != null);
    }

    [Fact]
    public async Task Post_ShouldForbidTheOwnerOfAPrescribedWorkoutAndGroupNothing()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(Route(SeedData.PrescribedPairWorkoutId), PrescribedPairBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await EntriesOf(SeedData.PrescribedPairWorkoutId)).Should().OnlyContain(entry => entry.SuperSeriesId == null);
    }

    [Fact]
    public async Task Post_ShouldForbidATrainerOnASelfCreatedWorkoutTheyDoNotOwnAndGroupNothing()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PostAsJsonAsync(Route(SeedData.SelfCreatedPairWorkoutId), SelfCreatedPairBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await EntriesOf(SeedData.SelfCreatedPairWorkoutId)).Should().OnlyContain(entry => entry.SuperSeriesId == null);
    }

    [Fact]
    public async Task Post_ShouldReturnNotFoundForAnUnknownWorkout()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PostAsJsonAsync(Route(Id.New().Value), PrescribedPairBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_ShouldReturnNotFoundForAnEntryOfAnotherWorkoutAndGroupNothing()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.PrescribedPairWorkoutId, "DRAFT");
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var body = new
        {
            exerciseIds = new[] { SeedData.PrescribedPairPressBancaEntryId, SeedData.TrainedSentadillaEntryId },
        };

        // Act
        var response = await client.PostAsJsonAsync(Route(SeedData.PrescribedPairWorkoutId), body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await EntriesOf(SeedData.PrescribedPairWorkoutId)).Should().OnlyContain(entry => entry.SuperSeriesId == null);
    }

    [Fact]
    public async Task Post_ShouldReturnNotFoundForCatalogExerciseIds()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.PrescribedPairWorkoutId, "DRAFT");
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var body = new { exerciseIds = new[] { SeedData.PressBancaId, SeedData.SentadillaId } };

        // Act
        var response = await client.PostAsJsonAsync(Route(SeedData.PrescribedPairWorkoutId), body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_ShouldReturnConflictForAnEntryAlreadyGroupedAndKeepTheExistingSuperSeries()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var stored = await EntriesOf(SeedData.PrescribedSuperSeriesWorkoutId);
        var body = new
        {
            exerciseIds = new[] { SeedData.PrescribedSuperSeriesSentadillaEntryId, SeedData.PrescribedSuperSeriesPlanchaEntryId },
        };

        // Act
        var response = await client.PostAsJsonAsync(Route(SeedData.PrescribedSuperSeriesWorkoutId), body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await EntriesOf(SeedData.PrescribedSuperSeriesWorkoutId)).Should().Equal(stored);
    }

    [Fact]
    public async Task Post_ShouldRejectASingleEntry()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.PrescribedPairWorkoutId, "DRAFT");
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var body = new { exerciseIds = new[] { SeedData.PrescribedPairPressBancaEntryId } };

        // Act
        var response = await client.PostAsJsonAsync(Route(SeedData.PrescribedPairWorkoutId), body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldRejectTheSameEntryTwice()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.PrescribedPairWorkoutId, "DRAFT");
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var body = new
        {
            exerciseIds = new[] { SeedData.PrescribedPairPressBancaEntryId, SeedData.PrescribedPairPressBancaEntryId },
        };

        // Act
        var response = await client.PostAsJsonAsync(Route(SeedData.PrescribedPairWorkoutId), body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldRejectEntriesWithDifferentNumberOfSetsAndGroupNothing()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "DRAFT");
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var body = new { exerciseIds = new[] { SeedData.TrainedSentadillaEntryId, SeedData.TrainedPlanchaEntryId } };

        // Act
        var response = await client.PostAsJsonAsync(Route(SeedData.TrainedWorkoutId), body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await EntriesOf(SeedData.TrainedWorkoutId)).Should().OnlyContain(entry => entry.SuperSeriesId == null);
    }

    [Fact]
    public async Task Post_ShouldRejectABodyWithoutExerciseIds()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PostAsJsonAsync(Route(SeedData.PrescribedPairWorkoutId), new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldRejectAnIdThatIsNotAGuid()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var body = new { exerciseIds = new[] { "not-a-guid", SeedData.PrescribedPairSentadillaEntryId.ToString() } };

        // Act
        var response = await client.PostAsJsonAsync(Route(SeedData.PrescribedPairWorkoutId), body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldRejectThePrescribingTrainerOnAStartedWorkoutWithConflict()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.PrescribedPairWorkoutId, "STARTED");
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(Route(SeedData.PrescribedPairWorkoutId), PrescribedPairBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await EntriesOf(SeedData.PrescribedPairWorkoutId)).Select(entry => entry.SuperSeriesId).Should().OnlyContain(id => id == null);
    }

    [Fact]
    public async Task Post_ShouldRejectTheOwnerOfAFinishedWorkoutWithoutTrainerWithConflict()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.SelfCreatedPairWorkoutId, "COMPLETED");
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.PostAsJsonAsync(Route(SeedData.SelfCreatedPairWorkoutId), SelfCreatedPairBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await EntriesOf(SeedData.SelfCreatedPairWorkoutId)).Select(entry => entry.SuperSeriesId).Should().OnlyContain(id => id == null);
    }

    [Fact]
    public async Task Post_ShouldRejectACallerWhoMayNotEditAStartedWorkoutWithForbidden()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.PrescribedPairWorkoutId, "STARTED");
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.PostAsJsonAsync(Route(SeedData.PrescribedPairWorkoutId), PrescribedPairBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_ShouldAllowAnAdminOnAStartedWorkout()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.PrescribedPairWorkoutId, "STARTED");
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(Route(SeedData.PrescribedPairWorkoutId), PrescribedPairBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await EntriesOf(SeedData.PrescribedPairWorkoutId)).Select(entry => entry.SuperSeriesId).Should().OnlyContain(id => id == 1);
    }
}
