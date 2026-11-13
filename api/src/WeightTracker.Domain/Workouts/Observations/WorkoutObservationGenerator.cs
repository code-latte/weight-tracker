using WeightTracker.Domain.Common;
using WeightTracker.Domain.Mesocycles;

namespace WeightTracker.Domain.Workouts.Observations;

/// <summary>
/// Every observation generated for a completed workout and the identifiers of the ones highlighted, most important first.
/// </summary>
public sealed record GeneratedObservations(IReadOnlyList<WorkoutObservation> Observations, IReadOnlyList<Id> Highlights);

/// <summary>
/// Applies the observation rules to a completed workout, comparing it with its prescription and with the athlete's earlier
/// sessions, then selects the few observations worth the trainer's attention. It is a pure function of its inputs: it
/// reads nothing and writes nothing, and every threshold comes from <see cref="ObservationThresholds"/>.
/// </summary>
/// <remarks>
/// It states measurable facts only. A rule that lacks the history it needs, or a value to compare, produces nothing.
/// </remarks>
public sealed class WorkoutObservationGenerator
{
    private readonly ObservationThresholds _thresholds;

    public WorkoutObservationGenerator(ObservationThresholds thresholds)
    {
        _thresholds = thresholds;
    }

    /// <summary>
    /// Generates the observations and highlights of a completed workout.
    /// </summary>
    /// <param name="workout">The completed workout.</param>
    /// <param name="weekType">Type of the mesocycle week the workout sits in, null when it sits in none. In a light week only compliance rules run.</param>
    /// <param name="history">Earlier sessions per exercise. An exercise without an entry has no history.</param>
    /// <param name="createdAt">When the observations are generated, in UTC.</param>
    public GeneratedObservations Generate(
        Workout workout,
        TrainingWeekType? weekType,
        IReadOnlyDictionary<Id, ExerciseHistory> history,
        DateTime createdAt)
    {
        var context = new Context(createdAt);
        var exercises = workout.Exercises;
        var light = weekType is TrainingWeekType.DELOAD or TrainingWeekType.RECOVERY;

        var executions = new List<(int Position, WorkoutExercise Exercise, ExerciseExecution? Execution, ExerciseHistory History)>();

        for (var position = 0; position < exercises.Count; position++)
        {
            var exercise = exercises[position];

            var execution = exercise.SetRecords.Count == 0
                ? null
                : new ExerciseExecution(
                    workout.Id,
                    DateOnly.FromDateTime(workout.WorkoutDate),
                    weekType,
                    exercise.PerSide,
                    Expand(exercise),
                    exercise.SetRecords.Select(record => new ExecutionSet(record.Result, record.Weight)));

            executions.Add((position, exercise, execution, history.GetValueOrDefault(exercise.ExerciseId) ?? ExerciseHistory.None));
        }

        var endsMissStreak = false;

        foreach (var (position, exercise, execution, exerciseHistory) in executions)
        {
            if (execution is null)
            {
                context.Add(
                    position, exercise.ExerciseId, ObservationCode.EXERCISE_SKIPPED, null, 0m,
                    ObservationEvidence.Empty.With("prescribedSets", exercise.TotalSets));

                continue;
            }

            AddTargetObservations(context, position, exercise.ExerciseId, execution);

            if (light)
                continue;

            var comparable = exerciseHistory.Executions
                .Where(earlier => earlier.IsComparableWith(execution.Kind, execution.PerSide))
                .ToList();

            AddComparisons(context, position, exercise.ExerciseId, execution, exerciseHistory, comparable);
            AddStreaks(context, position, exercise.ExerciseId, execution, comparable);

            endsMissStreak |= execution.MissedRepTarget == false && EndsMissStreak(comparable);
        }

        AddWorkoutObservations(context, executions.Select(entry => (entry.Exercise, entry.Execution)).ToList(), endsMissStreak);

        return Select(context.Candidates);
    }

