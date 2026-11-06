using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.Interfaces;

/// <summary>
/// Loads, saves and removes the WorkoutTemplate aggregate. None of the writes commits; the use case calls the
/// unit of work.
/// </summary>
public interface IWorkoutTemplateRepository
{
    /// <summary>
    /// Every template, ordered by name and then by id.
    /// </summary>
    public Task<IEnumerable<WorkoutTemplate>> GetAll(CancellationToken cancellationToken);

    /// <summary>
    /// The templates owned by a trainer, ordered by name and then by id.
    /// </summary>
    public Task<IEnumerable<WorkoutTemplate>> GetAllByTrainerId(Id trainerId, CancellationToken cancellationToken);

    public Task<WorkoutTemplate?> GetById(Id templateId, CancellationToken cancellationToken);

    public Task Save(WorkoutTemplate template, CancellationToken cancellationToken);

    /// <summary>
    /// Stages the deletion of a template with all its content. Workouts created from it are not affected.
    /// </summary>
    public Task Remove(WorkoutTemplate template, CancellationToken cancellationToken);
}
