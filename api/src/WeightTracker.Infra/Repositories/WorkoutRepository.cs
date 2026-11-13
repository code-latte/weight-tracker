using Microsoft.EntityFrameworkCore;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Mappers;
using WeightTracker.Infra.Persistence;

namespace WeightTracker.Infra.Repositories;

public class WorkoutRepository : IWorkoutRepository
{
    private readonly WeightTrackerDbContext _context;

    /// <summary>
    /// Row version of each workout read through <see cref="GetById"/> in this scope. <see cref="Save"/> re-reads the row,
    /// so without this the concurrency check would only cover the gap between that re-read and the commit.
    /// </summary>
    private readonly Dictionary<Guid, uint> _loadedVersions = [];

    public WorkoutRepository(WeightTrackerDbContext context)
    {
        _context = context;
    }

    public Task<IEnumerable<Workout>> GetAll(CancellationToken cancellationToken)
        => Query(workouts => workouts, cancellationToken);

    public Task<IEnumerable<Workout>> GetAllByUserId(Id userId, CancellationToken cancellationToken)
        => Query(workouts => workouts.Where(workout => workout.UserId == userId.Value), cancellationToken);

    public Task<IEnumerable<Workout>> GetAllByUserOrTrainerId(Id id, CancellationToken cancellationToken)
        => Query(workouts => workouts.Where(workout =>
            workout.UserId == id.Value || workout.TrainerId == id.Value), cancellationToken);

    public Task<IEnumerable<Workout>> GetAllByMesocycleId(Id mesocycleId, CancellationToken cancellationToken)
        => Query(workouts => workouts.Where(workout => workout.MesocycleId == mesocycleId.Value), cancellationToken);

    public Task<bool> ExistsByMesocycleWeek(Id mesocycleId, WeekNumber weekNumber, CancellationToken cancellationToken)
        => _context.Workouts.AnyAsync(workout =>
            workout.MesocycleId == mesocycleId.Value && workout.MesocycleWeekNumber == weekNumber.Value, cancellationToken);

    public async Task<Workout?> GetById(Id workoutId, CancellationToken cancellationToken)
    {
        var entity = await WithAggregateTables(_context.Workouts.AsNoTracking())
            .FirstOrDefaultAsync(workout => workout.Id == workoutId.Value, cancellationToken);

        if (entity is null)
            return null;

        _loadedVersions[entity.Id] = entity.Version;

        return WorkoutMapper.MapToDomain(entity);
    }

    public async Task Save(Workout workout, CancellationToken cancellationToken)
    {
        var entity = WorkoutMapper.MapToEntity(workout);

        if (workout.IsNew)
        {
            await _context.Workouts.AddAsync(entity, cancellationToken);
            return;
        }

        var tracked = await WithAggregateTables(_context.Workouts)
            .FirstOrDefaultAsync(stored => stored.Id == entity.Id, cancellationToken);

        if (tracked is null)
        {
            await _context.Workouts.AddAsync(entity, cancellationToken);
            return;
        }

        tracked.UserId = entity.UserId;
        tracked.TrainerId = entity.TrainerId;
        tracked.WorkoutDate = entity.WorkoutDate;
        tracked.Name = entity.Name;
        tracked.Description = entity.Description;
        tracked.Status = entity.Status;
        tracked.MesocycleId = entity.MesocycleId;
        tracked.MesocycleWeekNumber = entity.MesocycleWeekNumber;
        tracked.ReviewedAtUtc = entity.ReviewedAtUtc;
        tracked.ReviewedBy = entity.ReviewedBy;
        tracked.ResultsUpdatedAtUtc = entity.ResultsUpdatedAtUtc;
        tracked.StartedAtUtc = entity.StartedAtUtc;
        tracked.CompletedAtUtc = entity.CompletedAtUtc;

        // Compare against the version the caller decided on, not the one just re-read
        if (_loadedVersions.TryGetValue(tracked.Id, out var loadedVersion))
            _context.Entry(tracked).Property(stored => stored.Version).OriginalValue = loadedVersion;

        Reconcile(tracked, entity);
    }

    public async Task Delete(Workout workout, CancellationToken cancellationToken)
    {
        var tracked = await WithAggregateTables(_context.Workouts)
            .FirstOrDefaultAsync(stored => stored.Id == workout.Id.Value, cancellationToken);

        if (tracked is null)
            return;

        // Compare against the version the caller decided on, not the one just re-read
        if (_loadedVersions.TryGetValue(tracked.Id, out var loadedVersion))
            _context.Entry(tracked).Property(stored => stored.Version).OriginalValue = loadedVersion;

        _context.Workouts.Remove(tracked);
    }

