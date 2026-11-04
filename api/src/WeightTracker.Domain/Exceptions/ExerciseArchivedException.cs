using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class ExerciseArchivedException : DomainException
{
    public ExerciseArchivedException(Id exerciseId)
        : base($"Exercise '{exerciseId}' is archived")
    { }
}
