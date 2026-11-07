using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Covers <c>PUT /workouts/{workoutId}/status</c> for every role reachable with the seeded users.
/// The user is the athlete of the prescribed workouts and of <c>OwnedWorkoutId</c>, which has no trainer;
/// the trainer prescribed the former and owns <c>TrainerOwnWorkoutId</c>. Each is a stranger to the other's workout.
/// </summary>
public class WorkoutStatusChangeEndpointTests : WorkoutEndpointTest
{
    public WorkoutStatusChangeEndpointTests(ApiFactory factory) : base(factory)
    { }

    private static string StatusRoute(Guid workoutId) => $"/workouts/{workoutId}/status";

    private static Task<HttpResponseMessage> Put(HttpClient client, Guid workoutId, object body)
        => client.PutAsJsonAsync(StatusRoute(workoutId), body);

    private static Task<HttpResponseMessage> PutStatus(HttpClient client, Guid workoutId, string status)
        => Put(client, workoutId, new { status });

    /// <summary>The workout as an admin reads it, so a rejected call is checked independently of the caller.</summary>
    private async Task<string> StoredStatusOf(Guid workoutId)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        var workout = await admin.GetFromJsonAsync<JsonElement>($"/workouts/{workoutId}", JsonOptions);

        return workout.GetProperty("status").GetString()!;
    }

    [Fact]
    public async Task Put_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await PutStatus(Client, SeedData.TrainedWorkoutId, "DRAFT");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await PutStatus(Client, SeedData.TrainedWorkoutId, "DRAFT");

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
        var response = await PutStatus(client, SeedData.TrainedWorkoutId, "DRAFT");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldRejectARequestWithoutStatus()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await Put(client, SeedData.TrainedWorkoutId, new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await StoredStatusOf(SeedData.TrainedWorkoutId)).Should().Be("PUBLISHED");
    }

    [Fact]
    public async Task Put_ShouldRejectAnUnknownStatusName()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await PutStatus(client, SeedData.TrainedWorkoutId, "ARCHIVED");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await StoredStatusOf(SeedData.TrainedWorkoutId)).Should().Be("PUBLISHED");
    }

    [Fact]
    public async Task Put_ShouldRejectAStatusNumberThatIsNotDefined()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await Put(admin, SeedData.TrainedWorkoutId, new { status = 99 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await StoredStatusOf(SeedData.TrainedWorkoutId)).Should().Be("PUBLISHED");
    }

    [Fact]
    public async Task Put_ShouldReportAnUnknownWorkoutAsNotFound()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await PutStatus(client, Guid.NewGuid(), "PUBLISHED");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_ShouldNotMatchAMalformedWorkoutId()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PutAsJsonAsync("/workouts/not-a-guid/status", new { status = "PUBLISHED" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_ShouldReturnTheWorkoutWithTheNewStatusAndStoreIt()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await PutStatus(client, SeedData.PrescribedDraftWorkoutId, "PUBLISHED");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("id").GetGuid().Should().Be(SeedData.PrescribedDraftWorkoutId);
        body.GetProperty("status").GetString().Should().Be("PUBLISHED");
        body.GetProperty("exercises").GetArrayLength().Should().Be(1);
        (await StoredStatusOf(SeedData.PrescribedDraftWorkoutId)).Should().Be("PUBLISHED");
    }

    [Fact]
    public async Task Put_ShouldAcceptTheStatusAsANumber()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await Put(client, SeedData.PrescribedDraftWorkoutId, new { status = 2 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await StoredStatusOf(SeedData.PrescribedDraftWorkoutId)).Should().Be("PUBLISHED");
    }

    [Fact]
    public async Task Put_ShouldMakeAPublishedWorkoutVisibleToItsAthlete()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await PutStatus(trainer, SeedData.PrescribedDraftWorkoutId, "PUBLISHED");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await user.GetAsync($"/workouts/{SeedData.PrescribedDraftWorkoutId}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("DRAFT", "PUBLISHED")]
    [InlineData("PUBLISHED", "DRAFT")]
    public async Task PrescribingTrainer_ShouldPublishAndUnpublish(string from, string to)
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, from);
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await PutStatus(trainer, SeedData.TrainedWorkoutId, to);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await StoredStatusOf(SeedData.TrainedWorkoutId)).Should().Be(to);
    }

    [Theory]
    [InlineData("PUBLISHED", "STARTED")]
    [InlineData("STARTED", "COMPLETED")]
    public async Task PrescribingTrainer_ShouldNotBeAbleToMoveAWorkoutForward(string from, string to)
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, from);
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await PutStatus(trainer, SeedData.TrainedWorkoutId, to);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await StoredStatusOf(SeedData.TrainedWorkoutId)).Should().Be(from);
    }

    [Theory]
    [InlineData("PUBLISHED", "STARTED")]
    [InlineData("STARTED", "COMPLETED")]
    public async Task AthleteOfAPrescribedWorkout_ShouldMoveItForward(string from, string to)
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, from);
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await PutStatus(user, SeedData.TrainedWorkoutId, to);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await StoredStatusOf(SeedData.TrainedWorkoutId)).Should().Be(to);
    }

    [Fact]
    public async Task AthleteOfAPrescribedWorkout_ShouldNotBeAbleToUnpublishIt()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await PutStatus(user, SeedData.TrainedWorkoutId, "DRAFT");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await StoredStatusOf(SeedData.TrainedWorkoutId)).Should().Be("PUBLISHED");
    }

    [Theory]
    [InlineData("PUBLISHED")]
    [InlineData("STARTED")]
    public async Task AthleteOfAPrescribedDraft_ShouldNotBeAbleToChangeIt(string target)
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await PutStatus(user, SeedData.PrescribedDraftWorkoutId, target);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await StoredStatusOf(SeedData.PrescribedDraftWorkoutId)).Should().Be("DRAFT");
    }

    [Theory]
    [InlineData("DRAFT", "PUBLISHED")]
    [InlineData("PUBLISHED", "DRAFT")]
    [InlineData("PUBLISHED", "STARTED")]
    [InlineData("STARTED", "COMPLETED")]
    public async Task AthleteOfAWorkoutWithoutTrainer_ShouldMakeEveryBusinessTransition(string from, string to)
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.OwnedWorkoutId, from);
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await PutStatus(user, SeedData.OwnedWorkoutId, to);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await StoredStatusOf(SeedData.OwnedWorkoutId)).Should().Be(to);
    }

    [Theory]
    [InlineData("DRAFT", "STARTED")]
    [InlineData("DRAFT", "COMPLETED")]
    [InlineData("DRAFT", "DRAFT")]
    [InlineData("PUBLISHED", "PUBLISHED")]
    [InlineData("PUBLISHED", "COMPLETED")]
    [InlineData("STARTED", "DRAFT")]
    [InlineData("STARTED", "PUBLISHED")]
    [InlineData("COMPLETED", "DRAFT")]
    [InlineData("COMPLETED", "STARTED")]
    public async Task AthleteOfAWorkoutWithoutTrainer_ShouldBeRefusedAnyOtherMoveWithConflict(string from, string to)
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.OwnedWorkoutId, from);
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await PutStatus(user, SeedData.OwnedWorkoutId, to);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StoredStatusOf(SeedData.OwnedWorkoutId)).Should().Be(from);
    }

    [Fact]
    public async Task PrescribingTrainer_ShouldBeRefusedAMoveOutsideTheLifecycleWithConflict()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await PutStatus(trainer, SeedData.TrainedWorkoutId, "COMPLETED");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StoredStatusOf(SeedData.TrainedWorkoutId)).Should().Be("PUBLISHED");
    }

    [Fact]
    public async Task AthleteOfAPrescribedWorkout_ShouldBeRefusedAMoveBackwardsWithConflict()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "STARTED");
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await PutStatus(user, SeedData.TrainedWorkoutId, "PUBLISHED");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StoredStatusOf(SeedData.TrainedWorkoutId)).Should().Be("STARTED");
    }

    [Theory]
    [InlineData("COMPLETED", "DRAFT")]
    [InlineData("DRAFT", "COMPLETED")]
    [InlineData("STARTED", "PUBLISHED")]
    public async Task Admin_ShouldSkipTheLifecycle(string from, string to)
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, from);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await PutStatus(admin, SeedData.TrainedWorkoutId, to);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await StoredStatusOf(SeedData.TrainedWorkoutId)).Should().Be(to);
    }

    [Fact]
    public async Task Admin_ShouldSucceedWhenTheWorkoutAlreadyHasTheStatus()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await PutStatus(admin, SeedData.TrainedWorkoutId, "PUBLISHED");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await StoredStatusOf(SeedData.TrainedWorkoutId)).Should().Be("PUBLISHED");
    }

    [Fact]
    public async Task Trainer_ShouldNotChangeTheStatusOfAWorkoutThatIsNotTheirs()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await PutStatus(trainer, SeedData.OwnedWorkoutId, "PUBLISHED");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await StoredStatusOf(SeedData.OwnedWorkoutId)).Should().Be("DRAFT");
    }

    [Fact]
    public async Task User_ShouldNotChangeTheStatusOfAnotherUsersWorkout()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await PutStatus(user, SeedData.TrainerOwnWorkoutId, "PUBLISHED");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await StoredStatusOf(SeedData.TrainerOwnWorkoutId)).Should().Be("DRAFT");
    }

    [Fact]
    public async Task Trainer_ShouldMakeEveryBusinessTransitionOnTheirOwnWorkout()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.TrainerOwnWorkoutId, "PUBLISHED");
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await PutStatus(trainer, SeedData.TrainerOwnWorkoutId, "STARTED");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await StoredStatusOf(SeedData.TrainerOwnWorkoutId)).Should().Be("STARTED");
    }

    [Theory]
    [InlineData(SeedCredentials.Trainer)]
    [InlineData(SeedCredentials.User)]
    [InlineData(SeedCredentials.Admin)]
    public async Task Put_ShouldConflictWhenAnyoneSetsReviewedThroughTheStatusRoute(string callerEmail)
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "COMPLETED");
        var client = await AuthenticatedClientFor(callerEmail);

        // Act
        var response = await PutStatus(client, SeedData.TrainedWorkoutId, "REVIEWED");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StoredStatusOf(SeedData.TrainedWorkoutId)).Should().Be("COMPLETED");
    }

    [Theory]
    [InlineData("COMPLETED")]
    [InlineData("DRAFT")]
    public async Task Admin_ShouldConflictWhenMovingAReviewedWorkoutAway(string to)
    {
        // Arrange
        await GivenReviewed(SeedData.TrainedWorkoutId);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await PutStatus(admin, SeedData.TrainedWorkoutId, to);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StoredStatusOf(SeedData.TrainedWorkoutId)).Should().Be("REVIEWED");
    }
}
