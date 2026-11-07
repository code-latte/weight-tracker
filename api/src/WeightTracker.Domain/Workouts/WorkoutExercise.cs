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
    /// <exception cref="SetRecordNotAllowedForTargetException">When the entry has set records and a new prescription is not rep-based.</exception>
    internal void Replace(Effort effort, IEnumerable<SetPrescription> sets, bool perSide, TimeInMinutes? rest)
    {
        var prescriptions = EnsureValid(effort, sets);

        if (_setRecords.Count > 0 && AreRepBased(prescriptions) == false)
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
    /// Appends a set actually performed. Only its root calls it, because recording depends on the workout status.
    /// </summary>
    /// <exception cref="SetRecordNotAllowedForTargetException">When any prescription of the entry is not rep-based.</exception>
    internal void AddSetRecord(SetRecord setRecord)
    {
        if (AreRepBased(_sets) == false)
            throw new SetRecordNotAllowedForTargetException(ExerciseId);

        _setRecords.Add(setRecord);
    }

    /// <summary>
    /// Replaces the reps and weight of one set record. Only its root calls it, because recording depends on the workout status.
    /// </summary>
    /// <exception cref="SetRecordNotFoundException">When the set record is not recorded for this entry.</exception>
    internal void UpdateSetRecord(Id setRecordId, RepCount reps, Weight weight)
        => FindSetRecord(setRecordId).Change(reps, weight);

    /// <summary>
    /// Removes one set record; the others keep their order. Only its root calls it, because recording depends on the workout status.
    /// </summary>
    /// <exception cref="SetRecordNotFoundException">When the set record is not recorded for this entry.</exception>
    internal void RemoveSetRecord(Id setRecordId)
        => _setRecords.Remove(FindSetRecord(setRecordId));

    /// <summary>
    /// Puts the set records of this entry in the given order. Ids, reps, weight and registration times are kept.
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
    /// Set records carry a number of reps, so they only make sense for fixed reps, reps ranges and max reps.
    /// </summary>
    private static bool AreRepBased(IEnumerable<SetPrescription> prescriptions)
        => prescriptions.All(set => set.target is SetTarget.Reps or SetTarget.RepsRange or SetTarget.MaxReps);

    internal void JoinSuperSeries(SuperSeriesId superSeriesId) => SuperSeriesId = superSeriesId;

    internal void LeaveSuperSeries() => SuperSeriesId = null;
}
