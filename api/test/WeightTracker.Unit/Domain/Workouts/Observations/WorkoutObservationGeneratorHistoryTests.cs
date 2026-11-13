using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Workouts.Observations;
using Xunit;
using static WeightTracker.Unit.Domain.Workouts.Observations.ObservationTestData;

namespace WeightTracker.Unit.Domain.Workouts.Observations;

public class WorkoutObservationGeneratorHistoryTests
{
    private static readonly ObservationCode[] HistoryCodes =
    [
        ObservationCode.WEIGHT_INCREASE, ObservationCode.WEIGHT_DECREASE, ObservationCode.WEIGHT_PERSONAL_RECORD,
        ObservationCode.REPS_PERSONAL_RECORD, ObservationCode.VOLUME_INCREASE, ObservationCode.VOLUME_DECREASE,
        ObservationCode.REPS_INCREASE, ObservationCode.REPS_DECREASE, ObservationCode.REPEATED_TARGET_MISS,
        ObservationCode.WEIGHT_PLATEAU, ObservationCode.WEIGHT_UPTREND, ObservationCode.WEIGHT_DOWNTREND,
    ];

    private static GeneratedObservations GenerateFor(Id exercise, ExerciseExecution[] history, params (int Reps, decimal? Weight)[] sets)
    {
        var workout = CompletedWorkout(Exercise(exercise, [Sets(3, 10)], sets.Select(set => Rep(set.Reps, set.Weight)).ToArray()));

        return Generator().Generate(workout, null, HistoryOf(exercise, history), CreatedAt);
    }

    [Fact]
    public void Generate_ShouldOnlyReportComplianceForTheFirstSessionOfAnExercise()
    {
        // Arrange
        var bench = Id.New();
        var workout = CompletedWorkout(Exercise(bench, [Sets(3, 10)], Rep(10, 100), Rep(10, 100), Rep(10, 100)));

        // Act
        var result = Generator().Generate(workout, null, NoHistory, CreatedAt);

        // Assert
        result.Observations.Select(observation => observation.Code).Should().BeSubsetOf(
            [ObservationCode.ALL_TARGETS_MET, ObservationCode.WORKOUT_INCOMPLETE]);
    }

    [Theory]
    [InlineData(TrainingWeekType.DELOAD)]
    [InlineData(TrainingWeekType.RECOVERY)]
    public void Generate_ShouldNotCompareWithHistoryInALightWeek(TrainingWeekType weekType)
    {
        // Arrange
        var bench = Id.New();
        var workout = CompletedWorkout(Exercise(bench, [Sets(3, 10)], Rep(10, 80), Rep(10, 80), Rep(10, 80)));

        // Act
        var result = Generator().Generate(workout, weekType, HistoryOf(bench, TopOf(7, 100)), CreatedAt);

        // Assert
        result.Observations.Select(observation => observation.Code).Should().NotIntersectWith(HistoryCodes);
        Has(result, ObservationCode.ALL_TARGETS_MET).Should().BeTrue();
    }

    [Fact]
    public void Generate_ShouldCompareWithTheSessionBeforeALightWeekOne()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(
            bench, [TopOf(7, 60, weekType: TrainingWeekType.DELOAD), TopOf(14, 100)], (10, 105), (10, 105), (10, 105));

