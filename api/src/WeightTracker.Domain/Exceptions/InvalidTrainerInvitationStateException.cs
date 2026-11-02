using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

/// <summary>
/// Raised when a trainer invitation would be created or restored in a state that breaks its own rules.
/// </summary>
public sealed class InvalidTrainerInvitationStateException : DomainException
{
    public InvalidTrainerInvitationStateException(Id invitationId, string reason)
        : base($"Trainer invitation '{invitationId}' is not valid: {reason}")
    { }
}
