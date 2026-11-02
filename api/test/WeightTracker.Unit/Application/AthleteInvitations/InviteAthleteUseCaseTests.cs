using FluentAssertions;
using Moq;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.AthleteInvitations;
using WeightTracker.Domain.AthleteInvitations;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Application.AthleteInvitations;

public class InviteAthleteUseCaseTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 10, 0, 0, DateTimeKind.Utc);
    private const string InvitedEmail = "athlete@weighttracker.test";

    private static readonly GeneratedInvitationToken Token = new("raw-token", "token-hash", Now.AddDays(7));

    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);
    private readonly Mock<ITrainerAthleteRepository> _trainerAthletes = new(MockBehavior.Strict);
    private readonly Mock<ITrainerInvitationRepository> _invitations = new(MockBehavior.Strict);
    private readonly Mock<IInvitationTokenService> _tokens = new(MockBehavior.Strict);
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    private readonly User _trainer = AStoredUser("trainer@weighttracker.test", Role.TRAINER);

    public InviteAthleteUseCaseTests()
    {
        _clock.Setup(clock => clock.UtcNow).Returns(Now);
        _tokens.Setup(tokens => tokens.Generate(Now)).Returns(Token);
        _users.Setup(repository => repository.GetById(_trainer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_trainer);
    }

    private static User AStoredUser(string email, Role role, bool isActive = true) => User.Rehydrate(
        Id.New(), Email.From(email), PersonName.From("Ada"), PersonName.From("Lovelace"),
        new DateOnly(1988, 5, 12), role, PasswordHash.From("stored-hash"), Now.AddDays(-30),
        isActive, isActive ? null : Now.AddDays(-1));

    private void GivenInvitedUser(User? user)
        => _users
            .Setup(repository => repository.GetByEmail(Email.From(InvitedEmail), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

    private void GivenActiveRelationship(Id athleteId, bool exists)
        => _trainerAthletes
            .Setup(repository => repository.ExistsActiveByUserId(athleteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exists);

    private void GivenPending(params TrainerInvitation[] pending)
        => _invitations
            .Setup(repository => repository.GetAllPendingByTrainerIdAndEmail(
                _trainer.Id, Email.From(InvitedEmail), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pending);

    private void GivenSaveAndCommitSucceed()
    {
        _invitations
            .Setup(repository => repository.Save(It.IsAny<TrainerInvitation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private void VerifyNothingSavedOrCommitted()
    {
        _invitations.Verify(
            repository => repository.Save(It.IsAny<TrainerInvitation>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.VerifyNoOtherCalls();
    }

    private TrainerInvitation APendingInvitation(DateTime expiresAtUtc) => TrainerInvitation.Rehydrate(
        Id.New(), _trainer.Id, Email.From(InvitedEmail), null, "other-hash", TrainerInvitationStatus.PENDING,
        Now.AddDays(-10), expiresAtUtc, null);

    private InviteAthleteUseCase AUseCase() => new(
        _users.Object, _trainerAthletes.Object, _invitations.Object, _tokens.Object, _clock.Object, _unitOfWork.Object);

    [Fact]
    public async Task Execute_ShouldInviteARegisteredAthleteAndLinkTheirId()
    {
        // Arrange
        var athlete = AStoredUser(InvitedEmail, Role.USER);
        GivenInvitedUser(athlete);
        GivenActiveRelationship(athlete.Id, exists: false);
        GivenPending();
        GivenSaveAndCommitSucceed();

        // Act
        var created = await AUseCase().Execute(_trainer.Id, InvitedEmail, CancellationToken.None);

        // Assert
        created.Token.Should().Be("raw-token");
        created.View.Trainer.Should().BeSameAs(_trainer);
        created.View.Invitee.Should().BeSameAs(athlete);
        created.View.Invitation.UserId.Should().Be(athlete.Id);
        created.View.Invitation.TrainerId.Should().Be(_trainer.Id);
        created.View.Invitation.TokenHash.Should().Be("token-hash");
        created.View.Invitation.Status.Should().Be(TrainerInvitationStatus.PENDING);
        created.View.Invitation.CreatedAtUtc.Should().Be(Now);
        created.View.Invitation.ExpiresAtUtc.Should().Be(Now.AddDays(7));
        _invitations.Verify(repository => repository.Save(created.View.Invitation, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldInviteAnUnregisteredEmailWithoutUser()
    {
        // Arrange
        GivenInvitedUser(null);
        GivenPending();
        GivenSaveAndCommitSucceed();

        // Act
        var created = await AUseCase().Execute(_trainer.Id, InvitedEmail, CancellationToken.None);

        // Assert
        created.View.Invitation.UserId.Should().BeNull();
        created.View.Invitee.Should().BeNull();
        created.View.Trainer.Should().BeSameAs(_trainer);
        created.View.Invitation.InviteEmail.Should().Be(Email.From(InvitedEmail));
        _trainerAthletes.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(Role.TRAINER, true)]
    [InlineData(Role.ADMIN, true)]
    [InlineData(Role.USER, false)]
    public async Task Execute_ShouldRejectAnEmailThatCannotBeAnAthlete(Role role, bool isActive)
    {
        // Arrange
        GivenInvitedUser(AStoredUser(InvitedEmail, role, isActive));

        // Act
        var act = () => AUseCase().Execute(_trainer.Id, InvitedEmail, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidTrainerAthleteRoleException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnAthleteWhoAlreadyHasAnActiveTrainer()
    {
        // Arrange
        var athlete = AStoredUser(InvitedEmail, Role.USER);
        GivenInvitedUser(athlete);
        GivenActiveRelationship(athlete.Id, exists: true);

        // Act
        var act = () => AUseCase().Execute(_trainer.Id, InvitedEmail, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AthleteAlreadyHasTrainerException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectADuplicatedLivePendingInvitation()
    {
        // Arrange
        GivenInvitedUser(null);
        GivenPending(APendingInvitation(Now.AddDays(1)));

        // Act
        var act = () => AUseCase().Execute(_trainer.Id, InvitedEmail, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PendingInvitationAlreadyExistsException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldNotBeBlockedByAnExpiredPendingInvitation()
    {
        // Arrange
        GivenInvitedUser(null);
        GivenPending(APendingInvitation(Now.AddDays(-1)));
        GivenSaveAndCommitSucceed();

        // Act
        var created = await AUseCase().Execute(_trainer.Id, InvitedEmail, CancellationToken.None);

        // Assert
        created.View.Invitation.Status.Should().Be(TrainerInvitationStatus.PENDING);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldRejectAMalformedEmailWithoutLookingAnythingUp()
    {
        // Arrange
        var malformed = "not-an-email";

        // Act
        var act = () => AUseCase().Execute(_trainer.Id, malformed, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidEmailException>();
        _invitations.VerifyNoOtherCalls();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectADeactivatedTrainer()
    {
        // Arrange
        var deactivated = AStoredUser("old-trainer@weighttracker.test", Role.TRAINER, isActive: false);
        _users.Setup(repository => repository.GetById(deactivated.Id, It.IsAny<CancellationToken>())).ReturnsAsync(deactivated);

        // Act
        var act = () => AUseCase().Execute(deactivated.Id, InvitedEmail, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidTrainerAthleteRoleException>();
        VerifyNothingSavedOrCommitted();
    }
}
