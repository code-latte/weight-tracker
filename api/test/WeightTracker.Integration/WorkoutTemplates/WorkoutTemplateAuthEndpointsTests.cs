using System.Net;
using FluentAssertions;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.WorkoutTemplates;

/// <summary>
/// The access matrix of every <c>/workout-templates</c> route: no token, invalid token, regular user, unknown
/// template and another trainer's template. Routes are written with an <c>{id}</c> placeholder so the same list
/// serves the seeded template, an unknown id and a template of another trainer.
/// </summary>
public class WorkoutTemplateAuthEndpointsTests : WorkoutTemplateEndpointTest
{
    public WorkoutTemplateAuthEndpointsTests(ApiFactory factory) : base(factory)
    { }

    /// <summary>Every route that names a template.</summary>
    public static TheoryData<string, string> TemplateRoutes() => new()
    {
        { "GET", "/workout-templates/{id}" },
        { "PUT", "/workout-templates/{id}" },
        { "DELETE", "/workout-templates/{id}" },
        { "POST", "/workout-templates/{id}/exercises" },
        { "PUT", $"/workout-templates/{{id}}/exercises/{SeedData.SentadillaId}" },
        { "DELETE", $"/workout-templates/{{id}}/exercises/{SeedData.SentadillaId}" },
        { "PUT", "/workout-templates/{id}/exercises/order" },
        { "POST", "/workout-templates/{id}/superseries" },
        { "DELETE", "/workout-templates/{id}/superseries/1" },
        { "POST", "/workout-templates/{id}/workouts" },
    };

    /// <summary>Every route, including the two that name no template.</summary>
    public static TheoryData<string, string> AllRoutes()
    {
        var routes = new TheoryData<string, string>
        {
            { "GET", "/workout-templates" },
            { "POST", "/workout-templates" },
        };

        foreach (var route in TemplateRoutes())
            routes.Add((string)route[0], (string)route[1]);

        return routes;
    }

    private static string Resolve(string route, Guid templateId) => route.Replace("{id}", templateId.ToString());

    private static object? BodyFor(string method, string route) => (method, route) switch
    {
        ("POST", "/workout-templates") => new { name = "New template" },
        ("PUT", "/workout-templates/{id}") => new { name = "Renamed" },
        ("POST", var post) when post.EndsWith("/exercises") => AnExerciseBody(SeedData.CurlBicepsId, "MID", Reps(3, 10)),
        ("PUT", var put) when put.EndsWith("/order") => new { exerciseIds = new[] { SeedData.PlanchaId, SeedData.SentadillaId, SeedData.PressBancaId } },
        ("PUT", _) => AnExerciseBody(SeedData.SentadillaId, "MID", Reps(4, 6)),
        ("POST", var post) when post.EndsWith("/superseries") => new { exerciseIds = new[] { SeedData.TrainerTemplatePlanchaEntryId, Guid.NewGuid() } },
        ("POST", var post) when post.EndsWith("/workouts") => new { userId = SeedData.UserId, workoutDate = "2026-12-01" },
        _ => null,
    };

    [Theory]
    [MemberData(nameof(AllRoutes))]
    public async Task Route_ShouldRejectAnonymousCallersAndChangeNothing(string method, string route)
    {
        // Arrange
        var anonymous = Client;

        // Act
        var response = await Send(anonymous, method, Resolve(route, SeedData.TrainerTemplateId), BodyFor(method, route));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await SeededTemplateShouldBeUnchanged();
    }

    [Theory]
    [MemberData(nameof(AllRoutes))]
    public async Task Route_ShouldRejectAnInvalidTokenAndChangeNothing(string method, string route)
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Send(Client, method, Resolve(route, SeedData.TrainerTemplateId), BodyFor(method, route));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await SeededTemplateShouldBeUnchanged();
    }

    [Theory]
    [MemberData(nameof(AllRoutes))]
    public async Task Route_ShouldForbidARegularUserAndChangeNothing(string method, string route)
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await Send(user, method, Resolve(route, SeedData.TrainerTemplateId), BodyFor(method, route));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await SeededTemplateShouldBeUnchanged();
    }

    [Theory]
    [MemberData(nameof(TemplateRoutes))]
    public async Task Route_ShouldNotFindAnUnknownTemplate(string method, string route)
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await Send(admin, method, Resolve(route, Guid.NewGuid()), BodyFor(method, route));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [MemberData(nameof(TemplateRoutes))]
    public async Task Route_ShouldNotFindAnotherTrainersTemplateAndChangeNothing(string method, string route)
    {
        // Arrange
        var (_, otherTrainerEmail) = await GivenUser(Role.TRAINER);
        var otherTrainer = await AuthenticatedClientFor(otherTrainerEmail);

        // Act
        var response = await Send(otherTrainer, method, Resolve(route, SeedData.TrainerTemplateId), BodyFor(method, route));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await SeededTemplateShouldBeUnchanged();
    }
}
