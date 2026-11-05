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

    public async Task<Workout?> GetById(Id workoutId, CancellationToken cancellationToken)
    {
        var entity = await WithAggregateTables(_context.Workouts.AsNoTracking())
            .FirstOrDefaultAsync(workout => workout.Id == workoutId.Value, cancellationToken);

        if (entity is null)
            return null;

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

        Reconcile(tracked, entity);
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

    private static IQueryable<WorkoutEntity> WithAggregateTables(IQueryable<WorkoutEntity> workouts)
        => workouts
            .Include(workout => workout.WorkoutExercises)
            .ThenInclude(workoutExercise => workoutExercise.Sets);
}
