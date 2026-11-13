using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts.Observations;

namespace WeightTracker.Application.Interfaces;

/// <summary>
/// Loads and saves the WorkoutObservations aggregate. Saving does not commit; the caller calls the unit of work.
/// </summary>
public interface IWorkoutObservationsRepository
{
    /// <summary>
    /// The observations stored for a workout, null when none were generated.
    /// </summary>
    public Task<WorkoutObservations?> GetByWorkoutId(Id workoutId, CancellationToken cancellationToken);

    /// <summary>
    /// Stages a new aggregate, or replaces every stored observation of the workout with the aggregate's.
    /// </summary>
    public Task Save(WorkoutObservations observations, CancellationToken cancellationToken);
}