    private void AddWorkoutObservations(
        Context context,
        IReadOnlyList<(WorkoutExercise Exercise, ExerciseExecution? Execution)> entries,
        bool endsMissStreak)
    {
        var prescribed = entries.Sum(entry => entry.Exercise.TotalSets);
        var recorded = entries.Sum(entry => entry.Execution?.MatchedSets ?? 0);

        if (prescribed == 0)
            return;

        if (recorded < prescribed)
        {
            var completion = Round((decimal)recorded / prescribed * 100);

            var evidence = ObservationEvidence.Empty
                .With("recordedSets", recorded)
                .With("prescribedSets", prescribed)
                .With("completionPercent", completion);

            var priority = completion < _thresholds.SevereNonCompliancePercent ? HighlightPriority.CRITICAL : (HighlightPriority?)null;

            context.Add(WorkoutLevel, null, ObservationCode.WORKOUT_INCOMPLETE, priority, 100 - completion, evidence);

            return;
        }

        if (entries.All(entry => entry.Execution?.MetEveryTarget == true))
        {
            // Meeting every target is routine, so it is only worth a highlight when it ends a streak of misses
            context.Add(
                WorkoutLevel, null, ObservationCode.ALL_TARGETS_MET,
                endsMissStreak ? HighlightPriority.LOW : null, 0m,
                ObservationEvidence.Empty.With("prescribedSets", prescribed));
        }
    }

    private static void AddTargetObservations(Context context, int position, Id exerciseId, ExerciseExecution execution)
    {
        var below = execution.SetsBelowTarget();

        if (below.Count > 0)
        {
            var share = Round((decimal)below.Count / execution.MatchedSets * 100);

            context.Add(
                position, exerciseId, ObservationCode.REPS_BELOW_TARGET, null, share,
                Deviation(below, "targetMins"));
        }

        var above = execution.SetsAboveRange();

        if (above.Count > 0)
            context.Add(position, exerciseId, ObservationCode.REPS_ABOVE_TARGET, null, 0m, Deviation(above, "targetMaxes"));
    }

    private static ObservationEvidence Deviation(IReadOnlyList<SetDeviation> sets, string boundName)
        => ObservationEvidence.Empty
            .With("setPositions", sets.Select(set => (decimal)set.Position))
            .With("reps", sets.Select(set => (decimal)set.Reps))
            .With(boundName, sets.Select(set => (decimal)set.Bound));

    private void AddComparisons(
        Context context,
        int position,
        Id exerciseId,
        ExerciseExecution current,
        ExerciseHistory history,
        IReadOnlyList<ExerciseExecution> comparable)
    {
        var previous = comparable.FirstOrDefault();
        var baseline = comparable.Skip(1).FirstOrDefault();

        AddWeightRecord(context, position, exerciseId, current, history);
        AddRepsRecord(context, position, exerciseId, current, history);

        if (previous is null)
            return;

        // Weight
        if (current.MaxWeightKg is { } maxNow && previous.MaxWeightKg is { } maxBefore && maxBefore > 0 && maxNow != maxBefore)
        {
            AddChange(
                context, position, exerciseId, maxNow, maxBefore, baseline?.MaxWeightKg,
                ObservationCode.WEIGHT_INCREASE, ObservationCode.WEIGHT_DECREASE,
                "previousMaxKg", "currentMaxKg", "baselineMaxKg");
        }

        // Volume and total reps only mean something against a session prescribed the same number of sets
        if (current.Kind != ResultKind.REPS || previous.PrescribedSets != current.PrescribedSets)
            return;

        if (current.VolumeKg is { } volumeNow && previous.VolumeKg is { } volumeBefore && volumeBefore > 0 && volumeNow != volumeBefore)
        {
            var baselineVolume = baseline is not null && baseline.PrescribedSets == current.PrescribedSets ? baseline.VolumeKg : null;

            AddChange(
                context, position, exerciseId, volumeNow, volumeBefore, baselineVolume,
                ObservationCode.VOLUME_INCREASE, ObservationCode.VOLUME_DECREASE,
                "previousVolumeKg", "currentVolumeKg", "baselineVolumeKg");
        }

        if (current.RepSetCount == previous.RepSetCount && previous.TotalReps > 0 && current.TotalReps != previous.TotalReps)
        {
            var baselineReps = baseline is not null
                && baseline.PrescribedSets == current.PrescribedSets
                && baseline.RepSetCount == current.RepSetCount
                ? baseline.TotalReps
                : (decimal?)null;

            AddChange(
                context, position, exerciseId, current.TotalReps, previous.TotalReps, baselineReps,
                ObservationCode.REPS_INCREASE, ObservationCode.REPS_DECREASE,
                "previousTotalReps", "currentTotalReps", "baselineTotalReps",
                ("sets", current.RepSetCount));
        }
    }

