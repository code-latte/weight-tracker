using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Workouts;

/// <summary>
/// One exercise inside a workout: which exercise, how hard, and the ordered sets to perform.
/// </summary>
/// <remarks>
/// Entity of the Workout and WorkoutTemplate aggregates. It is only created, loaded and saved through its root,
/// <see cref="Workout"/> or <see cref="WorkoutTemplate"/>, and never shared between two roots.
/// The Exercise aggregate is referenced by identifier only.
/// </remarks>
public sealed class WorkoutExercise : Entity
{
    private readonly List<SetPrescription> _sets;

    private readonly List<SetRecord> _setRecords;

    /// <summary>
    /// Exercise performed in this entry. Immutable once the entry exists.
    /// </summary>
    public Id ExerciseId { get; }

    public Effort Effort { get; private set; }

    /// <summary>
    /// When true, the targets of the sets are per side (i.e. 1x8 reps means 8 on each side).
    /// </summary>
    public bool PerSide { get; private set; }

    /// <summary>
    /// Rest prescribed between sets, null when none is prescribed.
    /// </summary>
    public TimeInMinutes? Rest { get; private set; }

    /// <summary>
    /// Snapshot of the prescriptions, in the order they must be performed.
    /// </summary>
    public IReadOnlyList<SetPrescription> Sets => _sets.ToList();

    /// <summary>
    /// Total number of sets to perform: the sum of the counts of every prescription.
    /// </summary>
    public int TotalSets => _sets.Sum(set => set.count);

    /// <summary>
    /// How many recorded sets met their target. The prescriptions are expanded by count into one target per set and
    /// each set record is matched to the target at its own position. Records beyond the prescribed sets and prescribed
    /// sets without a record are not evaluated.
    /// </summary>
    /// <returns>The sets that met their target and the sets that were evaluated.</returns>
    public (int InTarget, int Evaluated) SetsInTarget()
    {
        var targets = _sets.SelectMany(set => Enumerable.Repeat(set.target, set.count)).ToList();

        var evaluated = _setRecords.Zip(targets, (record, target) => target.IsMetBy(record.Result)).ToList();

        return (evaluated.Count(isMet => isMet), evaluated.Count);
    }

    /// <summary>
    /// Superseries this entry belongs to, null when it is performed on its own.
    /// Only its root changes it, because the grouping rules span several entries.
    /// </summary>
    public SuperSeriesId? SuperSeriesId { get; private set; }

    /// <summary>
    /// Snapshot of the sets actually performed, in their order: a new set record goes last, whatever its
    /// registration time, and only a reorder moves them.
    /// </summary>
    public IReadOnlyList<SetRecord> SetRecords => _setRecords.ToList();

    private WorkoutExercise(Id id, Id exerciseId, Effort effort, IEnumerable<SetPrescription> sets, SuperSeriesId? superSeriesId, bool perSide, TimeInMinutes? rest, IEnumerable<SetRecord> setRecords, bool isNew)
    {
        var prescriptions = EnsureValid(effort, sets);

        this.Id = id;
        this.IsNew = isNew;
        ExerciseId = exerciseId;
        Effort = effort;
        PerSide = perSide;
        Rest = rest;
        _sets = prescriptions;
        SuperSeriesId = superSeriesId;
        _setRecords = setRecords.ToList();
    }

    /// <summary>
    /// Creates an entry that does not exist in the system yet.
    /// </summary>
    /// <exception cref="InvalidEffortException">When the effort is not a defined value.</exception>
    /// <exception cref="WorkoutExerciseRequiresSetsException">When no set prescription is given.</exception>
    public static WorkoutExercise Create(
        Id exerciseId,
        Effort effort,
        IEnumerable<SetPrescription> sets,
        bool perSide = false,
        TimeInMinutes? rest = null)
        => new(Id.New(), exerciseId, effort, sets, superSeriesId: null, perSide, rest, setRecords: [], isNew: true);

