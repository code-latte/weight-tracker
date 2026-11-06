using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Workouts;

/// <summary>
/// Reusable workout content owned by a trainer: name, description, exercises, sets and superseries.
/// Workouts are created from it by copy, so changing a template never changes a workout.
/// </summary>
/// <remarks>
/// A template has no athlete, date, status or mesocycle, and is never locked.
/// Its content rules are the same as the content rules of <see cref="Workout"/>.
/// </remarks>
public sealed class WorkoutTemplate : AggregateRoot
{
    private List<WorkoutExercise> _exercises;

    /// <summary>
    /// Trainer who owns the template. It never changes.
    /// </summary>
    public Id TrainerId { get; private set; }

    public WorkoutName Name { get; private set; }

    /// <summary>
    /// Optional free text explaining the template, <c>null</c> when it has none.
    /// </summary>
    public WorkoutDescription? Description { get; private set; }

    /// <summary>
    /// Snapshot of the exercises of this template, in the order they must be performed.
    /// The list is copied so callers cannot mutate the aggregate through it.
    /// </summary>
    public IReadOnlyList<WorkoutExercise> Exercises => _exercises.ToList();

    private WorkoutTemplate(Id trainerId, WorkoutName name, WorkoutDescription? description)
    {
        this.Id = Id.New();
        TrainerId = trainerId;
        Name = name;
        Description = description;
        _exercises = new List<WorkoutExercise>();
    }

    /// <summary>
    /// Creates an empty template for a trainer.
    /// </summary>
    public static WorkoutTemplate Create(Id trainerId, WorkoutName name, WorkoutDescription? description)
        => new(trainerId, name, description)
        {
            IsNew = true
        };

    /// <summary>
    /// Creates a template for the workout's trainer by copying the content of a workout: name, description and every
    /// exercise in order, with its effort, sets, per-side flag, rest and superseries. Copied entries get new identifiers,
    /// so the template never shares an entry with the workout. Owner, date, status and placement are not copied.
    /// </summary>
    /// <exception cref="WorkoutWithoutTrainerException">When the workout has no trainer to own the template.</exception>
    public static WorkoutTemplate CreateFromWorkout(Workout workout)
    {
        var trainerId = workout.TrainerId ?? throw new WorkoutWithoutTrainerException(workout.Id);

        var template = Create(trainerId, workout.Name, workout.Description);

        foreach (var source in workout.Exercises)
        {
            var copy = WorkoutExercise.Create(source.ExerciseId, source.Effort, source.Sets, source.PerSide, source.Rest);

            // Superseries ids are unique within the workout and the template starts empty, so they are kept as they are
            if (source.SuperSeriesId is not null)
                copy.JoinSuperSeries(source.SuperSeriesId);

            template._exercises.Add(copy);
        }

        return template;
    }

    /// <exception cref="ExerciseAlreadyInWorkoutException">When two entries share the same exercise.</exception>
    /// <exception cref="SuperSeriesRequiresTwoExercisesException">When a stored superseries has a single member.</exception>
    /// <exception cref="SuperSeriesSetCountMismatchException">When the members of a stored superseries have different set totals.</exception>
    public static WorkoutTemplate Rehydrate(
        Id id,
        Id trainerId,
        WorkoutName name,
        WorkoutDescription? description,
        IEnumerable<WorkoutExercise> exercises)
    {
        var entries = exercises.ToList();

        var duplicate = entries
            .GroupBy(entry => entry.ExerciseId)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
            throw new ExerciseAlreadyInWorkoutException(duplicate.Key, id);

        foreach (var group in entries.Where(entry => entry.SuperSeriesId is not null).GroupBy(entry => entry.SuperSeriesId))
            EnsureValidSuperSeries(id, group.ToList());

        return new WorkoutTemplate(trainerId, name, description)
        {
            Id = id,
            IsNew = false,
            _exercises = entries,
        };
    }

