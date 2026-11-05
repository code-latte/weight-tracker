using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class MesocycleWeekNotFoundException : DomainException
{
    public MesocycleWeekNotFoundException(WeekNumber weekNumber, Id mesocycleId)
        : base($"Week {weekNumber} is not defined in mesocycle '{mesocycleId}'")
    { }
}
