using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Workouts;

public sealed class Workout : AggregateRoot
{

    private DateTime _workoutDate;

    private List<WorkoutExercise> _exercises;

    /// <summary>
    /// Owner of the workout. It changes only through <see cref="Reassign"/>.
    /// </summary>
    public Id UserId { get; private set; }

    /// <summary>
    /// Trainer who prescribed the workout, when somebody other than the owner created it.
    /// </summary>
    public Id? TrainerId { get; private set; }

    public DateTime WorkoutDate => _workoutDate;

    public WorkoutName Name { get; private set; }

    /// <summary>
    /// Optional free text explaining the workout, <c>null</c> when it has none.
    /// </summary>
    public WorkoutDescription? Description { get; private set; }

    /// <summary>
    /// Where the workout is in its lifecycle. A new workout is always a draft.
    /// </summary>
    public WorkoutStatus Status { get; private set; }

    /// <summary>
    /// A started or finished workout no longer accepts changes to its content.
    /// </summary>
    public bool IsLocked => Status is WorkoutStatus.STARTED or WorkoutStatus.FINISHED;

    /// <summary>
    /// Mesocycle this workout is planned in, <c>null</c> for a standalone workout.
    /// Set together with <see cref="MesocycleWeekNumber"/>. The mesocycle is referenced by identifier only.
    /// </summary>
    public Id? MesocycleId { get; private set; }

    /// <summary>
    /// Week of the mesocycle this workout is planned in, <c>null</c> for a standalone workout.
    /// </summary>
    public WeekNumber? MesocycleWeekNumber { get; private set; }

    /// <summary>
    /// Snapshot of the exercises of this workout, in the order they must be performed.
    /// The list is copied so callers cannot mutate the aggregate through it.
    /// </summary>
    public IReadOnlyList<WorkoutExercise> Exercises => _exercises.ToList();

    private Workout(
        Id userId,
        Id? trainerId,
        WorkoutName name,
        WorkoutDescription? description,
        DateTime workoutDate,
        WorkoutStatus status)
    {
        this.Id = Id.New();
        UserId = userId;
        TrainerId = trainerId;
        Name = name;
        Description = description;
        _workoutDate = workoutDate.Date;
        Status = status;
        _exercises = new List<WorkoutExercise>();
    }

    /// <summary>
    /// Creates a workout for a user, optionally prescribed by a trainer.
    /// </summary>
    /// <exception cref="TrainerCannotBeWorkoutOwnerException">When the trainer is the owner.</exception>
    public static Workout Create(
        Id userId,
        Id? trainerId,
        WorkoutName name,
        WorkoutDescription? description,
        DateTime workoutDate)
    {
        EnsureTrainerIsNotOwner(userId, trainerId);

        return new Workout(userId, trainerId, name, description, workoutDate, WorkoutStatus.DRAFT)
        {
            IsNew = true
        };
    }

    /// <exception cref="InvalidWorkoutStatusException">When the status is not a defined value.</exception>
    /// <exception cref="ExerciseAlreadyInWorkoutException">When two entries share the same exercise.</exception>
    /// <exception cref="SuperSeriesRequiresTwoExercisesException">When a stored superseries has a single member.</exception>
    /// <exception cref="SuperSeriesSetCountMismatchException">When the members of a stored superseries have different set totals.</exception>
    /// <exception cref="IncompleteWorkoutPlacementException">When only one of the mesocycle id and week number is given.</exception>
    public static Workout Rehydrate(
        Id id,
        Id userId,
        Id? trainerId,
        WorkoutName name,
        WorkoutDescription? description,
        DateTime workoutdate,
        WorkoutStatus status,
        IEnumerable<WorkoutExercise> exercises,
        Id? mesocycleId = null,
        WeekNumber? mesocycleWeekNumber = null
    )
    {
        EnsureDefined(status);

        if ((mesocycleId is null) != (mesocycleWeekNumber is null))
            throw new IncompleteWorkoutPlacementException(id);

        var entries = exercises.ToList();

        var duplicate = entries
            .GroupBy(entry => entry.ExerciseId)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
            throw new ExerciseAlreadyInWorkoutException(duplicate.Key, id);

        foreach (var group in entries.Where(entry => entry.SuperSeriesId is not null).GroupBy(entry => entry.SuperSeriesId))
            EnsureValidSuperSeries(id, group.ToList());

        return new Workout(userId, trainerId, name, description, workoutdate, status)
        {
            Id = id,
            IsNew = false,
            _exercises = entries,
            MesocycleId = mesocycleId,
            MesocycleWeekNumber = mesocycleWeekNumber,
        };
    }

