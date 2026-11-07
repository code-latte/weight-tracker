using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class SetRecordOrderMismatchException : DomainException
{
    public SetRecordOrderMismatchException(Id exerciseId)
        : base($"The new order must list every set record of exercise '{exerciseId}' exactly once")
    { }
}
