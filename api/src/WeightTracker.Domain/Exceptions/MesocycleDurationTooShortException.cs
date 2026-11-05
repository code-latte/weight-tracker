using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class MesocycleDurationTooShortException : DomainException
{
    public MesocycleDurationTooShortException(int durationInWeeks, WeekNumber highestDefinedWeek, Id mesocycleId)
        : base($"Mesocycle '{mesocycleId}' cannot last {durationInWeeks} weeks while week {highestDefinedWeek} is defined")
    { }
}
