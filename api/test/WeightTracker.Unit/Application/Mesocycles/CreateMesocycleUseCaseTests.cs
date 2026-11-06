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
using WeightTracker.Domain.TrainerAthletes;
using WeightTracker.Domain.Users;
using Xunit;
using static WeightTracker.Unit.Application.Mesocycles.MesocycleTestUsers;

namespace WeightTracker.Unit.Application.Mesocycles;

public class CreateMesocycleUseCaseTests
{
    private static readonly MesocycleName Name = MesocycleName.From("Hypertrophy block");

    private static readonly MesocycleObjective Objective = MesocycleObjective.From("Build muscle");

    private static readonly DateTime StartDate = new(2026, 11, 9);

    private static readonly DurationInWeeks Duration = DurationInWeeks.From(4);

    private readonly Mock<IMesocycleRepository> _mesocycles = new(MockBehavior.Strict);
    private readonly Mock<ITrainerAthleteRepository> _relationships = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    private readonly User _trainer = AStoredUser(Role.TRAINER);
    private readonly User _athlete = AStoredUser(Role.USER);
    private readonly User _admin = AStoredUser(Role.ADMIN);

    public CreateMesocycleUseCaseTests()
    {
        _mesocycles.Setup(repository => repository.Save(It.IsAny<Mesocycle>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private CreateMesocycleUseCase AUseCase(params User[] extra)
    {
        var users = UsersHolding([_trainer, _athlete, _admin, .. extra]).Object;

        return new(_mesocycles.Object, _relationships.Object, new MesocycleAssignmentValidator(users),
            new MesocycleViewLoader(users), _unitOfWork.Object);
    }

    private void GivenActiveRelationship(bool exists)
        => _relationships
            .Setup(repository => repository.GetActiveByTrainerIdAndUserId(_trainer.Id, _athlete.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exists ? TrainerAthlete.Create(_trainer.Id, _athlete.Id, Now) : null);

    private Task<MesocycleView> Execute(CreateMesocycleUseCase useCase, Id callerId, Role role, Id? trainerId, Id athleteId)
        => useCase.Execute(callerId, role, trainerId, athleteId, Name, Objective, StartDate, Duration, CancellationToken.None);

    private void VerifyNothingSaved()
    {
        _mesocycles.Verify(repository => repository.Save(It.IsAny<Mesocycle>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Trainer_ShouldCreateAMesocycleForAnAthleteTheyActivelyTrain()
    {
        // Arrange
        GivenActiveRelationship(true);

        // Act
        var view = await Execute(AUseCase(), _trainer.Id, Role.TRAINER, null, _athlete.Id);

        // Assert
        view.Mesocycle.TrainerId.Should().Be(_trainer.Id);
        view.Mesocycle.AthleteId.Should().Be(_athlete.Id);
        view.Mesocycle.Weeks.Should().BeEmpty();
        view.Trainer.Should().Be(_trainer);
        view.Athlete.Should().Be(_athlete);
        _mesocycles.Verify(repository => repository.Save(view.Mesocycle, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Trainer_ShouldBeRefusedWithoutAnActiveRelationship()
    {
        // Arrange
        GivenActiveRelationship(false);

        // Act
        var act = () => Execute(AUseCase(), _trainer.Id, Role.TRAINER, null, _athlete.Id);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Trainer_ShouldNotSendATrainerId()
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        var act = () => Execute(useCase, _trainer.Id, Role.TRAINER, _trainer.Id, _athlete.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidMesocycleAssignmentException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task User_ShouldBeRefused()
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        var act = () => Execute(useCase, _athlete.Id, Role.USER, null, _athlete.Id);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Admin_ShouldCreateAMesocycleWithoutARelationship()
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        var view = await Execute(useCase, _admin.Id, Role.ADMIN, _trainer.Id, _athlete.Id);

        // Assert
        view.Mesocycle.TrainerId.Should().Be(_trainer.Id);
        view.Mesocycle.AthleteId.Should().Be(_athlete.Id);
        _relationships.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Admin_ShouldSendATrainerId()
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        var act = () => Execute(useCase, _admin.Id, Role.ADMIN, null, _athlete.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidMesocycleAssignmentException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Admin_ShouldNotNameAnAdminAsTrainer()
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        var act = () => Execute(useCase, _admin.Id, Role.ADMIN, _admin.Id, _athlete.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidMesocycleAssignmentException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Admin_ShouldNotCreateForAnUnknownAthlete()
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        var act = () => Execute(useCase, _admin.Id, Role.ADMIN, _trainer.Id, Id.New());

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Admin_ShouldNotCreateForAnAthleteWhoIsATrainer()
    {
        // Arrange
        var otherTrainer = AStoredUser(Role.TRAINER);
        var useCase = AUseCase(otherTrainer);

        // Act
        var act = () => Execute(useCase, _admin.Id, Role.ADMIN, _trainer.Id, otherTrainer.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidMesocycleAssignmentException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Admin_ShouldNotCreateForADeactivatedAthlete()
    {
        // Arrange
        var deactivated = AStoredUser(Role.USER, isActive: false);
        var useCase = AUseCase(deactivated);

        // Act
        var act = () => Execute(useCase, _admin.Id, Role.ADMIN, _trainer.Id, deactivated.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidMesocycleAssignmentException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Admin_ShouldNotNameAnUnknownTrainer()
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        var act = () => Execute(useCase, _admin.Id, Role.ADMIN, Id.New(), _athlete.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidMesocycleAssignmentException>();
        VerifyNothingSaved();
    }
}
