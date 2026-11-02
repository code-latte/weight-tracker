using FluentAssertions;
using WeightTracker.Domain.AthleteInvitations;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using Xunit;

namespace WeightTracker.Unit.Domain.AthleteInvitations;

public class TrainerInvitationTests
{
    private static readonly Id TrainerId = Id.New();
    private static readonly Email InviteEmail = Email.From("athlete@weighttracker.test");
    private static readonly DateTime CreatedAt = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ExpiresAt = CreatedAt.AddDays(7);
    private static readonly DateTime BeforeExpiry = CreatedAt.AddDays(1);
    private const string TokenHash = "0f1e2d3c4b5a69788796a5b4c3d2e1f00f1e2d3c4b5a69788796a5b4c3d2e1f0";

    private static TrainerInvitation APendingInvitation(Id? userId = null)
        => TrainerInvitation.Create(TrainerId, InviteEmail, userId, TokenHash, CreatedAt, ExpiresAt);

    private static TrainerInvitation AnInvitationIn(TrainerInvitationStatus status)
        => TrainerInvitation.Rehydrate(
            Id.New(), TrainerId, InviteEmail, Id.New(), TokenHash, status, CreatedAt, ExpiresAt,
            status == TrainerInvitationStatus.ACCEPTED ? BeforeExpiry : null);

    public static TheoryData<TrainerInvitationStatus, string> FinalStatusesAndActions()
    {
        var data = new TheoryData<TrainerInvitationStatus, string>();
        foreach (var status in new[]
                 {
                     TrainerInvitationStatus.ACCEPTED, TrainerInvitationStatus.REJECTED, TrainerInvitationStatus.REVOKED,
                 })
            foreach (var action in new[] { "revoke", "accept", "reject" })
                data.Add(status, action);
        return data;
    }

    private static void Apply(TrainerInvitation invitation, string action, DateTime nowUtc)
    {
        switch (action)
        {
            case "revoke": invitation.Revoke(nowUtc); break;
            case "accept": invitation.Accept(nowUtc); break;
            case "reject": invitation.Reject(nowUtc); break;
        }
    }

