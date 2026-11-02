using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.AthleteInvitations;

/// <summary>
/// <c>GET /athlete-invitations</c>. Scoped read: each case asserts what is excluded as well as what is listed.
/// </summary>
/// <remarks>
/// Baseline: the seeded trainer sent three invitations. A second trainer and their invitation are added where
/// exclusion is under test.
/// </remarks>
public class ListInvitationsEndpointTests : TrainerInvitationEndpointTest
{
    private const string OtherTrainerEmail = "other.trainer@weighttracker.test";

    private static readonly Guid[] SeededInvitationIds =
    [
        SeedData.PendingInvitationId, SeedData.ExpiredInvitationId, SeedData.UnregisteredInvitationId,
    ];

    public ListInvitationsEndpointTests(ApiFactory factory) : base(factory)
    { }

    private async Task<Guid> GivenAnotherTrainersInvitation()
    {
        await CreateUser(OtherTrainerEmail, Role.TRAINER);
        var otherTrainer = await AuthenticatedClientFor(OtherTrainerEmail);
        var response = await otherTrainer.PostAsJsonAsync(InvitationRoute, new { email = "someone.else@weighttracker.test" });
        response.EnsureSuccessStatusCode();

        return (await Body(response)).GetProperty("id").GetGuid();
    }

    /// <summary>
    /// Leaves the seeded trainer with one invitation in each status: accepted, rejected, revoked and pending
    /// (the seeded expired one, still stored as pending).
    /// </summary>
    private async Task GivenInvitationsInEveryStatus()
    {
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        (await user.PutAsJsonAsync(
            $"{InvitationRoute}/{SeedData.PendingInvitationId}/accept", new { token = SeedData.PendingInvitationToken }))
            .EnsureSuccessStatusCode();
        (await trainer.PutAsync($"{InvitationRoute}/{SeedData.UnregisteredInvitationId}/revoke", null))
            .EnsureSuccessStatusCode();

        await CreateUser("rejecting.athlete@weighttracker.test", Role.USER);
        var invited = await Body(await trainer.PostAsJsonAsync(
            InvitationRoute, new { email = "rejecting.athlete@weighttracker.test" }));
        var rejecting = await AuthenticatedClientFor("rejecting.athlete@weighttracker.test");
        (await rejecting.PutAsJsonAsync(
            $"{InvitationRoute}/{invited.GetProperty("id").GetGuid()}/reject",
            new { token = invited.GetProperty("token").GetString() }))
            .EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Get_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await Client.GetAsync(InvitationRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.GetAsync(InvitationRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_ShouldForbidAnAthleteAndReturnNoInvitations()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.GetAsync(InvitationRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.Content.ReadAsStringAsync()).Should().NotContain(SeedData.UnregisteredInvitationEmail);
    }

    [Fact]
    public async Task Get_ShouldListEveryTrainersInvitationsNewestFirstForAnAdmin()
    {
        // Arrange
        var othersId = await GivenAnotherTrainersInvitation();
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.GetAsync(InvitationRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await Items(response);
        items.Select(item => item.GetProperty("id").GetGuid())
            .Should().BeEquivalentTo([othersId, .. SeededInvitationIds]);
        items.First().GetProperty("id").GetGuid().Should().Be(othersId);
        items.Select(item => item.GetProperty("createdAtUtc").GetDateTime()).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task Get_ShouldListOnlyTheCallersInvitationsForATrainer()
    {
        // Arrange
        var othersId = await GivenAnotherTrainersInvitation();
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.GetAsync(InvitationRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ids = (await Items(response)).Select(item => item.GetProperty("id").GetGuid()).ToList();
        ids.Should().BeEquivalentTo(SeededInvitationIds);
        ids.Should().NotContain(othersId);
    }

    [Fact]
    public async Task Get_ShouldExposeOnlyTheListedFieldsAndNoSecret()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var storedHash = (await StoredInvitation(SeedData.PendingInvitationId)).TokenHash;

        // Act
        var response = await admin.GetAsync(InvitationRoute);

        // Assert
        var items = await Items(response);
        foreach (var item in items)
            ShouldHaveExactly(item,
                "id", "trainerId", "trainerName", "trainerSurname", "inviteEmail", "userId", "userName", "userSurname",
                "status", "createdAtUtc", "expiresAtUtc", "acceptedAtUtc");
        var registered = items.Single(item => item.GetProperty("id").GetGuid() == SeedData.PendingInvitationId);
        registered.GetProperty("trainerName").GetString().Should().Be("Grace");
        registered.GetProperty("trainerSurname").GetString().Should().Be("Hopper");
        registered.GetProperty("userName").GetString().Should().Be("Ada");
        registered.GetProperty("userSurname").GetString().Should().Be("Lovelace");
        var unregistered = items.Single(item => item.GetProperty("id").GetGuid() == SeedData.UnregisteredInvitationId);
        unregistered.GetProperty("userName").ValueKind.Should().Be(JsonValueKind.Null);
        unregistered.GetProperty("userSurname").ValueKind.Should().Be(JsonValueKind.Null);
        (await response.Content.ReadAsStringAsync()).Should()
            .NotContain(SeedData.PendingInvitationToken)
            .And.NotContain(storedHash);
    }

    [Theory]
    [InlineData(SeedCredentials.Admin)]
    [InlineData(SeedCredentials.Trainer)]
    public async Task Get_ShouldListInvitationsInEveryStatus(string callerEmail)
    {
        // Arrange
        await GivenInvitationsInEveryStatus();
        var client = await AuthenticatedClientFor(callerEmail);

        // Act
        var response = await client.GetAsync(InvitationRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await Items(response);
        items.Should().HaveCount(4);
        items.Select(item => item.GetProperty("status").GetString())
            .Should().BeEquivalentTo(["ACCEPTED", "REJECTED", "REVOKED", "PENDING"]);
        items.Single(item => item.GetProperty("id").GetGuid() == SeedData.PendingInvitationId)
            .GetProperty("status").GetString().Should().Be("ACCEPTED");
        items.Single(item => item.GetProperty("id").GetGuid() == SeedData.UnregisteredInvitationId)
            .GetProperty("status").GetString().Should().Be("REVOKED");
    }
}
