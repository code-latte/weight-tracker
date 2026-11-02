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
/// <c>PUT /trainer-athletes/{trainerId}/end</c>. The seeded trainer and user have one active relationship.
/// </summary>
public class EndTrainerAthleteEndpointTests : TrainerAthleteEndpointTest
{
    private static readonly string Route = $"/trainer-athletes/{SeedData.TrainerId}/end";

    private static readonly object ValidBody = new { athleteId = SeedData.UserId };

    public EndTrainerAthleteEndpointTests(ApiFactory factory) : base(factory)
    { }

    private async Task<TrainerAthleteStatus> SeededActiveStatus()
        => (TrainerAthleteStatus)(await StoredRelationships(SeedData.TrainerId, SeedData.UserId))
            .Single(relationship => relationship.Id == SeedData.ActiveTrainerAthleteId).Status;

    [Fact]
    public async Task Put_ShouldRejectAnonymousCallersAndChangeNothing()
    {
        // Arrange & Act
        var response = await Client.PutAsJsonAsync(Route, ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await SeededActiveStatus()).Should().Be(TrainerAthleteStatus.ACTIVE);
    }

    [Fact]
    public async Task Put_ShouldRejectAGarbageTokenAndChangeNothing()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.PutAsJsonAsync(Route, ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await SeededActiveStatus()).Should().Be(TrainerAthleteStatus.ACTIVE);
    }

    [Theory]
    [InlineData(SeedCredentials.Trainer)]
    [InlineData(SeedCredentials.User)]
    [InlineData(SeedCredentials.Admin)]
    public async Task Put_ShouldEndTheRelationshipForEitherSideOrAnAdmin(string callerEmail)
    {
        // Arrange
        var client = await AuthenticatedClientFor(callerEmail);

        // Act
        var response = await client.PutAsJsonAsync(Route, ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await Body(response);
        body.GetProperty("id").GetGuid().Should().Be(SeedData.ActiveTrainerAthleteId);
        body.GetProperty("status").GetString().Should().Be("ENDED");
        body.GetProperty("trainerName").GetString().Should().Be("Grace");
        body.GetProperty("trainerSurname").GetString().Should().Be("Hopper");
        body.GetProperty("athleteName").GetString().Should().Be("Ada");
        body.GetProperty("athleteSurname").GetString().Should().Be("Lovelace");
        body.GetProperty("finishedAtUtc").GetDateTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        (await SeededActiveStatus()).Should().Be(TrainerAthleteStatus.ENDED);
    }

    [Fact]
    public async Task Put_ShouldRemoveTheAthleteFromTheTrainersList()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        await trainer.PutAsJsonAsync(Route, ValidBody);

        // Assert
        (await Items(await trainer.GetAsync("/trainer-athletes"))).Should().BeEmpty();
    }

    [Fact]
    public async Task Put_ShouldForbidAnotherTrainer()
    {
        // Arrange
        await CreateUser("other.trainer@weighttracker.test", Role.TRAINER);
        var client = await AuthenticatedClientFor("other.trainer@weighttracker.test");

        // Act
        var response = await client.PutAsJsonAsync(Route, ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SeededActiveStatus()).Should().Be(TrainerAthleteStatus.ACTIVE);
    }

    [Fact]
    public async Task Put_ShouldForbidAnotherAthlete()
    {
        // Arrange
        await CreateUser("other.athlete@weighttracker.test", Role.USER);
        var client = await AuthenticatedClientFor("other.athlete@weighttracker.test");

        // Act
        var response = await client.PutAsJsonAsync(Route, ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SeededActiveStatus()).Should().Be(TrainerAthleteStatus.ACTIVE);
    }

    [Fact]
    public async Task Put_ShouldAnswerNotFoundOnceAlreadyEndedAndKeepTheEndDate()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var firstEnd = await Body(await admin.PutAsJsonAsync(Route, ValidBody));

        // Act
        var response = await admin.PutAsJsonAsync(Route, ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var stored = (await StoredRelationships(SeedData.TrainerId, SeedData.UserId))
            .Single(relationship => relationship.Id == SeedData.ActiveTrainerAthleteId);
        stored.FinishedAtUtc.Should().BeCloseTo(firstEnd.GetProperty("finishedAtUtc").GetDateTime(), TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Put_ShouldRejectAMissingAthleteId()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PutAsJsonAsync(Route, new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await SeededActiveStatus()).Should().Be(TrainerAthleteStatus.ACTIVE);
    }
}
