using System.Net;
using FluentAssertions;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Mesocycles;

/// <summary>
/// <c>GET /mesocycles/{id}</c>, <c>GET /mesocycles</c> and <c>GET /users/{userId}/mesocycles</c>. The seed holds
/// one four-week mesocycle from the trainer to the user, with weeks 1, 2 and 4 defined.
/// </summary>
public class MesocycleReadEndpointsTests : MesocycleEndpointTest
{
    private static readonly string UserMesocyclesRoute = $"/users/{SeedData.UserId}/mesocycles";

    public MesocycleReadEndpointsTests(ApiFactory factory) : base(factory)
    { }

    public static TheoryData<string> Routes() => new()
    {
        SeededMesocycleRoute,
        "/mesocycles",
        UserMesocyclesRoute,
    };

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task Get_ShouldRejectAnonymousCallers(string route)
    {
        // Arrange
        var anonymous = Client;

        // Act
        var response = await anonymous.GetAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task Get_ShouldRejectAGarbageToken(string route)
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.GetAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_ShouldReturnTheMesocycleWithItsWeeksAndPeople()
    {
        // Arrange
        var athlete = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await athlete.GetAsync(SeededMesocycleRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await Body(response);
        body.GetProperty("id").GetGuid().Should().Be(SeedData.MesocycleId);
        body.GetProperty("trainerId").GetGuid().Should().Be(SeedData.TrainerId);
        body.GetProperty("athleteId").GetGuid().Should().Be(SeedData.UserId);
        body.GetProperty("name").GetString().Should().Be(SeedData.MesocycleName);
        body.GetProperty("objective").GetString().Should().Be("Build muscle mass before the strength block");
        body.GetProperty("startDate").GetString().Should().Be("2026-10-01");
        body.GetProperty("durationInWeeks").GetInt32().Should().Be(SeedData.MesocycleDurationInWeeks);
        var weeks = body.GetProperty("weeks").EnumerateArray().ToList();
        weeks.Select(week => week.GetProperty("weekNumber").GetInt32()).Should().Equal(1, 2, 4);
        weeks.Select(week => week.GetProperty("trainingWeekType").GetString())
            .Should().Equal("ADAPTATION", "ACCUMULATION", "DELOAD");
        weeks.Select(week => week.EnumerateObject().Select(property => property.Name).ToList())
            .Should().AllSatisfy(names => names.Should().Equal("weekNumber", "trainingWeekType", "objective"));
        body.GetProperty("trainer").GetProperty("name").GetString().Should().Be("Grace");
        body.GetProperty("athlete").GetProperty("name").GetString().Should().Be("Ada");
    }

    [Fact]
    public async Task GetById_ShouldReturnAnEmptyWeekListForAMesocycleWithoutWeeks()
    {
        // Arrange
        var mesocycleId = await GivenMesocycle(SeedData.TrainerId, SeedData.UserId);
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.GetAsync($"/mesocycles/{mesocycleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Body(response)).GetProperty("weeks").GetArrayLength().Should().Be(0);
    }

    [Theory]
    [InlineData(SeedCredentials.User)]
    [InlineData(SeedCredentials.Trainer)]
    [InlineData(SeedCredentials.Admin)]
    public async Task GetById_ShouldLetTheAthleteTheTrainerAndAdminsRead(string callerEmail)
    {
        // Arrange
        var client = await AuthenticatedClientFor(callerEmail);

        // Act
        var response = await client.GetAsync(SeededMesocycleRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(Role.TRAINER)]
    [InlineData(Role.USER)]
    public async Task GetById_ShouldRefuseSomebodyWhoIsNotPartOfIt(Role role)
    {
        // Arrange
        var (_, email) = await GivenUser(role);
        var outsider = await AuthenticatedClientFor(email);

        // Act
        var response = await outsider.GetAsync(SeededMesocycleRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetById_ShouldNotFindAnUnknownMesocycle()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.GetAsync($"/mesocycles/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_ShouldStillLetTheTrainerReadOnceTheRelationshipHasEnded()
    {
        // Arrange
        await GivenSeededRelationshipEnded();
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.GetAsync(SeededMesocycleRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAll_ShouldGiveAUserOnlyTheirMesocycles()
    {
        // Arrange
        var (otherAthleteId, _) = await GivenUser(Role.USER);
        var otherMesocycleId = await GivenMesocycle(SeedData.TrainerId, otherAthleteId);
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.GetAsync("/mesocycles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ids = Ids(await Body(response));
        ids.Should().Equal(SeedData.MesocycleId);
        ids.Should().NotContain(otherMesocycleId);
    }

    [Fact]
    public async Task GetAll_ShouldGiveATrainerOnlyTheirMesocycles()
    {
        // Arrange
        var (otherTrainerId, _) = await GivenUser(Role.TRAINER);
        var otherMesocycleId = await GivenMesocycle(otherTrainerId, SeedData.UserId);
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.GetAsync("/mesocycles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ids = Ids(await Body(response));
        ids.Should().Equal(SeedData.MesocycleId);
        ids.Should().NotContain(otherMesocycleId);
    }

    [Fact]
    public async Task GetAll_ShouldGiveAnAdminEveryMesocycleLatestStartFirst()
    {
        // Arrange
        var (otherTrainerId, _) = await GivenUser(Role.TRAINER);
        var (otherAthleteId, _) = await GivenUser(Role.USER);
        var later = await GivenMesocycle(otherTrainerId, otherAthleteId, "2026-11-01");
        var earlier = await GivenMesocycle(otherTrainerId, SeedData.UserId, "2026-09-01");
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.GetAsync("/mesocycles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Ids(await Body(response)).Should().Equal(later, SeedData.MesocycleId, earlier);
    }

    [Fact]
    public async Task GetAll_ShouldReturnAnEmptyListWhenNothingIsInScope()
    {
        // Arrange
        var (_, email) = await GivenUser(Role.USER);
        var user = await AuthenticatedClientFor(email);

        // Act
        var response = await user.GetAsync("/mesocycles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Body(response)).GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetUserMesocycles_ShouldGiveATrainerOnlyTheMesocyclesTheyPrepareForThatAthlete()
    {
        // Arrange
        var (otherTrainerId, _) = await GivenUser(Role.TRAINER);
        var fromOtherTrainer = await GivenMesocycle(otherTrainerId, SeedData.UserId);
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.GetAsync(UserMesocyclesRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ids = Ids(await Body(response));
        ids.Should().Equal(SeedData.MesocycleId);
        ids.Should().NotContain(fromOtherTrainer);
    }

    [Fact]
    public async Task GetUserMesocycles_ShouldGiveAnAdminEveryMesocycleOfThatAthleteOnly()
    {
        // Arrange
        var (otherTrainerId, _) = await GivenUser(Role.TRAINER);
        var (otherAthleteId, _) = await GivenUser(Role.USER);
        var fromOtherTrainer = await GivenMesocycle(otherTrainerId, SeedData.UserId);
        var ofOtherAthlete = await GivenMesocycle(otherTrainerId, otherAthleteId);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.GetAsync(UserMesocyclesRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ids = Ids(await Body(response));
        ids.Should().BeEquivalentTo([SeedData.MesocycleId, fromOtherTrainer]);
        ids.Should().NotContain(ofOtherAthlete);
    }

    [Fact]
    public async Task GetUserMesocycles_ShouldGiveATrainerAnEmptyListForAnAthleteTheyHaveNoMesocycleFor()
    {
        // Arrange
        var (otherAthleteId, _) = await GivenUser(Role.USER);
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.GetAsync($"/users/{otherAthleteId}/mesocycles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Body(response)).GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetUserMesocycles_ShouldRefuseARegularUserEvenForThemselves()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.GetAsync(UserMesocyclesRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUserMesocycles_ShouldNotFindAnUnknownUser()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.GetAsync($"/users/{Guid.NewGuid()}/mesocycles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
