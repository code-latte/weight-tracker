using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class MesocycleWeekOutOfRangeException : DomainException
{
    public MesocycleWeekOutOfRangeException(WeekNumber weekNumber, int durationInWeeks, Id mesocycleId)
        : base($"Week {weekNumber} is beyond the {durationInWeeks} weeks of mesocycle '{mesocycleId}'")
    { }
}