        // Assert
        Observation(result, ObservationCode.WEIGHT_INCREASE, bench).Evidence.Number("previousMaxKg").Should().Be(100);
    }

    [Fact]
    public void Generate_ShouldNotCompareWithASessionPrescribedPerSideWhenTheCurrentOneIsNot()
    {
        // Arrange
        var bench = Id.New();
        var perSide = Earlier(7, (3, 10), [(10, 100), (10, 100), (10, 100)], perSide: true);

        // Act
        var result = GenerateFor(bench, [perSide], (10, 120), (10, 120), (10, 120));

        // Assert
        result.Observations.Select(observation => observation.Code).Should().NotIntersectWith(HistoryCodes);
    }

    [Fact]
    public void Generate_ShouldNotCompareWithASessionOfAnotherResultKind()
    {
        // Arrange
        var plank = Id.New();
        var workout = CompletedWorkout(Exercise(plank, [Timed(2, 40)], Held(40, 10), Held(40, 10)));
        var reps = TopOf(7, 5);

        // Act
        var result = Generator().Generate(workout, null, HistoryOf(plank, reps), CreatedAt);

        // Assert
        result.Observations.Select(observation => observation.Code).Should().NotIntersectWith(HistoryCodes);
    }

    [Fact]
    public void Generate_ShouldNotReportWeightOrVolumeWhenNoWeightWasRecorded()
    {
        // Arrange
        var bench = Id.New();
        var noWeights = Earlier(7, (3, 10), [(10, null), (10, null), (10, null)]);

        // Act
        var result = GenerateFor(bench, [noWeights], (12, null), (12, null), (12, null));

        // Assert
        result.Observations.Select(observation => observation.Code).Should().NotContain(
            [ObservationCode.WEIGHT_INCREASE, ObservationCode.VOLUME_INCREASE, ObservationCode.WEIGHT_PERSONAL_RECORD]);
        Has(result, ObservationCode.REPS_INCREASE, bench).Should().BeTrue();
    }

    [Fact]
    public void Generate_ShouldNotReportAChangeAgainstABaselineOfZero()
    {
        // Arrange
        var pushUp = Id.New();

        // Act
        var result = GenerateFor(pushUp, [TopOf(7, 0)], (10, 10), (10, 10), (10, 10));

        // Assert
        Has(result, ObservationCode.WEIGHT_INCREASE, pushUp).Should().BeFalse();
        Has(result, ObservationCode.WEIGHT_PERSONAL_RECORD, pushUp).Should().BeFalse();
    }

    [Fact]
    public void Generate_ShouldReportAHeavierWeightThanLastTime()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(bench, [TopOf(7, 100)], (10, 105), (10, 105), (10, 105));

        // Assert
        var increase = Observation(result, ObservationCode.WEIGHT_INCREASE, bench);
        increase.Severity.Should().Be(ObservationSeverity.INFO);
        increase.Evidence.Number("previousMaxKg").Should().Be(100);
        increase.Evidence.Number("currentMaxKg").Should().Be(105);
        increase.Evidence.Number("changePercent").Should().Be(5);
    }

    [Fact]
    public void Generate_ShouldNotReportAChangeWhenTheWeightIsTheSame()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(bench, [TopOf(7, 100)], (10, 100), (10, 100), (10, 100));

        // Assert
        Has(result, ObservationCode.WEIGHT_INCREASE, bench).Should().BeFalse();
        Has(result, ObservationCode.WEIGHT_DECREASE, bench).Should().BeFalse();
    }

    [Fact]
    public void Generate_ShouldReportALighterWeightWithTheBaselineWhenThereIsOne()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(bench, [TopOf(7, 100), TopOf(14, 110)], (10, 90), (10, 90), (10, 90));

        // Assert
        var decrease = Observation(result, ObservationCode.WEIGHT_DECREASE, bench);
        decrease.Evidence.Number("changePercent").Should().Be(-10);
        decrease.Evidence.Number("baselineMaxKg").Should().Be(110);
    }

    [Fact]
    public void Generate_ShouldReportANewWeightRecordAndKeepTheIncrease()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(bench, [TopOf(7, 100), TopOf(14, 120)], (10, 130), (10, 130), (10, 130));

        // Assert
        var record = Observation(result, ObservationCode.WEIGHT_PERSONAL_RECORD, bench);
        record.Severity.Should().Be(ObservationSeverity.SUCCESS);
        record.Evidence.Number("previousMaxKg").Should().Be(120);
        record.Evidence.Number("currentMaxKg").Should().Be(130);
        record.Evidence.Number("changePercent").Should().Be(8.33m);
        Has(result, ObservationCode.WEIGHT_INCREASE, bench).Should().BeTrue();
    }

    [Fact]
    public void Generate_ShouldNotReportAWeightRecordBelowTheBestEverEvenWhenHeavierThanLastTime()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(bench, [TopOf(7, 100), TopOf(14, 120)], (10, 110), (10, 110), (10, 110));

        // Assert
        Has(result, ObservationCode.WEIGHT_PERSONAL_RECORD, bench).Should().BeFalse();
        Has(result, ObservationCode.WEIGHT_INCREASE, bench).Should().BeTrue();
    }

    [Fact]
    public void Generate_ShouldReportMoreRepsAtAWeightUsedBefore()
    {
        // Arrange
        var bench = Id.New();
        var before = Earlier(7, (3, 10), [(8, 80), (6, 80), (6, 80)]);

        // Act
        var result = GenerateFor(bench, [before], (10, 80), (8, 80), (8, 80));

        // Assert
        var record = Observation(result, ObservationCode.REPS_PERSONAL_RECORD, bench);
        record.Severity.Should().Be(ObservationSeverity.SUCCESS);
        record.Evidence.Number("weightKg").Should().Be(80);
        record.Evidence.Number("previousBestReps").Should().Be(8);
        record.Evidence.Number("currentReps").Should().Be(10);
    }

    [Fact]
    public void Generate_ShouldReportTheHeaviestWeightWhenSeveralRepsRecordsQualify()
    {
        // Arrange
        var bench = Id.New();
        var before = Earlier(7, (3, 10), [(8, 60), (8, 80), (8, 80)]);

        // Act
        var result = GenerateFor(bench, [before], (10, 60), (10, 80), (8, 80));

        // Assert
        Observation(result, ObservationCode.REPS_PERSONAL_RECORD, bench).Evidence.Number("weightKg").Should().Be(80);
    }

    [Fact]
    public void Generate_ShouldNotReportARepsRecordAtAWeightNeverUsedBefore()
    {
        // Arrange
        var bench = Id.New();

        // Act
        var result = GenerateFor(bench, [TopOf(7, 80)], (12, 85), (12, 85), (12, 85));

        // Assert
        Has(result, ObservationCode.REPS_PERSONAL_RECORD, bench).Should().BeFalse();
    }

    [Fact]
    public void Generate_ShouldCompareRecordsAgainstEverySessionBeyondTheOnesListed()
    {
        // Arrange
        var bench = Id.New();
        var workout = CompletedWorkout(Exercise(bench, [Sets(3, 10)], Rep(10, 110), Rep(10, 110), Rep(10, 110)));
        var recent = new ExerciseHistory(
            [TopOf(7, 100)],
            [new ExerciseRecord(false, ResultKind.REPS, 125m, [new RepsAtWeight(125m, 5)])]);

        // Act
        var result = Generator().Generate(workout, null, new Dictionary<Id, ExerciseHistory> { [bench] = recent }, CreatedAt);

        // Assert
        Has(result, ObservationCode.WEIGHT_PERSONAL_RECORD, bench).Should().BeFalse();
        Has(result, ObservationCode.WEIGHT_INCREASE, bench).Should().BeTrue();
    }

    [Fact]
    public void Generate_ShouldReportMoreVolumeWithTheSameNumberOfPrescribedSets()
    {
        // Arrange
        var squat = Id.New();
        var before = Earlier(7, (3, 10), [(10, 80), (10, 80), (10, 80)]);

        // Act
        var result = GenerateFor(squat, [before], (11, 80), (11, 80), (11, 80));

        // Assert
        var increase = Observation(result, ObservationCode.VOLUME_INCREASE, squat);
        increase.Evidence.Number("previousVolumeKg").Should().Be(2400);
        increase.Evidence.Number("currentVolumeKg").Should().Be(2640);
        increase.Evidence.Number("changePercent").Should().Be(10);
    }

    [Fact]
    public void Generate_ShouldReportLessVolumeWithItsBaseline()
    {
        // Arrange
        var squat = Id.New();
        var before = Earlier(7, (3, 10), [(10, 80), (10, 80), (10, 80)]);
        var baseline = Earlier(14, (3, 10), [(10, 100), (10, 100), (10, 100)]);

        // Act
        var result = GenerateFor(squat, [before, baseline], (10, 70), (10, 70), (10, 70));

        // Assert
        var decrease = Observation(result, ObservationCode.VOLUME_DECREASE, squat);
        decrease.Evidence.Number("baselineVolumeKg").Should().Be(3000);
        decrease.Evidence.Number("currentVolumeKg").Should().Be(2100);
    }

    [Fact]
    public void Generate_ShouldNotReportVolumeOrRepsWhenThePrescribedSetCountChanged()
    {
        // Arrange
        var squat = Id.New();
        var before = Earlier(7, (4, 10), [(10, 80), (10, 80), (10, 80), (10, 80)]);

        // Act
        var result = GenerateFor(squat, [before], (12, 80), (12, 80), (12, 80));

        // Assert
        result.Observations.Select(observation => observation.Code).Should().NotContain(
            [ObservationCode.VOLUME_INCREASE, ObservationCode.VOLUME_DECREASE, ObservationCode.REPS_INCREASE, ObservationCode.REPS_DECREASE]);
    }

    [Fact]
    public void Generate_ShouldReportMoreTotalRepsInTheSameNumberOfSets()
    {
        // Arrange
        var curl = Id.New();
        var before = Earlier(7, (3, 10), [(10, null), (10, null), (10, null)]);

        // Act
        var result = GenerateFor(curl, [before], (11, null), (11, null), (11, null));

        // Assert
        var increase = Observation(result, ObservationCode.REPS_INCREASE, curl);
        increase.Evidence.Number("previousTotalReps").Should().Be(30);
        increase.Evidence.Number("currentTotalReps").Should().Be(33);
        increase.Evidence.Number("sets").Should().Be(3);
        increase.Evidence.Number("changePercent").Should().Be(10);
    }

    [Fact]
    public void Generate_ShouldReportFewerTotalReps()
    {
        // Arrange
        var curl = Id.New();
        var before = Earlier(7, (3, 10), [(10, null), (10, null), (10, null)]);

        // Act
        var result = GenerateFor(curl, [before], (10, null), (10, null), (8, null));

        // Assert
        Observation(result, ObservationCode.REPS_DECREASE, curl).Evidence.Number("currentTotalReps").Should().Be(28);
    }

    [Fact]
    public void Generate_ShouldNotReportTotalRepsWhenTheNumberOfRecordedSetsDiffers()
    {
        // Arrange
        var curl = Id.New();
        var before = Earlier(7, (3, 10), [(10, null), (10, null), (10, null)]);

        // Act
        var result = GenerateFor(curl, [before], (12, null), (12, null));

        // Assert
        Has(result, ObservationCode.REPS_INCREASE, curl).Should().BeFalse();
        Has(result, ObservationCode.REPS_DECREASE, curl).Should().BeFalse();
    }
}
