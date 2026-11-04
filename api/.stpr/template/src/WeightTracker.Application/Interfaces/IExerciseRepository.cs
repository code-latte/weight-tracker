using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;

namespace WeightTracker.Application.Interfaces;

/// <summary>
/// Loads and saves the Exercise aggregate: the system catalog and the custom exercises owned by users.
/// </summary>
public interface IExerciseRepository
{

    public Task<IEnumerable<Exercise>> GetAll(CancellationToken cancellationToken);

    /// <summary>
    /// Exercises with the given identifiers. Unknown ids are ignored; empty input returns empty.
    /// </summary>
    public Task<IEnumerable<Exercise>> GetAllByIds(IEnumerable<Id> exerciseIds, CancellationToken cancellationToken);

    /// <summary>
    /// Every system exercise, plus the exercises owned by the given user when one is given. Archived ones are
    /// included. Order is not guaranteed.
    /// </summary>
    public Task<IEnumerable<Exercise>> GetSystemAndOwnedBy(Id? ownerId, CancellationToken cancellationToken);

    public Task<Exercise?> GetById(Id exerciseId, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts a new exercise or updates a stored one, with its translations. It does not commit; the use case
    /// calls the unit of work.
    /// </summary>
    public Task Save(Exercise exercise, CancellationToken cancellationToken);

}
