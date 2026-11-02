using FluentAssertions;
using Moq;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.TrainerAthletes;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.TrainerAthletes;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Application.TrainerAthletes;

public class LinkAthleteToTrainerUseCaseTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);
    private readonly Mock<ITrainerAthleteRepository> _trainerAthletes = new(MockBehavior.Strict);
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    public LinkAthleteToTrainerUseCaseTests()
    {
        _clock.Setup(clock => clock.UtcNow).Returns(Now);
    }

    private static User AStoredUser(Role role, bool isActive = true) => User.Rehydrate(
        Id.New(), Email.From($"{Guid.NewGuid():N}@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"),
        new DateOnly(1988, 5, 12), role, PasswordHash.From("stored-hash"), Now.AddDays(-30),
        isActive, isActive ? null : Now.AddDays(-1));

    private void GivenStored(User user)
        => _users.Setup(repository => repository.GetById(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

    private void GivenActiveRelationship(Id athleteId, bool exists)
        => _trainerAthletes
            .Setup(repository => repository.ExistsActiveByUserId(athleteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exists);

    private void GivenSaveAndCommitSucceed()
    {
        _trainerAthletes
            .Setup(repository => repository.Save(It.IsAny<TrainerAthlete>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private void VerifyNothingSavedOrCommitted()
    {
        _trainerAthletes.Verify(
            repository => repository.Save(It.IsAny<TrainerAthlete>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.VerifyNoOtherCalls();
    }

    private LinkAthleteToTrainerUseCase AUseCase()
        => new(_users.Object, _trainerAthletes.Object, _clock.Object, _unitOfWork.Object);

    [Fact]
    public async Task Execute_ShouldCreateAnActiveRelationshipSaveAndCommitOnce()
    {
        // Arrange
        var trainer = AStoredUser(Role.TRAINER);
        var athlete = AStoredUser(Role.USER);
        GivenStored(trainer);
        GivenStored(athlete);
        GivenActiveRelationship(athlete.Id, exists: false);
        GivenSaveAndCommitSucceed();

        // Act
        var view = await AUseCase().Execute(trainer.Id, athlete.Id, CancellationToken.None);

        // Assert
        var relationship = view.Relationship;
        view.Trainer.Should().BeSameAs(trainer);
        view.Athlete.Should().BeSameAs(athlete);
        relationship.TrainerId.Should().Be(trainer.Id);
        relationship.UserId.Should().Be(athlete.Id);
        relationship.Status.Should().Be(TrainerAthleteStatus.ACTIVE);
        relationship.StartedAtUtc.Should().Be(Now);
        relationship.IsNew.Should().BeTrue();
        _trainerAthletes.Verify(repository => repository.Save(relationship, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldRejectAnUnknownTrainer()
    {
        // Arrange
        var trainerId = Id.New();
        _users.Setup(repository => repository.GetById(trainerId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        // Act
        var act = () => AUseCase().Execute(trainerId, Id.New(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnUnknownAthlete()
    {
        // Arrange
        var trainer = AStoredUser(Role.TRAINER);
        var athleteId = Id.New();
        GivenStored(trainer);
        _users.Setup(repository => repository.GetById(athleteId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        // Act
        var act = () => AUseCase().Execute(trainer.Id, athleteId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Theory]
    [InlineData(Role.ADMIN, true, Role.USER, true)]
    [InlineData(Role.USER, true, Role.USER, true)]
    [InlineData(Role.TRAINER, false, Role.USER, true)]
    [InlineData(Role.TRAINER, true, Role.TRAINER, true)]
    [InlineData(Role.TRAINER, true, Role.ADMIN, true)]
    [InlineData(Role.TRAINER, true, Role.USER, false)]
    public async Task Execute_ShouldRejectUsersThatCannotTakeTheirSide(
        Role trainerRole, bool trainerActive, Role athleteRole, bool athleteActive)
    {
        // Arrange
        var trainer = AStoredUser(trainerRole, trainerActive);
        var athlete = AStoredUser(athleteRole, athleteActive);
        GivenStored(trainer);
        GivenStored(athlete);

        // Act
        var act = () => AUseCase().Execute(trainer.Id, athlete.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidTrainerAthleteRoleException>();
        _trainerAthletes.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnAthleteWhoAlreadyHasAnActiveTrainer()
    {
        // Arrange
        var trainer = AStoredUser(Role.TRAINER);
        var athlete = AStoredUser(Role.USER);
        GivenStored(trainer);
        GivenStored(athlete);
        GivenActiveRelationship(athlete.Id, exists: true);

        // Act
        var act = () => AUseCase().Execute(trainer.Id, athlete.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AthleteAlreadyHasTrainerException>();
        VerifyNothingSavedOrCommitted();
    }
}
