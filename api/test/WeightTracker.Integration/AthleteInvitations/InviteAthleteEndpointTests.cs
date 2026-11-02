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
/// <c>POST /athlete-invitations</c>. The seeded user already has an active trainer, so registered invitees are
/// created per test.
/// </summary>
public class InviteAthleteEndpointTests : TrainerInvitationEndpointTest
{
    private const string NewAthleteEmail = "new.athlete@weighttracker.test";
    private const string UnregisteredEmail = "nobody.yet@weighttracker.test";

    /// <summary><c>TrainerInvitations:ExpirationDays</c> in appsettings.json.</summary>
    private const int ExpirationDays = 7;

    public InviteAthleteEndpointTests(ApiFactory factory) : base(factory)
    { }

    private async Task<HttpResponseMessage> InviteAsTrainer(string email)
    {
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        return await trainer.PostAsJsonAsync(InvitationRoute, new { email });
    }

    [Fact]
    public async Task Post_ShouldRejectAnonymousCallersAndCreateNothing()
    {
        // Arrange
        var before = await StoredInvitationCount();

        // Act
        var response = await Client.PostAsJsonAsync(InvitationRoute, new { email = UnregisteredEmail });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await StoredInvitationCount()).Should().Be(before);
    }

    [Fact]
    public async Task Post_ShouldRejectAGarbageTokenAndCreateNothing()
    {
        // Arrange
        var before = await StoredInvitationCount();
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.PostAsJsonAsync(InvitationRoute, new { email = UnregisteredEmail });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await StoredInvitationCount()).Should().Be(before);
    }

    [Theory]
    [InlineData(SeedCredentials.User)]
    [InlineData(SeedCredentials.Admin)]
    public async Task Post_ShouldForbidNonTrainersAndCreateNothing(string callerEmail)
    {
        // Arrange
        var before = await StoredInvitationCount();
        var client = await AuthenticatedClientFor(callerEmail);

        // Act
        var response = await client.PostAsJsonAsync(InvitationRoute, new { email = UnregisteredEmail });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await StoredInvitationCount()).Should().Be(before);
    }

    [Fact]
    public async Task Post_ShouldInviteARegisteredAthleteAndReturnTheTokenOnce()
    {
        // Arrange
        var athleteId = await CreateUser(NewAthleteEmail, Role.USER, "Marie", "Curie");

        // Act
        var response = await InviteAsTrainer(NewAthleteEmail);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await Body(response);
        ShouldHaveExactly(body,
            "id", "trainerId", "trainerName", "trainerSurname", "inviteEmail", "userId", "userName", "userSurname",
            "status", "createdAtUtc", "expiresAtUtc", "token");
        body.GetProperty("trainerName").GetString().Should().Be("Grace");
        body.GetProperty("trainerSurname").GetString().Should().Be("Hopper");
        body.GetProperty("userName").GetString().Should().Be("Marie");
        body.GetProperty("userSurname").GetString().Should().Be("Curie");
        body.GetProperty("trainerId").GetGuid().Should().Be(SeedData.TrainerId);
        body.GetProperty("inviteEmail").GetString().Should().Be(NewAthleteEmail);
        body.GetProperty("userId").GetGuid().Should().Be(athleteId);
        body.GetProperty("status").GetString().Should().Be("PENDING");
        var token = body.GetProperty("token").GetString();
        token.Should().NotBeNullOrWhiteSpace();
        var stored = await StoredInvitation(body.GetProperty("id").GetGuid());
        stored.TokenHash.Should().NotBe(token);
        stored.Status.Should().Be((int)TrainerInvitationStatus.PENDING);
    }

    [Fact]
    public async Task Post_ShouldExpireTheConfiguredNumberOfDaysAfterCreation()
    {
        // Arrange
        await CreateUser(NewAthleteEmail, Role.USER);

        // Act
        var response = await InviteAsTrainer(NewAthleteEmail);

        // Assert
        var body = await Body(response);
        body.GetProperty("expiresAtUtc").GetDateTime()
            .Should().Be(body.GetProperty("createdAtUtc").GetDateTime().AddDays(ExpirationDays));
    }

    [Fact]
    public async Task Post_ShouldInviteAnUnregisteredEmailWithoutUser()
    {
        // Arrange
        var before = await StoredInvitationCount();

        // Act
        var response = await InviteAsTrainer(UnregisteredEmail);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await Body(response);
        body.GetProperty("userId").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("userName").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("userSurname").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("trainerName").GetString().Should().Be("Grace");
        body.GetProperty("status").GetString().Should().Be("PENDING");
        (await StoredInvitationCount()).Should().Be(before + 1);
    }

    [Theory]
    [InlineData(SeedCredentials.Trainer)]
    [InlineData(SeedCredentials.Admin)]
    public async Task Post_ShouldRejectTheEmailOfATrainerOrAnAdmin(string invitedEmail)
    {
        // Arrange
        var before = await StoredInvitationCount();

        // Act
        var response = await InviteAsTrainer(invitedEmail);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StoredInvitationCount()).Should().Be(before);
    }

    [Fact]
    public async Task Post_ShouldRejectTheEmailOfADeactivatedAthlete()
    {
        // Arrange
        var athleteId = await CreateUser(NewAthleteEmail, Role.USER);
        await Deactivate(athleteId);
        var before = await StoredInvitationCount();

        // Act
        var response = await InviteAsTrainer(NewAthleteEmail);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StoredInvitationCount()).Should().Be(before);
    }

    [Fact]
    public async Task Post_ShouldRejectAnAthleteWhoAlreadyHasAnActiveTrainer()
    {
        // Arrange
        var before = await StoredInvitationCount();

        // Act
        var response = await InviteAsTrainer(SeedCredentials.User);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StoredInvitationCount()).Should().Be(before);
    }

    [Fact]
    public async Task Post_ShouldRejectADuplicatedPendingInvitation()
    {
        // Arrange
        var before = await StoredInvitationCount();

        // Act
        var response = await InviteAsTrainer(SeedData.UnregisteredInvitationEmail);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StoredInvitationCount()).Should().Be(before);
    }

    [Fact]
    public async Task Post_ShouldInviteAgainOnceThePreviousInvitationWasRevoked()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        (await trainer.PutAsync($"{InvitationRoute}/{SeedData.UnregisteredInvitationId}/revoke", null))
            .EnsureSuccessStatusCode();

        // Act
        var response = await trainer.PostAsJsonAsync(InvitationRoute, new { email = SeedData.UnregisteredInvitationEmail });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Post_ShouldNotBeBlockedByAnotherTrainersPendingInvitation()
    {
        // Arrange
        await CreateUser("other.trainer@weighttracker.test", Role.TRAINER);
        var otherTrainer = await AuthenticatedClientFor("other.trainer@weighttracker.test");

        // Act
        var response = await otherTrainer.PostAsJsonAsync(InvitationRoute, new { email = SeedData.UnregisteredInvitationEmail });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Post_ShouldRejectAMalformedEmail()
    {
        // Arrange
        var before = await StoredInvitationCount();

        // Act
        var response = await InviteAsTrainer("not-an-email");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await StoredInvitationCount()).Should().Be(before);
    }

    [Fact]
    public async Task Post_ShouldRejectAMissingEmail()
    {
        // Arrange
        var before = await StoredInvitationCount();
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(InvitationRoute, new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await StoredInvitationCount()).Should().Be(before);
    }
}
