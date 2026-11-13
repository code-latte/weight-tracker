using Microsoft.EntityFrameworkCore;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Workouts;
using WeightTracker.Domain.Workouts.Observations;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Exceptions;
using WeightTracker.Infra.Mappers;
using WeightTracker.Infra.Persistence;

namespace WeightTracker.Infra.Queries;

public class ExerciseExecutionHistoryQuery : IExerciseExecutionHistoryQuery
{
    private readonly WeightTrackerDbContext _context;

    public ExerciseExecutionHistoryQuery(WeightTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<ExecutionHistoryView> Get(
        Id ownerId,
        Id trainerId,
        Id anchorWorkoutId,
        DateOnly anchorDate,
        IReadOnlyCollection<Id> exerciseIds,
        int depth,
        CancellationToken cancellationToken)
    {
        var anchor = anchorWorkoutId.Value;

        var weekType = await _context.Workouts.AsNoTracking()
            .Where(workout => workout.Id == anchor)
            .SelectMany(
                workout => _context.MesocycleWeeks.Where(week =>
                    week.MesocycleId == workout.MesocycleId && week.WeekNumber == workout.MesocycleWeekNumber),
                (workout, week) => (int?)week.TrainingWeekType)
            .FirstOrDefaultAsync(cancellationToken);

        var exercises = new Dictionary<Id, ExerciseHistory>();

        // One walk per exercise, newest first, so each plan follows IX_Workouts_UserId_WorkoutDate_Id and stops after depth sessions
        foreach (var exerciseId in exerciseIds.Distinct())
        {
            var history = await LoadHistory(ownerId, trainerId, anchor, anchorDate, exerciseId, depth, cancellationToken);

            if (history.Executions.Count > 0)
                exercises[exerciseId] = history;
        }

        return new ExecutionHistoryView(weekType is null ? null : MapWeekType(weekType.Value), exercises);
    }

    private async Task<ExerciseHistory> LoadHistory(
        Id ownerId, Id trainerId, Guid anchor, DateOnly anchorDate, Id exerciseId, int depth, CancellationToken cancellationToken)
    {
        var sessions = await LoadSessions(ownerId.Value, trainerId.Value, anchor, anchorDate, exerciseId.Value, depth, cancellationToken);
        var records = await LoadRecords(ownerId.Value, trainerId.Value, anchor, anchorDate, exerciseId.Value, cancellationToken);

        return new ExerciseHistory(sessions, records);
    }

    private IQueryable<WorkoutEntity> EarlierFinishedWorkouts(Guid owner, Guid trainer, Guid anchor, DateOnly anchorDate)
    {
        var finished = new[] { (int)WorkoutStatus.COMPLETED, (int)WorkoutStatus.REVIEWED };

        return _context.Workouts.AsNoTracking()
            .Where(workout => workout.UserId == owner && workout.TrainerId == trainer && finished.Contains(workout.Status))
            // The date bound is redundant with the next condition, but it lets the index range stop at the anchor date
            .Where(workout => workout.WorkoutDate <= anchorDate)
            .Where(workout => workout.WorkoutDate < anchorDate || workout.Id.CompareTo(anchor) < 0);
    }

    private async Task<IReadOnlyList<ExerciseExecution>> LoadSessions(
        Guid owner, Guid trainer, Guid anchor, DateOnly anchorDate, Guid exercise, int depth, CancellationToken cancellationToken)
    {
        var sessions = await EarlierFinishedWorkouts(owner, trainer, anchor, anchorDate)
            .OrderByDescending(workout => workout.WorkoutDate)
            .ThenByDescending(workout => workout.Id)
            .SelectMany(
                workout => workout.WorkoutExercises.Where(entry => entry.ExerciseId == exercise && entry.SetRecords.Any()),
                (workout, entry) => new
                {
                    WorkoutId = workout.Id,
                    workout.WorkoutDate,
                    workout.MesocycleId,
                    workout.MesocycleWeekNumber,
                    EntryId = entry.Id,
                    entry.PerSide,
                })
            .Take(depth)
            .ToListAsync(cancellationToken);

        if (sessions.Count == 0)
            return [];

        var entryIds = sessions.Select(session => session.EntryId).ToList();
        var mesocycleIds = sessions.Where(session => session.MesocycleId is not null).Select(session => session.MesocycleId!.Value).Distinct().ToList();

        var weekRows = await _context.MesocycleWeeks.AsNoTracking()
            .Where(week => mesocycleIds.Contains(week.MesocycleId))
            .Select(week => new { week.MesocycleId, week.WeekNumber, week.TrainingWeekType })
            .ToListAsync(cancellationToken);

        var weeks = weekRows.Select(week => (week.MesocycleId, week.WeekNumber, week.TrainingWeekType)).ToList();

        var prescriptions = await _context.WorkoutExerciseSets.AsNoTracking()
            .Where(set => entryIds.Contains(set.WorkoutExerciseId))
            .OrderBy(set => set.WorkoutExerciseId)
            .ThenBy(set => set.Position)
            .ToListAsync(cancellationToken);

        var records = await _context.SetRecords.AsNoTracking()
            .Where(setRecord => entryIds.Contains(setRecord.WorkoutExerciseId))
            .OrderBy(setRecord => setRecord.WorkoutExerciseId)
            .ThenBy(setRecord => setRecord.Position)
            .ToListAsync(cancellationToken);

        var prescriptionsByEntry = prescriptions.ToLookup(set => set.WorkoutExerciseId);
        var recordsByEntry = records.ToLookup(setRecord => setRecord.WorkoutExerciseId);

        try
        {
            return sessions
                .Select(session => new ExerciseExecution(
                    Id.From(session.WorkoutId),
                    session.WorkoutDate,
                    WeekTypeOf(weeks, session.MesocycleId, session.MesocycleWeekNumber),
                    session.PerSide,
                    prescriptionsByEntry[session.EntryId].SelectMany(ExpandTargets),
                    recordsByEntry[session.EntryId].Select(MapSet)))
                .ToList();
        }
        catch (DomainException exception)
        {
            throw new PersistenceMappingException("Stored workout history could not be restored", exception);
        }
    }

    // The best marks over every earlier session outside a light week, grouped in SQL: one row per kind, per-side setting and
    // weight used, so the result stays small however long the history is
    private async Task<IReadOnlyList<ExerciseRecord>> LoadRecords(
        Guid owner, Guid trainer, Guid anchor, DateOnly anchorDate, Guid exercise, CancellationToken cancellationToken)
    {
        var rows = await (
            from workout in EarlierFinishedWorkouts(owner, trainer, anchor, anchorDate)
            from week in _context.MesocycleWeeks
                .Where(week => week.MesocycleId == workout.MesocycleId && week.WeekNumber == workout.MesocycleWeekNumber)
                .DefaultIfEmpty()
            where week == null
                || (week.TrainingWeekType != (int)TrainingWeekType.DELOAD && week.TrainingWeekType != (int)TrainingWeekType.RECOVERY)
            from entry in workout.WorkoutExercises.Where(entry => entry.ExerciseId == exercise)
            from setRecord in entry.SetRecords
            group setRecord by new { entry.PerSide, IsReps = setRecord.Reps != null, setRecord.Weight } into grouped
            select new { grouped.Key.PerSide, grouped.Key.IsReps, grouped.Key.Weight, MaxReps = grouped.Max(setRecord => setRecord.Reps) })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => (row.PerSide, row.IsReps))
            .Select(group => new ExerciseRecord(
                group.Key.PerSide,
                group.Key.IsReps ? ResultKind.REPS : ResultKind.DURATION,
                group.Max(row => row.Weight),
                group
                    .Where(row => row.IsReps && row.Weight is not null && row.MaxReps is not null)
                    .Select(row => new RepsAtWeight(row.Weight!.Value, row.MaxReps!.Value))
                    .ToList()))
            .ToList();
    }