    /// <summary>
    /// Restores an entry already stored in the system.
    /// Stored set records are restored as they are, whatever the current prescriptions.
    /// </summary>
    /// <exception cref="InvalidEffortException">When the effort is not a defined value.</exception>
    public static WorkoutExercise Rehydrate(
        Id id,
        Id exerciseId,
        Effort effort,
        IEnumerable<SetPrescription> sets,
        SuperSeriesId? superSeriesId = null,
        bool perSide = false,
        TimeInMinutes? rest = null,
        IEnumerable<SetRecord>? setRecords = null)
        => new(id, exerciseId, effort, sets, superSeriesId, perSide, rest, setRecords ?? [], isNew: false);

    /// <summary>
    /// Replaces the effort, the per-side flag, the rest and every set prescription. Nothing changes when the new values are invalid.
    /// Only its root calls it, because changing the total of sets can break a superseries.
    /// </summary>
    /// <exception cref="InvalidEffortException">When the effort is not a defined value.</exception>
    /// <exception cref="WorkoutExerciseRequiresSetsException">When no set prescription is given.</exception>
    /// <exception cref="SetRecordNotAllowedForTargetException">When the entry has set records and the new prescriptions are not of the same kind as the current ones.</exception>
    internal void Replace(Effort effort, IEnumerable<SetPrescription> sets, bool perSide, TimeInMinutes? rest)
    {
        var prescriptions = EnsureValid(effort, sets);

        if (_setRecords.Count > 0 && KindOf(prescriptions) != KindOf(_sets))
            throw new SetRecordNotAllowedForTargetException(ExerciseId);

        Effort = effort;
        PerSide = perSide;
        Rest = rest;
        _sets.Clear();
        _sets.AddRange(prescriptions);
    }

    private static List<SetPrescription> EnsureValid(Effort effort, IEnumerable<SetPrescription> sets)
    {
        if (Enum.IsDefined(effort) == false)
            throw new InvalidEffortException((int)effort);

        var prescriptions = sets.ToList();

        if (prescriptions.Count == 0)
            throw new WorkoutExerciseRequiresSetsException();

        return prescriptions;
    }

    /// <summary>
    /// Appends a set actually performed. Without a result, the set counts as done as prescribed: the value comes from the
    /// next prescribed set, the one at the position given by the sets already recorded. Only its root calls it, because
    /// recording depends on the workout status.
    /// </summary>
    /// <exception cref="SetRecordNotAllowedForTargetException">When the entry mixes rep-based and time-based prescriptions, or the result is not of the entry's kind.</exception>
    /// <exception cref="SetResultRequiredException">When no result is given and the next prescribed set has no single value or there is none left.</exception>
    internal void AddSetRecord(SetResult? result, Weight? weight, DateTime registeredAtUtc)
    {
        var kind = KindOf(_sets);

        if (kind == PrescriptionKind.Mixed)
            throw new SetRecordNotAllowedForTargetException(ExerciseId);

        var resolved = result ?? PrescribedResult();

        if (KindOf(resolved) != kind)
            throw new SetRecordNotAllowedForTargetException(ExerciseId);

        _setRecords.Add(SetRecord.Create(resolved, weight, registeredAtUtc));
    }

    /// <summary>
    /// Replaces the result and weight of one set record. Only its root calls it, because recording depends on the workout status.
    /// </summary>
    /// <exception cref="SetRecordNotFoundException">When the set record is not recorded for this entry.</exception>
    /// <exception cref="SetRecordNotAllowedForTargetException">When the entry mixes rep-based and time-based prescriptions, or the result is not of the entry's kind.</exception>
    internal void UpdateSetRecord(Id setRecordId, SetResult result, Weight? weight)
    {
        var setRecord = FindSetRecord(setRecordId);
        var kind = KindOf(_sets);

        if (kind == PrescriptionKind.Mixed || KindOf(result) != kind)
            throw new SetRecordNotAllowedForTargetException(ExerciseId);

        setRecord.Change(result, weight);
    }

