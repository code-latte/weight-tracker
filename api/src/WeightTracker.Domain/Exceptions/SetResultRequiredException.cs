using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class SetResultRequiredException : DomainException
{
    public SetResultRequiredException(Id exerciseId, string reason)
        : base($"A result is required to record a set of exercise '{exerciseId}': {reason}")
    { }
}
