using FluentAssertions;
using Moq;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Workouts;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Application.Workouts;

public class DeleteWorkoutUseCaseTests
{
    private static readonly DateTime WorkoutDate = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private static readonly Id OwnerId = Id.New();

    private static readonly Id TrainerId = Id.New();

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    /// <summary>A stored workout in the given status, prescribed by the trainer unless <paramref name="prescribed"/> is false.</summary>
    private static Workout AStoredWorkout(WorkoutStatus status = WorkoutStatus.DRAFT, bool prescribed = true)
        => Workout.Rehydrate(Id.New(), OwnerId, prescribed ? TrainerId : null, Name, null, WorkoutDate, status, [],
            reviewedAt: status == WorkoutStatus.REVIEWED ? WorkoutDate : null,
            reviewedBy: status == WorkoutStatus.REVIEWED ? TrainerId : null);

    private void GivenStored(Workout workout)
        => _workouts.Setup(repository => repository.GetById(workout.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workout);

    private void GivenDeleteAndCommitSucceed(Workout workout)
    {
        _workouts.Setup(repository => repository.Delete(workout, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private void VerifyNothingDeletedOrCommitted()
    {
        _workouts.Verify(repository => repository.Delete(It.IsAny<Workout>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.VerifyNoOtherCalls();
    }

    private DeleteWorkoutUseCase AUseCase() => new(_workouts.Object, new WorkoutEditAuthorizer(), _unitOfWork.Object);

    [Fact]
    public async Task Execute_ShouldLetThePrescribingTrainerDeleteADraftAndCommitOnce()
    {
        // Arrange
        var workout = AStoredWorkout();
        GivenStored(workout);
        GivenDeleteAndCommitSucceed(workout);

        // Act
        await AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, CancellationToken.None);

        // Assert
        _workouts.Verify(repository => repository.Delete(workout, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldLetAnAdminDeleteADraft()
    {
        // Arrange
        var workout = AStoredWorkout();
        GivenStored(workout);
        GivenDeleteAndCommitSucceed(workout);

        // Act
        await AUseCase().Execute(Id.New(), Role.ADMIN, workout.Id, CancellationToken.None);

        // Assert
        _workouts.Verify(repository => repository.Delete(workout, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldLetTheOwnerDeleteADraftWithoutTrainer()
    {
        // Arrange
        var workout = AStoredWorkout(prescribed: false);
        GivenStored(workout);
        GivenDeleteAndCommitSucceed(workout);

        // Act
        await AUseCase().Execute(OwnerId, Role.USER, workout.Id, CancellationToken.None);

        // Assert
        _workouts.Verify(repository => repository.Delete(workout, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldRejectAnUnknownWorkoutWithoutDeletingOrCommitting()
    {
        // Arrange
        var workoutId = Id.New();
        _workouts.Setup(repository => repository.GetById(workoutId, It.IsAny<CancellationToken>())).ReturnsAsync((Workout?)null);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, workoutId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutNotFoundException>();
        VerifyNothingDeletedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectTheOwnerOfAPrescribedWorkout()
    {
        // Arrange
        var workout = AStoredWorkout();
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(OwnerId, Role.USER, workout.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingDeletedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnotherTrainer()
    {
        // Arrange
        var workout = AStoredWorkout();
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.TRAINER, workout.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingDeletedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAUserWhoDoesNotOwnAWorkoutWithoutTrainer()
    {
        // Arrange
        var workout = AStoredWorkout(prescribed: false);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.USER, workout.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingDeletedOrCommitted();
    }

    [Theory]
    [InlineData(WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.COMPLETED)]
    [InlineData(WorkoutStatus.REVIEWED)]
    public async Task Execute_ShouldRejectAnAdminOnAWorkoutThatIsNoLongerADraft(WorkoutStatus status)
    {
        // Arrange
        var workout = AStoredWorkout(status);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.ADMIN, workout.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutDeletionLockedException>();
        VerifyNothingDeletedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectThePrescribingTrainerOnAPublishedWorkout()
    {
        // Arrange
        var workout = AStoredWorkout(WorkoutStatus.PUBLISHED);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutDeletionLockedException>();
        VerifyNothingDeletedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRefuseACallerWhoMayNotDeleteAPublishedWorkoutWithoutRevealingTheLock()
    {
        // Arrange
        var workout = AStoredWorkout(WorkoutStatus.PUBLISHED);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(OwnerId, Role.USER, workout.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingDeletedOrCommitted();
    }
}