    /// <summary>
    /// Value of the next prescribed set: the prescriptions expanded in order into one target per set, at the position given by the
    /// number of sets already recorded. Minutes become seconds, rounded to the nearest second.
    /// </summary>
    /// <exception cref="SetResultRequiredException">When every prescribed set is already recorded, or the target is a range or max reps.</exception>
    private SetResult PrescribedResult()
    {
        var targets = _sets.SelectMany(set => Enumerable.Repeat(set.target, set.count)).ToList();

        if (_setRecords.Count >= targets.Count)
            throw new SetResultRequiredException(ExerciseId, "every prescribed set is already recorded");

        var target = targets[_setRecords.Count];

        return target switch
        {
            SetTarget.Reps reps => new SetResult.Reps(RepCount.From(reps.value)),
            SetTarget.Duration duration => new SetResult.Duration(DurationInSeconds.From(duration.value)),
            SetTarget.Minutes minutes => new SetResult.Duration(DurationInSeconds.From(
                Math.Max(1, (int)decimal.Round(minutes.value.Value * 60, MidpointRounding.AwayFromZero)))),
            _ => throw new SetResultRequiredException(ExerciseId, $"the prescribed set is '{target}', which has no single value"),
        };
    }

    /// <summary>
    /// Removes one set record; the others keep their order. Only its root calls it, because recording depends on the workout status.
    /// </summary>
    /// <exception cref="SetRecordNotFoundException">When the set record is not recorded for this entry.</exception>
    internal void RemoveSetRecord(Id setRecordId)
        => _setRecords.Remove(FindSetRecord(setRecordId));

    /// <summary>
    /// Puts the set records of this entry in the given order. Ids, results, weights and registration times are kept.
    /// Only its root calls it, because recording depends on the workout status.
    /// </summary>
    /// <exception cref="SetRecordOrderMismatchException">When the ids are not exactly the set records of this entry.</exception>
    internal void ReorderSetRecords(IEnumerable<Id> setRecordIds)
    {
        var requested = setRecordIds.ToList();

        var matches = requested.Count == _setRecords.Count
            && requested.Distinct().Count() == requested.Count
            && requested.All(setRecordId => _setRecords.Any(record => record.Id == setRecordId));

        if (matches == false)
            throw new SetRecordOrderMismatchException(ExerciseId);

        var reordered = requested
            .Select(setRecordId => _setRecords.First(record => record.Id == setRecordId))
            .ToList();

        _setRecords.Clear();
        _setRecords.AddRange(reordered);
    }

    private SetRecord FindSetRecord(Id setRecordId)
        => _setRecords.FirstOrDefault(record => record.Id == setRecordId)
            ?? throw new SetRecordNotFoundException(setRecordId, ExerciseId);

    /// <summary>
    /// What the prescriptions of an entry measure. Set records only make sense when every prescription measures the same thing,
    /// and each record then measures that same thing.
    /// </summary>
    private enum PrescriptionKind { RepBased, TimeBased, Mixed }

    private static PrescriptionKind KindOf(IEnumerable<SetPrescription> prescriptions)
    {
        var targets = prescriptions.Select(set => set.target).ToList();

        if (targets.All(target => target is SetTarget.Reps or SetTarget.RepsRange or SetTarget.MaxReps))
            return PrescriptionKind.RepBased;

        if (targets.All(target => target is SetTarget.Duration or SetTarget.Minutes))
            return PrescriptionKind.TimeBased;

        return PrescriptionKind.Mixed;
    }

    private static PrescriptionKind KindOf(SetResult result)
        => result is SetResult.Reps ? PrescriptionKind.RepBased : PrescriptionKind.TimeBased;

    internal void JoinSuperSeries(SuperSeriesId superSeriesId) => SuperSeriesId = superSeriesId;

    internal void LeaveSuperSeries() => SuperSeriesId = null;
}
