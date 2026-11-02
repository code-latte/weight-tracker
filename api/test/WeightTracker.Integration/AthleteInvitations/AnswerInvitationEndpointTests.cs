using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Domain.AthleteInvitations;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.AthleteInvitations;

/// <summary>
/// <c>PUT /athlete-invitations/{invitationId}/accept</c> and <c>/reject</c>, which share every rule but the
/// resulting status. Each case runs for both actions.
/// </summary>
/// <remarks>
/// The seeded pending and expired invitations go to the seeded user; the unregistered one has no user.
/// </remarks>
public class AnswerInvitationEndpointTests : TrainerInvitationEndpointTest
{
    public AnswerInvitationEndpointTests(ApiFactory factory) : base(factory)
    { }

    private static string Route(Guid invitationId, string action) => $"{InvitationRoute}/{invitationId}/{action}";

    private static object TokenBody(string token = SeedData.PendingInvitationToken) => new { token };

    private async Task<TrainerInvitationStatus> StatusOf(Guid invitationId)
        => (TrainerInvitationStatus)(await StoredInvitation(invitationId)).Status;

    [Theory]
    [InlineData("accept")]
    [InlineData("reject")]
    public async Task Put_ShouldRejectAnonymousCallersAndChangeNothing(string action)
    {
        // Arrange & Act
        var response = await Client.PutAsJsonAsync(Route(SeedData.PendingInvitationId, action), TokenBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await StatusOf(SeedData.PendingInvitationId)).Should().Be(TrainerInvitationStatus.PENDING);
    }

