using WeightTracker.Domain.Common;
using WeightTracker.Domain.Mesocycles;

namespace WeightTracker.Application.Interfaces;

public interface IMesocycleRepository
{
    /// <summary>
    /// The mesocycle with its weeks, or <c>null</c> when none has that id.
    /// </summary>
    public Task<Mesocycle?> GetById(Id mesocycleId, CancellationToken cancellationToken);

    /// <summary>
    /// Every mesocycle with its weeks, latest start date first.
    /// </summary>
    public Task<IEnumerable<Mesocycle>> GetAll(CancellationToken cancellationToken);

    /// <summary>
    /// The mesocycles the trainer prepares, with their weeks, latest start date first.
    /// </summary>
    public Task<IEnumerable<Mesocycle>> GetAllByTrainerId(Id trainerId, CancellationToken cancellationToken);

    /// <summary>
    /// The mesocycles planned for the athlete, with their weeks, latest start date first.
    /// </summary>
    public Task<IEnumerable<Mesocycle>> GetAllByAthleteId(Id athleteId, CancellationToken cancellationToken);

    /// <summary>
    /// The mesocycles this trainer prepares for this athlete, with their weeks, latest start date first.
    /// </summary>
    public Task<IEnumerable<Mesocycle>> GetAllByTrainerIdAndAthleteId(Id trainerId, Id athleteId, CancellationToken cancellationToken);

    public Task Save(Mesocycle mesocycle, CancellationToken cancellationToken);
}
