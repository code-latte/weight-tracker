using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Mesocycles;

/// <summary>
/// <c>POST /mesocycles</c>, <c>PUT /mesocycles/{id}</c>, <c>POST /mesocycles/{id}/weeks</c>,
/// <c>PUT /mesocycles/{id}/weeks/{weekNumber}</c> and <c>DELETE /mesocycles/{id}/weeks/{weekNumber}</c>. The seeded
/// trainer actively trains the seeded user, and the seeded four-week mesocycle between them has weeks 1, 2 and 4
/// defined, with a published workout in week 1, a started one in week 2 and none in week 4.
/// </summary>
public class MesocycleWriteEndpointsTests : MesocycleEndpointTest
{
    private static readonly string WeeksRoute = $"{SeededMesocycleRoute}/weeks";

    private static readonly object UpdateBody = new
    {
        name = "Renamed block",
        objective = "New objective",
        startDate = "2026-10-05",
        durationInWeeks = 6,
    };

    private static object AWeekBody(int weekNumber, string trainingWeekType = "INTENSIFICATION")
        => new { weekNumber, trainingWeekType, objective = "Heavy triples" };

    private static readonly object RedefineBody = new { trainingWeekType = "DELOAD", objective = "Back off" };

    public MesocycleWriteEndpointsTests(ApiFactory factory) : base(factory)
    { }

    public static TheoryData<string, string> Routes() => new()
    {
        { "POST", "/mesocycles" },
        { "PUT", SeededMesocycleRoute },
        { "POST", WeeksRoute },
        { "PUT", $"{WeeksRoute}/2" },
        { "DELETE", $"{WeeksRoute}/4" },
    };

    private static object BodyFor(string method, string route) => (method, route) switch
    {
        ("POST", "/mesocycles") => AMesocycleBody(SeedData.UserId),
        ("PUT", var put) when put == SeededMesocycleRoute => UpdateBody,
        ("POST", _) => AWeekBody(3),
        _ => RedefineBody,
    };

    private static Task<HttpResponseMessage> Send(HttpClient client, string method, string route, object body)
        => method switch
        {
            "POST" => client.PostAsJsonAsync(route, body),
            "DELETE" => client.DeleteAsync(route),
            _ => client.PutAsJsonAsync(route, body),
        };