    [Theory]
    [InlineData("accept")]
    [InlineData("reject")]
    public async Task Put_ShouldRejectAGarbageTokenAndChangeNothing(string action)
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.PutAsJsonAsync(Route(SeedData.PendingInvitationId, action), TokenBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await StatusOf(SeedData.PendingInvitationId)).Should().Be(TrainerInvitationStatus.PENDING);
    }

    [Theory]
    [InlineData("accept", SeedCredentials.Trainer)]
    [InlineData("accept", SeedCredentials.Admin)]
    [InlineData("reject", SeedCredentials.Trainer)]
    [InlineData("reject", SeedCredentials.Admin)]
    public async Task Put_ShouldForbidTrainersAndAdmins(string action, string callerEmail)
    {
        // Arrange
        var client = await AuthenticatedClientFor(callerEmail);

        // Act
        var response = await client.PutAsJsonAsync(Route(SeedData.PendingInvitationId, action), TokenBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await StatusOf(SeedData.PendingInvitationId)).Should().Be(TrainerInvitationStatus.PENDING);
    }

    [Theory]
    [InlineData("accept")]
    [InlineData("reject")]
    public async Task Put_ShouldForbidAnAthleteWhoWasNotInvited(string action)
    {
        // Arrange
        await CreateUser("other.athlete@weighttracker.test", Role.USER);
        var client = await AuthenticatedClientFor("other.athlete@weighttracker.test");

        // Act
        var response = await client.PutAsJsonAsync(Route(SeedData.PendingInvitationId, action), TokenBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await StatusOf(SeedData.PendingInvitationId)).Should().Be(TrainerInvitationStatus.PENDING);
    }

    [Theory]
    [InlineData("accept")]
    [InlineData("reject")]
    public async Task Put_ShouldForbidEveryoneOnAnInvitationWithoutUser(string action)
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(
            Route(SeedData.UnregisteredInvitationId, action), TokenBody(SeedData.UnregisteredInvitationToken));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await StatusOf(SeedData.UnregisteredInvitationId)).Should().Be(TrainerInvitationStatus.PENDING);
    }

    [Theory]
    [InlineData("accept")]
    [InlineData("reject")]
    public async Task Put_ShouldForbidAWrongToken(string action)
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(Route(SeedData.PendingInvitationId, action), TokenBody("wrong-token"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await StatusOf(SeedData.PendingInvitationId)).Should().Be(TrainerInvitationStatus.PENDING);
    }

    [Theory]
    [InlineData("accept")]
    [InlineData("reject")]
    public async Task Put_ShouldRejectAMissingToken(string action)
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(Route(SeedData.PendingInvitationId, action), new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await StatusOf(SeedData.PendingInvitationId)).Should().Be(TrainerInvitationStatus.PENDING);
    }

    [Fact]
    public async Task Accept_ShouldAcceptAndRecordWhenWithoutExposingTheSecret()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(Route(SeedData.PendingInvitationId, "accept"), TokenBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await Body(response);
        body.GetProperty("status").GetString().Should().Be("ACCEPTED");
        body.GetProperty("trainerName").GetString().Should().Be("Grace");
        body.GetProperty("trainerSurname").GetString().Should().Be("Hopper");
        body.GetProperty("userName").GetString().Should().Be("Ada");
        body.GetProperty("userSurname").GetString().Should().Be("Lovelace");
        body.GetProperty("acceptedAtUtc").GetDateTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        body.TryGetProperty("token", out _).Should().BeFalse();
        (await response.Content.ReadAsStringAsync()).Should()
            .NotContain(SeedData.PendingInvitationToken)
            .And.NotContain((await StoredInvitation(SeedData.PendingInvitationId)).TokenHash);
        (await StatusOf(SeedData.PendingInvitationId)).Should().Be(TrainerInvitationStatus.ACCEPTED);
    }

    [Fact]
    public async Task Accept_ShouldNotLinkTheAthleteToTheTrainer()
    {
        // Arrange
        await CreateUser("new.athlete@weighttracker.test", Role.USER);
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var invited = await Body(await trainer.PostAsJsonAsync(InvitationRoute, new { email = "new.athlete@weighttracker.test" }));
        var athlete = await AuthenticatedClientFor("new.athlete@weighttracker.test");

        // Act
        var response = await athlete.PutAsJsonAsync(
            Route(invited.GetProperty("id").GetGuid(), "accept"), TokenBody(invited.GetProperty("token").GetString()!));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var listed = await Items(await trainer.GetAsync("/trainer-athletes"));
        listed.Select(item => item.GetProperty("email").GetString()).Should().NotContain("new.athlete@weighttracker.test");
    }

    [Fact]
    public async Task Reject_ShouldRejectWithoutAcceptanceDate()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(Route(SeedData.PendingInvitationId, "reject"), TokenBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await Body(response);
        body.GetProperty("status").GetString().Should().Be("REJECTED");
        body.GetProperty("trainerName").GetString().Should().Be("Grace");
        body.GetProperty("userName").GetString().Should().Be("Ada");
        body.GetProperty("acceptedAtUtc").ValueKind.Should().Be(JsonValueKind.Null);
        (await StatusOf(SeedData.PendingInvitationId)).Should().Be(TrainerInvitationStatus.REJECTED);
    }

    [Theory]
    [InlineData("accept")]
    [InlineData("reject")]
    public async Task Put_ShouldRejectAnAlreadyAcceptedInvitation(string action)
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);
        (await client.PutAsJsonAsync(Route(SeedData.PendingInvitationId, "accept"), TokenBody())).EnsureSuccessStatusCode();

        // Act
        var response = await client.PutAsJsonAsync(Route(SeedData.PendingInvitationId, action), TokenBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StatusOf(SeedData.PendingInvitationId)).Should().Be(TrainerInvitationStatus.ACCEPTED);
    }

    [Theory]
    [InlineData("accept")]
    [InlineData("reject")]
    public async Task Put_ShouldRejectAnExpiredInvitation(string action)
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(
            Route(SeedData.ExpiredInvitationId, action), TokenBody(SeedData.ExpiredInvitationToken));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StatusOf(SeedData.ExpiredInvitationId)).Should().Be(TrainerInvitationStatus.PENDING);
    }

    [Theory]
    [InlineData("accept")]
    [InlineData("reject")]
    public async Task Put_ShouldAnswerNotFoundForAnUnknownInvitation(string action)
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(Route(Guid.NewGuid(), action), TokenBody());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
