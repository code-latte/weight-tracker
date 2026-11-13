using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Users;
using WeightTracker.Application.UseCases.Workouts;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Application.Workouts;

public class GetWorkoutSummaryUseCaseTests
{
    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private static readonly DateTime TenOClock = new(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);

    // Strict: a read the use case must not make fails the test
    private readonly Mock<IExercisePreviousExecutionQuery> _previous = new(MockBehavior.Strict);

    private GetWorkoutSummaryUseCase AUseCase() => new(_workouts.Object, new WorkoutVisibility(_workouts.Object), _previous.Object);

    private static SetRecord Record(int reps, decimal? weight, int minute = 0)
        => SetRecord.Rehydrate(Id.New(), new SetResult.Reps(RepCount.From(reps)), weight is null ? null : Weight.From(weight.Value), TenOClock.AddMinutes(minute));

    private static SetRecord TimedRecord(int seconds, decimal? weight)
        => SetRecord.Rehydrate(Id.New(), new SetResult.Duration(DurationInSeconds.From(seconds)), weight is null ? null : Weight.From(weight.Value), TenOClock);

    private static WorkoutExercise AnEntry(Id exerciseId, SetPrescription prescription, params SetRecord[] records)
        => WorkoutExercise.Rehydrate(Id.New(), exerciseId, Effort.MID, [prescription], setRecords: records);

