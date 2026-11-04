using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class SystemExerciseNotEditableException : DomainException
{
    public SystemExerciseNotEditableException(Id exerciseId)
        : base($"Exercise '{exerciseId}' belongs to the system catalog and cannot be changed")
    { }
}
