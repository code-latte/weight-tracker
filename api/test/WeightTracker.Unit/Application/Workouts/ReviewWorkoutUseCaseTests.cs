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

public class ReviewWorkoutUseCaseTests
{
    private static readonly DateTime WorkoutDate = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private static readonly DateTime ResultsUpdatedAt = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime Now = new(2026, 11, 7, 18, 30, 0, DateTimeKind.Utc);

    private static readonly Id AthleteId = Id.New();

    private static readonly Id TrainerId = Id.New();

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);
    private readonly Mock<IClock> _clock = new();

    public ReviewWorkoutUseCaseTests() => _clock.Setup(clock => clock.UtcNow).Returns(Now);

    /// <summary>A stored completed workout whose results last changed at <see cref="ResultsUpdatedAt"/>.</summary>
    private static Workout AStoredCompletedWorkout(bool prescribed = true)
        => Workout.Rehydrate(Id.New(), AthleteId, prescribed ? TrainerId : null, Name, null, WorkoutDate, WorkoutStatus.COMPLETED, [],
            resultsUpdatedAt: ResultsUpdatedAt);

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

    private ReviewWorkoutUseCase AUseCase() => new(_workouts.Object, new WorkoutStatusAuthorizer(), _unitOfWork.Object, _clock.Object);

    [Fact]
    public async Task Execute_ShouldReviewAtTheClockTimeSaveAndCommitOnce()
    {
        // Arrange
        var workout = AStoredCompletedWorkout();
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var reviewed = await AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, ResultsUpdatedAt, CancellationToken.None);

        // Assert
        reviewed.Should().BeSameAs(workout);
        reviewed.Status.Should().Be(WorkoutStatus.REVIEWED);
        reviewed.ReviewedBy.Should().Be(TrainerId);
        reviewed.ReviewedAt.Should().Be(Now);
        _workouts.Verify(repository => repository.Save(workout, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldRecordTheAdminAsReviewer()
    {
        // Arrange
        var adminId = Id.New();
        var workout = AStoredCompletedWorkout();
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var reviewed = await AUseCase().Execute(adminId, Role.ADMIN, workout.Id, ResultsUpdatedAt, CancellationToken.None);

        // Assert
        reviewed.Status.Should().Be(WorkoutStatus.REVIEWED);
        reviewed.ReviewedBy.Should().Be(adminId);
    }

    [Fact]
    public async Task Execute_ShouldRejectAnUnknownWorkout()
    {
        // Arrange
        var workoutId = Id.New();
        _workouts.Setup(repository => repository.GetById(workoutId, It.IsAny<CancellationToken>())).ReturnsAsync((Workout?)null);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, workoutId, ResultsUpdatedAt, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRefuseTheAthleteBeforeLookingAtTheStatus()
    {
        // Arrange
        var workout = Workout.Rehydrate(Id.New(), AthleteId, TrainerId, Name, null, WorkoutDate, WorkoutStatus.STARTED, []);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(AthleteId, Role.USER, workout.Id, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        workout.Status.Should().Be(WorkoutStatus.STARTED);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldPropagateAWorkoutWithoutTrainerWithoutSavingOrCommitting()
    {
        // Arrange
        var workout = AStoredCompletedWorkout(prescribed: false);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.ADMIN, workout.Id, ResultsUpdatedAt, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutReviewNotAllowedException>();
        workout.Status.Should().Be(WorkoutStatus.COMPLETED);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldPropagateOutdatedResultsWithoutSavingOrCommitting()
    {
        // Arrange
        var workout = AStoredCompletedWorkout();
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, ResultsUpdatedAt.AddMinutes(-5), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutResultsChangedException>();
        workout.Status.Should().Be(WorkoutStatus.COMPLETED);
        VerifyNothingSavedOrCommitted();
    }
}
