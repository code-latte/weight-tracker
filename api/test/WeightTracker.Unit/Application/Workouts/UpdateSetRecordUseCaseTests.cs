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

public class UpdateSetRecordUseCaseTests
{
    private static readonly DateTime WorkoutDate = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly WorkoutName Name = WorkoutName.From("Leg day");
    private static readonly DateTime RecordedAt = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);

    private static readonly Id OwnerId = Id.New();

    private static readonly Id TrainerId = Id.New();

    private static readonly Id ExerciseId = Id.New();

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);
    private readonly Mock<IClock> _clock = new();

    private static readonly DateTime Now = new(2026, 11, 7, 18, 30, 0, DateTimeKind.Utc);

    public UpdateSetRecordUseCaseTests() => _clock.Setup(clock => clock.UtcNow).Returns(Now);

    private UpdateSetRecordUseCase AUseCase() => new(_workouts.Object, new WorkoutRecordingAuthorizer(), _unitOfWork.Object, _clock.Object);

    /// <summary>A stored workout with one rep-based exercise holding three set records, prescribed by the trainer unless told otherwise.</summary>
    private static Workout AStoredWorkout(out SetRecord[] setRecords, WorkoutStatus status = WorkoutStatus.STARTED, bool prescribed = true, Id? ownerId = null)
    {
        setRecords =
        [
            SetRecord.Rehydrate(Id.New(), RepCount.From(10), Weight.From(60), RecordedAt),
            SetRecord.Rehydrate(Id.New(), RepCount.From(8), Weight.From(62.5m), RecordedAt.AddMinutes(2)),
            SetRecord.Rehydrate(Id.New(), RepCount.From(7), Weight.From(65), RecordedAt.AddMinutes(4)),
        ];

        return Workout.Rehydrate(Id.New(), ownerId ?? OwnerId, prescribed ? TrainerId : null, Name, null, WorkoutDate, status,
            [WorkoutExercise.Rehydrate(Id.New(), ExerciseId, Effort.MID, [new SetPrescription(3, new SetTarget.Reps(10))], setRecords: setRecords)],
            reviewedAt: status == WorkoutStatus.REVIEWED ? RecordedAt : null,
            reviewedBy: status == WorkoutStatus.REVIEWED ? TrainerId : null);
    }

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

    private static IReadOnlyList<SetRecord> SetRecordsOf(Workout workout) => workout.Exercises.Single().SetRecords;

    [Fact]
    public async Task Execute_ShouldReplaceRepsAndWeightKeepTheRestAndCommitOnce()
    {
        // Arrange
        var workout = AStoredWorkout(out var setRecords);
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var result = await AUseCase().Execute(OwnerId, Role.USER, workout.Id, ExerciseId, setRecords[1].Id, 9, 67.5m, CancellationToken.None);

        // Assert
        result.ResultsUpdatedAt.Should().Be(Now);
        result.Should().BeSameAs(workout);
        SetRecordsOf(workout).Should().Equal(setRecords);
        SetRecordsOf(workout)[1].Reps.Value.Should().Be(9);
        SetRecordsOf(workout)[1].Weight.Value.Should().Be(67.5m);
        SetRecordsOf(workout)[1].RegisteredAtUtc.Should().Be(RecordedAt.AddMinutes(2));
        _workouts.Verify(repository => repository.Save(workout, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0, 60)]
    [InlineData(8, -5)]
    public async Task Execute_ShouldRejectInvalidValuesBeforeLoadingTheWorkout(int reps, decimal weight)
    {
        // Arrange
        var workoutId = Id.New();

        // Act
        var act = () => AUseCase().Execute(OwnerId, Role.USER, workoutId, ExerciseId, Id.New(), reps, weight, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>();
        _workouts.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldPropagateAnUnknownSetRecordWithoutSavingOrCommitting()
    {
        // Arrange
        var workout = AStoredWorkout(out var setRecords);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(OwnerId, Role.USER, workout.Id, ExerciseId, Id.New(), 9, 67.5m, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SetRecordNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldLetTheOwnerOfAPrescribedWorkoutDoIt()
    {
        // Arrange
        var workout = AStoredWorkout(out var setRecords);
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        await AUseCase().Execute(OwnerId, Role.USER, workout.Id, ExerciseId, setRecords[1].Id, 9, 67.5m, CancellationToken.None);

        // Assert
        SetRecordsOf(workout)[1].Reps.Value.Should().Be(9);
        SetRecordsOf(workout)[1].Weight.Value.Should().Be(67.5m);
    }

    [Fact]
    public async Task Execute_ShouldLetTheOwnerOfAWorkoutWithoutTrainerDoIt()
    {
        // Arrange
        var workout = AStoredWorkout(out var setRecords, prescribed: false);
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        await AUseCase().Execute(OwnerId, Role.USER, workout.Id, ExerciseId, setRecords[1].Id, 9, 67.5m, CancellationToken.None);

        // Assert
        SetRecordsOf(workout)[1].Reps.Value.Should().Be(9);
        SetRecordsOf(workout)[1].Weight.Value.Should().Be(67.5m);
    }

    [Fact]
    public async Task Execute_ShouldLetAnAdminDoItOnAWorkoutTheyDoNotOwn()
    {
        // Arrange
        var workout = AStoredWorkout(out var setRecords);
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        await AUseCase().Execute(Id.New(), Role.ADMIN, workout.Id, ExerciseId, setRecords[1].Id, 9, 67.5m, CancellationToken.None);

        // Assert
        SetRecordsOf(workout)[1].Reps.Value.Should().Be(9);
        SetRecordsOf(workout)[1].Weight.Value.Should().Be(67.5m);
    }

    [Fact]
    public async Task Execute_ShouldRejectAnUnknownWorkoutWithoutSavingOrCommitting()
    {
        // Arrange
        var workout = AStoredWorkout(out var setRecords);
        _workouts.Setup(repository => repository.GetById(workout.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Workout?)null);

        // Act
        var act = () => AUseCase().Execute(OwnerId, Role.USER, workout.Id, ExerciseId, setRecords[1].Id, 9, 67.5m, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectThePrescribingTrainerWithoutChangingAnything()
    {
        // Arrange
        var workout = AStoredWorkout(out var setRecords);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, ExerciseId, setRecords[1].Id, 9, 67.5m, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        SetRecordsOf(workout).Should().Equal(setRecords);
        SetRecordsOf(workout).Select(record => record.Reps.Value).Should().Equal(10, 8, 7);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectATrainerWhoOwnsAWorkoutWithoutTrainer()
    {
        // Arrange
        var workout = AStoredWorkout(out var setRecords, prescribed: false, ownerId: TrainerId);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, ExerciseId, setRecords[1].Id, 9, 67.5m, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        SetRecordsOf(workout).Should().Equal(setRecords);
        SetRecordsOf(workout).Select(record => record.Reps.Value).Should().Equal(10, 8, 7);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAUserWhoDoesNotOwnTheWorkout()
    {
        // Arrange
        var workout = AStoredWorkout(out var setRecords, prescribed: false);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.USER, workout.Id, ExerciseId, setRecords[1].Id, 9, 67.5m, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        SetRecordsOf(workout).Should().Equal(setRecords);
        SetRecordsOf(workout).Select(record => record.Reps.Value).Should().Equal(10, 8, 7);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldCheckThePermissionBeforeTheStatus()
    {
        // Arrange
        var workout = AStoredWorkout(out var setRecords, WorkoutStatus.DRAFT, prescribed: false);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.USER, workout.Id, ExerciseId, setRecords[1].Id, 9, 67.5m, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldPropagateAReviewedWorkoutEvenForAnAdmin()
    {
        // Arrange
        var workout = AStoredWorkout(out var setRecords, WorkoutStatus.REVIEWED);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.ADMIN, workout.Id, ExerciseId, setRecords[1].Id, 9, 67.5m, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutResultsLockedException>();
        SetRecordsOf(workout).Should().Equal(setRecords);
        SetRecordsOf(workout).Select(record => record.Reps.Value).Should().Equal(10, 8, 7);
        VerifyNothingSavedOrCommitted();
    }
}
