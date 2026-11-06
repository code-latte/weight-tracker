using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Application.Services;

public class MesocycleVisibilityTests
{
    private readonly Mock<IMesocycleRepository> _mesocycles = new(MockBehavior.Strict);

    private MesocycleVisibility AVisibility() => new(_mesocycles.Object);

    private static Mesocycle AMesocycle(Id trainerId, Id athleteId) => Mesocycle.Create(
        trainerId, athleteId, MesocycleName.From("Block"), MesocycleObjective.From("Objective"),
        new DateTime(2026, 11, 9), DurationInWeeks.From(4));

    [Fact]
    public async Task Admin_ShouldListEveryMesocycle()
    {
        // Arrange
        var all = new[] { AMesocycle(Id.New(), Id.New()), AMesocycle(Id.New(), Id.New()) };
        _mesocycles.Setup(repository => repository.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(all);

        // Act
        var visible = await AVisibility().VisibleTo(Id.New(), Role.ADMIN, CancellationToken.None);

        // Assert
        visible.Should().Equal(all);
    }

    [Fact]
    public async Task Trainer_ShouldListOnlyTheMesocyclesTheyPrepare()
    {
        // Arrange
        var callerId = Id.New();
        var theirs = new[] { AMesocycle(callerId, Id.New()) };
        _mesocycles.Setup(repository => repository.GetAllByTrainerId(callerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(theirs);

        // Act
        var visible = await AVisibility().VisibleTo(callerId, Role.TRAINER, CancellationToken.None);

        // Assert
        visible.Should().Equal(theirs);
        _mesocycles.Verify(repository => repository.GetAll(It.IsAny<CancellationToken>()), Times.Never);
        _mesocycles.Verify(
            repository => repository.GetAllByAthleteId(It.IsAny<Id>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task User_ShouldListOnlyTheMesocyclesPlannedForThem()
    {
        // Arrange
        var callerId = Id.New();
        var theirs = new[] { AMesocycle(Id.New(), callerId) };
        _mesocycles.Setup(repository => repository.GetAllByAthleteId(callerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(theirs);

        // Act
        var visible = await AVisibility().VisibleTo(callerId, Role.USER, CancellationToken.None);

        // Assert
        visible.Should().Equal(theirs);
        _mesocycles.Verify(repository => repository.GetAll(It.IsAny<CancellationToken>()), Times.Never);
        _mesocycles.Verify(
            repository => repository.GetAllByTrainerId(It.IsAny<Id>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnknownRole_ShouldBeRejectedBeforeAnyRead()
    {
        // Arrange & Act
        var act = async () => await AVisibility().VisibleTo(Id.New(), (Role)99, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
    }

    [Theory]
    [InlineData(Role.ADMIN, true)]
    [InlineData(Role.TRAINER, false)]
    [InlineData(Role.USER, false)]
    public void CanSee_ShouldLetOnlyAnAdminSeeAMesocycleTheyAreNotPartOf(Role role, bool expected)
    {
        // Arrange
        var mesocycle = AMesocycle(Id.New(), Id.New());

        // Act
        var canSee = AVisibility().CanSee(mesocycle, Id.New(), role);

        // Assert
        canSee.Should().Be(expected);
    }

    [Fact]
    public void CanSee_ShouldLetTheTrainerSeeTheirMesocycle()
    {
        // Arrange
        var trainerId = Id.New();
        var mesocycle = AMesocycle(trainerId, Id.New());

        // Act
        var canSee = AVisibility().CanSee(mesocycle, trainerId, Role.TRAINER);

        // Assert
        canSee.Should().BeTrue();
    }

    [Fact]
    public void CanSee_ShouldLetTheAthleteSeeTheirMesocycle()
    {
        // Arrange
        var athleteId = Id.New();
        var mesocycle = AMesocycle(Id.New(), athleteId);

        // Act
        var canSee = AVisibility().CanSee(mesocycle, athleteId, Role.USER);

        // Assert
        canSee.Should().BeTrue();
    }

    [Fact]
    public void CanSee_ShouldNotLetTheAthleteSeeItAsATrainer()
    {
        // Arrange
        var athleteId = Id.New();
        var mesocycle = AMesocycle(Id.New(), athleteId);

        // Act
        var canSee = AVisibility().CanSee(mesocycle, athleteId, Role.TRAINER);

        // Assert
        canSee.Should().BeFalse();
    }
}