    /// <summary>
    /// Replaces the name, the description and the day of the workout. Status and exercises are untouched.
    /// </summary>
    /// <param name="description">New description, <c>null</c> to clear it.</param>
    /// <param name="workoutDate">New day of the workout. Time is not kept.</param>
    public void UpdateDetails(WorkoutName name, WorkoutDescription? description, DateTime workoutDate)
    {
        Name = name;
        Description = description;
        _workoutDate = workoutDate.Date;
    }

    /// <summary>
    /// Hands the workout to another owner and trainer. Reserved for callers entitled to correct the assignment.
    /// </summary>
    /// <param name="trainerId">New trainer, <c>null</c> to leave the workout without trainer.</param>
    /// <exception cref="TrainerCannotBeWorkoutOwnerException">When the trainer is the owner.</exception>
    public void Reassign(Id userId, Id? trainerId)
    {
        EnsureTrainerIsNotOwner(userId, trainerId);

        UserId = userId;
        TrainerId = trainerId;
    }

    /// <summary>
    /// Places the workout in a week of a mesocycle, or moves it there when it is already placed.
    /// Whether the mesocycle exists and the week fits it is checked by the caller, which knows both aggregates.
    /// </summary>
    /// <exception cref="WorkoutPlacementLockedException">When the workout is started or finished.</exception>
    public void LinkToMesocycle(Id mesocycleId, WeekNumber weekNumber)
    {
        EnsurePlacementCanChange();

        MesocycleId = mesocycleId;
        MesocycleWeekNumber = weekNumber;
    }

    /// <summary>
    /// Takes the workout out of its mesocycle, leaving it standalone.
    /// </summary>
    /// <exception cref="WorkoutPlacementLockedException">When the workout is started or finished.</exception>
    public void UnlinkFromMesocycle()
    {
        EnsurePlacementCanChange();

        MesocycleId = null;
        MesocycleWeekNumber = null;
    }

    /// <summary>
    /// Moves the workout along its lifecycle: draft to published, published back to draft or on to started,
    /// and started to finished. Anything else, including staying in the same status, is refused.
    /// </summary>
    /// <exception cref="InvalidWorkoutStatusException">When the target is not a defined value.</exception>
    /// <exception cref="InvalidWorkoutStatusTransitionException">When the move is not part of the lifecycle.</exception>
    public void ChangeStatus(WorkoutStatus target)
    {
        EnsureDefined(target);

        if (CanTransitionTo(target) == false)
            throw new InvalidWorkoutStatusTransitionException(Status, target);

        Status = target;
    }

    /// <summary>
    /// Whether moving from the current status to the target is part of the lifecycle.
    /// </summary>
    public bool CanTransitionTo(WorkoutStatus target)
        => (Status, target) switch
        {
            (WorkoutStatus.DRAFT, WorkoutStatus.PUBLISHED) => true,
            (WorkoutStatus.PUBLISHED, WorkoutStatus.DRAFT) => true,
            (WorkoutStatus.PUBLISHED, WorkoutStatus.STARTED) => true,
            (WorkoutStatus.STARTED, WorkoutStatus.FINISHED) => true,
            _ => false,
        };

    /// <summary>
    /// Sets the status skipping the lifecycle. Reserved for callers that are entitled to correct a workout.
    /// </summary>
    /// <exception cref="InvalidWorkoutStatusException">When the target is not a defined value.</exception>
    public void OverrideStatus(WorkoutStatus target)
    {
        EnsureDefined(target);

        Status = target;
    }

