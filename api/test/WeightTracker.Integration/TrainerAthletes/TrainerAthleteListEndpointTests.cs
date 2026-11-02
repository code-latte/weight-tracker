using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.TrainerAthletes;

/// <summary>
/// <c>GET /trainer-athletes</c> and <c>GET /trainer-athletes/{trainerId}</c>. Scoped reads: each case asserts
/// what is excluded as well as what is listed.
/// </summary>
/// <remarks>
/// Baseline: the seeded trainer has one active relationship with the seeded user and one ended one with the same
/// user. A second trainer and their athlete are added where exclusion is under test.
/// </remarks>
public class TrainerAthleteListEndpointTests : TrainerAthleteEndpointTest
{
    private const string MineRoute = "/trainer-athletes";

    private static string ByTrainerRoute(Guid trainerId) => $"/trainer-athletes/{trainerId}";

    private const string OtherTrainerEmail = "other.trainer@weighttracker.test";
    private const string OtherAthleteEmail = "other.athlete@weighttracker.test";

    public TrainerAthleteListEndpointTests(ApiFactory factory) : base(factory)
    { }

    private async Task<(Guid TrainerId, Guid AthleteId)> GivenAnotherTrainerWithAnAthlete()
    {
        var trainerId = await CreateUser(OtherTrainerEmail, Role.TRAINER);
        var athleteId = await CreateUser(OtherAthleteEmail, Role.USER);
        await Link(trainerId, athleteId);

        return (trainerId, athleteId);
    }

    [Theory]
    [InlineData(MineRoute)]
    [InlineData("/trainer-athletes/00000000-0000-0000-0000-000000000002")]
    public async Task Get_ShouldRejectAnonymousCallers(string route)
    {
        // Arrange & Act
        var response = await Client.GetAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(MineRoute)]
    [InlineData("/trainer-athletes/00000000-0000-0000-0000-000000000002")]
    public async Task Get_ShouldRejectAGarbageToken(string route)
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.GetAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(SeedCredentials.User)]
    [InlineData(SeedCredentials.Admin)]
    public async Task GetMine_ShouldForbidNonTrainers(string callerEmail)
    {
        // Arrange
        var client = await AuthenticatedClientFor(callerEmail);

        // Act
        var response = await client.GetAsync(MineRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.Content.ReadAsStringAsync()).Should().NotContain(SeedCredentials.User);
    }

    [Fact]
    public async Task GetMine_ShouldListOnlyTheCallersActiveAthletes()
    {
        // Arrange
        var (_, otherAthleteId) = await GivenAnotherTrainerWithAnAthlete();
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.GetAsync(MineRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await Items(response);
        items.Select(item => item.GetProperty("id").GetGuid()).Should().Equal(SeedData.ActiveTrainerAthleteId);
        items.Select(item => item.GetProperty("id").GetGuid()).Should().NotContain(SeedData.EndedTrainerAthleteId);
        items.Select(item => item.GetProperty("athleteId").GetGuid()).Should().NotContain(otherAthleteId);
    }

    [Fact]
    public async Task GetMine_ShouldOrderAthletesByEmailAndExposeOnlyTheListedFields()
    {
        // Arrange
        var firstId = await CreateUser("aaa.athlete@weighttracker.test", Role.USER);
        await Link(SeedData.TrainerId, firstId);
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.GetAsync(MineRoute);

        // Assert
        var items = await Items(response);
        items.Select(item => item.GetProperty("email").GetString())
            .Should().Equal("aaa.athlete@weighttracker.test", SeedCredentials.User);
        foreach (var item in items)
            ShouldHaveExactly(item, "id", "athleteId", "email", "name", "surname", "trainerName", "trainerSurname", "startedAtUtc");
        items[1].GetProperty("name").GetString().Should().Be("Ada");
        items.Should().OnlyContain(item => item.GetProperty("trainerName").GetString() == "Grace"
            && item.GetProperty("trainerSurname").GetString() == "Hopper");
        items[1].GetProperty("surname").GetString().Should().Be("Lovelace");
        items[1].GetProperty("startedAtUtc").GetDateTime().Should().Be(SeedData.CreatedAt);
    }

    [Fact]
    public async Task GetMine_ShouldBeEmptyOnceTheOnlyRelationshipEnded()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        (await trainer.PutAsJsonAsync($"/trainer-athletes/{SeedData.TrainerId}/end", new { athleteId = SeedData.UserId }))
            .EnsureSuccessStatusCode();

        // Act
        var response = await trainer.GetAsync(MineRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Items(response)).Should().BeEmpty();
    }

    [Theory]
    [InlineData(SeedCredentials.Trainer)]
    [InlineData(SeedCredentials.User)]
    public async Task GetByTrainer_ShouldForbidNonAdmins(string callerEmail)
    {
        // Arrange
        var client = await AuthenticatedClientFor(callerEmail);

        // Act
        var response = await client.GetAsync(ByTrainerRoute(SeedData.TrainerId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.Content.ReadAsStringAsync()).Should().NotContain(SeedCredentials.User);
    }

    [Fact]
    public async Task GetByTrainer_ShouldListOnlyThatTrainersActiveAthletes()
    {
        // Arrange
        var (otherTrainerId, otherAthleteId) = await GivenAnotherTrainerWithAnAthlete();
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.GetAsync(ByTrainerRoute(otherTrainerId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await Items(response);
        items.Select(item => item.GetProperty("athleteId").GetGuid()).Should().Equal(otherAthleteId);
        items.Select(item => item.GetProperty("athleteId").GetGuid()).Should().NotContain(SeedData.UserId);
        foreach (var item in items)
            ShouldHaveExactly(item, "id", "athleteId", "email", "name", "surname", "trainerName", "trainerSurname", "startedAtUtc");
    }

    [Fact]
    public async Task GetByTrainer_ShouldExcludeEndedRelationships()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.GetAsync(ByTrainerRoute(SeedData.TrainerId));

        // Assert
        var ids = (await Items(response)).Select(item => item.GetProperty("id").GetGuid()).ToList();
        ids.Should().Equal(SeedData.ActiveTrainerAthleteId);
        ids.Should().NotContain(SeedData.EndedTrainerAthleteId);
    }

    [Fact]
    public async Task GetByTrainer_ShouldAnswerNotFoundForAnUnknownTrainer()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.GetAsync(ByTrainerRoute(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
