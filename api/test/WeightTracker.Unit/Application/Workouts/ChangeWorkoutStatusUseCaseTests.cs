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

public class ChangeWorkoutStatusUseCaseTests
{
    private static readonly DateTime WorkoutDate = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private static readonly Id AthleteId = Id.New();

    private static readonly Id TrainerId = Id.New();

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    private static readonly DateTime Now = new(2026, 11, 10, 7, 45, 0, DateTimeKind.Utc);
    private readonly Mock<IClock> _clock = new();

    public ChangeWorkoutStatusUseCaseTests() => _clock.Setup(clock => clock.UtcNow).Returns(Now);

    /// <summary>A stored workout prescribed by the trainer.</summary>
    private static Workout AStoredWorkoutIn(WorkoutStatus status)
        => Workout.Rehydrate(Id.New(), AthleteId, TrainerId, Name, null, WorkoutDate, status, []);

    private void GivenStored(Workout workout)
        => _workouts.Setup(repository => repository.GetById(workout.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workout);

    private void GivenSaveAndCommitSucceed(Workout workout)
    {
        _workouts.Setup(repository => repository.Save(workout, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private void VerifyNothingSavedOrCommitted()
    {
        _workouts.Verify(repository => repository.Save(It.IsAny<Workout>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.VerifyNoOtherCalls();
    }

    private ChangeWorkoutStatusUseCase AUseCase() => new(_workouts.Object, new WorkoutStatusAuthorizer(), _unitOfWork.Object, _clock.Object);

    [Fact]
    public async Task Execute_ShouldChangeTheStatusSaveAndCommitOnce()
    {
        // Arrange
        var workout = AStoredWorkoutIn(WorkoutStatus.DRAFT);
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var updated = await AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, WorkoutStatus.PUBLISHED, CancellationToken.None);

        // Assert
        updated.Status.Should().Be(WorkoutStatus.PUBLISHED);
        _workouts.Verify(repository => repository.Save(workout, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldLetAnAdminSkipTheLifecycle()
    {
        // Arrange
        var workout = AStoredWorkoutIn(WorkoutStatus.COMPLETED);
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var updated = await AUseCase().Execute(Id.New(), Role.ADMIN, workout.Id, WorkoutStatus.DRAFT, CancellationToken.None);

        // Assert
        updated.Status.Should().Be(WorkoutStatus.DRAFT);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldCompleteAtTheClockTime()
    {
        // Arrange
        var workout = AStoredWorkoutIn(WorkoutStatus.STARTED);
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var updated = await AUseCase().Execute(AthleteId, Role.USER, workout.Id, WorkoutStatus.COMPLETED, CancellationToken.None);

        // Assert
        updated.Status.Should().Be(WorkoutStatus.COMPLETED);
        updated.CompletedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Execute_ShouldLetAnAdminCompleteAtTheClockTime()
    {
        // Arrange
        var workout = AStoredWorkoutIn(WorkoutStatus.DRAFT);
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var updated = await AUseCase().Execute(Id.New(), Role.ADMIN, workout.Id, WorkoutStatus.COMPLETED, CancellationToken.None);

        // Assert
        updated.Status.Should().Be(WorkoutStatus.COMPLETED);
        updated.CompletedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Execute_ShouldStartAtTheClockTime()
    {
        // Arrange
        var workout = AStoredWorkoutIn(WorkoutStatus.PUBLISHED);
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var updated = await AUseCase().Execute(AthleteId, Role.USER, workout.Id, WorkoutStatus.STARTED, CancellationToken.None);

        // Assert
        updated.Status.Should().Be(WorkoutStatus.STARTED);
        updated.StartedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Execute_ShouldLetAnAdminStartAtTheClockTime()
    {
        // Arrange
        var workout = AStoredWorkoutIn(WorkoutStatus.DRAFT);
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var updated = await AUseCase().Execute(Id.New(), Role.ADMIN, workout.Id, WorkoutStatus.STARTED, CancellationToken.None);

        // Assert
        updated.Status.Should().Be(WorkoutStatus.STARTED);
        updated.StartedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Execute_ShouldRejectAnUnknownWorkoutWithoutSavingOrCommitting()
    {
        // Arrange
        var workoutId = Id.New();
        _workouts.Setup(repository => repository.GetById(workoutId, It.IsAny<CancellationToken>())).ReturnsAsync((Workout?)null);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, workoutId, WorkoutStatus.PUBLISHED, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectACallerWhoseRoleForbidsTheMoveAndChangeNothing()
    {
        // Arrange
        var workout = AStoredWorkoutIn(WorkoutStatus.PUBLISHED);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(AthleteId, Role.USER, workout.Id, WorkoutStatus.DRAFT, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        workout.Status.Should().Be(WorkoutStatus.PUBLISHED);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAMoveOutsideTheLifecycleAndChangeNothing()
    {
        // Arrange
        var workout = AStoredWorkoutIn(WorkoutStatus.DRAFT);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, WorkoutStatus.COMPLETED, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutStatusTransitionException>();
        workout.Status.Should().Be(WorkoutStatus.DRAFT);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnUndefinedStatusFromAnAdminAndChangeNothing()
    {
        // Arrange
        var workout = AStoredWorkoutIn(WorkoutStatus.DRAFT);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.ADMIN, workout.Id, (WorkoutStatus)99, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutStatusException>();
        workout.Status.Should().Be(WorkoutStatus.DRAFT);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldPropagateAnAdminSettingReviewedWithoutSavingOrCommitting()
    {
        // Arrange
        var workout = AStoredWorkoutIn(WorkoutStatus.COMPLETED);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.ADMIN, workout.Id, WorkoutStatus.REVIEWED, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutStatusTransitionException>();
        workout.Status.Should().Be(WorkoutStatus.COMPLETED);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldPropagateTheTrainerSettingReviewedWithoutSavingOrCommitting()
    {
        // Arrange
        var workout = AStoredWorkoutIn(WorkoutStatus.COMPLETED);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, WorkoutStatus.REVIEWED, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutStatusTransitionException>();
        workout.Status.Should().Be(WorkoutStatus.COMPLETED);
        VerifyNothingSavedOrCommitted();
    }
}
