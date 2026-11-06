using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Mesocycles;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Users;
using Xunit;
using static WeightTracker.Unit.Application.Mesocycles.MesocycleTestUsers;

namespace WeightTracker.Unit.Application.Mesocycles;

public class GetMesocycleByIdUseCaseTests
{
    private readonly Mock<IMesocycleRepository> _mesocycles = new(MockBehavior.Strict);

    private readonly User _trainer = AStoredUser(Role.TRAINER);

    private readonly User _athlete = AStoredUser(Role.USER);

    private GetMesocycleByIdUseCase AUseCase()
        => new(_mesocycles.Object, new MesocycleVisibility(_mesocycles.Object),
            new MesocycleViewLoader(UsersHolding(_trainer, _athlete).Object));

    private Mesocycle Stored()
    {
        var mesocycle = AStoredMesocycle(_trainer.Id, _athlete.Id);
        _mesocycles.Setup(repository => repository.GetById(mesocycle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mesocycle);

        return mesocycle;
    }

    [Fact]
    public async Task Athlete_ShouldReadTheirMesocycleWithItsPeople()
    {
        // Arrange
        var mesocycle = Stored();

        // Act
        var view = await AUseCase().Execute(_athlete.Id, Role.USER, mesocycle.Id, CancellationToken.None);

        // Assert
        view.Mesocycle.Should().BeSameAs(mesocycle);
        view.Trainer.Should().Be(_trainer);
        view.Athlete.Should().Be(_athlete);
    }

    [Fact]
    public async Task Trainer_ShouldReadTheirMesocycle()
    {
        // Arrange
        var mesocycle = Stored();

        // Act
        var view = await AUseCase().Execute(_trainer.Id, Role.TRAINER, mesocycle.Id, CancellationToken.None);

        // Assert
        view.Mesocycle.Should().BeSameAs(mesocycle);
    }

    [Fact]
    public async Task Admin_ShouldReadAMesocycleTheyAreNotPartOf()
    {
        // Arrange
        var mesocycle = Stored();

        // Act
        var view = await AUseCase().Execute(Id.New(), Role.ADMIN, mesocycle.Id, CancellationToken.None);

        // Assert
        view.Mesocycle.Should().BeSameAs(mesocycle);
    }

    [Fact]
    public async Task AnotherTrainer_ShouldBeRefused()
    {
        // Arrange
        var mesocycle = Stored();

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.TRAINER, mesocycle.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
    }

    [Fact]
    public async Task AnotherUser_ShouldBeRefused()
    {
        // Arrange
        var mesocycle = Stored();

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.USER, mesocycle.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
    }

    [Fact]
    public async Task UnknownId_ShouldNotBeFoundWhateverTheRole()
    {
        // Arrange
        var unknown = Id.New();
        _mesocycles.Setup(repository => repository.GetById(unknown, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mesocycle?)null);

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.USER, unknown, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MesocycleNotFoundException>();
    }
}
