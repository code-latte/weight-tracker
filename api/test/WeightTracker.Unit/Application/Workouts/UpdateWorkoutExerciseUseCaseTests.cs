using FluentAssertions;
using Moq;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Workouts;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Application.Workouts;

public class UpdateWorkoutExerciseUseCaseTests
{
    private static readonly DateTime WorkoutDate = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private static readonly Id OwnerId = Id.New();

    private static readonly Id TrainerId = Id.New();

    private static readonly SetPrescription ThreeByTen = new(3, new SetTarget.Reps(10));

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    /// <summary>A stored workout prescribed by the trainer, with one exercise.</summary>
    private static Workout AStoredWorkout(Id exerciseId)
        => Workout.Rehydrate(Id.New(), OwnerId, TrainerId, Name, null, WorkoutDate, WorkoutStatus.DRAFT,
            [WorkoutExercise.Rehydrate(Id.New(), exerciseId, Effort.MID, [ThreeByTen])]);

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

    private UpdateWorkoutExerciseUseCase AUseCase() => new(_workouts.Object, new WorkoutEditAuthorizer(), _unitOfWork.Object);

    [Fact]
    public async Task Execute_ShouldReplaceEffortAndSetsAndCommitOnce()
    {
        // Arrange
        var exerciseId = Id.New();
        var workout = AStoredWorkout(exerciseId);
        var twoByTwelve = new SetPrescription(2, new SetTarget.Reps(12));
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var entry = await AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, exerciseId, exerciseId, Effort.HIGH, [twoByTwelve], false, null, CancellationToken.None);

        // Assert
        entry.Effort.Should().Be(Effort.HIGH);
        entry.Sets.Should().Equal(twoByTwelve);
        _workouts.Verify(repository => repository.Save(workout, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldRejectARouteAndBodyThatNameDifferentExercisesBeforeLoadingAnything()
    {
        // Arrange
        var workoutId = Id.New();

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, workoutId, Id.New(), Id.New(), Effort.MID, [ThreeByTen], false, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseIdMismatchException>();
        _workouts.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnUnknownWorkoutWithoutSavingOrCommitting()
    {
        // Arrange
        var workoutId = Id.New();
        var exerciseId = Id.New();
        _workouts.Setup(repository => repository.GetById(workoutId, It.IsAny<CancellationToken>())).ReturnsAsync((Workout?)null);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, workoutId, exerciseId, exerciseId, Effort.MID, [ThreeByTen], false, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectTheOwnerOfAPrescribedWorkoutAndChangeNothing()
    {
        // Arrange
        var exerciseId = Id.New();
        var workout = AStoredWorkout(exerciseId);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(OwnerId, Role.USER, workout.Id, exerciseId, exerciseId, Effort.HIGH, [ThreeByTen], false, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        workout.Exercises.Single().Effort.Should().Be(Effort.MID);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnExerciseNotInTheWorkoutWithoutSavingOrCommitting()
    {
        // Arrange
        var workout = AStoredWorkout(Id.New());
        var absent = Id.New();
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, absent, absent, Effort.MID, [ThreeByTen], false, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotInWorkoutException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldReplaceThePerSideFlagAndTheRest()
    {
        // Arrange
        var exerciseId = Id.New();
        var workout = AStoredWorkout(exerciseId);
        var rest = TimeInMinutes.From(2m);
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var entry = await AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, exerciseId, exerciseId, Effort.MID, [ThreeByTen], true, rest, CancellationToken.None);

        // Assert
        entry.PerSide.Should().BeTrue();
        entry.Rest.Should().Be(rest);
    }
}
