using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Exceptions;

public sealed class TrainerInvitationNotFoundException : ApplicationLayerException
{
    public TrainerInvitationNotFoundException(Id invitationId)
        : base($"Trainer invitation '{invitationId}' was not found")
    { }
}