    private Workout Stored(Id ownerId, Id? trainerId, WorkoutStatus status, params WorkoutExercise[] entries)
    {
        var workout = Workout.Rehydrate(Id.New(), ownerId, trainerId, Name, null, new DateTime(2026, 11, 10), status, entries);
        _workouts
            .Setup(repository => repository.GetById(workout.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workout);

        return workout;
    }

    /// <summary>The previous executions the projection returns, whatever it is asked.</summary>
    private void GivenPrevious(params ExercisePreviousExecutionView[] executions)
        => _previous
            .Setup(query => query.Get(It.IsAny<Id>(), It.IsAny<IReadOnlyCollection<Id>>(), It.IsAny<DateOnly>(), It.IsAny<Id>(), It.IsAny<Id?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(executions);

    [Fact]
    public async Task Execute_ShouldThrowWhenTheWorkoutDoesNotExist()
    {
        // Arrange
        var workoutId = Id.New();
        _workouts
            .Setup(repository => repository.GetById(workoutId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workout?)null);

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.USER, workoutId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutNotFoundException>();
    }

    [Fact]
    public async Task Execute_ShouldRefuseAWorkoutOfAnotherUserWithoutReadingPreviousExecutions()
    {
        // Arrange
        var workout = Stored(Id.New(), null, WorkoutStatus.COMPLETED, AnEntry(Id.New(), new SetPrescription(3, new SetTarget.Reps(10))));

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.USER, workout.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        _previous.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldRefuseAnUnpublishedPrescribedDraftToItsAthlete()
    {
        // Arrange
        var athleteId = Id.New();
        var workout = Stored(athleteId, Id.New(), WorkoutStatus.DRAFT, AnEntry(Id.New(), new SetPrescription(3, new SetTarget.Reps(10))));

        // Act
        var act = () => AUseCase().Execute(athleteId, Role.USER, workout.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        _previous.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldComputeTheStatsOfEachExerciseAndTheTotals()
    {
        // Arrange
        var ownerId = Id.New();
        var press = Id.New();
        var row = Id.New();
        var workout = Stored(ownerId, null, WorkoutStatus.COMPLETED,
            AnEntry(press, new SetPrescription(3, new SetTarget.Reps(10)), Record(10, 40m, 0), Record(9, 40m, 1), Record(12, 45m, 2)),
            AnEntry(row, new SetPrescription(2, new SetTarget.RepsRange(8, 12)), Record(8, 30m)));
        GivenPrevious();

        // Act
        var summary = await AUseCase().Execute(ownerId, Role.USER, workout.Id, CancellationToken.None);

        // Assert
        summary.WorkoutId.Should().Be(workout.Id);
        summary.SetsInTarget.Should().Be(new SetsInTargetView(3, 4));
        summary.Volume.Should().Be(1540m);
        summary.Exercises.Select(exercise => exercise.ExerciseId).Should().Equal(press, row);
        summary.Exercises[0].SetsInTarget.Should().Be(new SetsInTargetView(2, 3));
        summary.Exercises[0].Volume.Should().Be(1300m);
        summary.Exercises[0].TopSet.Should().Be(new LastPerformanceSetView(12, null, 45m));
        summary.Exercises[1].SetsInTarget.Should().Be(new SetsInTargetView(1, 1));
        summary.Exercises[1].Volume.Should().Be(240m);
        summary.Exercises[1].TopSet.Should().Be(new LastPerformanceSetView(8, null, 30m));
    }

    [Fact]
    public async Task Execute_ShouldReportZeroOfZeroAndNullsForAnExerciseWithoutRecords()
    {
        // Arrange
        var ownerId = Id.New();
        var workout = Stored(ownerId, null, WorkoutStatus.STARTED, AnEntry(Id.New(), new SetPrescription(3, new SetTarget.Reps(10))));
        GivenPrevious();

        // Act
        var summary = await AUseCase().Execute(ownerId, Role.USER, workout.Id, CancellationToken.None);

        // Assert
        summary.SetsInTarget.Should().Be(new SetsInTargetView(0, 0));
        summary.Volume.Should().BeNull();
        summary.Exercises.Single().Volume.Should().BeNull();
        summary.Exercises.Single().TopSet.Should().BeNull();
        summary.Exercises.Single().Previous.Should().BeNull();
    }

    [Fact]
    public async Task Execute_ShouldLeaveOutExercisesWithoutVolumeFromTheWorkoutVolume()
    {
        // Arrange
        var ownerId = Id.New();
        var workout = Stored(ownerId, null, WorkoutStatus.COMPLETED,
            AnEntry(Id.New(), new SetPrescription(1, new SetTarget.Reps(10)), Record(10, 40m)),
            AnEntry(Id.New(), new SetPrescription(1, new SetTarget.Reps(10)), Record(10, null)),
            AnEntry(Id.New(), new SetPrescription(1, new SetTarget.Duration(45)), TimedRecord(45, 10m)));
        GivenPrevious();

        // Act
        var summary = await AUseCase().Execute(ownerId, Role.USER, workout.Id, CancellationToken.None);

        // Assert
        summary.Volume.Should().Be(400m);
        summary.Exercises.Select(exercise => exercise.Volume).Should().Equal(400m, null, null);
    }

    [Fact]
    public async Task Execute_ShouldReturnTheTopSetOfTheExerciseWhenSomeSetsHaveNoWeight()
    {
        // Arrange
        var ownerId = Id.New();
        var workout = Stored(ownerId, null, WorkoutStatus.COMPLETED,
            AnEntry(Id.New(), new SetPrescription(2, new SetTarget.Reps(5)), Record(15, null, 0), Record(5, 0m, 1)));
        GivenPrevious();

        // Act
        var summary = await AUseCase().Execute(ownerId, Role.USER, workout.Id, CancellationToken.None);

        // Assert
        summary.Exercises.Single().TopSet.Should().Be(new LastPerformanceSetView(5, null, 0m));
    }

    [Fact]
    public async Task Execute_ShouldAttachTheTopSetOfThePreviousExecutionOfEachExercise()
    {
        // Arrange
        var ownerId = Id.New();
        var press = Id.New();
        var row = Id.New();
        var workout = Stored(ownerId, null, WorkoutStatus.COMPLETED,
            AnEntry(press, new SetPrescription(1, new SetTarget.Reps(10)), Record(10, 60m)),
            AnEntry(row, new SetPrescription(1, new SetTarget.Reps(10)), Record(10, 40m)));
        var previousWorkoutId = Id.New();
        GivenPrevious(new ExercisePreviousExecutionView(press, previousWorkoutId, new DateOnly(2026, 11, 6),
            [new LastPerformanceSetView(10, null, 55m), new LastPerformanceSetView(6, null, 62.5m), new LastPerformanceSetView(8, null, 62.5m)]));

        // Act
        var summary = await AUseCase().Execute(ownerId, Role.USER, workout.Id, CancellationToken.None);

        // Assert
        summary.Exercises[0].Previous.Should().Be(new PreviousExecutionView(previousWorkoutId, new DateOnly(2026, 11, 6), new LastPerformanceSetView(8, null, 62.5m)));
        summary.Exercises[1].Previous.Should().BeNull();
    }

    [Fact]
    public async Task Execute_ShouldNotReadPreviousExecutionsWhenTheWorkoutHasNoExercises()
    {
        // Arrange
        var ownerId = Id.New();
        var workout = Stored(ownerId, null, WorkoutStatus.STARTED);

        // Act
        var summary = await AUseCase().Execute(ownerId, Role.USER, workout.Id, CancellationToken.None);

        // Assert
        summary.Exercises.Should().BeEmpty();
        summary.SetsInTarget.Should().Be(new SetsInTargetView(0, 0));
        summary.Volume.Should().BeNull();
        _previous.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldReadThePreviousExecutionsOfTheOwnerBeforeTheWorkoutForAnOwner()
    {
        // Arrange
        var ownerId = Id.New();
        var exerciseId = Id.New();
        var workout = Stored(ownerId, Id.New(), WorkoutStatus.COMPLETED, AnEntry(exerciseId, new SetPrescription(1, new SetTarget.Reps(10)), Record(10, 40m)));
        Id? prescribedBy = Id.New();
        _previous
            .Setup(query => query.Get(ownerId, It.Is<IReadOnlyCollection<Id>>(ids => ids.SequenceEqual(new[] { exerciseId })), new DateOnly(2026, 11, 10), workout.Id, It.IsAny<Id?>(), It.IsAny<CancellationToken>()))
            .Callback((Id _, IReadOnlyCollection<Id> _, DateOnly _, Id _, Id? requested, CancellationToken _) => prescribedBy = requested)
            .ReturnsAsync([]);

        // Act
        await AUseCase().Execute(ownerId, Role.USER, workout.Id, CancellationToken.None);

        // Assert
        prescribedBy.Should().BeNull();
    }

    [Fact]
    public async Task Execute_ShouldNotScopeThePreviousExecutionsForAnAdmin()
    {
        // Arrange
        var workout = Stored(Id.New(), Id.New(), WorkoutStatus.COMPLETED, AnEntry(Id.New(), new SetPrescription(1, new SetTarget.Reps(10)), Record(10, 40m)));
        Id? prescribedBy = Id.New();
        _previous
            .Setup(query => query.Get(workout.UserId, It.IsAny<IReadOnlyCollection<Id>>(), It.IsAny<DateOnly>(), workout.Id, It.IsAny<Id?>(), It.IsAny<CancellationToken>()))
            .Callback((Id _, IReadOnlyCollection<Id> _, DateOnly _, Id _, Id? requested, CancellationToken _) => prescribedBy = requested)
            .ReturnsAsync([]);

        // Act
        await AUseCase().Execute(Id.New(), Role.ADMIN, workout.Id, CancellationToken.None);

        // Assert
        prescribedBy.Should().BeNull();
    }

    [Fact]
    public async Task Execute_ShouldScopeThePreviousExecutionsToTheTrainerWhenTheTrainerDoesNotOwnTheWorkout()
    {
        // Arrange
        var trainerId = Id.New();
        var workout = Stored(Id.New(), trainerId, WorkoutStatus.COMPLETED, AnEntry(Id.New(), new SetPrescription(1, new SetTarget.Reps(10)), Record(10, 40m)));
        Id? prescribedBy = null;
        _previous
            .Setup(query => query.Get(workout.UserId, It.IsAny<IReadOnlyCollection<Id>>(), It.IsAny<DateOnly>(), workout.Id, It.IsAny<Id?>(), It.IsAny<CancellationToken>()))
            .Callback((Id _, IReadOnlyCollection<Id> _, DateOnly _, Id _, Id? requested, CancellationToken _) => prescribedBy = requested)
            .ReturnsAsync([]);

        // Act
        await AUseCase().Execute(trainerId, Role.TRAINER, workout.Id, CancellationToken.None);

        // Assert
        prescribedBy.Should().Be(trainerId);
    }

    [Fact]
    public async Task Execute_ShouldNotScopeThePreviousExecutionsWhenTheTrainerOwnsTheWorkout()
    {
        // Arrange
        var trainerId = Id.New();
        var workout = Stored(trainerId, null, WorkoutStatus.COMPLETED, AnEntry(Id.New(), new SetPrescription(1, new SetTarget.Reps(10)), Record(10, 40m)));
        Id? prescribedBy = Id.New();
        _previous
            .Setup(query => query.Get(trainerId, It.IsAny<IReadOnlyCollection<Id>>(), It.IsAny<DateOnly>(), workout.Id, It.IsAny<Id?>(), It.IsAny<CancellationToken>()))
            .Callback((Id _, IReadOnlyCollection<Id> _, DateOnly _, Id _, Id? requested, CancellationToken _) => prescribedBy = requested)
            .ReturnsAsync([]);

        // Act
        await AUseCase().Execute(trainerId, Role.TRAINER, workout.Id, CancellationToken.None);

        // Assert
        prescribedBy.Should().BeNull();
    }
}
