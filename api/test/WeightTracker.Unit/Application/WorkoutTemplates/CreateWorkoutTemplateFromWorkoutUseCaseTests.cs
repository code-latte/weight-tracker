using FluentAssertions;
using Moq;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.WorkoutTemplates;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;
using static WeightTracker.Unit.Application.Mesocycles.MesocycleTestUsers;

namespace WeightTracker.Unit.Application.WorkoutTemplates;

public class CreateWorkoutTemplateFromWorkoutUseCaseTests
{
    private static readonly DateTime WorkoutDate = new(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);
    private readonly Mock<IWorkoutTemplateRepository> _templates = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    private readonly User _trainer = AStoredUser(Role.TRAINER);
    private readonly User _otherTrainer = AStoredUser(Role.TRAINER);
    private readonly User _admin = AStoredUser(Role.ADMIN);
    private readonly User _athlete = AStoredUser(Role.USER);
    private readonly User _deactivatedTrainer = AStoredUser(Role.TRAINER, isActive: false);

    public CreateWorkoutTemplateFromWorkoutUseCaseTests()
    {
        _templates.Setup(repository => repository.Save(It.IsAny<WorkoutTemplate>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private Workout GivenWorkout(Id ownerId, Id? trainerId)
    {
        var workout = Workout.Create(ownerId, trainerId, WorkoutName.From("Leg day"), WorkoutDescription.From("Heavy"), WorkoutDate);
        workout.AddExercise(Id.New(), Effort.HIGH, [new SetPrescription(4, new SetTarget.Reps(8))]);
        workout.AddExercise(Id.New(), Effort.MID, [new SetPrescription(4, new SetTarget.Reps(10))]);
        workout.CreateSuperSeries(workout.Exercises.Select(entry => entry.Id));
        _workouts.Setup(repository => repository.GetById(workout.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workout);

        return workout;
    }

    private Task<WorkoutTemplate> Execute(Id callerId, Role role, Id workoutId)
        => new CreateWorkoutTemplateFromWorkoutUseCase(
                _workouts.Object, _templates.Object,
                new WorkoutTemplateTrainerValidator(UsersHolding(_trainer, _otherTrainer, _admin, _athlete, _deactivatedTrainer).Object),
                _unitOfWork.Object)
            .Execute(callerId, role, workoutId, CancellationToken.None);

    private void VerifyNothingSaved()
    {
        _templates.Verify(repository => repository.Save(It.IsAny<WorkoutTemplate>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_ShouldSaveTheTrainersWorkoutAsTheirTemplate()
    {
        // Arrange
        var workout = GivenWorkout(_athlete.Id, _trainer.Id);

        // Act
        var template = await Execute(_trainer.Id, Role.TRAINER, workout.Id);

        // Assert
        template.TrainerId.Should().Be(_trainer.Id);
        template.Name.Should().Be(workout.Name);
        template.Exercises.Select(entry => entry.ExerciseId).Should().Equal(workout.Exercises.Select(entry => entry.ExerciseId));
        _templates.Verify(repository => repository.Save(template, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _workouts.Verify(repository => repository.Save(It.IsAny<Workout>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_ShouldGiveTheTemplateToTheWorkoutTrainerWhenAnAdminSavesIt()
    {
        // Arrange
        var workout = GivenWorkout(_athlete.Id, _trainer.Id);

        // Act
        var template = await Execute(_admin.Id, Role.ADMIN, workout.Id);

        // Assert
        template.TrainerId.Should().Be(_trainer.Id);
    }

    [Fact]
    public async Task Execute_ShouldForbidAnotherTrainer()
    {
        // Arrange
        var workout = GivenWorkout(_athlete.Id, _trainer.Id);

        // Act
        var act = () => Execute(_otherTrainer.Id, Role.TRAINER, workout.Id);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Execute_ShouldForbidATrainerOnTheirOwnWorkoutWithoutTrainer()
    {
        // Arrange
        var workout = GivenWorkout(_trainer.Id, null);

        // Act
        var act = () => Execute(_trainer.Id, Role.TRAINER, workout.Id);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnAdminOnAWorkoutWithoutTrainer()
    {
        // Arrange
        var workout = GivenWorkout(_athlete.Id, null);

        // Act
        var act = () => Execute(_admin.Id, Role.ADMIN, workout.Id);

        // Assert
        await act.Should().ThrowAsync<WorkoutWithoutTrainerException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Execute_ShouldRejectAWorkoutWhoseTrainerIsAnAdmin()
    {
        // Arrange
        var workout = GivenWorkout(_athlete.Id, _admin.Id);

        // Act
        var act = () => Execute(_admin.Id, Role.ADMIN, workout.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutTemplateTrainerException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Execute_ShouldRejectAWorkoutWhoseTrainerIsDeactivated()
    {
        // Arrange
        var workout = GivenWorkout(_athlete.Id, _deactivatedTrainer.Id);

        // Act
        var act = () => Execute(_admin.Id, Role.ADMIN, workout.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutTemplateTrainerException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Execute_ShouldRefuseARegularUserOwningTheWorkout()
    {
        // Arrange
        var workout = GivenWorkout(_athlete.Id, _trainer.Id);

        // Act
        var act = () => Execute(_athlete.Id, Role.USER, workout.Id);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnUnknownWorkout()
    {
        // Arrange
        var unknown = Id.New();
        _workouts.Setup(repository => repository.GetById(unknown, It.IsAny<CancellationToken>())).ReturnsAsync((Workout?)null);

        // Act
        var act = () => Execute(_admin.Id, Role.ADMIN, unknown);

        // Assert
        await act.Should().ThrowAsync<WorkoutNotFoundException>();
        VerifyNothingSaved();
    }
}
