using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when a workout prescribed by one trainer would be placed in a mesocycle prepared by another.
/// </summary>
public sealed class MesocycleTrainerMismatchException : ApplicationLayerException
{
    public MesocycleTrainerMismatchException(Id trainerId, Id mesocycleId)
        : base($"A workout prescribed by '{trainerId}' cannot be placed in mesocycle '{mesocycleId}', which another trainer prepares")
    { }
}
