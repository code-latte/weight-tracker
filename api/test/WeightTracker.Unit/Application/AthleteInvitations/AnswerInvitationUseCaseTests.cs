using FluentAssertions;
using Moq;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.AthleteInvitations;
using WeightTracker.Domain.AthleteInvitations;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Application.AthleteInvitations;

/// <summary>
/// Accept, reject and revoke share their shape: load, check who is asking, (token,) act on the aggregate, save.
/// </summary>
public class AnswerInvitationUseCaseTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 10, 0, 0, DateTimeKind.Utc);

    private static readonly Id TrainerId = Id.New();
    private static readonly Id AthleteId = Id.New();

    private const string RawToken = "raw-token";
    private const string TokenHash = "token-hash";

    private readonly Mock<ITrainerInvitationRepository> _invitations = new(MockBehavior.Strict);
    private readonly Mock<IInvitationTokenService> _tokens = new(MockBehavior.Strict);
    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    public AnswerInvitationUseCaseTests()
    {
        _clock.Setup(clock => clock.UtcNow).Returns(Now);
        _tokens.Setup(tokens => tokens.Hash(RawToken)).Returns(TokenHash);
        _tokens.Setup(tokens => tokens.Hash(It.Is<string>(raw => raw != RawToken))).Returns("another-hash");
        _users
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([AStoredUser(TrainerId, "Grace", Role.TRAINER), AStoredUser(AthleteId, "Ada", Role.USER)]);
    }

    private static User AStoredUser(Id id, string name, Role role) => User.Rehydrate(
        id, Email.From($"{name.ToLowerInvariant()}@weighttracker.test"), PersonName.From(name), PersonName.From("Lovelace"),
        new DateOnly(1988, 5, 12), role, PasswordHash.From("stored-hash"), Now.AddDays(-30), true, null);

    private static TrainerInvitation AnInvitation(
        Id? userId,
        TrainerInvitationStatus status = TrainerInvitationStatus.PENDING,
        int expiresInDays = 7)
        => TrainerInvitation.Rehydrate(
            Id.New(), TrainerId, Email.From("athlete@weighttracker.test"), userId, TokenHash, status,
            Now.AddDays(-10), Now.AddDays(expiresInDays), null);

    private void GivenStored(TrainerInvitation invitation)
        => _invitations.Setup(repository => repository.GetById(invitation.Id, It.IsAny<CancellationToken>())).ReturnsAsync(invitation);

    private void GivenSaveAndCommitSucceed(TrainerInvitation invitation)
    {
        _invitations.Setup(repository => repository.Save(invitation, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private void VerifyNothingSavedOrCommitted()
    {
        _invitations.Verify(
            repository => repository.Save(It.IsAny<TrainerInvitation>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.VerifyNoOtherCalls();
    }

    private InvitationViewLoader Views() => new(_users.Object);

    private Task<InvitationView> Answer(string action, Id callerId, Id invitationId, string rawToken = RawToken)
        => action switch
        {
            "accept" => new AcceptInvitationUseCase(
                    _invitations.Object, Views(), _tokens.Object, _clock.Object, _unitOfWork.Object)
                .Execute(callerId, invitationId, rawToken, CancellationToken.None),
            "reject" => new RejectInvitationUseCase(
                    _invitations.Object, Views(), _tokens.Object, _clock.Object, _unitOfWork.Object)
                .Execute(callerId, invitationId, rawToken, CancellationToken.None),
            _ => new RevokeInvitationUseCase(_invitations.Object, Views(), _clock.Object, _unitOfWork.Object)
                .Execute(callerId, invitationId, CancellationToken.None),
        };

    [Theory]
    [InlineData("accept", TrainerInvitationStatus.ACCEPTED)]
    [InlineData("reject", TrainerInvitationStatus.REJECTED)]
    public async Task AcceptOrReject_ShouldAnswerAsTheInvitedUserWithTheToken(string action, TrainerInvitationStatus expected)
    {
        // Arrange
        var invitation = AnInvitation(AthleteId);
        GivenStored(invitation);
        GivenSaveAndCommitSucceed(invitation);

        // Act
        var answered = await Answer(action, AthleteId, invitation.Id);

        // Assert
        answered.Invitation.Status.Should().Be(expected);
        answered.Trainer.Name.Value.Should().Be("Grace");
        answered.Invitee!.Name.Value.Should().Be("Ada");
        answered.Invitation.AcceptedAtUtc.Should().Be(expected == TrainerInvitationStatus.ACCEPTED ? Now : null);
        _invitations.Verify(repository => repository.Save(invitation, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Revoke_ShouldRevokeAsTheInvitingTrainer()
    {
        // Arrange
        var invitation = AnInvitation(AthleteId);
        GivenStored(invitation);
        GivenSaveAndCommitSucceed(invitation);

        // Act
        var revoked = await Answer("revoke", TrainerId, invitation.Id);

        // Assert
        revoked.Invitation.Status.Should().Be(TrainerInvitationStatus.REVOKED);
        revoked.Trainer.Name.Value.Should().Be("Grace");
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("accept")]
    [InlineData("reject")]
    [InlineData("revoke")]
    public async Task Execute_ShouldRejectAnUnknownInvitation(string action)
    {
        // Arrange
        var invitationId = Id.New();
        _invitations
            .Setup(repository => repository.GetById(invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrainerInvitation?)null);

        // Act
        var act = () => Answer(action, AthleteId, invitationId);

        // Assert
        await act.Should().ThrowAsync<TrainerInvitationNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Theory]
    [InlineData("accept")]
    [InlineData("reject")]
    public async Task AcceptOrReject_ShouldRejectAnotherUser(string action)
    {
        // Arrange
        var invitation = AnInvitation(AthleteId);
        GivenStored(invitation);

        // Act
        var act = () => Answer(action, Id.New(), invitation.Id);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        _tokens.Verify(tokens => tokens.Hash(It.IsAny<string>()), Times.Never);
        VerifyNothingSavedOrCommitted();
    }

    [Theory]
    [InlineData("accept")]
    [InlineData("reject")]
    public async Task AcceptOrReject_ShouldRejectEveryoneForAnInvitationWithoutUser(string action)
    {
        // Arrange
        var invitation = AnInvitation(null);
        GivenStored(invitation);

        // Act
        var act = () => Answer(action, AthleteId, invitation.Id);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingSavedOrCommitted();
    }

    [Theory]
    [InlineData("accept")]
    [InlineData("reject")]
    public async Task AcceptOrReject_ShouldRejectAWrongToken(string action)
    {
        // Arrange
        var invitation = AnInvitation(AthleteId);
        GivenStored(invitation);

        // Act
        var act = () => Answer(action, AthleteId, invitation.Id, "wrong-token");

        // Assert
        await act.Should().ThrowAsync<InvalidInvitationTokenException>();
        invitation.Status.Should().Be(TrainerInvitationStatus.PENDING);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Revoke_ShouldRejectAnotherTrainer()
    {
        // Arrange
        var invitation = AnInvitation(AthleteId);
        GivenStored(invitation);

        // Act
        var act = () => Answer("revoke", Id.New(), invitation.Id);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingSavedOrCommitted();
    }

    [Theory]
    [InlineData("accept")]
    [InlineData("reject")]
    [InlineData("revoke")]
    public async Task Execute_ShouldRejectAnInvitationThatIsNoLongerPending(string action)
    {
        // Arrange
        var invitation = AnInvitation(AthleteId, TrainerInvitationStatus.REVOKED);
        GivenStored(invitation);
        var callerId = action == "revoke" ? TrainerId : AthleteId;

        // Act
        var act = () => Answer(action, callerId, invitation.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidTrainerInvitationStatusTransitionException>();
        VerifyNothingSavedOrCommitted();
    }

    [Theory]
    [InlineData("accept")]
    [InlineData("reject")]
    [InlineData("revoke")]
    public async Task Execute_ShouldRejectAnExpiredInvitation(string action)
    {
        // Arrange
        var invitation = AnInvitation(AthleteId, expiresInDays: -1);
        GivenStored(invitation);
        var callerId = action == "revoke" ? TrainerId : AthleteId;

        // Act
        var act = () => Answer(action, callerId, invitation.Id);

        // Assert
        await act.Should().ThrowAsync<TrainerInvitationExpiredException>();
        VerifyNothingSavedOrCommitted();
    }
}
