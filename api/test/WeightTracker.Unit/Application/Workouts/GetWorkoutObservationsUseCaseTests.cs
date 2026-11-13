using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Workouts;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using WeightTracker.Domain.Workouts.Observations;
using Xunit;

namespace WeightTracker.Unit.Application.Workouts;

public class GetWorkoutObservationsUseCaseTests
{
    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private static readonly DateTime CreatedAt = new(2026, 11, 13, 10, 0, 0, DateTimeKind.Utc);

    private static readonly Id AthleteId = Id.New();

    private static readonly Id TrainerId = Id.New();

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);

    // Strict: a read the use case must not make fails the test
    private readonly Mock<IWorkoutObservationsRepository> _observations = new(MockBehavior.Strict);

    private GetWorkoutObservationsUseCase AUseCase()
        => new(_workouts.Object, new WorkoutVisibility(_workouts.Object), _observations.Object);

    private Workout Stored(Id ownerId, Id? trainerId, WorkoutStatus status, params Id[] exerciseIds)
    {
        var workout = Workout.Rehydrate(
            Id.New(), ownerId, trainerId, Name, null, new DateTime(2026, 11, 13), status,
            exerciseIds.Select(exerciseId => WorkoutExercise.Rehydrate(Id.New(), exerciseId, Effort.MID, [new SetPrescription(3, new SetTarget.Reps(10))])));

        _workouts.Setup(repository => repository.GetById(workout.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workout);

        return workout;
    }

    private void GivenObservations(Workout workout, WorkoutObservations? observations)
        => _observations.Setup(repository => repository.GetByWorkoutId(workout.Id, It.IsAny<CancellationToken>())).ReturnsAsync(observations);

    private static WorkoutObservation AnObservation(Id? exerciseId, ObservationCode code, HighlightPriority? priority = null)
        => WorkoutObservation.Create(exerciseId, code, ObservationSeverity.INFO, priority, ObservationEvidence.Empty.With("changePercent", 5m), CreatedAt);

    [Fact]
    public async Task Execute_ShouldThrowWhenTheWorkoutDoesNotExist()
    {
        // Arrange
        var workoutId = Id.New();
        _workouts.Setup(repository => repository.GetById(workoutId, It.IsAny<CancellationToken>())).ReturnsAsync((Workout?)null);

        // Act
        var act = () => AUseCase().Execute(AthleteId, Role.USER, workoutId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutNotFoundException>();
        _observations.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(Role.USER)]
    [InlineData(Role.TRAINER)]
    public async Task Execute_ShouldRefuseACallerWhoCannotSeeTheWorkoutWithoutReadingObservations(Role role)
    {
        // Arrange
        var workout = Stored(AthleteId, TrainerId, WorkoutStatus.COMPLETED);

        // Act
        var act = () => AUseCase().Execute(Id.New(), role, workout.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        _observations.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldRefuseAnAthleteAPrescribedDraftNotYetPublished()
    {
        // Arrange
        var workout = Stored(AthleteId, TrainerId, WorkoutStatus.DRAFT);

        // Act
        var act = () => AUseCase().Execute(AthleteId, Role.USER, workout.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        _observations.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(Role.USER)]
    [InlineData(Role.TRAINER)]
    [InlineData(Role.ADMIN)]
    public async Task Execute_ShouldReturnEmptyListsWhenNothingWasGenerated(Role role)
    {
        // Arrange
        var workout = Stored(AthleteId, TrainerId, WorkoutStatus.STARTED);
        GivenObservations(workout, null);
        var callerId = role == Role.TRAINER ? TrainerId : AthleteId;

        // Act
        var view = await AUseCase().Execute(callerId, role, workout.Id, CancellationToken.None);

        // Assert
        view.Observations.Should().BeEmpty();
        view.Highlights.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldOrderObservationsWorkoutFirstThenByExercisePositionThenByCode()
    {
        // Arrange
        var first = Id.New();
        var second = Id.New();
        var workout = Stored(AthleteId, TrainerId, WorkoutStatus.COMPLETED, first, second);
        var secondIncrease = AnObservation(second, ObservationCode.WEIGHT_INCREASE);
        var firstVolume = AnObservation(first, ObservationCode.VOLUME_INCREASE);
        var firstWeight = AnObservation(first, ObservationCode.WEIGHT_INCREASE);
        var incomplete = AnObservation(null, ObservationCode.WORKOUT_INCOMPLETE);
        GivenObservations(workout, WorkoutObservations.Create(workout.Id, [secondIncrease, firstVolume, firstWeight, incomplete], []));

        // Act
        var view = await AUseCase().Execute(AthleteId, Role.USER, workout.Id, CancellationToken.None);

        // Assert
        view.Observations.Select(observation => observation.Id).Should().Equal(
            incomplete.Id, firstWeight.Id, firstVolume.Id, secondIncrease.Id);
    }

    [Fact]
    public async Task Execute_ShouldReturnTheHighlightsByRankReferringToTheirObservations()
    {
        // Arrange
        var bench = Id.New();
        var squat = Id.New();
        var workout = Stored(AthleteId, TrainerId, WorkoutStatus.COMPLETED, bench, squat);
        var record = AnObservation(bench, ObservationCode.WEIGHT_PERSONAL_RECORD, HighlightPriority.MEDIUM);
        var downtrend = AnObservation(squat, ObservationCode.WEIGHT_DOWNTREND, HighlightPriority.HIGH);
        var routine = AnObservation(squat, ObservationCode.REPS_ABOVE_TARGET);
        GivenObservations(workout, WorkoutObservations.Create(workout.Id, [record, downtrend, routine], [downtrend.Id, record.Id]));

        // Act
        var view = await AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, CancellationToken.None);

        // Assert
        view.Highlights.Select(highlight => (highlight.ObservationId, highlight.Rank, highlight.Priority)).Should().Equal(
            (downtrend.Id, 1, HighlightPriority.HIGH),
            (record.Id, 2, HighlightPriority.MEDIUM));
        view.Highlights.Select(highlight => highlight.ObservationId).Should().BeSubsetOf(view.Observations.Select(observation => observation.Id));
        view.Observations.Single(observation => observation.Id == routine.Id).Priority.Should().BeNull();
        view.Observations.Single(observation => observation.Id == record.Id).Data.Number("changePercent").Should().Be(5m);
    }

    [Fact]
    public async Task Execute_ShouldPutObservationsOfAnExerciseNoLongerInTheWorkoutLast()
    {
        // Arrange
        var bench = Id.New();
        var removed = Id.New();
        var workout = Stored(AthleteId, TrainerId, WorkoutStatus.COMPLETED, bench);
        var ofRemoved = AnObservation(removed, ObservationCode.WEIGHT_INCREASE);
        var ofBench = AnObservation(bench, ObservationCode.WEIGHT_INCREASE);
        GivenObservations(workout, WorkoutObservations.Create(workout.Id, [ofRemoved, ofBench], []));

        // Act
        var view = await AUseCase().Execute(AthleteId, Role.USER, workout.Id, CancellationToken.None);

        // Assert
        view.Observations.Select(observation => observation.Id).Should().Equal(ofBench.Id, ofRemoved.Id);
    }
}
