namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when a user cannot take the trainer or athlete side of a relationship or invitation: wrong role or
/// deactivated.
/// </summary>
public sealed class InvalidTrainerAthleteRoleException : ApplicationLayerException
{
    public InvalidTrainerAthleteRoleException(string reason) : base(reason)
    { }
}
