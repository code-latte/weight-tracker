using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class TrainerInvitationExpiredException : DomainException
{
    public TrainerInvitationExpiredException(Id invitationId)
        : base($"Trainer invitation '{invitationId}' has expired")
    { }
}
