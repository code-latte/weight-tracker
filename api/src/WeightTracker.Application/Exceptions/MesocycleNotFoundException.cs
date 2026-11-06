using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Exceptions;

public sealed class MesocycleNotFoundException : ApplicationLayerException
{
    public MesocycleNotFoundException(Id mesocycleId) : base($"Mesocycle '{mesocycleId}' was not found")
    { }
}
