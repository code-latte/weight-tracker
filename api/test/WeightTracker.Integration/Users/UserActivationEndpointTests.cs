using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Persistence;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Users;

/// <summary>
/// <c>PUT /users/{id}/activate</c> and <c>PUT /users/{id}/deactivate</c>. Each protected-route case runs on
/// both routes; the activate cases start from a deactivated user so a wrongly accepted call would show.
/// </summary>
public class UserActivationEndpointTests : IntegrationTest
{
    private const string Activate = "activate";
    private const string Deactivate = "deactivate";

    public UserActivationEndpointTests(ApiFactory factory) : base(factory)
    { }

    private static string RouteFor(Guid userId, string action) => $"/users/{userId}/{action}";

    private Task<UserEntity> StoredUser(Guid userId)
        => Factory.WithScope(services => services.GetRequiredService<WeightTrackerDbContext>()
            .Users.AsNoTracking().SingleAsync(user => user.Id == userId));

    private async Task DeactivateAsAdmin(Guid userId)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var response = await admin.PutAsync(RouteFor(userId, Deactivate), null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Leaves the seeded athlete in the state the action would change, and returns that state.
    /// </summary>
    private async Task<UserEntity> GivenTargetFor(string action)
    {
        if (action == Activate)
            await DeactivateAsAdmin(SeedData.UserId);

        return await StoredUser(SeedData.UserId);
    }

    private async Task AssertUnchanged(UserEntity before)
    {
        var after = await StoredUser(before.Id);
        after.IsActive.Should().Be(before.IsActive);
        after.DeactivatedAtUtc.Should().Be(before.DeactivatedAtUtc);
    }

    [Theory]
    [InlineData(Activate)]
    [InlineData(Deactivate)]
    public async Task Put_ShouldRejectAnonymousCallersAndChangeNothing(string action)
    {
        // Arrange
        var before = await GivenTargetFor(action);

        // Act
        var response = await Client.PutAsync(RouteFor(SeedData.UserId, action), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AssertUnchanged(before);
    }

    [Theory]
    [InlineData(Activate)]
    [InlineData(Deactivate)]
    public async Task Put_ShouldRejectAGarbageTokenAndChangeNothing(string action)
    {
        // Arrange
        var before = await GivenTargetFor(action);
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.PutAsync(RouteFor(SeedData.UserId, action), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AssertUnchanged(before);
    }

    [Theory]
    [InlineData(Activate, SeedCredentials.User)]
    [InlineData(Activate, SeedCredentials.Trainer)]
    [InlineData(Deactivate, SeedCredentials.User)]
    [InlineData(Deactivate, SeedCredentials.Trainer)]
    public async Task Put_ShouldForbidNonAdminCallersAndChangeNothing(string action, string callerEmail)
    {
        // Arrange
        var client = await AuthenticatedClientFor(callerEmail);
        var before = await GivenTargetFor(action);

        // Act
        var response = await client.PutAsync(RouteFor(SeedData.UserId, action), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await AssertUnchanged(before);
    }

    [Theory]
    [InlineData(Activate)]
    [InlineData(Deactivate)]
    public async Task Put_ShouldReturnNotFoundForAnUnknownUser(string action)
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PutAsync(RouteFor(Guid.NewGuid(), action), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(Activate)]
    [InlineData(Deactivate)]
    public async Task Put_ShouldNotMatchAMalformedId(string action)
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PutAsync($"/users/not-a-guid/{action}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000001")]
    [InlineData("00000000-0000-0000-0000-000000000002")]
    public async Task Deactivate_ShouldSwitchTheUserOffAndReturnIt(string userId)
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PutAsync(RouteFor(Guid.Parse(userId), Deactivate), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(
            "id", "email", "name", "surname", "role", "isActive", "deactivatedAtUtc");
        body.GetProperty("id").GetGuid().Should().Be(Guid.Parse(userId));
        body.GetProperty("isActive").GetBoolean().Should().BeFalse();
        body.GetProperty("deactivatedAtUtc").ValueKind.Should().Be(JsonValueKind.String);

        var stored = await StoredUser(Guid.Parse(userId));
        stored.IsActive.Should().BeFalse();
        stored.DeactivatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Deactivate_ShouldRejectAnAlreadyDeactivatedUserAndKeepTheDate()
    {
        // Arrange
        await DeactivateAsAdmin(SeedData.UserId);
        var before = await StoredUser(SeedData.UserId);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PutAsync(RouteFor(SeedData.UserId, Deactivate), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await AssertUnchanged(before);
    }

    [Fact]
    public async Task Deactivate_ShouldForbidAnAdminTargetingThemselves()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PutAsync(RouteFor(SeedData.AdminId, Deactivate), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await StoredUser(SeedData.AdminId)).IsActive.Should().BeTrue();
        var login = await Client.PostAsJsonAsync(
            "/auth/login", new { email = SeedCredentials.Admin, password = SeedCredentials.Password });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Activate_ShouldSwitchADeactivatedUserBackOnAndReturnIt()
    {
        // Arrange
        await DeactivateAsAdmin(SeedData.UserId);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PutAsync(RouteFor(SeedData.UserId, Activate), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(
            "id", "email", "name", "surname", "role", "isActive", "deactivatedAtUtc");
        body.GetProperty("isActive").GetBoolean().Should().BeTrue();
        body.GetProperty("deactivatedAtUtc").ValueKind.Should().Be(JsonValueKind.Null);

        var stored = await StoredUser(SeedData.UserId);
        stored.IsActive.Should().BeTrue();
        stored.DeactivatedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task Activate_ShouldRejectAnActiveUser()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PutAsync(RouteFor(SeedData.UserId, Activate), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StoredUser(SeedData.UserId)).IsActive.Should().BeTrue();
    }
}
