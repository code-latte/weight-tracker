using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Mesocycles;

namespace WeightTracker.Domain.Workouts.Observations;

/// <summary>
/// The sets recorded for one exercise in one workout, with the targets they were prescribed against. It is the unit the
/// observation rules compare: the current workout and every earlier session are both seen as executions.
/// </summary>
public sealed record ExerciseExecution
{
    public Id WorkoutId { get; }

    public DateOnly WorkoutDate { get; }

    /// <summary>
    /// Type of the mesocycle week the workout was placed in. Null when it was not placed in a mesocycle.
    /// </summary>
    public TrainingWeekType? WeekType { get; }

    /// <summary>
    /// Whether the targets were per side.
    /// </summary>
    public bool PerSide { get; }

    /// <summary>
    /// One target per prescribed set, in order. A recorded set is matched to the target at its own position.
    /// </summary>
    public IReadOnlyList<SetTarget> Targets { get; }

    /// <summary>
    /// The recorded sets, in order. There is at least one.
    /// </summary>
    public IReadOnlyList<ExecutionSet> Sets { get; }

    /// <exception cref="InvalidWorkoutObservationException">When there are no recorded sets.</exception>
    public ExerciseExecution(
        Id workoutId,
        DateOnly workoutDate,
        TrainingWeekType? weekType,
        bool perSide,
        IEnumerable<SetTarget> targets,
        IEnumerable<ExecutionSet> sets)
    {
        WorkoutId = workoutId;
        WorkoutDate = workoutDate;
        WeekType = weekType;
        PerSide = perSide;
        Targets = targets.ToList();
        Sets = sets.ToList();

        if (Sets.Count == 0)
            throw new InvalidWorkoutObservationException("an execution needs at least one recorded set");
    }

    public ResultKind Kind => Sets[0].Result is SetResult.Reps ? ResultKind.REPS : ResultKind.DURATION;

    public int PrescribedSets => Targets.Count;

    /// <summary>
    /// Whether the workout sat in a week meant to be lighter, where comparing with other weeks says nothing.
    /// </summary>
    public bool IsLightWeek => WeekType is TrainingWeekType.DELOAD or TrainingWeekType.RECOVERY;

    /// <summary>
    /// Whether it can be compared with another execution of the same kind and per-side setting: it must be of that kind
    /// and per-side setting and must not sit in a light week.
    /// </summary>
    public bool IsComparableWith(ResultKind kind, bool perSide) => IsLightWeek == false && Kind == kind && PerSide == perSide;

    /// <summary>
    /// The highest weight lifted in any set. Null when no set has weight.
    /// </summary>
    public decimal? MaxWeightKg => Sets.Where(set => set.Weight is not null).Select(set => (decimal?)set.Weight!.Value).Max();

    /// <summary>
    /// Sum of weight times reps over the rep sets that have both. Null when none does.
    /// </summary>
    public decimal? VolumeKg
    {
        get
        {
            var weighted = RepSets.Where(set => set.Weight is not null).ToList();

            return weighted.Count == 0 ? null : weighted.Sum(set => set.Weight!.Value * Reps(set));
        }
    }

    public int RepSetCount => RepSets.Count();

    public int TotalReps => RepSets.Sum(Reps);

    /// <summary>
    /// The sets that fell short of their rep target, with their 1-based position, reps and the minimum asked.
    /// Only fixed-rep and rep-range targets have a minimum.
    /// </summary>
    public IReadOnlyList<SetDeviation> SetsBelowTarget()
        => Matched()
            .Where(pair => pair.Set.Result is SetResult.Reps && MinReps(pair.Target) is not null && Reps(pair.Set) < MinReps(pair.Target))
            .Select(pair => new SetDeviation(pair.Position, Reps(pair.Set), MinReps(pair.Target)!.Value))
            .ToList();

    /// <summary>
    /// The sets above the maximum of their rep-range target, with their 1-based position, reps and the maximum.
    /// </summary>
    public IReadOnlyList<SetDeviation> SetsAboveRange()
        => Matched()
            .Where(pair => pair.Set.Result is SetResult.Reps && pair.Target is SetTarget.RepsRange range && Reps(pair.Set) > range.max)
            .Select(pair => new SetDeviation(pair.Position, Reps(pair.Set), ((SetTarget.RepsRange)pair.Target).max))
            .ToList();

    public bool MissedRepTarget => SetsBelowTarget().Count > 0;

    /// <summary>
    /// How many recorded sets have a prescribed set at their position.
    /// </summary>
    public int MatchedSets => Math.Min(Sets.Count, Targets.Count);

    /// <summary>
    /// Whether every prescribed set was recorded, met its target and none went above a range maximum.
    /// </summary>
    public bool MetEveryTarget
        => Targets.Count > 0
            && Sets.Count >= Targets.Count
            && Matched().All(pair => pair.Target.IsMetBy(pair.Set.Result))
            && SetsAboveRange().Count == 0;

    private IEnumerable<ExecutionSet> RepSets => Sets.Where(set => set.Result is SetResult.Reps);

    private IEnumerable<(int Position, ExecutionSet Set, SetTarget Target)> Matched()
        => Sets.Zip(Targets, (set, target) => (set, target)).Select((pair, index) => (index + 1, pair.set, pair.target));

    private static int Reps(ExecutionSet set) => ((SetResult.Reps)set.Result).Count.Value;

    private static int? MinReps(SetTarget target)
        => target switch
        {
            SetTarget.Reps reps => reps.value,
            SetTarget.RepsRange range => range.min,
            _ => null,
        };
}

/// <summary>
/// A set that went outside its rep target: its 1-based position, the reps done and the bound it broke.
/// </summary>
public sealed record SetDeviation(int Position, int Reps, int Bound);
