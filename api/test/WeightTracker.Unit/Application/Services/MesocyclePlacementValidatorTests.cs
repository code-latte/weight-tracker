using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Users;
using Xunit;
using static WeightTracker.Unit.Application.Mesocycles.MesocycleTestUsers;

namespace WeightTracker.Unit.Application.Services;

public class MesocyclePlacementValidatorTests
{
    private static readonly Id TrainerId = Id.New();

    private static readonly Id AthleteId = Id.New();

    private readonly Mock<IMesocycleRepository> _mesocycles = new(MockBehavior.Strict);

    private readonly Mesocycle _mesocycle = AStoredMesocycle(TrainerId, AthleteId);

    public MesocyclePlacementValidatorTests()
    {
        _mesocycles.Setup(repository => repository.GetById(_mesocycle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mesocycle);
    }

    private Task Place(Id ownerId, Id? trainerId, int weekNumber, Id callerId, Role callerRole, Id? mesocycleId = null)
        => new MesocyclePlacementValidator(_mesocycles.Object, new MesocycleEditAuthorizer()).EnsureCanPlace(
            ownerId, trainerId, mesocycleId ?? _mesocycle.Id, WeekNumber.From(weekNumber), callerId, callerRole, "test",
            CancellationToken.None);

    [Fact]
    public async Task EnsureCanPlace_ShouldAcceptTheMesocyclesTrainerPlacingTheirAthletesWorkout()
    {
        // Arrange
        var callerId = TrainerId;

        // Act
        var act = () => Place(AthleteId, TrainerId, 3, callerId, Role.TRAINER);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureCanPlace_ShouldAcceptAnAdminPlacingAWorkoutWithoutTrainer()
    {
        // Arrange
        var adminId = Id.New();

        // Act
        var act = () => Place(AthleteId, null, 4, adminId, Role.ADMIN);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureCanPlace_ShouldNotFindAnUnknownMesocycle()
    {
        // Arrange
        var unknown = Id.New();
        _mesocycles.Setup(repository => repository.GetById(unknown, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mesocycle?)null);

        // Act
        var act = () => Place(AthleteId, TrainerId, 1, TrainerId, Role.TRAINER, unknown);

        // Assert
        await act.Should().ThrowAsync<MesocycleNotFoundException>();
    }

    [Fact]
    public async Task EnsureCanPlace_ShouldRefuseTheAthleteBeforeCheckingTheWorkout()
    {
        // Arrange
        var callerId = AthleteId;

        // Act
        var act = () => Place(AthleteId, null, 9, callerId, Role.USER);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
    }

    [Fact]
    public async Task EnsureCanPlace_ShouldRefuseAnotherTrainer()
    {
        // Arrange
        var otherTrainerId = Id.New();

        // Act
        var act = () => Place(AthleteId, otherTrainerId, 1, otherTrainerId, Role.TRAINER);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
    }

    [Fact]
    public async Task EnsureCanPlace_ShouldRejectAWorkoutOfAnotherAthlete()
    {
        // Arrange
        var otherOwnerId = Id.New();

        // Act
        var act = () => Place(otherOwnerId, TrainerId, 1, TrainerId, Role.TRAINER);

        // Assert
        await act.Should().ThrowAsync<MesocycleAthleteMismatchException>();
    }

    [Fact]
    public async Task EnsureCanPlace_ShouldRejectAWorkoutWithAnotherTrainer()
    {
        // Arrange
        var otherTrainerId = Id.New();

        // Act
        var act = () => Place(AthleteId, otherTrainerId, 1, Id.New(), Role.ADMIN);

        // Assert
        await act.Should().ThrowAsync<MesocycleTrainerMismatchException>();
    }

    [Fact]
    public async Task EnsureCanPlace_ShouldRejectAWeekBeyondTheDuration()
    {
        // Arrange
        var callerId = TrainerId;

        // Act
        var act = () => Place(AthleteId, TrainerId, 5, callerId, Role.TRAINER);

        // Assert
        await act.Should().ThrowAsync<MesocycleWeekOutOfRangeException>();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void EnsureComplete_ShouldRejectHalfAPair(bool withMesocycle, bool withWeek)
    {
        // Arrange
        Id? mesocycleId = withMesocycle ? Id.New() : null;
        WeekNumber? weekNumber = withWeek ? WeekNumber.From(1) : null;

        // Act
        var act = () => MesocyclePlacementValidator.EnsureComplete(mesocycleId, weekNumber);

        // Assert
        act.Should().Throw<IncompleteMesocyclePlacementException>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EnsureComplete_ShouldAcceptBothOrNeither(bool present)
    {
        // Arrange
        Id? mesocycleId = present ? Id.New() : null;
        WeekNumber? weekNumber = present ? WeekNumber.From(1) : null;

        // Act
        var act = () => MesocyclePlacementValidator.EnsureComplete(mesocycleId, weekNumber);

        // Assert
        act.Should().NotThrow();
    }
}
