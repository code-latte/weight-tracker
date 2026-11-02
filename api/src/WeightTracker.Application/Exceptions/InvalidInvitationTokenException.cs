namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when the token sent to answer an invitation does not match it. The message does not say why, so the
/// response does not reveal anything about the invitation.
/// </summary>
public sealed class InvalidInvitationTokenException : ApplicationLayerException
{
    public InvalidInvitationTokenException()
        : base("The invitation token is not valid")
    { }
}
