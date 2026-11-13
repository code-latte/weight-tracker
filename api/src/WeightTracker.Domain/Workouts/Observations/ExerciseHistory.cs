using WeightTracker.Domain.Mesocycles;

namespace WeightTracker.Domain.Workouts.Observations;

/// <summary>
/// What an athlete did in earlier sessions of one exercise, prescribed by the same trainer, newest first, plus the
/// records over every comparable session, which can reach further back than the sessions listed.
/// </summary>
/// <param name="Executions">The latest earlier sessions, newest first.</param>
/// <param name="Records">The best marks over all earlier sessions that sat outside light weeks, by kind and per-side setting.</param>
public sealed record ExerciseHistory(IReadOnlyList<ExerciseExecution> Executions, IReadOnlyList<ExerciseRecord> Records)
{
    public static readonly ExerciseHistory None = new([], []);

    /// <summary>
    /// A history whose records are derived from the given executions only, which are taken newest first.
    /// </summary>
    public static ExerciseHistory Of(IEnumerable<ExerciseExecution> executions)
    {
        var all = executions.ToList();

        var records = all
            .Where(execution => execution.IsLightWeek == false)
            .GroupBy(execution => (execution.Kind, execution.PerSide))
            .Select(group => new ExerciseRecord(
                group.Key.PerSide,
                group.Key.Kind,
                group.Max(execution => execution.MaxWeightKg),
                group
                    .Where(execution => execution.Kind == ResultKind.REPS)
                    .SelectMany(execution => execution.Sets)
                    .Where(set => set.Weight is not null && set.Result is SetResult.Reps)
                    .GroupBy(set => set.Weight!.Value)
                    .Select(weight => new RepsAtWeight(weight.Key, weight.Max(set => ((SetResult.Reps)set.Result).Count.Value)))
                    .ToList()))
            .ToList();

        return new ExerciseHistory(all, records);
    }

    /// <summary>
    /// The record of the given kind and per-side setting, null when there is none.
    /// </summary>
    public ExerciseRecord? RecordFor(ResultKind kind, bool perSide)
        => Records.FirstOrDefault(record => record.Kind == kind && record.PerSide == perSide);
}

/// <summary>
/// The best marks ever reached in comparable sessions of one exercise.
/// </summary>
/// <param name="MaxWeightKg">The highest weight lifted. Null when no set had weight.</param>
/// <param name="BestReps">For each weight used, the most reps done in one set at exactly that weight.</param>
public sealed record ExerciseRecord(bool PerSide, ResultKind Kind, decimal? MaxWeightKg, IReadOnlyList<RepsAtWeight> BestReps);

/// <summary>
/// The most reps done in one set at one weight.
/// </summary>
public sealed record RepsAtWeight(decimal WeightKg, int Reps);
