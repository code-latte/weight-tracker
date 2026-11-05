using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Covers <c>PUT /workouts/{id}</c> for every role reachable with the seeded users. The user owns
/// <c>OwnedWorkoutId</c>, which has no trainer, and is the athlete of <c>TrainedWorkoutId</c>, prescribed by
/// the trainer. Every rejection is checked against the workout as an admin reads it afterwards.
/// </summary>
public class WorkoutUpdateEndpointTests : WorkoutEndpointTest
{
    private const string NewName = "Push day";

    private const string NewDescription = "Heavy presses";

    private const string NewDate = "2026-11-05";

    public WorkoutUpdateEndpointTests(ApiFactory factory) : base(factory)
    { }

    private static Task<HttpResponseMessage> Put(HttpClient client, Guid workoutId, object body)
        => client.PutAsJsonAsync($"/workouts/{workoutId}", body);

    /// <summary>New details for the owned workout, keeping its owner and its lack of trainer.</summary>
    private static object OwnedWorkoutChange()
        => new { name = NewName, description = NewDescription, workoutDate = NewDate, userId = SeedData.UserId, trainerId = (Guid?)null };

    /// <summary>New details for the trained workout, keeping its athlete and trainer.</summary>
    private static object TrainedWorkoutChange()
        => new { name = NewName, description = NewDescription, workoutDate = NewDate, userId = SeedData.UserId, trainerId = SeedData.TrainerId };

