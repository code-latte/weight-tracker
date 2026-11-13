using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts.Observations;
using Xunit;
using static WeightTracker.Unit.Domain.Workouts.Observations.ObservationTestData;

namespace WeightTracker.Unit.Domain.Workouts.Observations;

public class WorkoutObservationGeneratorHighlightTests
{
    private static Entry ThreeOfTen(Id exercise, decimal weight, int reps = 10)
        => Exercise(exercise, [Sets(3, 10)], Rep(reps, weight), Rep(reps, weight), Rep(reps, weight));

    private static GeneratedObservations GenerateFor(Id exercise, ExerciseExecution[] history, decimal weight, int reps = 10)
        => Generator().Generate(CompletedWorkout(ThreeOfTen(exercise, weight, reps)), null, HistoryOf(exercise, history), CreatedAt);

    private static ExerciseExecution Missed(int daysAgo) => Earlier(daysAgo, (3, 10), [(10, 50), (8, 50), (8, 50)]);

    [Fact]
    public void Generate_ShouldGiveAConfirmedWeightDeclineHighPriority()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(bench, [TopOf(7, 84), TopOf(14, 100)], 82);

        // Assert
        Observation(result, ObservationCode.WEIGHT_DECREASE, bench).Priority.Should().Be(HighlightPriority.HIGH);
    }

    [Fact]
    public void Generate_ShouldNotGiveASingleBadSessionAPriority()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(bench, [TopOf(7, 100), TopOf(14, 100)], 80);

        // Assert
        Observation(result, ObservationCode.WEIGHT_DECREASE, bench).Priority.Should().BeNull();
        result.Highlights.Should().BeEmpty();
    }

    [Fact]
    public void Generate_ShouldNotGiveADeclinePriorityWithoutABaseline()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(bench, [TopOf(7, 100)], 70);

        // Assert
        Observation(result, ObservationCode.WEIGHT_DECREASE, bench).Priority.Should().BeNull();
    }

    [Fact]
    public void Generate_ShouldGiveASignificantWeightRecordMediumPriority()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(bench, [TopOf(7, 120)], 130);

        // Assert
        Observation(result, ObservationCode.WEIGHT_PERSONAL_RECORD, bench).Priority.Should().Be(HighlightPriority.MEDIUM);
    }

    [Fact]
    public void Generate_ShouldNotGiveASmallWeightRecordAPriority()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(bench, [TopOf(7, 120)], 124);

        // Assert
        Observation(result, ObservationCode.WEIGHT_PERSONAL_RECORD, bench).Priority.Should().BeNull();
        result.Highlights.Should().BeEmpty();
    }

    [Fact]
    public void Generate_ShouldGiveExceptionalVolumeProgressionMediumPriorityAndASmallOneNone()
    {
        // Arrange
        var squat = Id.New();
        var other = Id.New();

        // Act
        var big = GenerateFor(squat, [Earlier(7, (3, 10), [(10, 80), (10, 80), (10, 80)])], 89.6m);
        var small = GenerateFor(other, [Earlier(7, (3, 10), [(10, 80), (10, 80), (10, 80)])], 84m);

        // Assert
        Observation(big, ObservationCode.VOLUME_INCREASE, squat).Priority.Should().Be(HighlightPriority.MEDIUM);
        Observation(small, ObservationCode.VOLUME_INCREASE, other).Priority.Should().BeNull();
    }

    [Fact]
    public void Generate_ShouldGivePerfectComplianceLowPriorityOnlyWhenItEndsRepeatedMisses()
    {
        // Arrange
        var bench = Id.New();
        var other = Id.New();

        // Act
        var afterMisses = GenerateFor(bench, [Missed(7), Missed(14), Missed(21)], 50);
        var routine = GenerateFor(other, [TopOf(7, 50)], 50);

        // Assert
        Observation(afterMisses, ObservationCode.ALL_TARGETS_MET).Priority.Should().Be(HighlightPriority.LOW);
        afterMisses.Highlights.Should().Contain(Observation(afterMisses, ObservationCode.ALL_TARGETS_MET).Id);
        Observation(routine, ObservationCode.ALL_TARGETS_MET).Priority.Should().BeNull();
    }

    [Fact]
    public void Generate_ShouldRankHighlightsByPriorityAndKeepAtMostThree()
    {
        // Arrange
        var missing = Id.New();
        var repeated = Id.New();
        var downtrend = Id.New();
        var recordA = Id.New();
        var recordB = Id.New();
        var workout = CompletedWorkout(
            Exercise(repeated, [Sets(3, 10)], Rep(10, 50), Rep(8, 50), Rep(8, 50)),
            ThreeOfTen(downtrend, 95),
            ThreeOfTen(recordA, 130),
            ThreeOfTen(recordB, 140),
            Exercise(missing, [Sets(20, 10)]));
        var history = new Dictionary<Id, ExerciseHistory>
        {
            [repeated] = ExerciseHistory.Of([Missed(7), Missed(14)]),
            [downtrend] = ExerciseHistory.Of([TopOf(7, 100), TopOf(14, 105), TopOf(21, 110)]),
            [recordA] = ExerciseHistory.Of([TopOf(7, 120)]),
            [recordB] = ExerciseHistory.Of([TopOf(7, 120)]),
        };

        // Act
        var result = Generator().Generate(workout, null, history, CreatedAt);

        // Assert
        var highlighted = result.Highlights
            .Select(id => result.Observations.Single(observation => observation.Id == id))
            .Select(observation => (observation.Code, observation.ExerciseId))
            .ToList();

        highlighted.Should().Equal(
            (ObservationCode.WORKOUT_INCOMPLETE, (Id?)null),
            (ObservationCode.REPEATED_TARGET_MISS, repeated),
            (ObservationCode.WEIGHT_DOWNTREND, downtrend));
        Has(result, ObservationCode.WEIGHT_PERSONAL_RECORD, recordA).Should().BeTrue();
        Has(result, ObservationCode.WEIGHT_PERSONAL_RECORD, recordB).Should().BeTrue();
    }

    [Fact]
    public void Generate_ShouldOrderTheSamePriorityByMagnitudeDescending()
    {
        // Arrange
        var small = Id.New();
        var big = Id.New();
        var workout = CompletedWorkout(ThreeOfTen(small, 130), ThreeOfTen(big, 140));
        var history = new Dictionary<Id, ExerciseHistory>
        {
            [small] = ExerciseHistory.Of([TopOf(7, 120)]),
            [big] = ExerciseHistory.Of([TopOf(7, 120)]),
        };

        // Act
        var result = Generator().Generate(workout, null, history, CreatedAt);

        // Assert
        result.Highlights.Select(id => result.Observations.Single(observation => observation.Id == id).ExerciseId)
            .Should().Equal(big, small);
    }

    [Fact]
    public void Generate_ShouldHighlightOnlyOneObservationPerExercise()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(bench, [Earlier(7, (3, 10), [(10, 120), (10, 120), (10, 120)])], 130, reps: 11);

        // Assert
        Has(result, ObservationCode.WEIGHT_PERSONAL_RECORD, bench).Should().BeTrue();
        Has(result, ObservationCode.VOLUME_INCREASE, bench).Should().BeTrue();
        result.Highlights.Should().ContainSingle();
        Observation(result, ObservationCode.VOLUME_INCREASE, bench).IsHighlighted.Should().BeFalse("ranks are assigned by the aggregate");
        result.Highlights.Single().Should().Be(Observation(result, ObservationCode.VOLUME_INCREASE, bench).Id);
    }

    [Fact]
    public void Generate_ShouldNeverPadHighlightsWithNonEligibleObservations()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(bench, [TopOf(7, 100)], 101);

        // Assert
        result.Observations.Should().NotBeEmpty();
        result.Highlights.Should().BeEmpty();
    }

    [Fact]
    public void Generate_ShouldHighlightASingleEligibleObservation()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(bench, [TopOf(7, 120)], 130);

        // Assert
        result.Highlights.Should().ContainSingle();
    }

    [Fact]
    public void Generate_ShouldProduceHighlightsTheAggregateAccepts()
    {
        // Arrange
        var bench = Id.New();
        var result = GenerateFor(bench, [TopOf(7, 120)], 130);

        // Act
        var observations = WorkoutObservations.Create(Id.New(), result.Observations, result.Highlights);

        // Assert
        observations.Highlights.Should().ContainSingle().Which.HighlightRank.Should().Be(1);
    }
}