    private async Task<int> MesocycleCount()
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        return (await Body(await admin.GetAsync("/mesocycles"))).GetArrayLength();
    }

    private async Task SeededMesocycleShouldBeUnchanged()
    {
        var mesocycle = await SeededMesocycle();
        mesocycle.GetProperty("name").GetString().Should().Be(SeedData.MesocycleName);
        mesocycle.GetProperty("durationInWeeks").GetInt32().Should().Be(SeedData.MesocycleDurationInWeeks);
        mesocycle.GetProperty("weeks").EnumerateArray()
            .Select(week => (week.GetProperty("weekNumber").GetInt32(), week.GetProperty("trainingWeekType").GetString()))
            .Should().Equal((1, "ADAPTATION"), (2, "ACCUMULATION"), (4, "DELOAD"));
    }

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task Write_ShouldRejectAnonymousCallersAndChangeNothing(string method, string route)
    {
        // Arrange
        var anonymous = Client;

        // Act
        var response = await Send(anonymous, method, route, BodyFor(method, route));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await SeededMesocycleShouldBeUnchanged();
    }

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task Write_ShouldRejectAGarbageTokenAndChangeNothing(string method, string route)
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Send(Client, method, route, BodyFor(method, route));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await SeededMesocycleShouldBeUnchanged();
    }

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task Write_ShouldRefuseTheAthleteAndChangeNothing(string method, string route)
    {
        // Arrange
        var athlete = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await Send(athlete, method, route, BodyFor(method, route));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await SeededMesocycleShouldBeUnchanged();
        (await MesocycleCount()).Should().Be(1);
    }

    [Theory]
    [InlineData("PUT", "")]
    [InlineData("POST", "/weeks")]
    [InlineData("PUT", "/weeks/2")]
    [InlineData("DELETE", "/weeks/4")]
    public async Task Write_ShouldRefuseAnotherTrainerAndChangeNothing(string method, string suffix)
    {
        // Arrange
        var (_, email) = await GivenUser(Role.TRAINER);
        var otherTrainer = await AuthenticatedClientFor(email);
        var route = SeededMesocycleRoute + suffix;

        // Act
        var response = await Send(otherTrainer, method, route, BodyFor(method, route));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await SeededMesocycleShouldBeUnchanged();
    }

    [Theory]
    [InlineData("PUT", "")]
    [InlineData("POST", "/weeks")]
    [InlineData("PUT", "/weeks/2")]
    [InlineData("DELETE", "/weeks/4")]
    public async Task Write_ShouldNotFindAnUnknownMesocycle(string method, string suffix)
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var route = $"/mesocycles/{Guid.NewGuid()}{suffix}";
        var body = BodyFor(method, SeededMesocycleRoute + suffix);

        // Act
        var response = await Send(trainer, method, route, body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ShouldLetATrainerCreateForAnAthleteTheyActivelyTrain()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync("/mesocycles", AMesocycleBody(SeedData.UserId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await Body(response);
        var id = body.GetProperty("id").GetGuid();
        response.Headers.Location!.ToString().Should().EndWith($"/mesocycles/{id}");
        body.GetProperty("trainerId").GetGuid().Should().Be(SeedData.TrainerId);
        body.GetProperty("athleteId").GetGuid().Should().Be(SeedData.UserId);
        body.GetProperty("startDate").GetString().Should().Be("2026-11-09");
        body.GetProperty("weeks").GetArrayLength().Should().Be(0);
        body.GetProperty("trainer").GetProperty("name").GetString().Should().Be("Grace");
        (await trainer.GetAsync($"/mesocycles/{id}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_ShouldRefuseATrainerWithoutAnActiveRelationship()
    {
        // Arrange
        await GivenSeededRelationshipEnded();
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync("/mesocycles", AMesocycleBody(SeedData.UserId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await MesocycleCount()).Should().Be(1);
    }

    [Fact]
    public async Task Create_ShouldRefuseATrainerWhoSendsATrainerId()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync("/mesocycles", AMesocycleBody(SeedData.UserId, SeedData.TrainerId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await MesocycleCount()).Should().Be(1);
    }

    [Fact]
    public async Task Create_ShouldLetAnAdminCreateForATrainerAndAthleteWithoutRelationship()
    {
        // Arrange
        var (otherTrainerId, _) = await GivenUser(Role.TRAINER);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync("/mesocycles", AMesocycleBody(SeedData.UserId, otherTrainerId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await Body(response);
        body.GetProperty("trainerId").GetGuid().Should().Be(otherTrainerId);
        body.GetProperty("athleteId").GetGuid().Should().Be(SeedData.UserId);
    }

    [Fact]
    public async Task Create_ShouldRefuseAnAdminWhoOmitsTheTrainer()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync("/mesocycles", AMesocycleBody(SeedData.UserId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await MesocycleCount()).Should().Be(1);
    }

    [Fact]
    public async Task Create_ShouldNotFindAnUnknownAthlete()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync("/mesocycles", AMesocycleBody(Guid.NewGuid(), SeedData.TrainerId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await MesocycleCount()).Should().Be(1);
    }

    [Fact]
    public async Task Create_ShouldRefuseAnAthleteWhoIsNotARegularUser()
    {
        // Arrange
        var (otherTrainerId, _) = await GivenUser(Role.TRAINER);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync("/mesocycles", AMesocycleBody(otherTrainerId, SeedData.TrainerId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await MesocycleCount()).Should().Be(1);
    }

    [Fact]
    public async Task Create_ShouldRefuseAnAdminAsTrainer()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync("/mesocycles", AMesocycleBody(SeedData.UserId, SeedData.AdminId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await MesocycleCount()).Should().Be(1);
    }

    [Fact]
    public async Task Create_ShouldRefuseAnUnknownTrainer()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync("/mesocycles", AMesocycleBody(SeedData.UserId, Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await MesocycleCount()).Should().Be(1);
    }

    [Fact]
    public async Task Create_ShouldRefuseADeactivatedAthlete()
    {
        // Arrange
        var (athleteId, _) = await GivenUser(Role.USER);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        (await admin.PutAsync($"/users/{athleteId}/deactivate", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await admin.PostAsJsonAsync("/mesocycles", AMesocycleBody(athleteId, SeedData.TrainerId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await MesocycleCount()).Should().Be(1);
    }

    [Theory]
    [InlineData("name", "   ")]
    [InlineData("objective", "   ")]
    public async Task Create_ShouldRejectABlankNameOrObjective(string field, string value)
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var body = new Dictionary<string, object?>
        {
            ["athleteId"] = SeedData.UserId,
            ["name"] = "Strength block",
            ["objective"] = "Peak strength",
            ["startDate"] = "2026-11-09",
            ["durationInWeeks"] = 4,
            [field] = value,
        };

        // Act
        var response = await trainer.PostAsJsonAsync("/mesocycles", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await MesocycleCount()).Should().Be(1);
    }

    [Fact]
    public async Task Create_ShouldRejectAMissingObjective()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync("/mesocycles", new
        {
            athleteId = SeedData.UserId,
            name = "Strength block",
            startDate = "2026-11-09",
            durationInWeeks = 4,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await MesocycleCount()).Should().Be(1);
    }

    [Fact]
    public async Task Update_ShouldReplaceTheDetailsAndKeepTheWeeks()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync(SeededMesocycleRoute, UpdateBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await Body(response);
        body.GetProperty("name").GetString().Should().Be("Renamed block");
        body.GetProperty("objective").GetString().Should().Be("New objective");
        body.GetProperty("startDate").GetString().Should().Be("2026-10-05");
        body.GetProperty("durationInWeeks").GetInt32().Should().Be(6);
        body.GetProperty("trainerId").GetGuid().Should().Be(SeedData.TrainerId);
        body.GetProperty("weeks").EnumerateArray().Select(week => week.GetProperty("weekNumber").GetInt32())
            .Should().Equal(1, 2, 4);
        (await SeededMesocycle()).GetProperty("name").GetString().Should().Be("Renamed block");
    }

    [Fact]
    public async Task Update_ShouldStillBeAllowedOnceTheRelationshipHasEnded()
    {
        // Arrange
        await GivenSeededRelationshipEnded();
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync(SeededMesocycleRoute, UpdateBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_ShouldRejectADurationBelowADefinedWeek()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync(SeededMesocycleRoute, new
        {
            name = "Renamed block",
            objective = "New objective",
            startDate = "2026-10-05",
            durationInWeeks = 3,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await SeededMesocycleShouldBeUnchanged();
    }

    [Fact]
    public async Task Update_ShouldRejectADurationOutOfRange()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync(SeededMesocycleRoute, new
        {
            name = "Renamed block",
            objective = "New objective",
            startDate = "2026-10-05",
            durationInWeeks = 53,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await SeededMesocycleShouldBeUnchanged();
    }

    [Fact]
    public async Task DefineWeek_ShouldAddAWeekInAGap()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(WeeksRoute, AWeekBody(3));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().EndWith(SeededMesocycleRoute);
        var weeks = (await Body(response)).GetProperty("weeks").EnumerateArray().ToList();
        weeks.Select(week => week.GetProperty("weekNumber").GetInt32()).Should().Equal(1, 2, 3, 4);
        weeks[2].GetProperty("trainingWeekType").GetString().Should().Be("INTENSIFICATION");
        weeks[2].GetProperty("objective").GetString().Should().Be("Heavy triples");
    }

    [Fact]
    public async Task DefineWeek_ShouldLetAnAdminDefineAWeekOnAnyMesocycle()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(WeeksRoute, AWeekBody(3, "REALIZATION"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task DefineWeek_ShouldRejectAWeekAlreadyDefined()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(WeeksRoute, AWeekBody(2, "DELOAD"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await SeededMesocycleShouldBeUnchanged();
    }

    [Fact]
    public async Task DefineWeek_ShouldRejectAWeekBeyondTheDuration()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(WeeksRoute, AWeekBody(5));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await SeededMesocycleShouldBeUnchanged();
    }

    [Theory]
    [InlineData(0, "INTENSIFICATION")]
    [InlineData(3, "PEAKING")]
    public async Task DefineWeek_ShouldRejectAnInvalidNumberOrType(int weekNumber, string trainingWeekType)
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(WeeksRoute, AWeekBody(weekNumber, trainingWeekType));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await SeededMesocycleShouldBeUnchanged();
    }

    [Fact]
    public async Task RedefineWeek_ShouldReplaceTheTypeAndObjective()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync($"{WeeksRoute}/2", RedefineBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var second = (await Body(response)).GetProperty("weeks").EnumerateArray()
            .Single(week => week.GetProperty("weekNumber").GetInt32() == 2);
        second.GetProperty("trainingWeekType").GetString().Should().Be("DELOAD");
        second.GetProperty("objective").GetString().Should().Be("Back off");
    }

    [Fact]
    public async Task RedefineWeek_ShouldNotFindAWeekThatIsNotDefined()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync($"{WeeksRoute}/3", RedefineBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await SeededMesocycleShouldBeUnchanged();
    }

    [Fact]
    public async Task RedefineWeek_ShouldRejectABlankObjective()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync($"{WeeksRoute}/2", new { trainingWeekType = "DELOAD", objective = " " });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await SeededMesocycleShouldBeUnchanged();
    }

    [Fact]
    public async Task RemoveWeek_ShouldRemoveAWeekWithoutWorkoutsAndKeepTheRest()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.DeleteAsync($"{WeeksRoute}/4");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var mesocycle = await SeededMesocycle();
        mesocycle.GetProperty("durationInWeeks").GetInt32().Should().Be(SeedData.MesocycleDurationInWeeks);
        mesocycle.GetProperty("weeks").EnumerateArray()
            .Select(week => week.GetProperty("weekNumber").GetInt32())
            .Should().Equal(1, 2);
    }

    [Fact]
    public async Task RemoveWeek_ShouldLetAnAdminRemoveAWeekOfAnyMesocycle()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.DeleteAsync($"{WeeksRoute}/4");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SeededMesocycle()).GetProperty("weeks").EnumerateArray()
            .Select(week => week.GetProperty("weekNumber").GetInt32())
            .Should().Equal(1, 2);
    }

    [Fact]
    public async Task RemoveWeek_ShouldRejectAWeekWithAPlannedWorkout()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.DeleteAsync($"{WeeksRoute}/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await SeededMesocycleShouldBeUnchanged();
    }

    [Fact]
    public async Task RemoveWeek_ShouldRejectAWeekWithAStartedWorkout()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.DeleteAsync($"{WeeksRoute}/2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await SeededMesocycleShouldBeUnchanged();
    }

    [Fact]
    public async Task RemoveWeek_ShouldNotFindAWeekThatIsNotDefined()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.DeleteAsync($"{WeeksRoute}/3");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await SeededMesocycleShouldBeUnchanged();
    }

    [Fact]
    public async Task RemoveWeek_ShouldRejectAnInvalidWeekNumber()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.DeleteAsync($"{WeeksRoute}/0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await SeededMesocycleShouldBeUnchanged();
    }

    [Fact]
    public async Task RemoveWeek_ShouldLetTheWeekBeDefinedAgain()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        (await trainer.DeleteAsync($"{WeeksRoute}/4")).StatusCode
            .Should().Be(HttpStatusCode.NoContent, "the week must be removed first");

        // Act
        var response = await trainer.PostAsJsonAsync(WeeksRoute, AWeekBody(4, "REALIZATION"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var fourth = (await Body(response)).GetProperty("weeks").EnumerateArray()
            .Single(week => week.GetProperty("weekNumber").GetInt32() == 4);
        fourth.GetProperty("trainingWeekType").GetString().Should().Be("REALIZATION");
        fourth.GetProperty("objective").GetString().Should().Be("Heavy triples");
    }
}
