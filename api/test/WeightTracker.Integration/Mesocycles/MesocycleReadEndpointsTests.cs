using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Mesocycles;

/// <summary>
/// <c>GET /mesocycles/{id}</c>, <c>GET /mesocycles/{id}/workouts</c>, <c>GET /mesocycles</c> and
/// <c>GET /users/{userId}/mesocycles</c>. The seed holds one four-week mesocycle from the trainer to the user, with
/// weeks 1, 2 and 4 defined, the trained workout placed in week 1 and the prescribed started workout in week 2.
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
        SeededMesocycleWorkoutsRoute,
    };

    private static readonly string SeededMesocycleWorkoutsRoute = $"{SeededMesocycleRoute}/workouts";

    /// <summary>
    /// Places the seeded prescribed draft in week 3 of the seeded mesocycle through <c>PUT /workouts/{id}</c>,
    /// as its trainer.
    /// </summary>
    private async Task GivenPrescribedDraftPlacedInTheSeededMesocycle()
    {
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        var response = await trainer.PutAsJsonAsync($"/workouts/{SeedData.PrescribedDraftWorkoutId}", new
        {
            name = "Planned draft",
            workoutDate = "2026-10-14",
            userId = SeedData.UserId,
            trainerId = SeedData.TrainerId,
            mesocycleId = SeedData.MesocycleId,
            mesocycleWeekNumber = 3,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, "the draft must be placed first");
    }

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

    [Fact]
    public async Task GetWorkouts_ShouldListEveryWeekByWeekNumberAndLeaveOutEveryOtherWorkout()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.GetAsync(SeededMesocycleWorkoutsRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var workouts = await Body(response);
        Ids(workouts).Should().Equal(SeedData.TrainedWorkoutId, SeedData.PrescribedStartedWorkoutId);
        Ids(workouts).Should().NotContain([SeedData.OwnedWorkoutId, SeedData.PrescribedDraftWorkoutId]);
        workouts.EnumerateArray().Select(workout => workout.GetProperty("mesocycleWeekNumber").GetInt32())
            .Should().Equal(1, 2);
        workouts.EnumerateArray().Select(workout => workout.GetProperty("mesocycleId").GetGuid())
            .Should().AllBeEquivalentTo(SeedData.MesocycleId);
        workouts[0].GetProperty("athlete").GetProperty("id").GetGuid().Should().Be(SeedData.UserId);
        workouts[0].GetProperty("trainer").GetProperty("id").GetGuid().Should().Be(SeedData.TrainerId);
    }

    [Fact]
    public async Task GetWorkouts_ShouldLeaveOutTheWorkoutsOfAnotherMesocycle()
    {
        // Arrange
        var otherMesocycleId = await GivenMesocycle(SeedData.TrainerId, SeedData.UserId);
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var created = await trainer.PostAsJsonAsync("/workouts", new
        {
            userId = SeedData.UserId,
            name = "Other block day",
            mesocycleId = otherMesocycleId,
            mesocycleWeekNumber = 1,
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created, "the other mesocycle's workout must be arranged first");
        var otherWorkoutId = (await Body(created)).GetProperty("id").GetGuid();

        // Act
        var response = await trainer.GetAsync(SeededMesocycleWorkoutsRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Ids(await Body(response)).Should().NotContain(otherWorkoutId);
    }

    [Fact]
    public async Task GetWorkouts_ShouldReturnAnEmptyListForAMesocycleWithoutWorkouts()
    {
        // Arrange
        var mesocycleId = await GivenMesocycle(SeedData.TrainerId, SeedData.UserId);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.GetAsync($"/mesocycles/{mesocycleId}/workouts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Body(response)).GetArrayLength().Should().Be(0);
    }

    [Theory]
    [InlineData(SeedCredentials.User)]
    [InlineData(SeedCredentials.Trainer)]
    [InlineData(SeedCredentials.Admin)]
    public async Task GetWorkouts_ShouldLetTheAthleteTheTrainerAndAdminsRead(string callerEmail)
    {
        // Arrange
        var client = await AuthenticatedClientFor(callerEmail);

        // Act
        var response = await client.GetAsync(SeededMesocycleWorkoutsRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Ids(await Body(response)).Should().Equal(SeedData.TrainedWorkoutId, SeedData.PrescribedStartedWorkoutId);
    }

    [Fact]
    public async Task GetWorkouts_ShouldHideAPrescribedDraftFromTheAthleteButNotFromTheTrainer()
    {
        // Arrange
        await GivenPrescribedDraftPlacedInTheSeededMesocycle();
        var athlete = await AuthenticatedClientFor(SeedCredentials.User);
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var responses = await Task.WhenAll(
            athlete.GetAsync(SeededMesocycleWorkoutsRoute), trainer.GetAsync(SeededMesocycleWorkoutsRoute));

        // Assert
        Ids(await Body(responses[0])).Should().NotContain(SeedData.PrescribedDraftWorkoutId);
        Ids(await Body(responses[1])).Should().Contain(SeedData.PrescribedDraftWorkoutId);
    }

    [Theory]
    [InlineData(Role.TRAINER)]
    [InlineData(Role.USER)]
    public async Task GetWorkouts_ShouldRefuseSomebodyWhoIsNotPartOfTheMesocycle(Role role)
    {
        // Arrange
        var (_, email) = await GivenUser(role);
        var outsider = await AuthenticatedClientFor(email);

        // Act
        var response = await outsider.GetAsync(SeededMesocycleWorkoutsRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetWorkouts_ShouldNotFindAnUnknownMesocycle()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.GetAsync($"/mesocycles/{Guid.NewGuid()}/workouts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
