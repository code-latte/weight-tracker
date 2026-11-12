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
    /// Only a draft accepts changes to its content. A published workout must go back to draft first.
    /// </summary>
    public bool IsLocked => Status != WorkoutStatus.DRAFT;

    /// <summary>
    /// A started, completed or reviewed workout has been executed, so its planning context is kept.
    /// </summary>
    public bool IsExecuted => Status is WorkoutStatus.STARTED or WorkoutStatus.COMPLETED or WorkoutStatus.REVIEWED;

    /// <summary>
    /// Last time the workout reached completed, in UTC, <c>null</c> until it is first completed.
    /// Leaving completed keeps it, so the status tells whether the workout is completed now.
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// When the workout was reviewed, in UTC, <c>null</c> until it is reviewed.
    /// </summary>
    public DateTime? ReviewedAt { get; private set; }

    /// <summary>
    /// User who reviewed the workout, <c>null</c> until it is reviewed.
    /// </summary>
    public Id? ReviewedBy { get; private set; }

    /// <summary>
    /// Last time the set records of the workout changed, in UTC, <c>null</c> while they never changed.
    /// </summary>
    public DateTime? ResultsUpdatedAt { get; private set; }

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

    /// <summary>
    /// Creates a draft workout for a user by copying the content of a template: name, description and every
    /// exercise in order, with its effort, sets, per-side flag, rest and superseries. The template's trainer
    /// prescribes the workout. Copied entries get new identifiers, so the workout never shares an entry with the template.
    /// </summary>
    /// <exception cref="TrainerCannotBeWorkoutOwnerException">When the template's trainer is the owner.</exception>
    public static Workout CreateFromTemplate(WorkoutTemplate template, Id userId, DateTime workoutDate)
    {
        var workout = Create(userId, template.TrainerId, template.Name, template.Description, workoutDate);

        foreach (var source in template.Exercises)
        {
            var copy = WorkoutExercise.Create(source.ExerciseId, source.Effort, source.Sets, source.PerSide, source.Rest);

            // Superseries ids are unique within the template and the workout starts empty, so they are kept as they are
            if (source.SuperSeriesId is not null)
                copy.JoinSuperSeries(source.SuperSeriesId);

            workout._exercises.Add(copy);
        }

        return workout;
    }

    /// <exception cref="InvalidWorkoutStatusException">When the status is not a defined value.</exception>
    /// <exception cref="ExerciseAlreadyInWorkoutException">When two entries share the same exercise.</exception>
    /// <exception cref="SuperSeriesRequiresTwoExercisesException">When a stored superseries has a single member.</exception>
    /// <exception cref="SuperSeriesSetCountMismatchException">When the members of a stored superseries have different set totals.</exception>
    /// <exception cref="IncompleteWorkoutPlacementException">When only one of the mesocycle id and week number is given.</exception>
    /// <exception cref="InvalidWorkoutStatusException">When the review metadata does not match the status.</exception>
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
        WeekNumber? mesocycleWeekNumber = null,
        DateTime? reviewedAt = null,
        Id? reviewedBy = null,
        DateTime? resultsUpdatedAt = null,
        DateTime? completedAt = null
    )
    {
        EnsureDefined(status);

        // A reviewed workout always carries who reviewed it and when, and nothing else does
        var hasReview = reviewedAt is not null && reviewedBy is not null;
        var hasPartialReview = (reviewedAt is null) != (reviewedBy is null);

        if (hasPartialReview || hasReview != (status == WorkoutStatus.REVIEWED))
            throw new InvalidWorkoutStatusException((int)status);

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
            ReviewedAt = reviewedAt,
            ReviewedBy = reviewedBy,
            ResultsUpdatedAt = resultsUpdatedAt,
            CompletedAt = completedAt,
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
    /// Confirms the workout may be deleted. Only a draft can be, whoever asks, an admin included,
    /// so nothing published, executed or reviewed is ever lost.
    /// </summary>
    /// <exception cref="WorkoutDeletionLockedException">When the workout is no longer a draft.</exception>
    public void EnsureCanBeDeleted()
    {
        if (Status != WorkoutStatus.DRAFT)
            throw new WorkoutDeletionLockedException(this.Id, Status);
    }

    /// <summary>
    /// Places the workout in a week of a mesocycle, or moves it there when it is already placed.
    /// Whether the mesocycle exists and the week fits it is checked by the caller, which knows both aggregates.
    /// </summary>
    /// <exception cref="WorkoutPlacementLockedException">When the workout is started, completed or reviewed.</exception>
    public void LinkToMesocycle(Id mesocycleId, WeekNumber weekNumber)
    {
        EnsurePlacementCanChange();

        MesocycleId = mesocycleId;
        MesocycleWeekNumber = weekNumber;
    }

    /// <summary>
    /// Takes the workout out of its mesocycle, leaving it standalone.
    /// </summary>
    /// <exception cref="WorkoutPlacementLockedException">When the workout is started, completed or reviewed.</exception>
    public void UnlinkFromMesocycle()
    {
        EnsurePlacementCanChange();

        MesocycleId = null;
        MesocycleWeekNumber = null;
    }

    /// <summary>
    /// Moves the workout along its lifecycle: draft to published, published back to draft or on to started,
    /// and started to completed. Anything else, including staying in the same status, is refused.
    /// A completed workout becomes reviewed only through <see cref="MarkReviewed"/>.
    /// </summary>
    /// <param name="target">Status to move to</param>
    /// <param name="changedAtUtc">When the change happens, in UTC. Recorded as <see cref="CompletedAt"/> when completing.</param>
    /// <exception cref="InvalidWorkoutStatusException">When the target is not a defined value.</exception>
    /// <exception cref="InvalidWorkoutStatusTransitionException">When the move is not part of the lifecycle.</exception>
    public void ChangeStatus(WorkoutStatus target, DateTime changedAtUtc)
    {
        EnsureDefined(target);

        if (CanTransitionTo(target) == false)
            throw new InvalidWorkoutStatusTransitionException(Status, target);

        SetStatus(target, changedAtUtc);
    }

    /// <summary>
    /// Whether moving from the current status to the target is part of the lifecycle. Reviewing is not, because
    /// it records who reviewed and when; see <see cref="MarkReviewed"/>.
    /// </summary>
    public bool CanTransitionTo(WorkoutStatus target)
        => (Status, target) switch
        {
            (WorkoutStatus.DRAFT, WorkoutStatus.PUBLISHED) => true,
            (WorkoutStatus.PUBLISHED, WorkoutStatus.DRAFT) => true,
            (WorkoutStatus.PUBLISHED, WorkoutStatus.STARTED) => true,
            (WorkoutStatus.STARTED, WorkoutStatus.COMPLETED) => true,
            _ => false,
        };

    /// <summary>
    /// Sets the status skipping the lifecycle. Reserved for callers that are entitled to correct a workout.
    /// A review is never set or undone this way: reviewing goes through <see cref="MarkReviewed"/>, and
    /// unlocking a reviewed workout needs its own audited operation.
    /// </summary>
    /// <param name="target">Status to set</param>
    /// <param name="changedAtUtc">When the change happens, in UTC. Recorded as <see cref="CompletedAt"/> when completing.</param>
    /// <exception cref="InvalidWorkoutStatusException">When the target is not a defined value.</exception>
    /// <exception cref="InvalidWorkoutStatusTransitionException">When the target or the current status is reviewed.</exception>
    public void OverrideStatus(WorkoutStatus target, DateTime changedAtUtc)
    {
        EnsureDefined(target);

        if (target == WorkoutStatus.REVIEWED || Status == WorkoutStatus.REVIEWED)
            throw new InvalidWorkoutStatusTransitionException(Status, target);

        SetStatus(target, changedAtUtc);
    }

    private void SetStatus(WorkoutStatus target, DateTime changedAtUtc)
    {
        Status = target;

        // Every completion overwrites the date; any other status keeps the last one
        if (target == WorkoutStatus.COMPLETED)
            CompletedAt = ToStoredPrecision(changedAtUtc);
    }

    /// <summary>
    /// Marks a completed workout as reviewed by its trainer, recording who reviewed it and when. The review is
    /// refused when the results changed after the reviewer read them, so nobody reviews outdated results.
    /// </summary>
    /// <param name="reviewerId">User reviewing the workout</param>
    /// <param name="reviewedAtUtc">When the review happens, in UTC</param>
    /// <param name="expectedResultsUpdatedAt">Last change of the results the reviewer read, <c>null</c> when there was none</param>
    /// <exception cref="WorkoutReviewNotAllowedException">When the workout is not completed or has no prescribing trainer.</exception>
    /// <exception cref="WorkoutResultsChangedException">When the results changed after the reviewer read them.</exception>
    public void MarkReviewed(Id reviewerId, DateTime reviewedAtUtc, DateTime? expectedResultsUpdatedAt)
    {
        if (Status != WorkoutStatus.COMPLETED)
            throw new WorkoutReviewNotAllowedException(this.Id, Status, $"only a {WorkoutStatus.COMPLETED} workout can be reviewed");

        // Without a trainer, completed is the final status
        if (TrainerId is null)
            throw new WorkoutReviewNotAllowedException(this.Id, Status, "it has no prescribing trainer");

        if (ToStoredPrecision(expectedResultsUpdatedAt) != ResultsUpdatedAt)
            throw new WorkoutResultsChangedException(this.Id);

        Status = WorkoutStatus.REVIEWED;
        ReviewedAt = ToStoredPrecision(reviewedAtUtc);
        ReviewedBy = reviewerId;
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
    /// <exception cref="SetRecordNotAllowedForTargetException">When the entry has set records and the new prescriptions are not of the same kind as the current ones.</exception>
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
    /// Records one set actually performed for an exercise: the reps done or the seconds held, and the weight when it is tracked.
    /// Without a result the set counts as done as prescribed, and its value is copied from the next prescribed set. Recording is
    /// not a content edit, so it is not affected by <see cref="IsLocked"/>; it is only allowed while the workout is started or completed.
    /// </summary>
    /// <param name="exerciseId">Catalog exercise of this workout the set was performed for</param>
    /// <param name="result">Reps or seconds performed, which may differ from the prescription. Null means done as prescribed</param>
    /// <param name="weight">Weight lifted, in kilograms. Null when it is not tracked</param>
    /// <param name="registeredAtUtc">When the set was registered, in UTC</param>
    /// <exception cref="WorkoutResultsLockedException">When the workout is not started or completed.</exception>
    /// <exception cref="ExerciseNotInWorkoutException">When the exercise is not in the workout.</exception>
    /// <exception cref="SetRecordNotAllowedForTargetException">When the exercise mixes rep-based and time-based prescriptions, or the result is not of the exercise's kind.</exception>
    /// <exception cref="SetResultRequiredException">When no result is given and the next prescribed set is a range or max reps, or there is none left.</exception>
    public void AddSetRecord(Id exerciseId, SetResult? result, Weight? weight, DateTime registeredAtUtc)
    {
        RecordingEntry(exerciseId).AddSetRecord(result, weight, registeredAtUtc);
        TouchResults(registeredAtUtc);
    }

    /// <summary>
    /// Corrects the result and weight of a set already recorded. Its identity, registration time and position are kept.
    /// Like recording, it is not a content edit and is only allowed while the workout is started or completed.
    /// </summary>
    /// <param name="exerciseId">Catalog exercise of this workout the set was recorded for</param>
    /// <param name="setRecordId">Set record to correct</param>
    /// <param name="result">Reps or seconds performed</param>
    /// <param name="weight">Weight lifted, in kilograms. Null clears it</param>
    /// <param name="updatedAtUtc">When the correction happens, in UTC</param>
    /// <exception cref="WorkoutResultsLockedException">When the workout is not started or completed.</exception>
    /// <exception cref="ExerciseNotInWorkoutException">When the exercise is not in the workout.</exception>
    /// <exception cref="SetRecordNotFoundException">When the set record is not recorded for that exercise.</exception>
    /// <exception cref="SetRecordNotAllowedForTargetException">When the exercise mixes rep-based and time-based prescriptions, or the result is not of the exercise's kind.</exception>
    public void UpdateSetRecord(Id exerciseId, Id setRecordId, SetResult result, Weight? weight, DateTime updatedAtUtc)
    {
        RecordingEntry(exerciseId).UpdateSetRecord(setRecordId, result, weight);
        TouchResults(updatedAtUtc);
    }

    /// <summary>
    /// Removes a set already recorded; the other set records of the exercise keep their order.
    /// Like recording, it is not a content edit and is only allowed while the workout is started or completed.
    /// </summary>
    /// <param name="updatedAtUtc">When the removal happens, in UTC</param>
    /// <exception cref="WorkoutResultsLockedException">When the workout is not started or completed.</exception>
    /// <exception cref="ExerciseNotInWorkoutException">When the exercise is not in the workout.</exception>
    /// <exception cref="SetRecordNotFoundException">When the set record is not recorded for that exercise.</exception>
    public void RemoveSetRecord(Id exerciseId, Id setRecordId, DateTime updatedAtUtc)
    {
        RecordingEntry(exerciseId).RemoveSetRecord(setRecordId);
        TouchResults(updatedAtUtc);
    }

    /// <summary>
    /// Puts the set records of one exercise in the given order. Ids, results, weights and registration times are kept.
    /// Like recording, it is not a content edit and is only allowed while the workout is started or completed.
    /// </summary>
    /// <param name="exerciseId">Catalog exercise of this workout whose set records are reordered</param>
    /// <param name="setRecordIds">Set records of that exercise, every one exactly once, in the new order</param>
    /// <param name="updatedAtUtc">When the reorder happens, in UTC</param>
    /// <exception cref="WorkoutResultsLockedException">When the workout is not started or completed.</exception>
    /// <exception cref="ExerciseNotInWorkoutException">When the exercise is not in the workout.</exception>
    /// <exception cref="SetRecordOrderMismatchException">When the ids are not exactly the set records of that exercise.</exception>
    public void ReorderSetRecords(Id exerciseId, IEnumerable<Id> setRecordIds, DateTime updatedAtUtc)
    {
        RecordingEntry(exerciseId).ReorderSetRecords(setRecordIds);
        TouchResults(updatedAtUtc);
    }

    /// <summary>
    /// Entry whose set records may change: the workout must be started or completed and hold the exercise.
    /// Completed lets the athlete correct the results; a reviewed workout keeps them as they were reviewed.
    /// </summary>
    private WorkoutExercise RecordingEntry(Id exerciseId)
    {
        if (Status is not (WorkoutStatus.STARTED or WorkoutStatus.COMPLETED))
            throw new WorkoutResultsLockedException(this.Id, Status);

        return _exercises.FirstOrDefault(item => item.ExerciseId == exerciseId)
            ?? throw new ExerciseNotInWorkoutException(exerciseId, this.Id);
    }

    /// <summary>
    /// Called only after a set record change succeeded, so a refused change keeps the previous time.
    /// </summary>
    private void TouchResults(DateTime changedAtUtc)
        => ResultsUpdatedAt = ToStoredPrecision(changedAtUtc);

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
    /// An executed workout keeps its planning context, whoever asks.
    /// </summary>
    private void EnsurePlacementCanChange()
    {
        if (IsExecuted)
            throw new WorkoutPlacementLockedException(this.Id, Status);
    }

    /// <summary>
    /// The database keeps timestamps to the microsecond. Truncating here makes a time read back from storage
    /// equal to the one held in memory, which the review relies on to detect changed results.
    /// </summary>
    private static DateTime ToStoredPrecision(DateTime value)
        => new(value.Ticks - value.Ticks % 10, DateTimeKind.Utc);

    private static DateTime? ToStoredPrecision(DateTime? value)
        => value is null ? null : ToStoredPrecision(value.Value);

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
