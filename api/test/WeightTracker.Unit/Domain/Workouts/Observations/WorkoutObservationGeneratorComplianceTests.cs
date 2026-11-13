using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts.Observations;
using Xunit;
using static WeightTracker.Unit.Domain.Workouts.Observations.ObservationTestData;

namespace WeightTracker.Unit.Domain.Workouts.Observations;

public class WorkoutObservationGeneratorComplianceTests
{
    [Fact]
    public void Generate_ShouldReportAnIncompleteWorkoutWithItsCompletion()
    {
        // Arrange
        var bench = Id.New();
        var squat = Id.New();
        var workout = CompletedWorkout(
            Exercise(bench, [Sets(5, 10)], Rep(10), Rep(10), Rep(10), Rep(10), Rep(10)),
            Exercise(squat, [Sets(10, 10)], Rep(10), Rep(10), Rep(10), Rep(10)));

        // Act
        var result = Generator().Generate(workout, null, NoHistory, CreatedAt);

        // Assert
        var incomplete = Observation(result, ObservationCode.WORKOUT_INCOMPLETE);
        incomplete.Severity.Should().Be(ObservationSeverity.WARNING);
        incomplete.Evidence.Number("recordedSets").Should().Be(9);
        incomplete.Evidence.Number("prescribedSets").Should().Be(15);
        incomplete.Evidence.Number("completionPercent").Should().Be(60);
    }

    [Fact]
    public void Generate_ShouldMarkSevereNonComplianceAsCriticalAndMildAsNotEligible()
    {
        // Arrange
        var severe = CompletedWorkout(Exercise(Id.New(), [Sets(10, 10)], Rep(10), Rep(10), Rep(10), Rep(10), Rep(10), Rep(10)));
        var mild = CompletedWorkout(Exercise(Id.New(), [Sets(10, 10)], Rep(10), Rep(10), Rep(10), Rep(10), Rep(10), Rep(10), Rep(10), Rep(10)));

        // Act
        var severeResult = Generator().Generate(severe, null, NoHistory, CreatedAt);
        var mildResult = Generator().Generate(mild, null, NoHistory, CreatedAt);

        // Assert
        Observation(severeResult, ObservationCode.WORKOUT_INCOMPLETE).Priority.Should().Be(HighlightPriority.CRITICAL);
        Observation(mildResult, ObservationCode.WORKOUT_INCOMPLETE).Priority.Should().BeNull();
    }

    [Fact]
    public void Generate_ShouldReportAnExerciseWithoutSetsAsSkippedAndNeverHighlightIt()
    {
        // Arrange
        var bench = Id.New();
        var squat = Id.New();
        var workout = CompletedWorkout(
            Exercise(bench, [Sets(3, 10)], Rep(10), Rep(10), Rep(10)),
            Exercise(squat, [Sets(3, 10)]));

        // Act
        var result = Generator().Generate(workout, null, NoHistory, CreatedAt);

        // Assert
        var skipped = Observation(result, ObservationCode.EXERCISE_SKIPPED, squat);
        skipped.Severity.Should().Be(ObservationSeverity.WARNING);
        skipped.Evidence.Number("prescribedSets").Should().Be(3);
        skipped.Priority.Should().BeNull();
        result.Highlights.Should().NotContain(skipped.Id);
    }

    [Fact]
    public void Generate_ShouldReportRepsBelowTheRangeMinimum()
    {
        // Arrange
        var bench = Id.New();
        var workout = CompletedWorkout(Exercise(bench, [Range(4, 8, 12)], Rep(10), Rep(9), Rep(6), Rep(5)));

        // Act
        var result = Generator().Generate(workout, null, NoHistory, CreatedAt);

        // Assert
        var below = Observation(result, ObservationCode.REPS_BELOW_TARGET, bench);
        below.Severity.Should().Be(ObservationSeverity.WARNING);
        below.Evidence.Numbers("setPositions").Should().Equal(3, 4);
        below.Evidence.Numbers("reps").Should().Equal(6, 5);
        below.Evidence.Numbers("targetMins").Should().Equal(8, 8);
    }

    [Fact]
    public void Generate_ShouldReportRepsAboveTheRangeMaximum()
    {
        // Arrange
        var bench = Id.New();
        var workout = CompletedWorkout(Exercise(bench, [Range(3, 8, 12)], Rep(14), Rep(10), Rep(10)));

        // Act
        var result = Generator().Generate(workout, null, NoHistory, CreatedAt);

        // Assert
        var above = Observation(result, ObservationCode.REPS_ABOVE_TARGET, bench);
        above.Severity.Should().Be(ObservationSeverity.INFO);
        above.Evidence.Numbers("setPositions").Should().Equal(1);
        above.Evidence.Numbers("reps").Should().Equal(14);
        above.Evidence.Numbers("targetMaxes").Should().Equal(12);
    }