    /// <summary>
    /// A prescribed workout is invisible to its athlete until the trainer publishes it.
    /// A workout without trainer is the athlete's own plan, so it is never hidden from them.
    /// </summary>
    /// <param name="callerId">User asking to see the workout.</param>
    public bool IsHiddenFrom(Id callerId)
        => Status == WorkoutStatus.DRAFT && TrainerId is not null && UserId == callerId;

    /// <summary>
    /// Appends an exercise to the end of the workout with a given effort and ordered sets.
    /// </summary>
    /// <param name="exerciseId">Exercise to include into the workout</param>
    /// <param name="effort">How hard the exercise must be performed</param>
    /// <param name="sets">Ordered config of number of sets x reps (time, max)</param>
    /// <param name="perSide">Whether the targets of the sets are per side</param>
    /// <param name="rest">Rest prescribed between sets, null when none</param>
    /// <exception cref="ExerciseAlreadyInWorkoutException">When the exercise is already in the workout.</exception>
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
    /// <exception cref="ExerciseNotInWorkoutException">When the exercise is not in the workout.</exception>
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

    /// <exception cref="ExerciseNotInWorkoutException">When the exercise is not in the workout.</exception>
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
    /// Puts the exercises of this workout in the given order. Entry ids, effort, sets and superseries are kept.
    /// </summary>
    /// <param name="exerciseIds">Catalog exercises of this workout, every one exactly once, in the new order.</param>
    /// <exception cref="WorkoutExerciseOrderMismatchException">When the ids are not exactly the exercises of the workout.</exception>
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
    /// Groups exercises of this workout into a superseries: the first set of every exercise is done
    /// back to back, then rest, then the next set of every exercise.
    /// </summary>
    /// <param name="workoutExerciseIds">Identifiers of the entries of this workout to group, not of the catalog exercises</param>
    /// <returns>The identifier of the new superseries, unique inside this workout.</returns>
    /// <exception cref="SuperSeriesRequiresTwoExercisesException">When fewer than two entries are given.</exception>
    /// <exception cref="DuplicateExerciseInSuperSeriesException">When the same entry is given twice.</exception>
    /// <exception cref="WorkoutExerciseNotInWorkoutException">When an entry is not in the workout.</exception>
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
    /// Ungroups a superseries. Its exercises stay in the workout.
    /// </summary>
    /// <exception cref="SuperSeriesNotFoundException">When no exercise of the workout belongs to it.</exception>
    public void BreakSuperSeries(SuperSeriesId superSeriesId)
    {
        var members = _exercises.Where(item => item.SuperSeriesId == superSeriesId).ToList();

        if (members.Count == 0)
            throw new SuperSeriesNotFoundException(superSeriesId, this.Id);

        foreach (var member in members)
            member.LeaveSuperSeries();
    }

    private static void EnsureTrainerIsNotOwner(Id userId, Id? trainerId)
    {
        if (trainerId is not null && trainerId == userId)
            throw new TrainerCannotBeWorkoutOwnerException(userId);
    }

    /// <summary>
    /// A started or finished workout keeps its planning context, whoever asks.
    /// </summary>
    private void EnsurePlacementCanChange()
    {
        if (IsLocked)
            throw new WorkoutPlacementLockedException(this.Id, Status);
    }

    private static void EnsureDefined(WorkoutStatus status)
    {
        if (Enum.IsDefined(status) == false)
            throw new InvalidWorkoutStatusException((int)status);
    }

    private static void EnsureValidSuperSeries(Id workoutId, IReadOnlyList<WorkoutExercise> members)
    {
        if (members.Count < 2)
            throw new SuperSeriesRequiresTwoExercisesException(members.Count);

        if (members.Select(member => member.TotalSets).Distinct().Count() > 1)
            throw new SuperSeriesSetCountMismatchException(workoutId);
    }
}
