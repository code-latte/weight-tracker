using FluentAssertions;
using Moq;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Mesocycles;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Users;
using Xunit;
using static WeightTracker.Unit.Application.Mesocycles.MesocycleTestUsers;

namespace WeightTracker.Unit.Application.Mesocycles;

/// <summary>
/// Covers the use cases that change an existing mesocycle: its details and its weeks.
/// </summary>
public class MesocycleWriteUseCaseTests
{
    private static readonly MesocycleObjective WeekObjective = MesocycleObjective.From("Week objective");

    private readonly Mock<IMesocycleRepository> _mesocycles = new(MockBehavior.Strict);
    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    private readonly User _trainer = AStoredUser(Role.TRAINER);
    private readonly User _athlete = AStoredUser(Role.USER);

    private readonly Mesocycle _mesocycle;

    public MesocycleWriteUseCaseTests()
    {
        _mesocycle = Mesocycle.Rehydrate(
            Id.New(), _trainer.Id, _athlete.Id, MesocycleName.From("Block"), MesocycleObjective.From("Objective"),
            new DateTime(2026, 11, 9), DurationInWeeks.From(4),
            [MesocycleWeek.Rehydrate(Id.New(), WeekNumber.From(2), TrainingWeekType.ACCUMULATION, WeekObjective)]);

        _mesocycles.Setup(repository => repository.GetById(_mesocycle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mesocycle);
        _mesocycles.Setup(repository => repository.Save(It.IsAny<Mesocycle>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _workouts.Setup(repository => repository.ExistsByMesocycleWeek(
                It.IsAny<Id>(), It.IsAny<WeekNumber>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private MesocycleViewLoader Views() => new(UsersHolding(_trainer, _athlete).Object);

    private UpdateMesocycleUseCase AnUpdate() => new(_mesocycles.Object, new MesocycleEditAuthorizer(), Views(), _unitOfWork.Object);

    private DefineMesocycleWeekUseCase ADefine() => new(_mesocycles.Object, new MesocycleEditAuthorizer(), Views(), _unitOfWork.Object);

    private RedefineMesocycleWeekUseCase ARedefine() => new(_mesocycles.Object, new MesocycleEditAuthorizer(), Views(), _unitOfWork.Object);

    private RemoveMesocycleWeekUseCase ARemove() => new(_mesocycles.Object, _workouts.Object, new MesocycleEditAuthorizer(), _unitOfWork.Object);

    private Task<MesocycleView> Update(Id callerId, Role role, int durationInWeeks, Id? mesocycleId = null)
        => AnUpdate().Execute(
            callerId, role, mesocycleId ?? _mesocycle.Id, MesocycleName.From("Renamed"), MesocycleObjective.From("New objective"),
            new DateTime(2026, 11, 16, 9, 0, 0), DurationInWeeks.From(durationInWeeks), CancellationToken.None);

    private Task<MesocycleView> Define(Id callerId, Role role, int weekNumber, Id? mesocycleId = null)
        => ADefine().Execute(
            callerId, role, mesocycleId ?? _mesocycle.Id, WeekNumber.From(weekNumber), TrainingWeekType.INTENSIFICATION,
            WeekObjective, CancellationToken.None);

    private Task<MesocycleView> Redefine(Id callerId, Role role, int weekNumber)
        => ARedefine().Execute(
            callerId, role, _mesocycle.Id, WeekNumber.From(weekNumber), TrainingWeekType.DELOAD,
            MesocycleObjective.From("Back off"), CancellationToken.None);

    private Task Remove(Id callerId, Role role, int weekNumber, Id? mesocycleId = null)
        => ARemove().Execute(
            callerId, role, mesocycleId ?? _mesocycle.Id, WeekNumber.From(weekNumber), CancellationToken.None);

    private void GivenWorkoutsInWeek(int weekNumber)
        => _workouts.Setup(repository => repository.ExistsByMesocycleWeek(
                _mesocycle.Id, WeekNumber.From(weekNumber), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

    private Id GivenUnknownMesocycle()
    {
        var unknown = Id.New();
        _mesocycles.Setup(repository => repository.GetById(unknown, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mesocycle?)null);

        return unknown;
    }

    private void VerifyNothingSaved()
    {
        _mesocycles.Verify(repository => repository.Save(It.IsAny<Mesocycle>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_ShouldReplaceTheDetailsAndKeepTheWeeks()
    {
        // Arrange
        var trainerId = _trainer.Id;

        // Act
        var view = await Update(trainerId, Role.TRAINER, 6);

        // Assert
        view.Mesocycle.Name.Value.Should().Be("Renamed");
        view.Mesocycle.Objective.Value.Should().Be("New objective");
        view.Mesocycle.StartDate.Should().Be(new DateTime(2026, 11, 16));
        view.Mesocycle.DurationInWeeks.Value.Should().Be(6);
        view.Mesocycle.Weeks.Select(week => week.WeekNumber.Value).Should().Equal(2);
        view.Mesocycle.TrainerId.Should().Be(_trainer.Id);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_ShouldRejectADurationBelowADefinedWeekAndKeepTheName()
    {
        // Arrange
        var trainerId = _trainer.Id;

        // Act
        var act = () => Update(trainerId, Role.TRAINER, 1);

        // Assert
        await act.Should().ThrowAsync<MesocycleDurationTooShortException>();
        _mesocycle.Name.Value.Should().Be("Block");
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Update_ShouldLetAnAdminChangeAnyMesocycle()
    {
        // Arrange
        var adminId = Id.New();

        // Act
        var view = await Update(adminId, Role.ADMIN, 4);

        // Assert
        view.Mesocycle.Name.Value.Should().Be("Renamed");
    }

    [Fact]
    public async Task Update_ShouldRefuseAnotherTrainer()
    {
        // Arrange
        var otherTrainerId = Id.New();

        // Act
        var act = () => Update(otherTrainerId, Role.TRAINER, 4);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Update_ShouldRefuseTheAthlete()
    {
        // Arrange
        var athleteId = _athlete.Id;

        // Act
        var act = () => Update(athleteId, Role.USER, 4);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Update_ShouldNotFindAnUnknownMesocycleBeforeCheckingPermissions()
    {
        // Arrange
        var unknown = GivenUnknownMesocycle();

        // Act
        var act = () => Update(Id.New(), Role.USER, 4, unknown);

        // Assert
        await act.Should().ThrowAsync<MesocycleNotFoundException>();
    }

    [Fact]
    public async Task Define_ShouldAddANewWeek()
    {
        // Arrange
        var trainerId = _trainer.Id;

        // Act
        var view = await Define(trainerId, Role.TRAINER, 3);

        // Assert
        view.Mesocycle.Weeks.Select(week => week.WeekNumber.Value).Should().Equal(2, 3);
        view.Mesocycle.Weeks.Last().TrainingWeekType.Should().Be(TrainingWeekType.INTENSIFICATION);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Define_ShouldRejectAWeekAlreadyDefined()
    {
        // Arrange
        var trainerId = _trainer.Id;

        // Act
        var act = () => Define(trainerId, Role.TRAINER, 2);

        // Assert
        await act.Should().ThrowAsync<DuplicateMesocycleWeekException>();
        _mesocycle.Weeks.Single().TrainingWeekType.Should().Be(TrainingWeekType.ACCUMULATION);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Define_ShouldRejectAWeekBeyondTheDuration()
    {
        // Arrange
        var trainerId = _trainer.Id;

        // Act
        var act = () => Define(trainerId, Role.TRAINER, 5);

        // Assert
        await act.Should().ThrowAsync<MesocycleWeekOutOfRangeException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Define_ShouldRefuseAnotherTrainer()
    {
        // Arrange
        var otherTrainerId = Id.New();

        // Act
        var act = () => Define(otherTrainerId, Role.TRAINER, 3);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Define_ShouldNotFindAnUnknownMesocycle()
    {
        // Arrange
        var unknown = GivenUnknownMesocycle();

        // Act
        var act = () => Define(_trainer.Id, Role.TRAINER, 3, unknown);

        // Assert
        await act.Should().ThrowAsync<MesocycleNotFoundException>();
    }

    [Fact]
    public async Task Define_ShouldLetAnAdminDefineAWeekOnAnyMesocycle()
    {
        // Arrange
        var adminId = Id.New();

        // Act
        var view = await Define(adminId, Role.ADMIN, 1);

        // Assert
        view.Mesocycle.Weeks.Select(week => week.WeekNumber.Value).Should().Equal(1, 2);
    }

    [Fact]
    public async Task Redefine_ShouldReplaceTheTypeAndObjectiveOfADefinedWeek()
    {
        // Arrange
        var trainerId = _trainer.Id;

        // Act
        var view = await Redefine(trainerId, Role.TRAINER, 2);

        // Assert
        var week = view.Mesocycle.Weeks.Single();
        week.WeekNumber.Value.Should().Be(2);
        week.TrainingWeekType.Should().Be(TrainingWeekType.DELOAD);
        week.Objective.Value.Should().Be("Back off");
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Redefine_ShouldNotFindAWeekThatIsNotDefined()
    {
        // Arrange
        var trainerId = _trainer.Id;

        // Act
        var act = () => Redefine(trainerId, Role.TRAINER, 3);

        // Assert
        await act.Should().ThrowAsync<MesocycleWeekNotFoundException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Redefine_ShouldRefuseTheAthlete()
    {
        // Arrange
        var athleteId = _athlete.Id;

        // Act
        var act = () => Redefine(athleteId, Role.USER, 2);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Remove_ShouldRemoveAWeekWithoutWorkouts()
    {
        // Arrange
        var trainerId = _trainer.Id;

        // Act
        await Remove(trainerId, Role.TRAINER, 2);

        // Assert
        _mesocycle.Weeks.Should().BeEmpty();
        _mesocycle.DurationInWeeks.Value.Should().Be(4);
        _mesocycles.Verify(repository => repository.Save(_mesocycle, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Remove_ShouldLetAnAdminRemoveAWeekOfAnyMesocycle()
    {
        // Arrange
        var adminId = Id.New();

        // Act
        await Remove(adminId, Role.ADMIN, 2);

        // Assert
        _mesocycle.Weeks.Should().BeEmpty();
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Remove_ShouldRefuseTheAthlete()
    {
        // Arrange
        var athleteId = _athlete.Id;

        // Act
        var act = () => Remove(athleteId, Role.USER, 2);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        _mesocycle.Weeks.Should().ContainSingle();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Remove_ShouldRefuseAnotherTrainer()
    {
        // Arrange
        var otherTrainerId = Id.New();

        // Act
        var act = () => Remove(otherTrainerId, Role.TRAINER, 2);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        _mesocycle.Weeks.Should().ContainSingle();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Remove_ShouldNotFindAnUnknownMesocycle()
    {
        // Arrange
        var unknown = GivenUnknownMesocycle();

        // Act
        var act = () => Remove(_trainer.Id, Role.TRAINER, 2, unknown);

        // Assert
        await act.Should().ThrowAsync<MesocycleNotFoundException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Remove_ShouldNotFindAWeekThatIsNotDefinedWithoutLookingForWorkouts()
    {
        // Arrange
        GivenWorkoutsInWeek(3);

        // Act
        var act = () => Remove(_trainer.Id, Role.TRAINER, 3);

        // Assert
        await act.Should().ThrowAsync<MesocycleWeekNotFoundException>();
        _workouts.Verify(repository => repository.ExistsByMesocycleWeek(
            It.IsAny<Id>(), It.IsAny<WeekNumber>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Remove_ShouldRejectAWeekWithWorkoutsAndKeepIt()
    {
        // Arrange
        GivenWorkoutsInWeek(2);

        // Act
        var act = () => Remove(_trainer.Id, Role.TRAINER, 2);

        // Assert
        await act.Should().ThrowAsync<MesocycleWeekHasWorkoutsException>();
        _mesocycle.Weeks.Should().ContainSingle(week => week.WeekNumber == WeekNumber.From(2));
        VerifyNothingSaved();
    }
}
