using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Workouts.Observations;
using Xunit;
using static WeightTracker.Unit.Domain.Workouts.Observations.ObservationTestData;

namespace WeightTracker.Unit.Domain.Workouts.Observations;

public class WorkoutObservationGeneratorStreakTests
{
    private static ExerciseExecution Missed(int daysAgo, TrainingWeekType? weekType = null)
        => Earlier(daysAgo, (3, 10), [(10, 50), (8, 50), (8, 50)], weekType);

    private static ExerciseExecution Met(int daysAgo, TrainingWeekType? weekType = null)
        => Earlier(daysAgo, (3, 10), [(10, 50), (10, 50), (10, 50)], weekType);

    private static GeneratedObservations GenerateFor(Id exercise, ExerciseExecution[] history, params (int Reps, decimal? Weight)[] sets)
    {
        var workout = CompletedWorkout(Exercise(exercise, [Sets(3, 10)], sets.Select(set => Rep(set.Reps, set.Weight)).ToArray()));

        return Generator().Generate(workout, null, HistoryOf(exercise, history), CreatedAt);
    }

    [Fact]
    public void Generate_ShouldReportTheThirdMissInARow()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(bench, [Missed(7), Missed(14)], (10, 50), (8, 50), (8, 50));

        // Assert
        var repeated = Observation(result, ObservationCode.REPEATED_TARGET_MISS, bench);
        repeated.Severity.Should().Be(ObservationSeverity.WARNING);
        repeated.Priority.Should().Be(HighlightPriority.HIGH);
        repeated.Evidence.Number("consecutiveMisses").Should().Be(3);
    }

    [Fact]
    public void Generate_ShouldReportTheFullLengthOfALongerStreak()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(bench, [Missed(7), Missed(14), Missed(21), Met(28)], (10, 50), (8, 50), (8, 50));

        // Assert
        Observation(result, ObservationCode.REPEATED_TARGET_MISS, bench).Evidence.Number("consecutiveMisses").Should().Be(4);
    }

    [Fact]
    public void Generate_ShouldNotReportARepeatedMissWhenAnEarlierSessionMetItsTargets()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(bench, [Met(7), Missed(14)], (10, 50), (8, 50), (8, 50));

        // Assert
        Has(result, ObservationCode.REPEATED_TARGET_MISS, bench).Should().BeFalse();
    }

    [Fact]
    public void Generate_ShouldNeitherCountNorBreakAStreakWithALightWeekSession()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(
            bench, [Missed(7), Met(10, TrainingWeekType.DELOAD), Missed(14)], (10, 50), (8, 50), (8, 50));

        // Assert
        Observation(result, ObservationCode.REPEATED_TARGET_MISS, bench).Evidence.Number("consecutiveMisses").Should().Be(3);
    }

    [Fact]
    public void Generate_ShouldReportAPlateauAfterFourExecutionsWithoutExceedingTheBaseline()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(
            bench, [TopOf(7, 100), TopOf(14, 97.5m), TopOf(21, 100), TopOf(28, 100)], (10, 100), (10, 100), (10, 100));

        // Assert
        var plateau = Observation(result, ObservationCode.WEIGHT_PLATEAU, bench);
        plateau.Severity.Should().Be(ObservationSeverity.INFO);
        plateau.Evidence.Number("baselineMaxKg").Should().Be(100);
        plateau.Evidence.Numbers("windowMaxWeightsKg").Should().Equal(100, 97.5m, 100, 100);
        plateau.Evidence.Number("executions").Should().Be(4);
        plateau.Priority.Should().BeNull();
    }

    [Fact]
    public void Generate_ShouldNotReportAPlateauWhenAnExecutionExceededTheBaseline()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(
            bench, [TopOf(7, 100), TopOf(14, 105), TopOf(21, 100), TopOf(28, 100)], (10, 100), (10, 100), (10, 100));

        // Assert
        Has(result, ObservationCode.WEIGHT_PLATEAU, bench).Should().BeFalse();
    }

    [Fact]
    public void Generate_ShouldReportADowntrendWithItsEvidence()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(bench, [TopOf(7, 100), TopOf(14, 105), TopOf(21, 110)], (10, 95), (10, 95), (10, 95));

        // Assert
        var trend = Observation(result, ObservationCode.WEIGHT_DOWNTREND, bench);
        trend.Severity.Should().Be(ObservationSeverity.INFO);
        trend.Priority.Should().Be(HighlightPriority.HIGH);
        trend.Evidence.Numbers("previousWeightsKg").Should().Equal(110, 105, 100);
        trend.Evidence.Number("currentWeightKg").Should().Be(95);
        trend.Evidence.Number("consecutiveDecreases").Should().Be(3);
    }

    [Fact]
    public void Generate_ShouldReportAnUptrendWithItsEvidence()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(bench, [TopOf(7, 105), TopOf(14, 100), TopOf(21, 95)], (10, 110), (10, 110), (10, 110));

        // Assert
        var trend = Observation(result, ObservationCode.WEIGHT_UPTREND, bench);
        trend.Severity.Should().Be(ObservationSeverity.SUCCESS);
        trend.Priority.Should().BeNull();
        trend.Evidence.Numbers("previousWeightsKg").Should().Equal(95, 100, 105);
        trend.Evidence.Number("consecutiveIncreases").Should().Be(3);
    }

    [Fact]
    public void Generate_ShouldNotReportATrendWhenAStepIsFlat()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(bench, [TopOf(7, 105), TopOf(14, 105), TopOf(21, 100)], (10, 110), (10, 110), (10, 110));

        // Assert
        Has(result, ObservationCode.WEIGHT_UPTREND, bench).Should().BeFalse();
    }

    [Fact]
    public void Generate_ShouldNotReportATrendWithOneStepTooFew()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(bench, [TopOf(7, 100), TopOf(14, 105)], (10, 95), (10, 95), (10, 95));

        // Assert
        Has(result, ObservationCode.WEIGHT_DOWNTREND, bench).Should().BeFalse();
    }
}
