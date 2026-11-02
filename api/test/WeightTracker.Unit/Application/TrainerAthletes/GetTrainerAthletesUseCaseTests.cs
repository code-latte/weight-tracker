using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.TrainerAthletes;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.TrainerAthletes;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Application.TrainerAthletes;

public class GetTrainerAthletesUseCaseTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);
    private readonly Mock<ITrainerAthleteRepository> _trainerAthletes = new(MockBehavior.Strict);

    private static User AStoredUser(string email, Role role) => User.Rehydrate(
        Id.New(), Email.From(email), PersonName.From("Ada"), PersonName.From("Lovelace"),
        new DateOnly(1988, 5, 12), role, PasswordHash.From("stored-hash"), Now.AddDays(-30), true, null);

    private GetTrainerAthletesUseCase AUseCase()
        => new(_users.Object, _trainerAthletes.Object, new TrainerAthleteViewLoader(_users.Object));

    [Fact]
    public async Task Execute_ShouldListActiveAthletesOrderedByEmail()
    {
        // Arrange
        var trainer = AStoredUser("trainer@weighttracker.test", Role.TRAINER);
        var zoe = AStoredUser("zoe@weighttracker.test", Role.USER);
        var ana = AStoredUser("ana@weighttracker.test", Role.USER);
        var relationships = new[]
        {
            TrainerAthlete.Rehydrate(Id.New(), trainer.Id, zoe.Id, TrainerAthleteStatus.ACTIVE, Now, null),
            TrainerAthlete.Rehydrate(Id.New(), trainer.Id, ana.Id, TrainerAthleteStatus.ACTIVE, Now, null),
        };
        _users.Setup(repository => repository.GetById(trainer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(trainer);
        _trainerAthletes
            .Setup(repository => repository.GetAllActiveByTrainerId(trainer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(relationships);
        _users
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([trainer, zoe, ana]);

        // Act
        var athletes = await AUseCase().Execute(trainer.Id, CancellationToken.None);

        // Assert
        athletes.Select(view => view.Athlete.Email.Value)
            .Should().Equal("ana@weighttracker.test", "zoe@weighttracker.test");
        athletes.Select(view => view.Relationship.UserId).Should().Equal(ana.Id, zoe.Id);
        athletes.Should().OnlyContain(view => view.Trainer == trainer);
    }

    [Fact]
    public async Task Execute_ShouldReturnAnEmptyListForAUserWithoutRelationships()
    {
        // Arrange
        var user = AStoredUser("someone@weighttracker.test", Role.USER);
        _users.Setup(repository => repository.GetById(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _trainerAthletes
            .Setup(repository => repository.GetAllActiveByTrainerId(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _users
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var athletes = await AUseCase().Execute(user.Id, CancellationToken.None);

        // Assert
        athletes.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnUnknownTrainer()
    {
        // Arrange
        var trainerId = Id.New();
        _users.Setup(repository => repository.GetById(trainerId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        // Act
        var act = () => AUseCase().Execute(trainerId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
        _trainerAthletes.VerifyNoOtherCalls();
    }
}
