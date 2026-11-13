using Microsoft.EntityFrameworkCore;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts.Observations;
using WeightTracker.Infra.Mappers;
using WeightTracker.Infra.Persistence;

namespace WeightTracker.Infra.Repositories;

public class WorkoutObservationsRepository : IWorkoutObservationsRepository
{
    private readonly WeightTrackerDbContext _context;

    public WorkoutObservationsRepository(WeightTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<WorkoutObservations?> GetByWorkoutId(Id workoutId, CancellationToken cancellationToken)
    {
        var entities = await _context.WorkoutObservations
            .AsNoTracking()
            .Where(observation => observation.WorkoutId == workoutId.Value)
            .ToListAsync(cancellationToken);

        return entities.Count == 0 ? null : WorkoutObservationsMapper.MapToDomain(workoutId, entities);
    }

    /// <remarks>
    /// The observations of a workout are replaced as a whole, never edited: the stored rows are removed and the aggregate's
    /// rows added. An aggregate without observations stores nothing, so it ends up as no rows.
    /// </remarks>
    public async Task Save(WorkoutObservations observations, CancellationToken cancellationToken)
    {
        if (observations.IsNew == false)
        {
            var stored = await _context.WorkoutObservations
                .Where(observation => observation.WorkoutId == observations.WorkoutId.Value)
                .ToListAsync(cancellationToken);

            _context.WorkoutObservations.RemoveRange(stored);
        }

        await _context.WorkoutObservations.AddRangeAsync(WorkoutObservationsMapper.MapToEntities(observations), cancellationToken);
    }
}