    /// <summary>
    /// Brings the persisted children of the aggregate in line with the incoming state.
    /// </summary>
    /// <remarks>
    /// The children are reconciled by the identity of the <c>WorkoutExercise</c> entity rather
    /// than replaced wholesale, so an entry that survives the update keeps its row. An entry
    /// removed and added again for the same exercise is a new entity: its old row is deleted
    /// and a new one inserted. <c>(workout_id, exercise_id)</c> is a unique index in the EF
    /// model, so EF orders that delete before the insert within the same commit.
    /// Removals are expressed by detaching from the navigation so EF cascades them as
    /// orphans, instead of mixing explicit deletes with collection reassignment.
    /// </remarks>
    private static void Reconcile(WorkoutEntity tracked, WorkoutEntity incoming)
    {
        var incomingById = incoming.WorkoutExercises.ToDictionary(item => item.Id);

        var dropped = tracked.WorkoutExercises
            .Where(item => incomingById.ContainsKey(item.Id) == false)
            .ToList();

        foreach (var item in dropped)
            tracked.WorkoutExercises.Remove(item);

        foreach (var incomingItem in incoming.WorkoutExercises)
        {
            var existing = tracked.WorkoutExercises
                .FirstOrDefault(item => item.Id == incomingItem.Id);

            if (existing is null)
            {
                tracked.WorkoutExercises.Add(incomingItem);
                continue;
            }

            existing.Position = incomingItem.Position;
            existing.Effort = incomingItem.Effort;
            existing.PerSide = incomingItem.PerSide;
            existing.RestMinutes = incomingItem.RestMinutes;
            existing.SuperSeriesId = incomingItem.SuperSeriesId;

            // Sets carry no identity outside the aggregate, so they are always replaced
            existing.Sets.Clear();

            foreach (var set in incomingItem.Sets)
            {
                set.WorkoutExerciseId = existing.Id;
                existing.Sets.Add(set);
            }

            ReconcileSetRecords(existing, incomingItem);
        }
    }

    /// <summary>
    /// Set records have identity, so unlike the sets they are matched by id: new ones are inserted, the ones no
    /// longer in the aggregate are cascaded as orphans, and the ones that survive take the reps, weight and
    /// position of the aggregate, because they can be corrected and reordered. Their registration time never changes.
    /// </summary>
    private static void ReconcileSetRecords(WorkoutExerciseEntity tracked, WorkoutExerciseEntity incoming)
    {
        var incomingById = incoming.SetRecords.ToDictionary(setRecord => setRecord.Id);

        tracked.SetRecords.RemoveAll(setRecord => incomingById.ContainsKey(setRecord.Id) == false);

        var trackedById = tracked.SetRecords.ToDictionary(setRecord => setRecord.Id);

        foreach (var setRecord in incoming.SetRecords)
        {
            if (trackedById.TryGetValue(setRecord.Id, out var existing))
            {
                existing.Position = setRecord.Position;
                existing.Reps = setRecord.Reps;
                existing.DurationSeconds = setRecord.DurationSeconds;
                existing.Weight = setRecord.Weight;
                continue;
            }

            setRecord.WorkoutExerciseId = tracked.Id;
            tracked.SetRecords.Add(setRecord);
        }
    }

    private async Task<IEnumerable<Workout>> Query(
        Func<IQueryable<WorkoutEntity>, IQueryable<WorkoutEntity>> filter,
        CancellationToken cancellationToken)
    {
        var entities = await WithAggregateTables(filter(_context.Workouts.AsNoTracking()))
            .OrderBy(workout => workout.WorkoutDate)
            .ToListAsync(cancellationToken);

        return entities.Select(WorkoutMapper.MapToDomain).ToList();
    }

    /// <remarks>
    /// Sets and set records are sibling collections of each exercise. Loaded in one query they would
    /// multiply into one row per set prescription and set record, so each collection gets its own query.
    /// </remarks>
    private static IQueryable<WorkoutEntity> WithAggregateTables(IQueryable<WorkoutEntity> workouts)
        => workouts
            .Include(workout => workout.WorkoutExercises)
            .ThenInclude(workoutExercise => workoutExercise.Sets)
            .Include(workout => workout.WorkoutExercises)
            .ThenInclude(workoutExercise => workoutExercise.SetRecords)
            .AsSplitQuery();
}