    private static IEnumerable<SetTarget> ExpandTargets(WorkoutExerciseSetEntity set)
    {
        var target = WorkoutMapper.MapTargetToDomain(set.TargetType, set.TargetValue, set.TargetMaxValue, set.TargetMinutes);

        return Enumerable.Repeat(target, set.Count);
    }

    private static ExecutionSet MapSet(SetRecordEntity setRecord)
    {
        SetResult result = setRecord.Reps is not null
            ? new SetResult.Reps(RepCount.From(setRecord.Reps.Value))
            : new SetResult.Duration(DurationInSeconds.From(
                setRecord.DurationSeconds ?? throw new PersistenceMappingException($"set record '{setRecord.Id}' has neither reps nor duration")));

        return new ExecutionSet(result, setRecord.Weight is null ? null : Weight.From(setRecord.Weight.Value));
    }

    private static TrainingWeekType? WeekTypeOf(
        IReadOnlyList<(Guid MesocycleId, int WeekNumber, int TrainingWeekType)> weeks, Guid? mesocycleId, int? weekNumber)
    {
        if (mesocycleId is null || weekNumber is null)
            return null;

        var match = weeks.Where(week => week.MesocycleId == mesocycleId && week.WeekNumber == weekNumber).ToList();

        return match.Count == 0 ? null : MapWeekType(match[0].TrainingWeekType);
    }

    private static TrainingWeekType MapWeekType(int weekType)
    {
        if (Enum.IsDefined(typeof(TrainingWeekType), weekType) == false)
            throw new PersistenceMappingException($"'{weekType}' is not a known training week type");

        return (TrainingWeekType)weekType;
    }
}
