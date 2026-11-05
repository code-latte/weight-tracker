using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class MesocycleTrainerIsAthleteException : DomainException
{
    public MesocycleTrainerIsAthleteException(Id userId)
        : base($"User '{userId}' cannot be both the trainer and the athlete of a mesocycle")
    { }
}