    // One change of a metric against the previous execution. A decrease also names the execution before the previous one
    // when there is a value for it, because only a decline confirmed against it is worth a highlight
    private void AddChange(
        Context context,
        int position,
        Id exerciseId,
        decimal now,
        decimal before,
        decimal? baseline,
        ObservationCode increase,
        ObservationCode decrease,
        string previousName,
        string currentName,
        string baselineName,
        (string Name, decimal Value)? extra = null)
    {
        var change = Percent(now, before);
        var isIncrease = now > before;

        var evidence = ObservationEvidence.Empty
            .With(previousName, before)
            .With(currentName, now);

        if (extra is { } more)
            evidence = evidence.With(more.Name, more.Value);

        evidence = evidence.With("changePercent", change);

        if (isIncrease)
        {
            var priority = change >= _thresholds.ExceptionalProgressionPercent ? HighlightPriority.MEDIUM : (HighlightPriority?)null;
            context.Add(position, exerciseId, increase, priority, Math.Abs(change), evidence);

            return;
        }

        var confirmed = false;

        if (baseline is { } start && start > 0)
        {
            evidence = evidence.With(baselineName, start);
            confirmed = Drop(start, before) >= _thresholds.SignificantDeclinePercent
                && Drop(start, now) >= _thresholds.SignificantDeclinePercent;
        }

        context.Add(position, exerciseId, decrease, confirmed ? HighlightPriority.HIGH : null, Math.Abs(change), evidence);
    }

    private void AddWeightRecord(Context context, int position, Id exerciseId, ExerciseExecution current, ExerciseHistory history)
    {
        if (history.RecordFor(current.Kind, current.PerSide) is not { } record)
            return;

        if (current.MaxWeightKg is { } now && record.MaxWeightKg is { } best && best > 0 && now > best)
        {
            var change = Percent(now, best);

            context.Add(
                position, exerciseId, ObservationCode.WEIGHT_PERSONAL_RECORD,
                change >= _thresholds.SignificantRecordPercent ? HighlightPriority.MEDIUM : null, change,
                ObservationEvidence.Empty
                    .With("previousMaxKg", best)
                    .With("currentMaxKg", now)
                    .With("changePercent", change));
        }
    }

    // More reps than ever in one set at a weight used before. When several weights qualify the heaviest is reported
    private static void AddRepsRecord(Context context, int position, Id exerciseId, ExerciseExecution current, ExerciseHistory history)
    {
        if (current.Kind != ResultKind.REPS || history.RecordFor(current.Kind, current.PerSide) is not { } record)
            return;

        var qualifying = current.Sets
            .Where(set => set.Weight is not null && set.Result is SetResult.Reps)
            .GroupBy(set => set.Weight!.Value)
            .Select(group => (WeightKg: group.Key, Reps: group.Max(set => ((SetResult.Reps)set.Result).Count.Value)))
            .Select(now => (now.WeightKg, now.Reps, Best: record.BestReps.FirstOrDefault(best => best.WeightKg == now.WeightKg)))
            .Where(entry => entry.Best is not null && entry.Reps > entry.Best.Reps)
            .OrderByDescending(entry => entry.WeightKg)
            .FirstOrDefault();

        if (qualifying.Best is null)
            return;

        var change = Percent(qualifying.Reps, qualifying.Best.Reps);

        context.Add(
            position, exerciseId, ObservationCode.REPS_PERSONAL_RECORD, null, change,
            ObservationEvidence.Empty
                .With("weightKg", qualifying.WeightKg)
                .With("previousBestReps", qualifying.Best.Reps)
                .With("currentReps", qualifying.Reps));
    }

