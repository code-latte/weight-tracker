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

public class GetAllUsersUseCaseTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);
    private readonly Mock<ITrainerAthleteRepository> _trainerAthletes = new(MockBehavior.Strict);

    private GetAllUsersUseCase AUseCase() => new(_users.Object, _trainerAthletes.Object);

    private static User AUser(string email, Role role) => User.Rehydrate(
        Id.New(), Email.From(email), PersonName.From("Ada"), PersonName.From("Lovelace"),
        new DateOnly(1988, 5, 12), role, PasswordHash.From("stored-hash"), Now, true, null);

    private static TrainerAthlete AnActiveRelationship(User trainer, User athlete)
        => TrainerAthlete.Rehydrate(Id.New(), trainer.Id, athlete.Id, TrainerAthleteStatus.ACTIVE, Now, null);

    private void GivenUsers(params User[] users)
        => _users.Setup(repository => repository.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(users);

    private void GivenActiveRelationships(params TrainerAthlete[] relationships)
        => _trainerAthletes
            .Setup(repository => repository.GetAllActiveByUserIds(
                It.IsAny<IReadOnlyCollection<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(relationships);

    [Fact]
    public async Task Admin_ShouldSeeEveryUserInRepositoryOrder()
    {
        // Arrange
        var all = new[]
        {
            AUser("admin@weighttracker.test", Role.ADMIN),
            AUser("trainer@weighttracker.test", Role.TRAINER),
            AUser("user@weighttracker.test", Role.USER),
        };
        GivenUsers(all);
        GivenActiveRelationships();

        // Act
        var result = await AUseCase().Execute(Role.ADMIN, CancellationToken.None);

        // Assert
        result.Select(view => view.User).Should().Equal(all);
        _users.Verify(repository => repository.GetAll(It.IsAny<CancellationToken>()), Times.Once);
        _users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Admin_ShouldSeeTheTrainerOfAnAthleteWithAnActiveRelationship()
    {
        // Arrange
        var trainer = AUser("trainer@weighttracker.test", Role.TRAINER);
        var athlete = AUser("user@weighttracker.test", Role.USER);
        GivenUsers(trainer, athlete);
        GivenActiveRelationships(AnActiveRelationship(trainer, athlete));
        _users
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([trainer]);

        // Act
        var result = await AUseCase().Execute(Role.ADMIN, CancellationToken.None);

        // Assert
        result.Single(view => view.User == athlete).Trainer.Should().Be(trainer);
        _users.Verify(repository => repository.GetAllByIds(
            It.Is<IEnumerable<Id>>(ids => ids.SequenceEqual(new[] { trainer.Id })), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Admin_ShouldSeeNoTrainerForAnAthleteWithoutAnActiveRelationship()
    {
        // Arrange
        var athlete = AUser("user@weighttracker.test", Role.USER);
        GivenUsers(athlete);
        GivenActiveRelationships();

        // Act
        var result = await AUseCase().Execute(Role.ADMIN, CancellationToken.None);

        // Assert
        result.Single().Trainer.Should().BeNull();
        _users.Verify(
            repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Admin_ShouldSeeNoTrainerForTrainersAndAdministrators()
    {
        // Arrange
        var admin = AUser("admin@weighttracker.test", Role.ADMIN);
        var trainer = AUser("trainer@weighttracker.test", Role.TRAINER);
        GivenUsers(admin, trainer);
        GivenActiveRelationships();

        // Act
        var result = await AUseCase().Execute(Role.ADMIN, CancellationToken.None);

        // Assert
        result.Should().OnlyContain(view => view.Trainer == null);
        _trainerAthletes.Verify(repository => repository.GetAllActiveByUserIds(
            It.Is<IReadOnlyCollection<Id>>(ids => ids.Count == 0), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(Role.TRAINER)]
    [InlineData(Role.USER)]
    public async Task NonAdmin_ShouldBeRejectedAsUnauthorized(Role callerRole)
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        var act = async () => await useCase.Execute(callerRole, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        _users.VerifyNoOtherCalls();
        _trainerAthletes.VerifyNoOtherCalls();
    }
}
