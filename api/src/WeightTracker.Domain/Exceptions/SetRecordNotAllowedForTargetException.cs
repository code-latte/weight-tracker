using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class SetRecordNotAllowedForTargetException : DomainException
{
    public SetRecordNotAllowedForTargetException(Id exerciseId)
        : base($"Exercise '{exerciseId}' must keep only rep-based sets to hold set records")
    { }
}
