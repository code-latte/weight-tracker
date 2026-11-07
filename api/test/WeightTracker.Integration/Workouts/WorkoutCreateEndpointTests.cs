using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Covers <c>POST /workouts</c> for every role, using only the seeded users. Each creation is checked
/// through <c>GET /workouts</c>, and each rejection must leave the set of workouts unchanged.
/// </summary>
public class WorkoutCreateEndpointTests : IntegrationTest
{
    private const string Date = "2026-10-20";

    public WorkoutCreateEndpointTests(ApiFactory factory) : base(factory)
    { }

    private async Task<IReadOnlySet<Guid>> WorkoutIdsSeenBy(string email)
    {
        var client = await AuthenticatedClientFor(email);
        var workouts = await client.GetFromJsonAsync<JsonElement>("/workouts", JsonOptions);

        return workouts.EnumerateArray().Select(workout => workout.GetProperty("id").GetGuid()).ToHashSet();
    }

    private static async Task<JsonElement> BodyOf(HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

    private async Task SetActivationAsAdmin(Guid userId, string action)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var response = await admin.PutAsync($"/users/{userId}/{action}", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public static TheoryData<string, object, HttpStatusCode> Rejections => new()
    {
        // USER
        { SeedCredentials.User, new { name = "Leg day", userId = SeedData.TrainerId, workoutDate = Date }, HttpStatusCode.BadRequest },
        { SeedCredentials.User, new { name = "Leg day", trainerId = SeedData.TrainerId, workoutDate = Date }, HttpStatusCode.BadRequest },
        // TRAINER
        { SeedCredentials.Trainer, new { name = "Leg day", workoutDate = Date }, HttpStatusCode.BadRequest },
        { SeedCredentials.Trainer, new { name = "Leg day", userId = SeedData.TrainerId, workoutDate = Date }, HttpStatusCode.BadRequest },
        { SeedCredentials.Trainer, new { name = "Leg day", userId = SeedData.UserId, trainerId = SeedData.TrainerId, workoutDate = Date }, HttpStatusCode.BadRequest },
        { SeedCredentials.Trainer, new { name = "Leg day", userId = SeedData.AdminId, workoutDate = Date }, HttpStatusCode.Forbidden },
        { SeedCredentials.Trainer, new { name = "Leg day", userId = Guid.NewGuid(), workoutDate = Date }, HttpStatusCode.NotFound },
        // ADMIN
        { SeedCredentials.Admin, new { name = "Leg day", workoutDate = Date }, HttpStatusCode.BadRequest },
        { SeedCredentials.Admin, new { name = "Leg day", userId = SeedData.AdminId, workoutDate = Date }, HttpStatusCode.BadRequest },
        { SeedCredentials.Admin, new { name = "Leg day", userId = SeedData.UserId, trainerId = SeedData.UserId, workoutDate = Date }, HttpStatusCode.BadRequest },
        { SeedCredentials.Admin, new { name = "Leg day", userId = SeedData.TrainerId, trainerId = SeedData.TrainerId, workoutDate = Date }, HttpStatusCode.BadRequest },
        { SeedCredentials.Admin, new { name = "Leg day", userId = Guid.NewGuid(), workoutDate = Date }, HttpStatusCode.NotFound },
        // STRUCTURAL
        { SeedCredentials.User, new { }, HttpStatusCode.BadRequest },
        { SeedCredentials.User, new { name = "Leg day", workoutDate = "not-a-date" }, HttpStatusCode.BadRequest },
        { SeedCredentials.User, new { name = "Leg day", workoutDate = "2026-10-20T10:30:00" }, HttpStatusCode.BadRequest },
        { SeedCredentials.User, new { name = "Leg day" }, HttpStatusCode.BadRequest },
        { SeedCredentials.User, new { workoutDate = Date }, HttpStatusCode.BadRequest },
        { SeedCredentials.User, new { name = "   ", workoutDate = Date }, HttpStatusCode.BadRequest },
        { SeedCredentials.User, new { name = new string('a', 257), workoutDate = Date }, HttpStatusCode.BadRequest },
        { SeedCredentials.User, new { name = "Leg day", description = new string('a', 1025), workoutDate = Date }, HttpStatusCode.BadRequest },
    };

    [Fact]
    public async Task Post_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await Client.PostAsJsonAsync("/workouts", new { name = "Leg day", workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.PostAsJsonAsync("/workouts", new { name = "Leg day", workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_ShouldRejectATokenWhoseUserNoLongerExistsAndCreateNothing()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);
        await Factory.RemoveUser(SeedCredentials.User);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var before = (await admin.GetFromJsonAsync<JsonElement>("/workouts", JsonOptions)).GetArrayLength();

        // Act
        var response = await client.PostAsJsonAsync("/workouts", new { name = "Leg day", workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var after = (await admin.GetFromJsonAsync<JsonElement>("/workouts", JsonOptions)).GetArrayLength();
        after.Should().Be(before);
    }

    [Fact]
    public async Task Post_ShouldCreateTheWorkoutForTheCallerWhenAUserSendsOnlyTheDate()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync("/workouts", new { name = "Leg day", workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await BodyOf(response);
        body.GetProperty("userId").GetGuid().Should().Be(SeedData.UserId);
        body.GetProperty("trainerId").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("workoutDate").GetString().Should().Be(Date);
        body.GetProperty("exercises").GetArrayLength().Should().Be(0);
        (await WorkoutIdsSeenBy(SeedCredentials.User)).Should().Contain(body.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Post_ShouldPointLocationAtTheNewWorkout()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync("/workouts", new { name = "Leg day", workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await BodyOf(response)).GetProperty("id").GetGuid();
        response.Headers.Location!.AbsolutePath.Should().Be($"/workouts/{id}");
        var followed = await client.GetAsync(response.Headers.Location);
        followed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await BodyOf(followed)).GetProperty("id").GetGuid().Should().Be(id);
    }

    [Fact]
    public async Task Post_ShouldAcceptTheCallersOwnIdWhenAUserSendsIt()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync("/workouts", new { name = "Leg day", userId = SeedData.UserId, workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await BodyOf(response)).GetProperty("userId").GetGuid().Should().Be(SeedData.UserId);
    }

    [Fact]
    public async Task Post_ShouldPrescribeTheWorkoutWhenATrainerCreatesItForAUser()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PostAsJsonAsync("/workouts", new { name = "Leg day", userId = SeedData.UserId, workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await BodyOf(response);
        var id = body.GetProperty("id").GetGuid();
        body.GetProperty("userId").GetGuid().Should().Be(SeedData.UserId);
        body.GetProperty("trainerId").GetGuid().Should().Be(SeedData.TrainerId);
        body.GetProperty("status").GetString().Should().Be("DRAFT");
        (await WorkoutIdsSeenBy(SeedCredentials.User)).Should().NotContain(id);
        (await WorkoutIdsSeenBy(SeedCredentials.Trainer)).Should().Contain(id);
    }

    [Fact]
    public async Task Post_ShouldCreateTheWorkoutWithoutTrainerWhenAnAdminSendsNone()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PostAsJsonAsync("/workouts", new { name = "Leg day", userId = SeedData.UserId, workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await BodyOf(response);
        var id = body.GetProperty("id").GetGuid();
        body.GetProperty("userId").GetGuid().Should().Be(SeedData.UserId);
        body.GetProperty("trainerId").ValueKind.Should().Be(JsonValueKind.Null);
        (await WorkoutIdsSeenBy(SeedCredentials.User)).Should().Contain(id);
        (await WorkoutIdsSeenBy(SeedCredentials.Trainer)).Should().NotContain(id);
    }

    [Fact]
    public async Task Post_ShouldAssignTheTrainerWhenAnAdminSendsOne()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PostAsJsonAsync("/workouts",
            new { name = "Leg day", userId = SeedData.UserId, trainerId = SeedData.TrainerId, workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await BodyOf(response);
        body.GetProperty("trainerId").GetGuid().Should().Be(SeedData.TrainerId);
        (await WorkoutIdsSeenBy(SeedCredentials.Trainer)).Should().Contain(body.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Post_ShouldRejectAnAdminNamingAUserAsTrainer()
    {
        // Arrange
        var before = await WorkoutIdsSeenBy(SeedCredentials.Admin);
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PostAsJsonAsync("/workouts",
            new { name = "Leg day", userId = SeedData.TrainerId, trainerId = SeedData.UserId, workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await WorkoutIdsSeenBy(SeedCredentials.Admin)).Should().BeEquivalentTo(before);
    }

    [Theory]
    [MemberData(nameof(Rejections))]
    public async Task Post_ShouldRejectTheRequestAndCreateNothing(string caller, object body, HttpStatusCode expected)
    {
        // Arrange
        var before = await WorkoutIdsSeenBy(SeedCredentials.Admin);
        var client = await AuthenticatedClientFor(caller);

        // Act
        var response = await client.PostAsJsonAsync("/workouts", body);

        // Assert
        response.StatusCode.Should().Be(expected);
        (await WorkoutIdsSeenBy(SeedCredentials.Admin)).Should().BeEquivalentTo(before);
    }

    [Fact]
    public async Task Post_ShouldCreateTheWorkoutOfAUserAsADraftTheyCanSee()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync("/workouts", new { name = "Leg day", workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await BodyOf(response);
        body.GetProperty("status").GetString().Should().Be("DRAFT");
        (await WorkoutIdsSeenBy(SeedCredentials.User)).Should().Contain(body.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Post_ShouldCreateTheWorkoutOfATrainerAsADraft()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PostAsJsonAsync("/workouts", new { name = "Leg day", userId = SeedData.UserId, workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await BodyOf(response);
        body.GetProperty("status").GetString().Should().Be("DRAFT");
        var created = await client.GetFromJsonAsync<JsonElement>($"/workouts/{body.GetProperty("id").GetGuid()}", JsonOptions);
        created.GetProperty("status").GetString().Should().Be("DRAFT");
    }

    [Fact]
    public async Task Post_ShouldCreateAnAdminsPrescribedWorkoutAsADraftHiddenFromItsAthlete()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync("/workouts", new
        {
            name = "Leg day",
            userId = SeedData.UserId,
            trainerId = SeedData.TrainerId,
            workoutDate = Date,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await BodyOf(response);
        body.GetProperty("status").GetString().Should().Be("DRAFT");
        (await WorkoutIdsSeenBy(SeedCredentials.User)).Should().NotContain(body.GetProperty("id").GetGuid());
        (await WorkoutIdsSeenBy(SeedCredentials.Trainer)).Should().Contain(body.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Post_ShouldIgnoreAStatusSentByTheCaller()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync("/workouts", new { name = "Leg day", workoutDate = Date, status = "COMPLETED" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await BodyOf(response)).GetProperty("status").GetString().Should().Be("DRAFT");
    }

    [Fact]
    public async Task Post_ShouldStoreTheNameAndTheDescriptionTrimmed()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync("/workouts", new { name = "  Leg day  ", description = "  Focus on tempo  ", workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await BodyOf(response);
        body.GetProperty("name").GetString().Should().Be("Leg day");
        body.GetProperty("description").GetString().Should().Be("Focus on tempo");
        var stored = await client.GetFromJsonAsync<JsonElement>($"/workouts/{body.GetProperty("id").GetGuid()}", JsonOptions);
        stored.GetProperty("name").GetString().Should().Be("Leg day");
        stored.GetProperty("description").GetString().Should().Be("Focus on tempo");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Post_ShouldStoreAMissingOrBlankDescriptionAsNull(string? description)
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync("/workouts", new { name = "Leg day", description, workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await BodyOf(response)).GetProperty("description").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Post_ShouldAcceptANameAndADescriptionAtTheirLimits()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);
        var name = new string('n', 256);
        var description = new string('d', 1024);

        // Act
        var response = await client.PostAsJsonAsync("/workouts", new { name, description, workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await BodyOf(response);
        body.GetProperty("name").GetString().Should().Be(name);
        body.GetProperty("description").GetString().Should().Be(description);
    }

    [Fact]
    public async Task Post_ShouldRejectATrainerPrescribingToADeactivatedUserAndCreateNothing()
    {
        // Arrange
        await SetActivationAsAdmin(SeedData.UserId, "deactivate");
        var before = await WorkoutIdsSeenBy(SeedCredentials.Admin);
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PostAsJsonAsync("/workouts", new { name = "Leg day", userId = SeedData.UserId, workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await WorkoutIdsSeenBy(SeedCredentials.Admin)).Should().BeEquivalentTo(before);
    }

    [Fact]
    public async Task Post_ShouldRejectAnAdminCreatingAWorkoutForADeactivatedUserAndCreateNothing()
    {
        // Arrange
        await SetActivationAsAdmin(SeedData.UserId, "deactivate");
        var before = await WorkoutIdsSeenBy(SeedCredentials.Admin);
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PostAsJsonAsync("/workouts", new { name = "Leg day", userId = SeedData.UserId, workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await WorkoutIdsSeenBy(SeedCredentials.Admin)).Should().BeEquivalentTo(before);
    }

    [Fact]
    public async Task Post_ShouldRejectAnAdminAssigningADeactivatedTrainerAndCreateNothing()
    {
        // Arrange
        await SetActivationAsAdmin(SeedData.TrainerId, "deactivate");
        var before = await WorkoutIdsSeenBy(SeedCredentials.Admin);
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PostAsJsonAsync("/workouts",
            new { name = "Leg day", userId = SeedData.UserId, trainerId = SeedData.TrainerId, workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await WorkoutIdsSeenBy(SeedCredentials.Admin)).Should().BeEquivalentTo(before);
    }

    [Fact]
    public async Task Post_ShouldLetATrainerPrescribeToAReactivatedUser()
    {
        // Arrange
        await SetActivationAsAdmin(SeedData.UserId, "deactivate");
        await SetActivationAsAdmin(SeedData.UserId, "activate");
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PostAsJsonAsync("/workouts", new { name = "Leg day", userId = SeedData.UserId, workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await BodyOf(response)).GetProperty("userId").GetGuid().Should().Be(SeedData.UserId);
    }
}