    /// <summary>
    /// Replaces the name and the description. The trainer and the exercises are untouched.
    /// </summary>
    /// <param name="description">New description, <c>null</c> to clear it.</param>
    public void UpdateDetails(WorkoutName name, WorkoutDescription? description)
    {
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Appends an exercise to the end of the template with a given effort and ordered sets.
    /// </summary>
    /// <param name="exerciseId">Exercise to include into the template</param>
    /// <param name="effort">How hard the exercise must be performed</param>
    /// <param name="sets">Ordered config of number of sets x reps (time, max)</param>
    /// <param name="perSide">Whether the targets of the sets are per side</param>
    /// <param name="rest">Rest prescribed between sets, null when none</param>
    /// <exception cref="ExerciseAlreadyInWorkoutException">When the exercise is already in the template.</exception>
    /// <exception cref="InvalidEffortException">When the effort is not a defined value.</exception>
    public void AddExercise(Id exerciseId, Effort effort, IEnumerable<SetPrescription> sets, bool perSide = false, TimeInMinutes? rest = null)
    {
        if (_exercises.Any(entry => entry.ExerciseId == exerciseId))
            throw new ExerciseAlreadyInWorkoutException(exerciseId, this.Id);

        _exercises.Add(WorkoutExercise.Create(exerciseId, effort, sets, perSide, rest));
    }

    /// <summary>
    /// Replaces the effort, the per-side flag, the rest and every set prescription of an exercise. The entry keeps its identifier,
    /// position and superseries.
    /// </summary>
    /// <exception cref="ExerciseNotInWorkoutException">When the exercise is not in the template.</exception>
    /// <exception cref="SuperSeriesSetCountMismatchException">When the entry is in a superseries and the new total of sets differs from the other members.</exception>
    /// <exception cref="InvalidEffortException">When the effort is not a defined value.</exception>
    /// <exception cref="WorkoutExerciseRequiresSetsException">When no set prescription is given.</exception>
    public void UpdateExercise(Id exerciseId, Effort effort, IEnumerable<SetPrescription> sets, bool perSide = false, TimeInMinutes? rest = null)
    {
        var entry = _exercises.FirstOrDefault(item => item.ExerciseId == exerciseId)
            ?? throw new ExerciseNotInWorkoutException(exerciseId, this.Id);

        var prescriptions = sets.ToList();

        if (entry.SuperSeriesId is not null)
        {
            var newTotal = prescriptions.Sum(set => set.count);
            var others = _exercises.Where(item => item.SuperSeriesId == entry.SuperSeriesId && item != entry);

            if (others.Any(other => other.TotalSets != newTotal))
                throw new SuperSeriesSetCountMismatchException(this.Id);
        }

        entry.Replace(effort, prescriptions, perSide, rest);
    }

    /// <exception cref="ExerciseNotInWorkoutException">When the exercise is not in the template.</exception>
    public void RemoveExercise(Id exerciseId)
    {
        var entry = _exercises.FirstOrDefault(item => item.ExerciseId == exerciseId)
            ?? throw new ExerciseNotInWorkoutException(exerciseId, this.Id);

        _exercises.Remove(entry);

        // A superseries cannot be left with a single exercise
        if (entry.SuperSeriesId is not null)
        {
            var remaining = _exercises.Where(item => item.SuperSeriesId == entry.SuperSeriesId).ToList();

            if (remaining.Count == 1)
                remaining[0].LeaveSuperSeries();
        }
    }

    /// <summary>
    /// Puts the exercises of this template in the given order. Entry ids, effort, sets and superseries are kept.
    /// </summary>
    /// <param name="exerciseIds">Catalog exercises of this template, every one exactly once, in the new order.</param>
    /// <exception cref="WorkoutExerciseOrderMismatchException">When the ids are not exactly the exercises of the template.</exception>
    public void ReorderExercises(IEnumerable<Id> exerciseIds)
    {
        var requested = exerciseIds.ToList();

        var matches = requested.Count == _exercises.Count
            && requested.Distinct().Count() == requested.Count
            && requested.All(exerciseId => _exercises.Any(entry => entry.ExerciseId == exerciseId));

        if (matches == false)
            throw new WorkoutExerciseOrderMismatchException(this.Id);

        _exercises = requested
            .Select(exerciseId => _exercises.First(entry => entry.ExerciseId == exerciseId))
            .ToList();
    }

    /// <summary>
    /// Groups exercises of this template into a superseries.
    /// </summary>
    /// <param name="workoutExerciseIds">Identifiers of the entries of this template to group, not of the catalog exercises</param>
    /// <returns>The identifier of the new superseries, unique inside this template.</returns>
    /// <exception cref="SuperSeriesRequiresTwoExercisesException">When fewer than two entries are given.</exception>
    /// <exception cref="DuplicateExerciseInSuperSeriesException">When the same entry is given twice.</exception>
    /// <exception cref="WorkoutExerciseNotInWorkoutException">When an entry is not in the template.</exception>
    /// <exception cref="ExerciseAlreadyInSuperSeriesException">When an entry already belongs to a superseries.</exception>
    /// <exception cref="SuperSeriesSetCountMismatchException">When the entries have different numbers of sets.</exception>
    public SuperSeriesId CreateSuperSeries(IEnumerable<Id> workoutExerciseIds)
    {
        var requested = workoutExerciseIds.ToList();

        if (requested.Count < 2)
            throw new SuperSeriesRequiresTwoExercisesException(requested.Count);

        var duplicate = requested.GroupBy(workoutExerciseId => workoutExerciseId).FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
            throw new DuplicateExerciseInSuperSeriesException(duplicate.Key);

        var members = requested
            .Select(workoutExerciseId => _exercises.FirstOrDefault(item => item.Id == workoutExerciseId)
                ?? throw new WorkoutExerciseNotInWorkoutException(workoutExerciseId, this.Id))
            .ToList();

        var alreadyGrouped = members.FirstOrDefault(member => member.SuperSeriesId is not null);

        if (alreadyGrouped is not null)
            throw new ExerciseAlreadyInSuperSeriesException(alreadyGrouped.Id, this.Id);

        if (members.Select(member => member.TotalSets).Distinct().Count() > 1)
            throw new SuperSeriesSetCountMismatchException(this.Id);

        var nextValue = _exercises.Where(item => item.SuperSeriesId is not null).Select(item => item.SuperSeriesId!.Value).DefaultIfEmpty(0).Max() + 1;
        var superSeriesId = SuperSeriesId.From(nextValue);

        foreach (var member in members)
            member.JoinSuperSeries(superSeriesId);

        return superSeriesId;
    }

    /// <summary>
    /// Ungroups a superseries. Its exercises stay in the template.
    /// </summary>
    /// <exception cref="SuperSeriesNotFoundException">When no exercise of the template belongs to it.</exception>
    public void BreakSuperSeries(SuperSeriesId superSeriesId)
    {
        var members = _exercises.Where(item => item.SuperSeriesId == superSeriesId).ToList();

        if (members.Count == 0)
            throw new SuperSeriesNotFoundException(superSeriesId, this.Id);

        foreach (var member in members)
            member.LeaveSuperSeries();
    }

    private static void EnsureValidSuperSeries(Id templateId, IReadOnlyList<WorkoutExercise> members)
    {
        if (members.Count < 2)
            throw new SuperSeriesRequiresTwoExercisesException(members.Count);

        if (members.Select(member => member.TotalSets).Distinct().Count() > 1)
            throw new SuperSeriesSetCountMismatchException(templateId);
    }
}
