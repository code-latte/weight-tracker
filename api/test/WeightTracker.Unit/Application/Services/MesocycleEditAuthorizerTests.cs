using FluentAssertions;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using Xunit;
using static WeightTracker.Unit.Application.Mesocycles.MesocycleTestUsers;

namespace WeightTracker.Unit.Application.Services;

public class MesocycleEditAuthorizerTests
{
    private static readonly Id TrainerId = Id.New();

    private static readonly Id AthleteId = Id.New();

    [Fact]
    public void Admin_ShouldEditAnyMesocycle()
    {
        // Arrange
        var mesocycle = AStoredMesocycle(TrainerId, AthleteId);

        // Act
        var act = () => new MesocycleEditAuthorizer().EnsureCanEdit(mesocycle, Id.New(), Role.ADMIN, "test");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Trainer_ShouldEditTheirMesocycle()
    {
        // Arrange
        var mesocycle = AStoredMesocycle(TrainerId, AthleteId);

        // Act
        var act = () => new MesocycleEditAuthorizer().EnsureCanEdit(mesocycle, TrainerId, Role.TRAINER, "test");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AnotherTrainer_ShouldBeRefused()
    {
        // Arrange
        var mesocycle = AStoredMesocycle(TrainerId, AthleteId);

        // Act
        var act = () => new MesocycleEditAuthorizer().EnsureCanEdit(mesocycle, Id.New(), Role.TRAINER, "test");

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Fact]
    public void Athlete_ShouldBeRefused()
    {
        // Arrange
        var mesocycle = AStoredMesocycle(TrainerId, AthleteId);

        // Act
        var act = () => new MesocycleEditAuthorizer().EnsureCanEdit(mesocycle, AthleteId, Role.USER, "test");

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }
}
