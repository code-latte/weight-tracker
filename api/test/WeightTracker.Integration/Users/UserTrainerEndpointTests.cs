using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Users;

/// <summary>
/// Covers <c>GET /users/{userId}/trainer</c>. The seeded user has an active relationship with the seeded trainer
/// (and an ended one with the same trainer), so it is the athlete with a trainer; the seeded trainer and admin cannot
/// be athletes. Every other state is arranged through the API.
/// </summary>
public class UserTrainerEndpointTests : IntegrationTest
{
    public UserTrainerEndpointTests(ApiFactory factory) : base(factory)
    { }

    private static string TrainerRoute(Guid userId) => $"/users/{userId}/trainer";

    /// <summary>
    /// Creates a user with the user role through <c>POST /users</c> as the admin and returns their id and email.
    /// They never had a trainer.
    /// </summary>
    private async Task<(Guid Id, string Email)> GivenAthleteWithoutTrainer()
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var email = $"{Guid.NewGuid():N}@weighttracker.test";

        var response = await admin.PostAsJsonAsync("/users", new
        {
            email,
            password = SeedCredentials.Password,
            role = (int)Role.USER,
            name = "Extra",
            surname = "User",
            dateBirth = "1990-01-01",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created, "the extra user must be arranged first");

        return ((await Body(response)).GetProperty("id").GetGuid(), email);
    }

    private static async Task<JsonElement> Body(HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

    private static void ShouldBeTheSeededTrainer(JsonElement trainer)
    {
        trainer.GetProperty("id").GetGuid().Should().Be(SeedData.TrainerId);
        trainer.GetProperty("email").GetString().Should().Be(SeedData.TrainerEmail);
        trainer.GetProperty("role").GetString().Should().Be("TRAINER");
        trainer.EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo("id", "email", "name", "surname", "role");
    }

    private static async Task ShouldBeEmpty(HttpResponseMessage response)
        => (await response.Content.ReadAsStringAsync()).Should().BeEmpty();

    [Fact]
    public async Task Get_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await Client.GetAsync(TrainerRoute(SeedData.UserId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.GetAsync(TrainerRoute(SeedData.UserId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_ShouldGiveTheUserTheTrainerOfTheirActiveRelationship()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.GetAsync(TrainerRoute(SeedData.UserId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ShouldBeTheSeededTrainer(await Body(response));
    }

    [Fact]
    public async Task Get_ShouldGiveAnAdminTheTrainerOfAnAthlete()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.GetAsync(TrainerRoute(SeedData.UserId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ShouldBeTheSeededTrainer(await Body(response));
    }

    [Fact]
    public async Task Get_ShouldStillGiveTheUserADeactivatedTrainer()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        (await admin.PutAsync($"/users/{SeedData.TrainerId}/deactivate", null)).StatusCode
            .Should().Be(HttpStatusCode.OK, "the trainer must be deactivated first");
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.GetAsync(TrainerRoute(SeedData.UserId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ShouldBeTheSeededTrainer(await Body(response));
    }

    [Fact]
    public async Task Get_ShouldAnswerNoContentOnceTheRelationshipHasEnded()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        (await user.PutAsJsonAsync($"/trainer-athletes/{SeedData.TrainerId}/end", new { athleteId = SeedData.UserId }))
            .StatusCode.Should().Be(HttpStatusCode.OK, "the relationship must be ended first");

        // Act
        var response = await user.GetAsync(TrainerRoute(SeedData.UserId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await ShouldBeEmpty(response);
    }

    [Fact]
    public async Task Get_ShouldAnswerNoContentForAUserWhoNeverHadATrainer()
    {
        // Arrange
        var (athleteId, email) = await GivenAthleteWithoutTrainer();
        var athlete = await AuthenticatedClientFor(email);

        // Act
        var response = await athlete.GetAsync(TrainerRoute(athleteId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await ShouldBeEmpty(response);
    }

    [Fact]
    public async Task Get_ShouldAnswerNoContentWhenAnAdminAsksAboutATrainer()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.GetAsync(TrainerRoute(SeedData.TrainerId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await ShouldBeEmpty(response);
    }

    [Fact]
    public async Task Get_ShouldForbidAUserAskingAboutAnotherUser()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.GetAsync(TrainerRoute(SeedData.TrainerId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_ShouldForbidAUserAskingAboutAnUnknownIdWithoutRevealingIt()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.GetAsync(TrainerRoute(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_ShouldForbidATrainerAskingAboutTheirAthlete()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.GetAsync(TrainerRoute(SeedData.UserId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_ShouldForbidATrainerAskingAboutThemselves()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.GetAsync(TrainerRoute(SeedData.TrainerId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFoundWhenAnAdminAsksAboutAnUnknownUser()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.GetAsync(TrainerRoute(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
