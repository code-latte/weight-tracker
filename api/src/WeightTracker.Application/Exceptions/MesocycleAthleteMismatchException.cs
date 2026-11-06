using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when a workout would be placed in a mesocycle planned for another athlete.
/// </summary>
public sealed class MesocycleAthleteMismatchException : ApplicationLayerException
{
    public MesocycleAthleteMismatchException(Id ownerId, Id mesocycleId)
        : base($"A workout owned by '{ownerId}' cannot be placed in mesocycle '{mesocycleId}', which is for another athlete")
    { }
}