    /// <summary>The workout as an admin reads it, so a call is checked independently of the caller.</summary>
    private async Task<JsonElement> StoredWorkout(Guid workoutId)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        return await admin.GetFromJsonAsync<JsonElement>($"/workouts/{workoutId}", JsonOptions);
    }

    private async Task<IReadOnlySet<Guid>> WorkoutIdsSeenBy(string email)
    {
        var client = await AuthenticatedClientFor(email);
        var workouts = await client.GetFromJsonAsync<JsonElement>("/workouts", JsonOptions);

        return workouts.EnumerateArray().Select(workout => workout.GetProperty("id").GetGuid()).ToHashSet();
    }

    private async Task DeactivateAsAdmin(Guid userId)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var response = await admin.PutAsync($"/users/{userId}/deactivate", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task ShouldBeUnchangedOwnedWorkout()
    {
        var stored = await StoredWorkout(SeedData.OwnedWorkoutId);

        stored.GetProperty("name").GetString().Should().Be(SeedData.OwnedWorkoutName);
        stored.GetProperty("description").GetString().Should().Be(SeedData.OwnedWorkoutDescription);
        stored.GetProperty("workoutDate").GetString().Should().Be("2026-10-01");
        stored.GetProperty("userId").GetGuid().Should().Be(SeedData.UserId);
        stored.GetProperty("trainerId").ValueKind.Should().Be(JsonValueKind.Null);
    }

    private async Task ShouldBeUnchangedTrainedWorkout()
    {
        var stored = await StoredWorkout(SeedData.TrainedWorkoutId);

        stored.GetProperty("name").GetString().Should().Be(SeedData.TrainedWorkoutName);
        stored.GetProperty("description").ValueKind.Should().Be(JsonValueKind.Null);
        stored.GetProperty("workoutDate").GetString().Should().Be("2026-10-03");
        stored.GetProperty("userId").GetGuid().Should().Be(SeedData.UserId);
        stored.GetProperty("trainerId").GetGuid().Should().Be(SeedData.TrainerId);
    }

    // ---- Authentication

    [Fact]
    public async Task Put_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await Put(Client, SeedData.OwnedWorkoutId, OwnedWorkoutChange());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await ShouldBeUnchangedOwnedWorkout();
    }

    [Fact]
    public async Task Put_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Put(Client, SeedData.OwnedWorkoutId, OwnedWorkoutChange());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await ShouldBeUnchangedOwnedWorkout();
    }

    // ---- Not found

    [Fact]
    public async Task Put_ShouldReportAnUnknownWorkoutAsNotFound()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await Put(admin, Guid.NewGuid(), OwnedWorkoutChange());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---- Structural and value errors

    public static TheoryData<object> InvalidBodies => new()
    {
        new { description = NewDescription, workoutDate = NewDate, userId = SeedData.UserId },
        new { name = NewName, description = NewDescription, userId = SeedData.UserId },
        new { name = NewName, description = NewDescription, workoutDate = NewDate },
        new { name = "   ", workoutDate = NewDate, userId = SeedData.UserId },
        new { name = new string('a', 257), workoutDate = NewDate, userId = SeedData.UserId },
        new { name = NewName, description = new string('a', 1025), workoutDate = NewDate, userId = SeedData.UserId },
    };

    [Theory]
    [MemberData(nameof(InvalidBodies))]
    public async Task Put_ShouldRejectAnInvalidBodyAndChangeNothing(object body)
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await Put(user, SeedData.OwnedWorkoutId, body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await ShouldBeUnchangedOwnedWorkout();
    }

    // ---- Details

    [Fact]
    public async Task OwnerOfASelfPlannedWorkout_ShouldUpdateItsDetails()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await Put(user, SeedData.OwnedWorkoutId, OwnedWorkoutChange());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("name").GetString().Should().Be(NewName);
        var stored = await StoredWorkout(SeedData.OwnedWorkoutId);
        stored.GetProperty("name").GetString().Should().Be(NewName);
        stored.GetProperty("description").GetString().Should().Be(NewDescription);
        stored.GetProperty("workoutDate").GetString().Should().Be(NewDate);
        stored.GetProperty("status").GetString().Should().Be("DRAFT");
        stored.GetProperty("exercises").EnumerateArray().Single()
            .GetProperty("exerciseId").GetGuid().Should().Be(SeedData.PressBancaId);
    }

    [Fact]
    public async Task PrescribingTrainer_ShouldUpdateTheDetailsAndKeepTheStatusAndExercises()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await Put(trainer, SeedData.TrainedWorkoutId, TrainedWorkoutChange());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await StoredWorkout(SeedData.TrainedWorkoutId);
        stored.GetProperty("name").GetString().Should().Be(NewName);
        stored.GetProperty("description").GetString().Should().Be(NewDescription);
        stored.GetProperty("workoutDate").GetString().Should().Be(NewDate);
        stored.GetProperty("status").GetString().Should().Be("PUBLISHED");
        stored.GetProperty("exercises").EnumerateArray().Select(entry => entry.GetProperty("exerciseId").GetGuid())
            .Should().Equal(SeedData.SentadillaId, SeedData.PlanchaId);
    }

    [Fact]
    public async Task PrescribingTrainer_ShouldKeepTheMesocyclePlacementWithoutExposingIt()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await Put(trainer, SeedData.TrainedWorkoutId, TrainedWorkoutChange());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.TryGetProperty("mesocycleId", out _).Should().BeFalse();
        body.TryGetProperty("mesocycleWeekNumber", out _).Should().BeFalse();
        var stored = await Factory.WithScope(services => services.GetRequiredService<IWorkoutRepository>()
            .GetById(Id.From(SeedData.TrainedWorkoutId), CancellationToken.None));
        stored!.MesocycleId.Should().Be(Id.From(SeedData.MesocycleId));
        stored.MesocycleWeekNumber.Should().Be(WeekNumber.From(1));
    }

    [Fact]
    public async Task Admin_ShouldUpdateTheDetailsOfAWorkoutTheyNeitherOwnNorTrain()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await Put(admin, SeedData.TrainedWorkoutId, TrainedWorkoutChange());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await StoredWorkout(SeedData.TrainedWorkoutId);
        stored.GetProperty("name").GetString().Should().Be(NewName);
        stored.GetProperty("userId").GetGuid().Should().Be(SeedData.UserId);
        stored.GetProperty("trainerId").GetGuid().Should().Be(SeedData.TrainerId);
    }

    [Fact]
    public async Task Put_ShouldClearTheDescription()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await Put(user, SeedData.OwnedWorkoutId,
            new { name = NewName, description = (string?)null, workoutDate = NewDate, userId = SeedData.UserId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await StoredWorkout(SeedData.OwnedWorkoutId)).GetProperty("description").ValueKind.Should().Be(JsonValueKind.Null);
    }

    // ---- Edit rule

    [Fact]
    public async Task AthleteOfAPrescribedWorkout_ShouldNotBeAbleToUpdateIt()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await Put(user, SeedData.TrainedWorkoutId, TrainedWorkoutChange());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await ShouldBeUnchangedTrainedWorkout();
    }

    [Fact]
    public async Task Stranger_ShouldNotBeAbleToUpdateAWorkout()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await Put(trainer, SeedData.OwnedWorkoutId, OwnedWorkoutChange());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await ShouldBeUnchangedOwnedWorkout();
    }

    // ---- Lock

    [Fact]
    public async Task PrescribingTrainer_ShouldNotBeAbleToUpdateAStartedWorkout()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "STARTED");
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await Put(trainer, SeedData.TrainedWorkoutId, TrainedWorkoutChange());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await ShouldBeUnchangedTrainedWorkout();
    }

    [Fact]
    public async Task Admin_ShouldUpdateAFinishedWorkout()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "FINISHED");
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await Put(admin, SeedData.TrainedWorkoutId, TrainedWorkoutChange());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await StoredWorkout(SeedData.TrainedWorkoutId);
        stored.GetProperty("name").GetString().Should().Be(NewName);
        stored.GetProperty("status").GetString().Should().Be("FINISHED");
    }

    // ---- Reassignment by non-admins

    [Fact]
    public async Task PrescribingTrainer_ShouldNotBeAbleToChangeTheAthlete()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await Put(trainer, SeedData.TrainedWorkoutId,
            new { name = NewName, workoutDate = NewDate, userId = SeedData.AdminId, trainerId = SeedData.TrainerId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await ShouldBeUnchangedTrainedWorkout();
    }

    [Fact]
    public async Task PrescribingTrainer_ShouldNotBeAbleToDropThemselvesAsTrainer()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await Put(trainer, SeedData.TrainedWorkoutId,
            new { name = NewName, workoutDate = NewDate, userId = SeedData.UserId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await ShouldBeUnchangedTrainedWorkout();
    }

    [Fact]
    public async Task OwnerOfASelfPlannedWorkout_ShouldNotBeAbleToAddATrainer()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await Put(user, SeedData.OwnedWorkoutId,
            new { name = NewName, workoutDate = NewDate, userId = SeedData.UserId, trainerId = SeedData.TrainerId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await ShouldBeUnchangedOwnedWorkout();
    }

    // ---- Reassignment by admins

    [Fact]
    public async Task Admin_ShouldMoveAWorkoutToAnotherOwner()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await Put(admin, SeedData.OwnedWorkoutId,
            new { name = NewName, workoutDate = NewDate, userId = SeedData.TrainerId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await StoredWorkout(SeedData.OwnedWorkoutId)).GetProperty("userId").GetGuid().Should().Be(SeedData.TrainerId);
        var previousOwner = await AuthenticatedClientFor(SeedCredentials.User);
        (await previousOwner.GetAsync($"/workouts/{SeedData.OwnedWorkoutId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await WorkoutIdsSeenBy(SeedCredentials.User)).Should().NotContain(SeedData.OwnedWorkoutId);
        (await WorkoutIdsSeenBy(SeedCredentials.Trainer)).Should().Contain(SeedData.OwnedWorkoutId);
    }

    [Fact]
    public async Task Admin_ShouldAssignATrainer()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await Put(admin, SeedData.OwnedWorkoutId,
            new { name = NewName, workoutDate = NewDate, userId = SeedData.UserId, trainerId = SeedData.TrainerId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await StoredWorkout(SeedData.OwnedWorkoutId)).GetProperty("trainerId").GetGuid().Should().Be(SeedData.TrainerId);
    }

    [Fact]
    public async Task Admin_ShouldRemoveTheTrainer()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await Put(admin, SeedData.TrainedWorkoutId,
            new { name = NewName, workoutDate = NewDate, userId = SeedData.UserId, trainerId = (Guid?)null });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await StoredWorkout(SeedData.TrainedWorkoutId)).GetProperty("trainerId").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Admin_ShouldNotMoveAWorkoutToAnUnknownOwner()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await Put(admin, SeedData.OwnedWorkoutId,
            new { name = NewName, workoutDate = NewDate, userId = Guid.NewGuid() });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await ShouldBeUnchangedOwnedWorkout();
    }

    public static TheoryData<object> InvalidAssignments => new()
    {
        // The admin makes themselves the owner
        new { name = NewName, workoutDate = NewDate, userId = SeedData.AdminId },
        // A regular user cannot train
        new { name = NewName, workoutDate = NewDate, userId = SeedData.UserId, trainerId = SeedData.UserId },
    };

    [Theory]
    [MemberData(nameof(InvalidAssignments))]
    public async Task Admin_ShouldNotMakeAnInvalidAssignment(object body)
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await Put(admin, SeedData.OwnedWorkoutId, body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await ShouldBeUnchangedOwnedWorkout();
    }

    [Fact]
    public async Task Admin_ShouldNotMakeTheTrainerTheOwner()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await Put(admin, SeedData.TrainedWorkoutId,
            new { name = NewName, workoutDate = NewDate, userId = SeedData.TrainerId, trainerId = SeedData.TrainerId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await ShouldBeUnchangedTrainedWorkout();
    }

    [Fact]
    public async Task Admin_ShouldNotMoveAWorkoutToADeactivatedOwner()
    {
        // Arrange
        await DeactivateAsAdmin(SeedData.TrainerId);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await Put(admin, SeedData.OwnedWorkoutId,
            new { name = NewName, workoutDate = NewDate, userId = SeedData.TrainerId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await ShouldBeUnchangedOwnedWorkout();
    }

    [Fact]
    public async Task Admin_ShouldNotAssignADeactivatedTrainer()
    {
        // Arrange
        await DeactivateAsAdmin(SeedData.TrainerId);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await Put(admin, SeedData.OwnedWorkoutId,
            new { name = NewName, workoutDate = NewDate, userId = SeedData.UserId, trainerId = SeedData.TrainerId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await ShouldBeUnchangedOwnedWorkout();
    }

    [Fact]
    public async Task Admin_ShouldStillUpdateAWorkoutWhoseUnchangedOwnerIsDeactivated()
    {
        // Arrange
        await DeactivateAsAdmin(SeedData.UserId);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await Put(admin, SeedData.TrainedWorkoutId, TrainedWorkoutChange());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await StoredWorkout(SeedData.TrainedWorkoutId)).GetProperty("name").GetString().Should().Be(NewName);
    }
}
