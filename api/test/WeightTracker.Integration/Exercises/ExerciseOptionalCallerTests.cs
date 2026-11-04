using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Exercises;

/// <summary>
/// The exercise reads are public but answer differently to an authenticated caller, so a token that is sent
/// has to be valid. Other public routes must keep ignoring the header.
/// </summary>
public class ExerciseOptionalCallerTests : IntegrationTest
{
    public ExerciseOptionalCallerTests(ApiFactory factory) : base(factory)
    { }

    [Fact]
    public async Task GetAll_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.GetAsync("/exercises");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_ShouldRejectAGarbageTokenEvenForASystemExercise()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.GetAsync($"/exercises/{SeedData.SentadillaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_ShouldRejectATokenWhoseUserNoLongerExists()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);
        await Factory.RemoveUser(SeedCredentials.Trainer);

        // Act
        var response = await client.GetAsync("/exercises");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_ShouldServeAnAnonymousCallerWithoutAnyHeader()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/exercises");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAll_ShouldServeAValidToken()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.GetAsync("/exercises");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_ShouldIgnoreAStaleAuthorizationHeader()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.PostAsJsonAsync("/auth/login", new { email = SeedCredentials.User, password = SeedCredentials.Password });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
