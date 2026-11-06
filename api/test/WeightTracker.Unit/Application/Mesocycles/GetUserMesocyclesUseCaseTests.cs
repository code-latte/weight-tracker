using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Mesocycles;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using Xunit;
using static WeightTracker.Unit.Application.Mesocycles.MesocycleTestUsers;

namespace WeightTracker.Unit.Application.Mesocycles;

public class GetUserMesocyclesUseCaseTests
{
    private readonly Mock<IMesocycleRepository> _mesocycles = new(MockBehavior.Strict);

    private readonly User _trainer = AStoredUser(Role.TRAINER);

    private readonly User _otherTrainer = AStoredUser(Role.TRAINER);

    private readonly User _athlete = AStoredUser(Role.USER);

    private GetUserMesocyclesUseCase AUseCase()
    {
        var users = UsersHolding(_trainer, _otherTrainer, _athlete);

        return new(_mesocycles.Object, users.Object, new MesocycleViewLoader(users.Object));
    }

    [Fact]
    public async Task Trainer_ShouldGetOnlyTheMesocyclesTheyPrepareForThatAthlete()
    {
        // Arrange
        var theirs = AStoredMesocycle(_trainer.Id, _athlete.Id);
        _mesocycles
            .Setup(repository => repository.GetAllByTrainerIdAndAthleteId(_trainer.Id, _athlete.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([theirs]);

        // Act
        var views = await AUseCase().Execute(_trainer.Id, Role.TRAINER, _athlete.Id, CancellationToken.None);

        // Assert
        views.Select(view => view.Mesocycle).Should().Equal(theirs);
        _mesocycles.Verify(
            repository => repository.GetAllByAthleteId(It.IsAny<Id>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Admin_ShouldGetEveryMesocycleOfThatAthlete()
    {
        // Arrange
        var fromTrainer = AStoredMesocycle(_trainer.Id, _athlete.Id);
        var fromOtherTrainer = AStoredMesocycle(_otherTrainer.Id, _athlete.Id);
        _mesocycles.Setup(repository => repository.GetAllByAthleteId(_athlete.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([fromTrainer, fromOtherTrainer]);

        // Act
        var views = await AUseCase().Execute(Id.New(), Role.ADMIN, _athlete.Id, CancellationToken.None);

        // Assert
        views.Select(view => view.Mesocycle).Should().Equal(fromTrainer, fromOtherTrainer);
        views.Select(view => view.Trainer).Should().Equal(_trainer, _otherTrainer);
    }

    [Fact]
    public async Task Trainer_ShouldGetAnEmptyListForAnAthleteTheyHaveNoMesocycleFor()
    {
        // Arrange
        _mesocycles
            .Setup(repository => repository.GetAllByTrainerIdAndAthleteId(_trainer.Id, _athlete.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var views = await AUseCase().Execute(_trainer.Id, Role.TRAINER, _athlete.Id, CancellationToken.None);

        // Assert
        views.Should().BeEmpty();
    }

    [Fact]
    public async Task User_ShouldBeRefusedEvenForThemselves()
    {
        // Arrange
        var athleteId = _athlete.Id;

        // Act
        var act = () => AUseCase().Execute(athleteId, Role.USER, athleteId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
    }

    [Fact]
    public async Task UnknownUser_ShouldNotBeFound()
    {
        // Arrange
        var unknown = Id.New();

        // Act
        var act = () => AUseCase().Execute(_trainer.Id, Role.TRAINER, unknown, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
    }
}
