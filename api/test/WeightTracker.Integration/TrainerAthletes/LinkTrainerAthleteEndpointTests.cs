using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WeightTracker.Domain.TrainerAthletes;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.TrainerAthletes;

/// <summary>
/// <c>POST /trainer-athletes/{trainerId}</c>.
/// </summary>
public class LinkTrainerAthleteEndpointTests : TrainerAthleteEndpointTest
{
    private const string NewAthleteEmail = "new.athlete@weighttracker.test";

    public LinkTrainerAthleteEndpointTests(ApiFactory factory) : base(factory)
    { }

    private static string Route(Guid trainerId) => $"/trainer-athletes/{trainerId}";

    [Fact]
    public async Task Post_ShouldRejectAnonymousCallersAndCreateNothing()
    {
        // Arrange
        var before = await StoredRelationshipCount();

        // Act
        var response = await Client.PostAsJsonAsync(Route(SeedData.TrainerId), new { athleteId = SeedData.UserId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await StoredRelationshipCount()).Should().Be(before);
    }

    [Fact]
    public async Task Post_ShouldRejectAGarbageTokenAndCreateNothing()
    {
        // Arrange
        var before = await StoredRelationshipCount();
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.PostAsJsonAsync(Route(SeedData.TrainerId), new { athleteId = SeedData.UserId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await StoredRelationshipCount()).Should().Be(before);
    }

    [Fact]
    public async Task Post_ShouldForbidATrainerEvenForThemselves()
    {
        // Arrange
        var athleteId = await CreateUser(NewAthleteEmail, Role.USER);
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PostAsJsonAsync(Route(SeedData.TrainerId), new { athleteId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await StoredRelationships(SeedData.TrainerId, athleteId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Post_ShouldForbidAnAthleteLinkingThemselves()
    {
        // Arrange
        var athleteId = await CreateUser(NewAthleteEmail, Role.USER);
        var client = await AuthenticatedClientFor(NewAthleteEmail);

        // Act
        var response = await client.PostAsJsonAsync(Route(SeedData.TrainerId), new { athleteId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await StoredRelationships(SeedData.TrainerId, athleteId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Post_ShouldLinkAnAthleteAndListThemUnderTheTrainer()
    {
        // Arrange
        var athleteId = await CreateUser(NewAthleteEmail, Role.USER, "Marie", "Curie");
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(Route(SeedData.TrainerId), new { athleteId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await Body(response);
        ShouldHaveExactly(body,
            "id", "trainerId", "trainerName", "trainerSurname", "athleteId", "athleteName", "athleteSurname",
            "status", "startedAtUtc", "finishedAtUtc");
        body.GetProperty("trainerName").GetString().Should().Be("Grace");
        body.GetProperty("trainerSurname").GetString().Should().Be("Hopper");
        body.GetProperty("athleteName").GetString().Should().Be("Marie");
        body.GetProperty("athleteSurname").GetString().Should().Be("Curie");
        body.GetProperty("trainerId").GetGuid().Should().Be(SeedData.TrainerId);
        body.GetProperty("athleteId").GetGuid().Should().Be(athleteId);
        body.GetProperty("status").GetString().Should().Be("ACTIVE");
        body.GetProperty("finishedAtUtc").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
        var listed = await Items(await admin.GetAsync(Route(SeedData.TrainerId)));
        listed.Select(item => item.GetProperty("athleteId").GetGuid()).Should().Contain(athleteId);
    }

    [Fact]
    public async Task Post_ShouldRejectAnUnknownTrainer()
    {
        // Arrange
        var athleteId = await CreateUser(NewAthleteEmail, Role.USER);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(Route(Guid.NewGuid()), new { athleteId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await StoredRelationships(SeedData.TrainerId, athleteId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Post_ShouldRejectAnUnknownAthlete()
    {
        // Arrange
        var before = await StoredRelationshipCount();
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(Route(SeedData.TrainerId), new { athleteId = Guid.NewGuid() });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await StoredRelationshipCount()).Should().Be(before);
    }

    [Fact]
    public async Task Post_ShouldRejectAnAdminAsTrainer()
    {
        // Arrange
        var athleteId = await CreateUser(NewAthleteEmail, Role.USER);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(Route(SeedData.AdminId), new { athleteId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StoredRelationships(SeedData.AdminId, athleteId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Post_ShouldRejectATrainerAsAthlete()
    {
        // Arrange
        var otherTrainerId = await CreateUser("other.trainer@weighttracker.test", Role.TRAINER);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(Route(SeedData.TrainerId), new { athleteId = otherTrainerId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StoredRelationships(SeedData.TrainerId, otherTrainerId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Post_ShouldRejectADeactivatedAthlete()
    {
        // Arrange
        var athleteId = await CreateUser(NewAthleteEmail, Role.USER);
        await Deactivate(athleteId);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(Route(SeedData.TrainerId), new { athleteId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StoredRelationships(SeedData.TrainerId, athleteId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Post_ShouldRejectADeactivatedTrainer()
    {
        // Arrange
        var athleteId = await CreateUser(NewAthleteEmail, Role.USER);
        await Deactivate(SeedData.TrainerId);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(Route(SeedData.TrainerId), new { athleteId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StoredRelationships(SeedData.TrainerId, athleteId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Post_ShouldRejectAnAthleteAlreadyLinkedToAnotherTrainer()
    {
        // Arrange
        var otherTrainerId = await CreateUser("other.trainer@weighttracker.test", Role.TRAINER);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(Route(otherTrainerId), new { athleteId = SeedData.UserId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StoredRelationships(otherTrainerId, SeedData.UserId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Post_ShouldRejectAnAthleteAlreadyLinkedToTheSameTrainer()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(Route(SeedData.TrainerId), new { athleteId = SeedData.UserId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StoredRelationships(SeedData.TrainerId, SeedData.UserId))
            .Count(relationship => relationship.Status == (int)TrainerAthleteStatus.ACTIVE).Should().Be(1);
    }

    [Fact]
    public async Task Post_ShouldLinkTheSamePairAgainOnceTheRelationshipEnded()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        (await admin.PutAsJsonAsync($"{Route(SeedData.TrainerId)}/end", new { athleteId = SeedData.UserId }))
            .EnsureSuccessStatusCode();

        // Act
        var response = await admin.PostAsJsonAsync(Route(SeedData.TrainerId), new { athleteId = SeedData.UserId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var newId = (await Body(response)).GetProperty("id").GetGuid();
        newId.Should().NotBe(SeedData.ActiveTrainerAthleteId);
        var stored = await StoredRelationships(SeedData.TrainerId, SeedData.UserId);
        stored.Single(relationship => relationship.Id == SeedData.ActiveTrainerAthleteId)
            .Status.Should().Be((int)TrainerAthleteStatus.ENDED);
        stored.Single(relationship => relationship.Id == newId).Status.Should().Be((int)TrainerAthleteStatus.ACTIVE);
    }

    [Fact]
    public async Task Post_ShouldRejectAMissingAthleteId()
    {
        // Arrange
        var before = await StoredRelationshipCount();
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(Route(SeedData.TrainerId), new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await StoredRelationshipCount()).Should().Be(before);
    }

    [Fact]
    public async Task Post_ShouldRejectAMalformedAthleteId()
    {
        // Arrange
        var before = await StoredRelationshipCount();
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(Route(SeedData.TrainerId), new { athleteId = "not-a-guid" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await StoredRelationshipCount()).Should().Be(before);
    }
}
