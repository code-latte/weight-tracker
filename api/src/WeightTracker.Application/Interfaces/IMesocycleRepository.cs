using WeightTracker.Domain.Common;
using WeightTracker.Domain.Mesocycles;

namespace WeightTracker.Application.Interfaces;

public interface IMesocycleRepository
{
    /// <summary>
    /// The mesocycle with its weeks, or <c>null</c> when none has that id.
    /// </summary>
    public Task<Mesocycle?> GetById(Id mesocycleId, CancellationToken cancellationToken);

    public Task Save(Mesocycle mesocycle, CancellationToken cancellationToken);
}