    [Fact]
    public void Generate_ShouldNotReportRepsAboveTargetForMaxRepsOrFixedReps()
    {
        // Arrange
        var curl = Id.New();
        var bench = Id.New();
        var workout = CompletedWorkout(
            Exercise(curl, [Max(1)], Rep(20)),
            Exercise(bench, [Sets(1, 10)], Rep(20)));

        // Act
        var result = Generator().Generate(workout, null, NoHistory, CreatedAt);

        // Assert
        Has(result, ObservationCode.REPS_ABOVE_TARGET, curl).Should().BeFalse();
        Has(result, ObservationCode.REPS_ABOVE_TARGET, bench).Should().BeFalse();
    }

    [Fact]
    public void Generate_ShouldNotCountASetBeyondThePrescriptionForCompliance()
    {
        // Arrange
        var bench = Id.New();
        var workout = CompletedWorkout(Exercise(bench, [Sets(3, 10)], Rep(10, 50), Rep(10, 50), Rep(10, 50), Rep(5, 50)));

        // Act
        var result = Generator().Generate(workout, null, NoHistory, CreatedAt);

        // Assert
        Has(result, ObservationCode.REPS_BELOW_TARGET, bench).Should().BeFalse();
        Has(result, ObservationCode.WORKOUT_INCOMPLETE).Should().BeFalse();
        Has(result, ObservationCode.ALL_TARGETS_MET).Should().BeTrue();
    }

    [Fact]
    public void Generate_ShouldReportAllTargetsMetWhenEverySetIsRecordedWithinItsTarget()
    {
        // Arrange
        var workout = CompletedWorkout(
            Exercise(Id.New(), [Range(2, 8, 12)], Rep(10), Rep(12)),
            Exercise(Id.New(), [Sets(1, 10)], Rep(10)));

        // Act
        var result = Generator().Generate(workout, null, NoHistory, CreatedAt);

        // Assert
        var met = Observation(result, ObservationCode.ALL_TARGETS_MET);
        met.Severity.Should().Be(ObservationSeverity.SUCCESS);
        met.Evidence.Number("prescribedSets").Should().Be(3);
        met.Priority.Should().BeNull();
        result.Highlights.Should().BeEmpty();
    }

    [Fact]
    public void Generate_ShouldNotReportAllTargetsMetWhenASetIsAboveItsRangeMaximum()
    {
        // Arrange
        var workout = CompletedWorkout(Exercise(Id.New(), [Range(2, 8, 12)], Rep(14), Rep(10)));

        // Act
        var result = Generator().Generate(workout, null, NoHistory, CreatedAt);

        // Assert
        Has(result, ObservationCode.ALL_TARGETS_MET).Should().BeFalse();
    }

    [Fact]
    public void Generate_ShouldNotReportAllTargetsMetWhenASetMissesItsTarget()
    {
        // Arrange
        var workout = CompletedWorkout(Exercise(Id.New(), [Sets(2, 10)], Rep(10), Rep(8)));

        // Act
        var result = Generator().Generate(workout, null, NoHistory, CreatedAt);

        // Assert
        Has(result, ObservationCode.ALL_TARGETS_MET).Should().BeFalse();
    }

    [Fact]
    public void Generate_ShouldGenerateNothingForAWorkoutWithoutExercises()
    {
        // Arrange
        var workout = CompletedWorkout();

        // Act
        var result = Generator().Generate(workout, null, NoHistory, CreatedAt);

        // Assert
        result.Observations.Should().BeEmpty();
        result.Highlights.Should().BeEmpty();
    }

    [Fact]
    public void Generate_ShouldOrderObservationsWithTheWorkoutFirstThenByExercisePositionAndCode()
    {
        // Arrange
        var first = Id.New();
        var second = Id.New();
        var workout = CompletedWorkout(
            Exercise(first, [Range(2, 8, 12)], Rep(14), Rep(5)),
            Exercise(second, [Sets(3, 10)]));

        // Act
        var result = Generator().Generate(workout, null, NoHistory, CreatedAt);

        // Assert
        result.Observations.Select(observation => (observation.Code, observation.ExerciseId)).Should().Equal(
            (ObservationCode.WORKOUT_INCOMPLETE, (Id?)null),
            (ObservationCode.REPS_BELOW_TARGET, first),
            (ObservationCode.REPS_ABOVE_TARGET, first),
            (ObservationCode.EXERCISE_SKIPPED, second));
    }
}