    private void AddStreaks(
        Context context,
        int position,
        Id exerciseId,
        ExerciseExecution current,
        IReadOnlyList<ExerciseExecution> comparable)
    {
        // Repeated failure: this execution and the ones right before it, all with a rep target missed
        if (current.MissedRepTarget)
        {
            var streak = 1 + comparable.TakeWhile(earlier => earlier.MissedRepTarget).Count();

            if (streak >= _thresholds.RepeatedMissCount)
            {
                var below = current.SetsBelowTarget().Count;

                context.Add(
                    position, exerciseId, ObservationCode.REPEATED_TARGET_MISS, HighlightPriority.HIGH,
                    Round((decimal)below / current.MatchedSets * 100),
                    ObservationEvidence.Empty.With("consecutiveMisses", streak));
            }
        }

        AddPlateau(context, position, exerciseId, current, comparable);
        AddTrend(context, position, exerciseId, current, comparable);
    }

    // No progression: this execution and the ones before it, PlateauCount in total, never went above the best of the one before them
    private void AddPlateau(
        Context context, int position, Id exerciseId, ExerciseExecution current, IReadOnlyList<ExerciseExecution> comparable)
    {
        var windowPrevious = _thresholds.PlateauCount - 1;

        if (comparable.Count < _thresholds.PlateauCount || current.MaxWeightKg is not { } now)
            return;

        var window = comparable.Take(windowPrevious).Select(earlier => earlier.MaxWeightKg).ToList();
        var start = comparable[windowPrevious].MaxWeightKg;

        if (start is not { } baseline || baseline <= 0 || window.Any(max => max is null))
            return;

        var weights = window.Select(max => max!.Value).Reverse().Append(now).ToList();

        if (weights.All(weight => weight <= baseline) == false)
            return;

        context.Add(
            position, exerciseId, ObservationCode.WEIGHT_PLATEAU, null, 0m,
            ObservationEvidence.Empty
                .With("baselineMaxKg", baseline)
                .With("windowMaxWeightsKg", weights)
                .With("executions", weights.Count));
    }

    // Trend: the maximum weight moved the same way in each of the last TrendCount executions, this one included
    private void AddTrend(
        Context context, int position, Id exerciseId, ExerciseExecution current, IReadOnlyList<ExerciseExecution> comparable)
    {
        if (current.MaxWeightKg is not { } now)
            return;

        // Newest first, up to the first execution without weight
        var weights = new List<decimal> { now };

        foreach (var earlier in comparable)
        {
            if (earlier.MaxWeightKg is not { } weight)
                break;

            weights.Add(weight);
        }

        if (weights.Count < 2 || weights[0] == weights[1])
            return;

        var rising = weights[0] > weights[1];
        var steps = 1;

        while (steps + 1 < weights.Count && (weights[steps] > weights[steps + 1]) == rising && weights[steps] != weights[steps + 1])
            steps++;

        if (steps < _thresholds.TrendCount)
            return;

        var earlierWeights = weights.Skip(1).Take(steps).Reverse().ToList();

        if (rising)
        {
            context.Add(
                position, exerciseId, ObservationCode.WEIGHT_UPTREND, null, Math.Abs(Percent(now, earlierWeights[0])),
                ObservationEvidence.Empty
                    .With("previousWeightsKg", earlierWeights)
                    .With("currentWeightKg", now)
                    .With("consecutiveIncreases", steps));

            return;
        }

        context.Add(
            position, exerciseId, ObservationCode.WEIGHT_DOWNTREND, HighlightPriority.HIGH, Math.Abs(Percent(now, earlierWeights[0])),
            ObservationEvidence.Empty
                .With("previousWeightsKg", earlierWeights)
                .With("currentWeightKg", now)
                .With("consecutiveDecreases", steps));
    }

