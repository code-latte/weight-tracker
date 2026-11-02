using WeightTracker.Domain.Common;
using WeightTracker.Domain.TrainerAthletes;

namespace WeightTracker.Application.Interfaces;

public interface ITrainerAthleteRepository
{

    /// <summary>
    /// The active relationship between this trainer and athlete, if any. Ended ones are ignored.
    /// </summary>
    public Task<TrainerAthlete?> GetActiveByTrainerIdAndUserId(Id trainerId, Id userId, CancellationToken cancellationToken);

    /// <summary>
    /// Whether the athlete has an active relationship with any trainer.
    /// </summary>
    public Task<bool> ExistsActiveByUserId(Id userId, CancellationToken cancellationToken);

    /// <summary>
    /// Every active relationship of the trainer, in no particular order.
    /// </summary>
    public Task<IEnumerable<TrainerAthlete>> GetAllActiveByTrainerId(Id trainerId, CancellationToken cancellationToken);

    /// <summary>
    /// Every active relationship of the given athletes, in no particular order. Ended ones are ignored.
    /// </summary>
    public Task<IEnumerable<TrainerAthlete>> GetAllActiveByUserIds(IReadOnlyCollection<Id> userIds, CancellationToken cancellationToken);

    public Task Save(TrainerAthlete relationship, CancellationToken cancellationToken);

}
