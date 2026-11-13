using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Workouts.Observations;
using Xunit;

namespace WeightTracker.Unit.Domain.Workouts.Observations;

public class WorkoutObservationsTests
{
    private static readonly DateTime CreatedAt = new(2026, 11, 13, 10, 0, 0, DateTimeKind.Utc);

    private static WorkoutObservation AnObservation(
        ObservationCode code = ObservationCode.WEIGHT_INCREASE,
        HighlightPriority? priority = HighlightPriority.MEDIUM,
        Id? exerciseId = null)
        => WorkoutObservation.Create(exerciseId ?? Id.New(), code, ObservationSeverity.INFO, priority, ObservationEvidence.Empty, CreatedAt);

    [Fact]
    public void Create_ShouldIdentifyTheAggregateByTheWorkoutAndMarkItAsNew()
    {
        // Arrange
        var workoutId = Id.New();

        // Act
        var observations = WorkoutObservations.Create(workoutId, [AnObservation()], []);

        // Assert
        observations.Id.Should().Be(workoutId);
        observations.WorkoutId.Should().Be(workoutId);
        observations.IsNew.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldRankTheHighlightsInTheOrderGiven()
    {
        // Arrange
        var first = AnObservation(ObservationCode.WEIGHT_INCREASE);
        var second = AnObservation(ObservationCode.VOLUME_INCREASE);
        var third = AnObservation(ObservationCode.REPS_INCREASE);

        // Act
        var observations = WorkoutObservations.Create(Id.New(), [first, second, third], [third.Id, first.Id]);

        // Assert
        third.HighlightRank.Should().Be(1);
        first.HighlightRank.Should().Be(2);
        second.HighlightRank.Should().BeNull();
        observations.Highlights.Should().Equal(third, first);
        observations.Observations.Should().HaveCount(3);
    }

    [Fact]
    public void Create_ShouldAcceptNoHighlights()
    {
        // Arrange
        // Act
        var observations = WorkoutObservations.Create(Id.New(), [AnObservation()], []);

        // Assert
        observations.Highlights.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldRejectMoreThanThreeHighlights()
    {
        // Arrange
        var entries = Enumerable.Range(0, 4).Select(_ => AnObservation()).ToList();

        // Act
        var act = () => WorkoutObservations.Create(Id.New(), entries, entries.Select(entry => entry.Id).ToList());

        // Assert
        act.Should().Throw<InvalidWorkoutObservationException>();
    }

    [Fact]
    public void Create_ShouldRejectAHighlightWithoutPriority()
    {
        // Arrange
        var routine = AnObservation(priority: null);

        // Act
        var act = () => WorkoutObservations.Create(Id.New(), [routine], [routine.Id]);

        // Assert
        act.Should().Throw<InvalidWorkoutObservationException>();
    }

    [Fact]
    public void Create_ShouldRejectTheSameHighlightTwice()
    {
        // Arrange
        var entry = AnObservation();

        // Act
        var act = () => WorkoutObservations.Create(Id.New(), [entry], [entry.Id, entry.Id]);

        // Assert
        act.Should().Throw<InvalidWorkoutObservationException>();
    }

    [Fact]
    public void Create_ShouldRejectAHighlightThatIsNotAnObservation()
    {
        // Arrange
        var entry = AnObservation();

        // Act
        var act = () => WorkoutObservations.Create(Id.New(), [entry], [Id.New()]);

        // Assert
        act.Should().Throw<InvalidWorkoutObservationException>();
    }

    [Fact]
    public void Create_ShouldRejectTwoObservationsWithTheSameCodeAndExercise()
    {
        // Arrange
        var exerciseId = Id.New();
        var first = AnObservation(ObservationCode.WEIGHT_INCREASE, exerciseId: exerciseId);
        var second = AnObservation(ObservationCode.WEIGHT_INCREASE, exerciseId: exerciseId);

        // Act
        var act = () => WorkoutObservations.Create(Id.New(), [first, second], []);

        // Assert
        act.Should().Throw<InvalidWorkoutObservationException>();
    }

    [Fact]
    public void Create_ShouldAcceptTheSameCodeOnDifferentExercises()
    {
        // Arrange
        var first = AnObservation(ObservationCode.WEIGHT_INCREASE);
        var second = AnObservation(ObservationCode.WEIGHT_INCREASE);

        // Act
        var observations = WorkoutObservations.Create(Id.New(), [first, second], []);

        // Assert
        observations.Observations.Should().HaveCount(2);
    }

    [Fact]
    public void Create_ShouldRejectObservationsThatAreAlreadyHighlighted()
    {
        // Arrange
        var entry = AnObservation();
        WorkoutObservations.Create(Id.New(), [entry], [entry.Id]);

        // Act
        var act = () => WorkoutObservations.Create(Id.New(), [entry], []);

        // Assert
        act.Should().Throw<InvalidWorkoutObservationException>();
    }

    [Fact]
    public void Replace_ShouldSwapEveryObservationAndHighlight()
    {
        // Arrange
        var old = AnObservation();
        var observations = WorkoutObservations.Create(Id.New(), [old], [old.Id]);
        var replacement = AnObservation(ObservationCode.VOLUME_DECREASE, HighlightPriority.HIGH);

        // Act
        observations.Replace([replacement], [replacement.Id]);

        // Assert
        observations.Observations.Should().Equal(replacement);
        observations.Highlights.Should().Equal(replacement);
        replacement.Id.Should().NotBe(old.Id);
    }

    [Fact]
    public void Replace_ShouldChangeNothingWhenTheNewOnesAreInvalid()
    {
        // Arrange
        var old = AnObservation();
        var observations = WorkoutObservations.Create(Id.New(), [old], [old.Id]);
        var routine = AnObservation(priority: null);

        // Act
        var act = () => observations.Replace([routine], [routine.Id]);

        // Assert
        act.Should().Throw<InvalidWorkoutObservationException>();
        observations.Observations.Should().Equal(old);
        observations.Highlights.Should().Equal(old);
    }

    [Fact]
    public void Rehydrate_ShouldKeepIdsAndRanksAndNotBeNew()
    {
        // Arrange
        var workoutId = Id.New();
        var first = Stored(rank: 2);
        var second = Stored(rank: 1);
        var routine = Stored(rank: null);

        // Act
        var observations = WorkoutObservations.Rehydrate(workoutId, [first, second, routine]);

        // Assert
        observations.IsNew.Should().BeFalse();
        observations.WorkoutId.Should().Be(workoutId);
        observations.Highlights.Should().Equal(second, first);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public void Rehydrate_ShouldRejectRanksThatDoNotStartAtOneOrExceedThree(int rank)
    {
        // Arrange
        var entry = Stored(rank);

        // Act
        var act = () => WorkoutObservations.Rehydrate(Id.New(), [entry]);

        // Assert
        act.Should().Throw<InvalidWorkoutObservationException>();
    }

    [Fact]
    public void Rehydrate_ShouldRejectDuplicateRanks()
    {
        // Arrange
        var first = Stored(rank: 1);
        var second = Stored(rank: 1);

        // Act
        var act = () => WorkoutObservations.Rehydrate(Id.New(), [first, second]);

        // Assert
        act.Should().Throw<InvalidWorkoutObservationException>();
    }

    [Fact]
    public void Rehydrate_ShouldRejectARankWithoutPriority()
    {
        // Arrange
        var entry = Stored(rank: 1, priority: null);

        // Act
        var act = () => WorkoutObservations.Rehydrate(Id.New(), [entry]);

        // Assert
        act.Should().Throw<InvalidWorkoutObservationException>();
    }

    [Fact]
    public void Observation_ShouldRejectAnUndefinedCodeSeverityOrPriority()
    {
        // Arrange
        // Act
        var code = () => WorkoutObservation.Create(null, (ObservationCode)99, ObservationSeverity.INFO, null, ObservationEvidence.Empty, CreatedAt);
        var severity = () => WorkoutObservation.Create(null, ObservationCode.WORKOUT_INCOMPLETE, (ObservationSeverity)99, null, ObservationEvidence.Empty, CreatedAt);
        var priority = () => WorkoutObservation.Create(null, ObservationCode.WORKOUT_INCOMPLETE, ObservationSeverity.INFO, (HighlightPriority)99, ObservationEvidence.Empty, CreatedAt);

        // Assert
        code.Should().Throw<InvalidWorkoutObservationException>();
        severity.Should().Throw<InvalidWorkoutObservationException>();
        priority.Should().Throw<InvalidWorkoutObservationException>();
    }

    private static WorkoutObservation Stored(int? rank, HighlightPriority? priority = HighlightPriority.HIGH)
        => WorkoutObservation.Rehydrate(
            Id.New(), Id.New(), ObservationCode.WEIGHT_DECREASE, ObservationSeverity.INFO, priority, rank, ObservationEvidence.Empty, CreatedAt);
}
