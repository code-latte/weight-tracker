using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

/// <summary>
/// Raised when a trainer–athlete relationship would be restored with an end date that contradicts its status.
/// </summary>
public sealed class InvalidTrainerAthleteStateException : DomainException
{
    public InvalidTrainerAthleteStateException(Id relationshipId)
        : base($"Trainer–athlete relationship '{relationshipId}' must have an end date, not earlier than its start, exactly when it has ended")
    { }
}
