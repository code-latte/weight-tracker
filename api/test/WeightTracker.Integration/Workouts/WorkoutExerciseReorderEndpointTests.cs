using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Covers <c>PUT /workouts/{workoutId}/exercises/order</c>, including every branch of the workout edit rule
/// reachable with the seeded users. A second trainer on a prescribed workout is covered by unit tests.
/// </summary>
public class WorkoutExerciseReorderEndpointTests : WorkoutExerciseEndpointTest
{
    public WorkoutExerciseReorderEndpointTests(ApiFactory factory) : base(factory)
    { }

    private static string OrderRoute(Guid workoutId) => $"/workouts/{workoutId}/exercises/order";

    private static object Order(params Guid[] exerciseIds) => new { exerciseIds };

    /// <summary>The seeded trained workout holds Sentadilla then Plancha; this swaps them.</summary>
    private static readonly object Swapped = Order(SeedData.PlanchaId, SeedData.SentadillaId);

    [Fact]
    public async Task Put_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await Client.PutAsJsonAsync(OrderRoute(SeedData.TrainedWorkoutId), Swapped);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.PutAsJsonAsync(OrderRoute(SeedData.TrainedWorkoutId), Swapped);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldRejectATokenWhoseUserNoLongerExists()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);
        await Factory.RemoveUser(SeedCredentials.Trainer);

        // Act
        var response = await client.PutAsJsonAsync(OrderRoute(SeedData.TrainedWorkoutId), Swapped);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldStoreTheNewOrderForThePrescribingTrainer()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "DRAFT");
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var before = (await EntriesOf(SeedData.TrainedWorkoutId)).ToDictionary(entry => entry.GetProperty("id").GetGuid());

        // Act
        var response = await client.PutAsJsonAsync(OrderRoute(SeedData.TrainedWorkoutId), Swapped);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        ExerciseIds(body.GetProperty("exercises").EnumerateArray()).Should().Equal(SeedData.PlanchaId, SeedData.SentadillaId);
        var entries = await EntriesOf(SeedData.TrainedWorkoutId);
        ExerciseIds(entries).Should().Equal(SeedData.PlanchaId, SeedData.SentadillaId);
        entries[0].GetProperty("id").GetGuid().Should().Be(SeedData.TrainedPlanchaEntryId);
        entries[1].GetProperty("id").GetGuid().Should().Be(SeedData.TrainedSentadillaEntryId);
        entries.Should().AllSatisfy(entry =>
        {
            var previous = before[entry.GetProperty("id").GetGuid()];
            entry.GetProperty("effort").GetString().Should().Be(previous.GetProperty("effort").GetString());
            SetsOf(entry).Should().Equal(SetsOf(previous));
        });
    }

    [Fact]
    public async Task Put_ShouldStoreTheNewOrderForTheOwnerOfASelfCreatedWorkout()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);
        var reversed = ExerciseIds(await EntriesOf(SeedData.SelfCreatedSuperSeriesWorkoutId)).Reverse().ToArray();

        // Act
        var response = await client.PutAsJsonAsync(OrderRoute(SeedData.SelfCreatedSuperSeriesWorkoutId), Order(reversed));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entries = await EntriesOf(SeedData.SelfCreatedSuperSeriesWorkoutId);
        ExerciseIds(entries).Should().Equal(reversed);
        entries.Select(entry => entry.GetProperty("superSeriesId").GetInt32()).Distinct().Should().ContainSingle();
    }

    [Fact]
    public async Task Put_ShouldStoreTheNewOrderForAnAdmin()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PutAsJsonAsync(OrderRoute(SeedData.TrainedWorkoutId), Swapped);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ExerciseIds(await EntriesOf(SeedData.TrainedWorkoutId)).Should().Equal(SeedData.PlanchaId, SeedData.SentadillaId);
    }

    [Fact]
    public async Task Put_ShouldForbidTheOwnerOfAPrescribedWorkoutAndKeepTheOrder()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(OrderRoute(SeedData.TrainedWorkoutId), Swapped);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ExerciseIds(await EntriesOf(SeedData.TrainedWorkoutId)).Should().Equal(SeedData.SentadillaId, SeedData.PlanchaId);
    }

    [Fact]
    public async Task Put_ShouldForbidATrainerOnASelfCreatedWorkoutTheyDoNotOwn()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PutAsJsonAsync(OrderRoute(SeedData.OwnedWorkoutId), Order(SeedData.PressBancaId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Put_ShouldReturnNotFoundForAnUnknownWorkout()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PutAsJsonAsync(OrderRoute(Guid.NewGuid()), Swapped);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_ShouldRejectAMissingListAsBadRequest()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PutAsJsonAsync(OrderRoute(SeedData.TrainedWorkoutId), new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ExerciseIds(await EntriesOf(SeedData.TrainedWorkoutId)).Should().Equal(SeedData.SentadillaId, SeedData.PlanchaId);
    }

    [Fact]
    public async Task Put_ShouldRejectAListWithAMissingExerciseWithConflictAndKeepTheOrder()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PutAsJsonAsync(OrderRoute(SeedData.TrainedWorkoutId), Order(SeedData.PlanchaId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ExerciseIds(await EntriesOf(SeedData.TrainedWorkoutId)).Should().Equal(SeedData.SentadillaId, SeedData.PlanchaId);
    }

    [Fact]
    public async Task Put_ShouldRejectAListWithAnExerciseNotInTheWorkoutWithConflictAndKeepTheOrder()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PutAsJsonAsync(
            OrderRoute(SeedData.TrainedWorkoutId), Order(SeedData.PlanchaId, SeedData.CurlBicepsId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ExerciseIds(await EntriesOf(SeedData.TrainedWorkoutId)).Should().Equal(SeedData.SentadillaId, SeedData.PlanchaId);
    }

    [Fact]
    public async Task Put_ShouldRejectAListWithARepeatedExerciseWithConflictAndKeepTheOrder()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PutAsJsonAsync(
            OrderRoute(SeedData.TrainedWorkoutId), Order(SeedData.PlanchaId, SeedData.PlanchaId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ExerciseIds(await EntriesOf(SeedData.TrainedWorkoutId)).Should().Equal(SeedData.SentadillaId, SeedData.PlanchaId);
    }

    [Fact]
    public async Task Put_ShouldRejectThePrescribingTrainerOnAStartedWorkoutWithConflict()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync(OrderRoute(SeedData.PrescribedStartedWorkoutId), Order(SeedData.PressBancaId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Put_ShouldAllowAnAdminOnAStartedWorkout()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PutAsJsonAsync(OrderRoute(SeedData.PrescribedStartedWorkoutId), Order(SeedData.PressBancaId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
