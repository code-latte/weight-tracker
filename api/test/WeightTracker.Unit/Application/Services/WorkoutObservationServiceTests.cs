using FluentAssertions;
using Moq;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts;
using WeightTracker.Domain.Workouts.Observations;
using Xunit;
using static WeightTracker.Unit.Domain.Workouts.Observations.ObservationTestData;

namespace WeightTracker.Unit.Application.Services;

public class WorkoutObservationServiceTests
{
    private static readonly DateTime Now = new(2026, 11, 13, 12, 0, 0, DateTimeKind.Utc);

    private static readonly ObservationThresholds Thresholds = new(3, 4, 3, 70, 15, 5, 10);

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);
    private readonly Mock<IExerciseExecutionHistoryQuery> _history = new(MockBehavior.Strict);
    private readonly Mock<IWorkoutObservationsRepository> _observations = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);
    private readonly Mock<IClock> _clock = new();

    public WorkoutObservationServiceTests() => _clock.Setup(clock => clock.UtcNow).Returns(Now);

    private WorkoutObservationService AService()
        => new(_workouts.Object, _history.Object, _observations.Object, new WorkoutObservationGenerator(Thresholds), Thresholds, _unitOfWork.Object, _clock.Object);

    private static Workout APrescribedWorkout(Id exerciseId)
        => CompletedWorkout(Exercise(exerciseId, [Sets(3, 10)], Rep(10, 50), Rep(10, 50), Rep(10, 50)));

    private static Workout AWorkoutWithoutTrainer(Id exerciseId)
        => Workout.Rehydrate(
            Id.New(), Id.New(), null, WorkoutName.From("Own plan"), null, new DateTime(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc),
            WorkoutStatus.COMPLETED, [WorkoutExercise.Rehydrate(Id.New(), exerciseId, Effort.MID, [Sets(3, 10)], setRecords: [Rep(10, 50)])]);

    private void GivenStored(Workout workout)
        => _workouts.Setup(repository => repository.GetById(workout.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workout);

    private void GivenHistory(Workout workout, ExecutionHistoryView view)
        => _history
            .Setup(query => query.Get(
                workout.UserId,
                workout.TrainerId!,
                workout.Id,
                DateOnly.FromDateTime(workout.WorkoutDate),
                It.Is<IReadOnlyCollection<Id>>(ids => ids.SequenceEqual(workout.Exercises.Select(entry => entry.ExerciseId))),
                Thresholds.HistoryDepth,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(view);

    private void GivenNothingStored(Workout workout)
        => _observations.Setup(repository => repository.GetByWorkoutId(workout.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkoutObservations?)null);

    private void GivenSaveAndCommitSucceed(Action<WorkoutObservations> capture)
    {
        _observations.Setup(repository => repository.Save(It.IsAny<WorkoutObservations>(), It.IsAny<CancellationToken>()))
            .Callback((WorkoutObservations saved, CancellationToken _) => capture(saved))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Generate_ShouldStoreTheGeneratedObservationsOfAPrescribedWorkoutAndCommitOnce()
    {
        // Arrange
        var bench = Id.New();
        var workout = APrescribedWorkout(bench);
        GivenStored(workout);
        GivenHistory(workout, new ExecutionHistoryView(null, new Dictionary<Id, ExerciseHistory>()));
        GivenNothingStored(workout);
        WorkoutObservations? saved = null;
        GivenSaveAndCommitSucceed(observations => saved = observations);

        // Act
        await AService().Generate(workout.Id, CancellationToken.None);

        // Assert
        saved.Should().NotBeNull();
        saved!.IsNew.Should().BeTrue();
        saved.WorkoutId.Should().Be(workout.Id);
        saved.Observations.Select(observation => observation.Code).Should().Equal(ObservationCode.ALL_TARGETS_MET);
        saved.Observations.Single().CreatedAt.Should().Be(Now);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Generate_ShouldGiveTheRulesTheHistoryAndWeekTheQueryReturned()
    {
        // Arrange
        var bench = Id.New();
        var workout = APrescribedWorkout(bench);
        GivenStored(workout);
        var history = new Dictionary<Id, ExerciseHistory> { [bench] = ExerciseHistory.Of([TopOf(7, 40)]) };
        GivenHistory(workout, new ExecutionHistoryView(null, history));
        GivenNothingStored(workout);
        WorkoutObservations? saved = null;
        GivenSaveAndCommitSucceed(observations => saved = observations);

        // Act
        await AService().Generate(workout.Id, CancellationToken.None);

        // Assert
        saved!.Observations.Select(observation => observation.Code).Should().Contain(ObservationCode.WEIGHT_INCREASE);
    }

    [Fact]
    public async Task Generate_ShouldReplaceTheStoredObservationsOfAWorkoutCompletedAgain()
    {
        // Arrange
        var bench = Id.New();
        var workout = APrescribedWorkout(bench);
        GivenStored(workout);
        GivenHistory(workout, new ExecutionHistoryView(null, new Dictionary<Id, ExerciseHistory>()));
        var old = WorkoutObservation.Create(null, ObservationCode.WORKOUT_INCOMPLETE, ObservationSeverity.WARNING, null, ObservationEvidence.Empty, Now);
        var stored = WorkoutObservations.Rehydrate(workout.Id, [WorkoutObservation.Rehydrate(
            old.Id, null, old.Code, old.Severity, null, null, ObservationEvidence.Empty, Now)]);
        _observations.Setup(repository => repository.GetByWorkoutId(workout.Id, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
        WorkoutObservations? saved = null;
        GivenSaveAndCommitSucceed(observations => saved = observations);

        // Act
        await AService().Generate(workout.Id, CancellationToken.None);

        // Assert
        saved.Should().BeSameAs(stored);
        saved!.IsNew.Should().BeFalse();
        saved.Observations.Select(observation => observation.Code).Should().Equal(ObservationCode.ALL_TARGETS_MET);
        saved.Observations.Should().NotContain(observation => observation.Id == old.Id);
    }

    [Fact]
    public async Task Generate_ShouldStoreNothingForAWorkoutWithoutTrainerThatHasNoObservations()
    {
        // Arrange
        var workout = AWorkoutWithoutTrainer(Id.New());
        GivenStored(workout);
        GivenNothingStored(workout);

        // Act
        await AService().Generate(workout.Id, CancellationToken.None);

        // Assert
        _history.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();
        _observations.Verify(repository => repository.Save(It.IsAny<WorkoutObservations>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Generate_ShouldClearTheObservationsOfAWorkoutThatNoLongerHasATrainer()
    {
        // Arrange
        var workout = AWorkoutWithoutTrainer(Id.New());
        GivenStored(workout);
        var stored = WorkoutObservations.Rehydrate(workout.Id, [WorkoutObservation.Rehydrate(
            Id.New(), null, ObservationCode.WORKOUT_INCOMPLETE, ObservationSeverity.WARNING, null, null, ObservationEvidence.Empty, Now)]);
        _observations.Setup(repository => repository.GetByWorkoutId(workout.Id, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
        WorkoutObservations? saved = null;
        GivenSaveAndCommitSucceed(observations => saved = observations);

        // Act
        await AService().Generate(workout.Id, CancellationToken.None);

        // Assert
        saved!.Observations.Should().BeEmpty();
        _history.VerifyNoOtherCalls();
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Generate_ShouldStoreNothingForAPrescribedWorkoutWithoutExercises()
    {
        // Arrange
        var workout = CompletedWorkout();
        GivenStored(workout);
        GivenNothingStored(workout);

        // Act
        await AService().Generate(workout.Id, CancellationToken.None);

        // Assert
        _history.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Generate_ShouldRejectAnUnknownWorkoutWithoutStoringAnything()
    {
        // Arrange
        var workoutId = Id.New();
        _workouts.Setup(repository => repository.GetById(workoutId, It.IsAny<CancellationToken>())).ReturnsAsync((Workout?)null);

        // Act
        var act = () => AService().Generate(workoutId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutNotFoundException>();
        _observations.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Generate_ShouldNotCommitWhenTheHistoryCannotBeRead()
    {
        // Arrange
        var workout = APrescribedWorkout(Id.New());
        GivenStored(workout);
        _history
            .Setup(query => query.Get(
                It.IsAny<Id>(), It.IsAny<Id>(), It.IsAny<Id>(), It.IsAny<DateOnly>(), It.IsAny<IReadOnlyCollection<Id>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database down"));

        // Act
        var act = () => AService().Generate(workout.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _unitOfWork.VerifyNoOtherCalls();
    }
}