    // Whether the executions right before the current one all missed their rep target, so meeting every target now ends the streak
    private bool EndsMissStreak(IReadOnlyList<ExerciseExecution> comparable)
        => comparable.Count >= _thresholds.RepeatedMissCount
            && comparable.Take(_thresholds.RepeatedMissCount).All(earlier => earlier.MissedRepTarget);

    // Highest priority first, then the biggest deviation, then the position in the workout; one per exercise; at most three
    private static GeneratedObservations Select(List<Candidate> candidates)
    {
        // Grouping keeps the first appearance of each exercise, which is its best candidate, in ranking order. A workout-level
        // observation has no exercise, so it is its own group
        var highlights = candidates
            .Where(candidate => candidate.Observation.Priority is not null)
            .OrderBy(candidate => candidate.Observation.Priority)
            .ThenByDescending(candidate => candidate.Magnitude)
            .ThenBy(candidate => candidate.Position)
            .ThenBy(candidate => candidate.Observation.Code)
            .GroupBy(candidate => candidate.Observation.ExerciseId ?? candidate.Observation.Id)
            .Select(group => group.First().Observation.Id)
            .Take(WorkoutObservations.MaxHighlights)
            .ToList();

        var observations = candidates
            .OrderBy(candidate => candidate.Position)
            .ThenBy(candidate => candidate.Observation.Code)
            .Select(candidate => candidate.Observation)
            .ToList();

        return new GeneratedObservations(observations, highlights);
    }

    private const int WorkoutLevel = -1;

    private static IReadOnlyList<SetTarget> Expand(WorkoutExercise exercise)
        => exercise.Sets.SelectMany(set => Enumerable.Repeat(set.target, set.count)).ToList();

    private static decimal Percent(decimal current, decimal baseline) => Round((current - baseline) / baseline * 100);

    private static decimal Drop(decimal baseline, decimal value) => (baseline - value) / baseline * 100;

    private static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    private static ObservationSeverity SeverityOf(ObservationCode code)
        => code switch
        {
            ObservationCode.WORKOUT_INCOMPLETE
                or ObservationCode.EXERCISE_SKIPPED
                or ObservationCode.REPS_BELOW_TARGET
                or ObservationCode.REPEATED_TARGET_MISS => ObservationSeverity.WARNING,

            ObservationCode.ALL_TARGETS_MET
                or ObservationCode.WEIGHT_PERSONAL_RECORD
                or ObservationCode.REPS_PERSONAL_RECORD
                or ObservationCode.WEIGHT_UPTREND => ObservationSeverity.SUCCESS,

            _ => ObservationSeverity.INFO,
        };

    private sealed record Candidate(WorkoutObservation Observation, decimal Magnitude, int Position);

    private sealed class Context
    {
        private readonly DateTime _createdAt;

        public List<Candidate> Candidates { get; } = [];

        public Context(DateTime createdAt)
        {
            _createdAt = createdAt;
        }

        public void Add(
            int position,
            Id? exerciseId,
            ObservationCode code,
            HighlightPriority? priority,
            decimal magnitude,
            ObservationEvidence evidence)
            => Candidates.Add(new Candidate(
                WorkoutObservation.Create(exerciseId, code, SeverityOf(code), priority, evidence, _createdAt),
                magnitude,
                position));
    }
}
