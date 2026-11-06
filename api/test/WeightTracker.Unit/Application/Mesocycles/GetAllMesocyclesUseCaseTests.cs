using FluentAssertions;
using Moq;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Mesocycles;
using WeightTracker.Domain.Users;
using Xunit;
using static WeightTracker.Unit.Application.Mesocycles.MesocycleTestUsers;

namespace WeightTracker.Unit.Application.Mesocycles;

public class GetAllMesocyclesUseCaseTests
{
    private readonly Mock<IMesocycleRepository> _mesocycles = new(MockBehavior.Strict);

    [Fact]
    public async Task Execute_ShouldReturnTheScopedMesocyclesWithTheirPeopleInOrder()
    {
        // Arrange
        var trainer = AStoredUser(Role.TRAINER);
        var ada = AStoredUser(Role.USER);
        var alan = AStoredUser(Role.USER);
        var later = AStoredMesocycle(trainer.Id, ada.Id);
        var earlier = AStoredMesocycle(trainer.Id, alan.Id);
        _mesocycles.Setup(repository => repository.GetAllByTrainerId(trainer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([later, earlier]);
        var useCase = new GetAllMesocyclesUseCase(
            new MesocycleVisibility(_mesocycles.Object), new MesocycleViewLoader(UsersHolding(trainer, ada, alan).Object));

        // Act
        var views = await useCase.Execute(trainer.Id, Role.TRAINER, CancellationToken.None);

        // Assert
        views.Select(view => view.Mesocycle).Should().Equal(later, earlier);
        views.Select(view => view.Athlete).Should().Equal(ada, alan);
    }

    [Fact]
    public async Task Execute_ShouldReturnAnEmptyListWhenNothingIsInScope()
    {
        // Arrange
        var user = AStoredUser(Role.USER);
        _mesocycles.Setup(repository => repository.GetAllByAthleteId(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var useCase = new GetAllMesocyclesUseCase(
            new MesocycleVisibility(_mesocycles.Object), new MesocycleViewLoader(UsersHolding(user).Object));

        // Act
        var views = await useCase.Execute(user.Id, Role.USER, CancellationToken.None);

        // Assert
        views.Should().BeEmpty();
    }
}
