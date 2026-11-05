using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class DuplicateMesocycleWeekException : DomainException
{
    public DuplicateMesocycleWeekException(WeekNumber weekNumber, Id mesocycleId)
        : base($"Week {weekNumber} is defined more than once in mesocycle '{mesocycleId}'")
    { }
}
