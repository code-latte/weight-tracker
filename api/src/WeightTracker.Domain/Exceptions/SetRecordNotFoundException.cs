using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class SetRecordNotFoundException : DomainException
{
    public SetRecordNotFoundException(Id setRecordId, Id exerciseId)
        : base($"Set record '{setRecordId}' is not recorded for exercise '{exerciseId}'")
    { }
}
