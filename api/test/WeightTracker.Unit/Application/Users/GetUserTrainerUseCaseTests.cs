using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.Users;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.TrainerAthletes;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Application.Users;

public class GetUserTrainerUseCaseTests
{
    private static readonly DateTime Now = new(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);
    private readonly Mock<ITrainerAthleteRepository> _trainerAthletes = new(MockBehavior.Strict);

    private readonly User _trainer = AUser(Role.TRAINER);

    private readonly User _athlete = AUser(Role.USER);

    private GetUserTrainerUseCase AUseCase() => new(_users.Object, _trainerAthletes.Object);

    private static User AUser(Role role, bool isActive = true) => User.Rehydrate(
        Id.New(), Email.From($"{Guid.NewGuid():N}@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"),
        new DateOnly(1988, 5, 12), role, PasswordHash.From("stored-hash"), Now, isActive, isActive ? null : Now);

    private void GivenStored(params User[] users)
    {
        foreach (var user in users)
            _users.Setup(repository => repository.GetById(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
    }

    private void GivenActiveRelationshipsOf(User athlete, params TrainerAthlete[] relationships)
        => _trainerAthletes
            .Setup(repository => repository.GetAllActiveByUserIds(
                It.Is<IReadOnlyCollection<Id>>(ids => ids.SequenceEqual(new[] { athlete.Id })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(relationships);

    private static TrainerAthlete AnActiveRelationship(User trainer, User athlete)
        => TrainerAthlete.Rehydrate(Id.New(), trainer.Id, athlete.Id, TrainerAthleteStatus.ACTIVE, Now, null);

    [Fact]
    public async Task User_ShouldGetTheTrainerOfTheirActiveRelationship()
    {
        // Arrange
        GivenStored(_athlete, _trainer);
        GivenActiveRelationshipsOf(_athlete, AnActiveRelationship(_trainer, _athlete));

        // Act
        var trainer = await AUseCase().Execute(_athlete.Id, Role.USER, _athlete.Id, CancellationToken.None);

        // Assert
        trainer.Should().BeSameAs(_trainer);
    }

    [Fact]
    public async Task User_ShouldGetNoTrainerWithoutAnActiveRelationship()
    {
        // Arrange
        GivenStored(_athlete);
        GivenActiveRelationshipsOf(_athlete);

        // Act
        var trainer = await AUseCase().Execute(_athlete.Id, Role.USER, _athlete.Id, CancellationToken.None);

        // Assert
        trainer.Should().BeNull();
    }

    [Fact]
    public async Task User_ShouldBeRefusedForAnotherUserBeforeAnyLookup()
    {
        // Arrange
        var other = AUser(Role.USER);

        // Act
        var act = () => AUseCase().Execute(_athlete.Id, Role.USER, other.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        _users.VerifyNoOtherCalls();
        _trainerAthletes.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Trainer_ShouldBeRefusedEvenForThemselves(bool askingAboutThemselves)
    {
        // Arrange
        var target = askingAboutThemselves ? _trainer.Id : _athlete.Id;

        // Act
        var act = () => AUseCase().Execute(_trainer.Id, Role.TRAINER, target, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        _users.VerifyNoOtherCalls();
        _trainerAthletes.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Admin_ShouldGetTheTrainerOfAnAthlete()
    {
        // Arrange
        GivenStored(_athlete, _trainer);
        GivenActiveRelationshipsOf(_athlete, AnActiveRelationship(_trainer, _athlete));

        // Act
        var trainer = await AUseCase().Execute(Id.New(), Role.ADMIN, _athlete.Id, CancellationToken.None);

        // Assert
        trainer.Should().BeSameAs(_trainer);
    }

    [Theory]
    [InlineData(Role.TRAINER)]
    [InlineData(Role.ADMIN)]
    public async Task Admin_ShouldGetNoTrainerForSomeoneWhoCannotBeAnAthleteWithoutQueryingRelationships(Role role)
    {
        // Arrange
        var target = AUser(role);
        GivenStored(target);

        // Act
        var trainer = await AUseCase().Execute(Id.New(), Role.ADMIN, target.Id, CancellationToken.None);

        // Assert
        trainer.Should().BeNull();
        _trainerAthletes.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Admin_ShouldGetNotFoundForAnUnknownUser()
    {
        // Arrange
        var unknown = Id.New();
        _users.Setup(repository => repository.GetById(unknown, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.ADMIN, unknown, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
        _trainerAthletes.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task User_ShouldStillGetADeactivatedTrainer()
    {
        // Arrange
        var deactivated = AUser(Role.TRAINER, isActive: false);
        GivenStored(_athlete, deactivated);
        GivenActiveRelationshipsOf(_athlete, AnActiveRelationship(deactivated, _athlete));

        // Act
        var trainer = await AUseCase().Execute(_athlete.Id, Role.USER, _athlete.Id, CancellationToken.None);

        // Assert
        trainer.Should().BeSameAs(deactivated);
    }
}