    [Fact]
    public void Create_ShouldStartPendingWithEveryProvidedField()
    {
        // Arrange
        var userId = Id.New();

        // Act
        var invitation = APendingInvitation(userId);

        // Assert
        invitation.IsNew.Should().BeTrue();
        invitation.Id.Value.Should().NotBe(Guid.Empty);
        invitation.TrainerId.Should().Be(TrainerId);
        invitation.InviteEmail.Should().Be(InviteEmail);
        invitation.UserId.Should().Be(userId);
        invitation.TokenHash.Should().Be(TokenHash);
        invitation.Status.Should().Be(TrainerInvitationStatus.PENDING);
        invitation.CreatedAtUtc.Should().Be(CreatedAt);
        invitation.ExpiresAtUtc.Should().Be(ExpiresAt);
        invitation.AcceptedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldAllowAnUnregisteredEmail()
    {
        // Arrange & Act
        var invitation = APendingInvitation();

        // Assert
        invitation.UserId.Should().BeNull();
        invitation.Status.Should().Be(TrainerInvitationStatus.PENDING);
    }

    [Fact]
    public void Rehydrate_ShouldRestoreEveryFieldAndNotBeNew()
    {
        // Arrange
        var id = Id.New();
        var acceptedAt = BeforeExpiry;

        // Act
        var invitation = TrainerInvitation.Rehydrate(
            id, TrainerId, InviteEmail, null, TokenHash, TrainerInvitationStatus.ACCEPTED, CreatedAt, ExpiresAt, acceptedAt);

        // Assert
        invitation.IsNew.Should().BeFalse();
        invitation.Id.Should().Be(id);
        invitation.UserId.Should().BeNull();
        invitation.Status.Should().Be(TrainerInvitationStatus.ACCEPTED);
        invitation.AcceptedAtUtc.Should().Be(acceptedAt);
    }

    [Fact]
    public void Revoke_ShouldRevokeAPendingInvitation()
    {
        // Arrange
        var invitation = APendingInvitation(Id.New());

        // Act
        invitation.Revoke(BeforeExpiry);

        // Assert
        invitation.Status.Should().Be(TrainerInvitationStatus.REVOKED);
        invitation.AcceptedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Accept_ShouldAcceptAPendingInvitationAndRecordWhen()
    {
        // Arrange
        var invitation = APendingInvitation(Id.New());

        // Act
        invitation.Accept(BeforeExpiry);

        // Assert
        invitation.Status.Should().Be(TrainerInvitationStatus.ACCEPTED);
        invitation.AcceptedAtUtc.Should().Be(BeforeExpiry);
    }

    [Fact]
    public void Reject_ShouldRejectAPendingInvitation()
    {
        // Arrange
        var invitation = APendingInvitation(Id.New());

        // Act
        invitation.Reject(BeforeExpiry);

        // Assert
        invitation.Status.Should().Be(TrainerInvitationStatus.REJECTED);
        invitation.AcceptedAtUtc.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(FinalStatusesAndActions))]
    public void Actions_ShouldRejectAnInvitationThatIsNoLongerPending(TrainerInvitationStatus status, string action)
    {
        // Arrange
        var invitation = AnInvitationIn(status);

        // Act
        var act = () => Apply(invitation, action, BeforeExpiry);

        // Assert
        act.Should().Throw<InvalidTrainerInvitationStatusTransitionException>();
        invitation.Status.Should().Be(status);
    }

    [Theory]
    [InlineData("revoke", 0)]
    [InlineData("accept", 0)]
    [InlineData("reject", 0)]
    [InlineData("revoke", 1)]
    [InlineData("accept", 1)]
    [InlineData("reject", 1)]
    public void Actions_ShouldRejectAnExpiredInvitation(string action, int daysPastExpiry)
    {
        // Arrange
        var invitation = APendingInvitation(Id.New());

        // Act
        var act = () => Apply(invitation, action, ExpiresAt.AddDays(daysPastExpiry));

        // Assert
        act.Should().Throw<TrainerInvitationExpiredException>();
        invitation.Status.Should().Be(TrainerInvitationStatus.PENDING);
        invitation.AcceptedAtUtc.Should().BeNull();
    }

    [Fact]
    public void IsPendingAt_ShouldBeTrueBeforeExpiry()
    {
        // Arrange
        var invitation = APendingInvitation();

        // Act
        var isPending = invitation.IsPendingAt(BeforeExpiry);

        // Assert
        isPending.Should().BeTrue();
    }

    [Fact]
    public void IsPendingAt_ShouldBeFalseOnceExpired()
    {
        // Arrange
        var invitation = APendingInvitation();

        // Act
        var isPending = invitation.IsPendingAt(ExpiresAt);

        // Assert
        isPending.Should().BeFalse();
    }

    [Fact]
    public void IsPendingAt_ShouldBeFalseForAFinalStatus()
    {
        // Arrange
        var invitation = AnInvitationIn(TrainerInvitationStatus.REVOKED);

        // Act
        var isPending = invitation.IsPendingAt(BeforeExpiry);

        // Assert
        isPending.Should().BeFalse();
    }

    [Theory]
    [InlineData(TokenHash, true)]
    [InlineData("ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff", false)]
    [InlineData("short", false)]
    public void MatchesToken_ShouldOnlyMatchTheStoredHash(string candidate, bool expected)
    {
        // Arrange
        var invitation = APendingInvitation();

        // Act
        var matches = invitation.MatchesToken(candidate);

        // Assert
        matches.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldRejectAnEmptyTokenHash(string tokenHash)
    {
        // Arrange & Act
        var act = () => TrainerInvitation.Create(TrainerId, InviteEmail, null, tokenHash, CreatedAt, ExpiresAt);

        // Assert
        act.Should().Throw<InvalidTrainerInvitationStateException>();
    }

    [Fact]
    public void Create_ShouldRejectATokenHashLongerThanTheLimit()
    {
        // Arrange
        var tooLong = new string('a', HashedTokens.MaxLength + 1);

        // Act
        var act = () => TrainerInvitation.Create(TrainerId, InviteEmail, null, tooLong, CreatedAt, ExpiresAt);

        // Assert
        act.Should().Throw<InvalidTrainerInvitationStateException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldRejectAnExpiryNotAfterCreation(int expiresInDays)
    {
        // Arrange & Act
        var act = () => TrainerInvitation.Create(
            TrainerId, InviteEmail, null, TokenHash, CreatedAt, CreatedAt.AddDays(expiresInDays));

        // Assert
        act.Should().Throw<InvalidTrainerInvitationStateException>();
    }

    [Fact]
    public void Rehydrate_ShouldRejectAnAcceptedInvitationWithoutAcceptanceDate()
    {
        // Arrange & Act
        var act = () => TrainerInvitation.Rehydrate(
            Id.New(), TrainerId, InviteEmail, Id.New(), TokenHash, TrainerInvitationStatus.ACCEPTED,
            CreatedAt, ExpiresAt, null);

        // Assert
        act.Should().Throw<InvalidTrainerInvitationStateException>();
    }

    [Theory]
    [InlineData(TrainerInvitationStatus.PENDING)]
    [InlineData(TrainerInvitationStatus.REJECTED)]
    [InlineData(TrainerInvitationStatus.REVOKED)]
    public void Rehydrate_ShouldRejectAnAcceptanceDateOnAnInvitationThatWasNotAccepted(TrainerInvitationStatus status)
    {
        // Arrange & Act
        var act = () => TrainerInvitation.Rehydrate(
            Id.New(), TrainerId, InviteEmail, Id.New(), TokenHash, status, CreatedAt, ExpiresAt, BeforeExpiry);

        // Assert
        act.Should().Throw<InvalidTrainerInvitationStateException>();
    }

    [Fact]
    public void Rehydrate_ShouldRejectAnExpiryNotAfterCreation()
    {
        // Arrange & Act
        var act = () => TrainerInvitation.Rehydrate(
            Id.New(), TrainerId, InviteEmail, null, TokenHash, TrainerInvitationStatus.PENDING,
            CreatedAt, CreatedAt, null);

        // Assert
        act.Should().Throw<InvalidTrainerInvitationStateException>();
    }

    [Fact]
    public void Rehydrate_ShouldRejectAnEmptyTokenHash()
    {
        // Arrange & Act
        var act = () => TrainerInvitation.Rehydrate(
            Id.New(), TrainerId, InviteEmail, null, "", TrainerInvitationStatus.PENDING, CreatedAt, ExpiresAt, null);

        // Assert
        act.Should().Throw<InvalidTrainerInvitationStateException>();
    }
}
