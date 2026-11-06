using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when a mesocycle week would be removed while workouts are still placed in it.
/// </summary>
public sealed class MesocycleWeekHasWorkoutsException : ApplicationLayerException
{
    public MesocycleWeekHasWorkoutsException(WeekNumber weekNumber, Id mesocycleId)
        : base($"Week {weekNumber.Value} of mesocycle '{mesocycleId}' cannot be removed while workouts are placed in it")
    { }
}
