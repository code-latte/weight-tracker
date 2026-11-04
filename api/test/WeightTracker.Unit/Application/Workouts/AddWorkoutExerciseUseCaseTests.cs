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

public class AddWorkoutExerciseUseCaseTests
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

    private readonly Mock<IExerciseRepository> _exercises = new(MockBehavior.Strict);

    private AddWorkoutExerciseUseCase AUseCase()
        => new(_workouts.Object, _exercises.Object, new WorkoutEditAuthorizer(), new ExerciseVisibility(new WorkoutVisibility(_workouts.Object)), _unitOfWork.Object);

    private void GivenInCatalog(Id exerciseId)
        => _exercises.Setup(repository => repository.GetById(exerciseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Exercise.Rehydrate(exerciseId, null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, []));

    private void GivenCustomInCatalog(Exercise exercise)
        => _exercises.Setup(repository => repository.GetById(exercise.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exercise);

    private static Exercise ACustomExercise(Id ownerId, DateTime? archivedAtUtc = null)
        => Exercise.Rehydrate(Id.New(), null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, [], ownerId, archivedAtUtc);

    [Fact]
    public async Task Execute_ShouldAppendTheExerciseAndCommitOnce()
    {
        // Arrange
        var workout = AStoredWorkout(Id.New());
        var exerciseId = Id.New();
        GivenStored(workout);
        GivenInCatalog(exerciseId);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var entry = await AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, exerciseId, Effort.HIGH, [ThreeByTen], false, null, CancellationToken.None);

        // Assert
        entry.ExerciseId.Should().Be(exerciseId);
        entry.Effort.Should().Be(Effort.HIGH);
        workout.Exercises.Last().Id.Should().Be(entry.Id);
        _workouts.Verify(repository => repository.Save(workout, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldRejectAnUnknownWorkoutWithoutSavingOrCommitting()
    {
        // Arrange
        var workoutId = Id.New();
        _workouts.Setup(repository => repository.GetById(workoutId, It.IsAny<CancellationToken>())).ReturnsAsync((Workout?)null);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, workoutId, Id.New(), Effort.MID, [ThreeByTen], false, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectTheOwnerOfAPrescribedWorkoutWithoutSavingOrCommitting()
    {
        // Arrange
        var workout = AStoredWorkout(Id.New());
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(OwnerId, Role.USER, workout.Id, Id.New(), Effort.MID, [ThreeByTen], false, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        workout.Exercises.Should().HaveCount(1);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnExerciseMissingFromTheCatalogWithoutSavingOrCommitting()
    {
        // Arrange
        var workout = AStoredWorkout(Id.New());
        var exerciseId = Id.New();
        GivenStored(workout);
        _exercises.Setup(repository => repository.GetById(exerciseId, It.IsAny<CancellationToken>())).ReturnsAsync((Exercise?)null);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, exerciseId, Effort.MID, [ThreeByTen], false, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnExerciseAlreadyInTheWorkoutWithoutSavingOrCommitting()
    {
        // Arrange
        var existing = Id.New();
        var workout = AStoredWorkout(existing);
        GivenStored(workout);
        GivenInCatalog(existing);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, existing, Effort.MID, [ThreeByTen], false, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseAlreadyInWorkoutException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectEmptySetsWithoutSavingOrCommitting()
    {
        // Arrange
        var workout = AStoredWorkout(Id.New());
        var exerciseId = Id.New();
        GivenStored(workout);
        GivenInCatalog(exerciseId);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, exerciseId, Effort.MID, [], false, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutExerciseRequiresSetsException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldStoreThePerSideFlagAndTheRest()
    {
        // Arrange
        var workout = AStoredWorkout(Id.New());
        var exerciseId = Id.New();
        var rest = TimeInMinutes.From(1.5m);
        GivenStored(workout);
        GivenInCatalog(exerciseId);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var entry = await AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, exerciseId, Effort.HIGH, [ThreeByTen], true, rest, CancellationToken.None);

        // Assert
        entry.PerSide.Should().BeTrue();
        entry.Rest.Should().Be(rest);
    }

    [Fact]
    public async Task Execute_ShouldAcceptACustomExerciseOwnedByThePrescribingTrainer()
    {
        // Arrange
        var workout = AStoredWorkout(Id.New());
        var custom = ACustomExercise(TrainerId);
        GivenStored(workout);
        GivenCustomInCatalog(custom);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var entry = await AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, custom.Id, Effort.MID, [ThreeByTen], false, null, CancellationToken.None);

        // Assert
        entry.ExerciseId.Should().Be(custom.Id);
    }

    [Fact]
    public async Task Execute_ShouldAcceptACustomExerciseOwnedByTheOwnerOfASelfPlannedWorkout()
    {
        // Arrange
        var workout = Workout.Rehydrate(Id.New(), OwnerId, null, Name, null, WorkoutDate, WorkoutStatus.DRAFT, []);
        var custom = ACustomExercise(OwnerId);
        GivenStored(workout);
        GivenCustomInCatalog(custom);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var entry = await AUseCase().Execute(OwnerId, Role.USER, workout.Id, custom.Id, Effort.MID, [ThreeByTen], false, null, CancellationToken.None);

        // Assert
        entry.ExerciseId.Should().Be(custom.Id);
    }

    [Fact]
    public async Task Execute_ShouldRejectACustomExerciseOfSomeoneElseThatTheCallerCanSee()
    {
        // Arrange
        var workout = AStoredWorkout(Id.New());
        var custom = ACustomExercise(Id.New());
        GivenStored(workout);
        GivenCustomInCatalog(custom);

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.ADMIN, workout.Id, custom.Id, Effort.MID, [ThreeByTen], false, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotUsableInWorkoutException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectTheOwnersExerciseInAWorkoutPrescribedByATrainer()
    {
        // Arrange
        var workout = AStoredWorkout(Id.New());
        var custom = ACustomExercise(OwnerId);
        GivenStored(workout);
        GivenCustomInCatalog(custom);

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.ADMIN, workout.Id, custom.Id, Effort.MID, [ThreeByTen], false, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotUsableInWorkoutException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnArchivedCustomExercise()
    {
        // Arrange
        var workout = AStoredWorkout(Id.New());
        var custom = ACustomExercise(TrainerId, archivedAtUtc: new DateTime(2026, 11, 1));
        GivenStored(workout);
        GivenCustomInCatalog(custom);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, custom.Id, Effort.MID, [ThreeByTen], false, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseArchivedException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldTreatACustomExerciseTheCallerCannotSeeAsMissing()
    {
        // Arrange
        var workout = AStoredWorkout(Id.New());
        var custom = ACustomExercise(Id.New());
        GivenStored(workout);
        GivenCustomInCatalog(custom);
        _workouts.Setup(repository => repository.GetAllByUserOrTrainerId(TrainerId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, custom.Id, Effort.MID, [ThreeByTen], false, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }
}
