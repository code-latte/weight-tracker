using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when a trainer invites an email they already have a pending, unexpired invitation for.
/// </summary>
public sealed class PendingInvitationAlreadyExistsException : ApplicationLayerException
{
    public PendingInvitationAlreadyExistsException(Email inviteEmail)
        : base($"There is already a pending invitation for '{inviteEmail}'")
    { }
}
