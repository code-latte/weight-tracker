using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WeightTracker.Domain.AthleteInvitations;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.AthleteInvitations;

/// <summary>
/// <c>PUT /athlete-invitations/{invitationId}/revoke</c>. The seeded trainer sent every seeded invitation.
/// </summary>
public class RevokeInvitationEndpointTests : TrainerInvitationEndpointTest
{
    public RevokeInvitationEndpointTests(ApiFactory factory) : base(factory)
    { }

    private static string Route(Guid invitationId) => $"{InvitationRoute}/{invitationId}/revoke";

    private async Task<TrainerInvitationStatus> StatusOf(Guid invitationId)
        => (TrainerInvitationStatus)(await StoredInvitation(invitationId)).Status;

    [Fact]
    public async Task Put_ShouldRejectAnonymousCallersAndChangeNothing()
    {
        // Arrange & Act
        var response = await Client.PutAsync(Route(SeedData.PendingInvitationId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await StatusOf(SeedData.PendingInvitationId)).Should().Be(TrainerInvitationStatus.PENDING);
    }

    [Fact]
    public async Task Put_ShouldRejectAGarbageTokenAndChangeNothing()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.PutAsync(Route(SeedData.PendingInvitationId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await StatusOf(SeedData.PendingInvitationId)).Should().Be(TrainerInvitationStatus.PENDING);
    }

    [Theory]
    [InlineData(SeedCredentials.Admin)]
    [InlineData(SeedCredentials.User)]
    public async Task Put_ShouldForbidAdminsAndAthletes(string callerEmail)
    {
        // Arrange
        var client = await AuthenticatedClientFor(callerEmail);

        // Act
        var response = await client.PutAsync(Route(SeedData.PendingInvitationId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await StatusOf(SeedData.PendingInvitationId)).Should().Be(TrainerInvitationStatus.PENDING);
    }

    [Fact]
    public async Task Put_ShouldForbidATrainerWhoDidNotSendIt()
    {
        // Arrange
        await CreateUser("other.trainer@weighttracker.test", Role.TRAINER);
        var otherTrainer = await AuthenticatedClientFor("other.trainer@weighttracker.test");

        // Act
        var response = await otherTrainer.PutAsync(Route(SeedData.PendingInvitationId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await StatusOf(SeedData.PendingInvitationId)).Should().Be(TrainerInvitationStatus.PENDING);
    }

    [Fact]
    public async Task Put_ShouldRevokeAPendingInvitationWithoutExposingItsSecret()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsync(Route(SeedData.PendingInvitationId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await Body(response);
        ShouldHaveExactly(body,
            "id", "trainerId", "trainerName", "trainerSurname", "inviteEmail", "userId", "userName", "userSurname",
            "status", "createdAtUtc", "expiresAtUtc", "acceptedAtUtc");
        body.GetProperty("trainerName").GetString().Should().Be("Grace");
        body.GetProperty("trainerSurname").GetString().Should().Be("Hopper");
        body.GetProperty("userName").GetString().Should().Be("Ada");
        body.GetProperty("userSurname").GetString().Should().Be("Lovelace");
        body.GetProperty("status").GetString().Should().Be("REVOKED");
        (await response.Content.ReadAsStringAsync()).Should()
            .NotContain(SeedData.PendingInvitationToken)
            .And.NotContain((await StoredInvitation(SeedData.PendingInvitationId)).TokenHash);
        (await StatusOf(SeedData.PendingInvitationId)).Should().Be(TrainerInvitationStatus.REVOKED);
    }

    [Fact]
    public async Task Put_ShouldRejectAnInvitationThatWasAlreadyRejected()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        (await user.PutAsJsonAsync(
            $"{InvitationRoute}/{SeedData.PendingInvitationId}/reject", new { token = SeedData.PendingInvitationToken }))
            .EnsureSuccessStatusCode();
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsync(Route(SeedData.PendingInvitationId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StatusOf(SeedData.PendingInvitationId)).Should().Be(TrainerInvitationStatus.REJECTED);
    }

    [Fact]
    public async Task Put_ShouldRejectAnExpiredInvitation()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsync(Route(SeedData.ExpiredInvitationId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StatusOf(SeedData.ExpiredInvitationId)).Should().Be(TrainerInvitationStatus.PENDING);
    }

    [Fact]
    public async Task Put_ShouldAnswerNotFoundForAnUnknownInvitation()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsync(Route(Guid.NewGuid()), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
