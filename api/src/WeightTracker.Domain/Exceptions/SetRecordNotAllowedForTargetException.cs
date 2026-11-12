using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class SetRecordNotAllowedForTargetException : DomainException
{
    public SetRecordNotAllowedForTargetException(Id exerciseId)
        : base($"Exercise '{exerciseId}' must be prescribed only by reps or only by time, and its set records must be of that same kind")
    { }
}
