namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when the trainer or athlete of a new mesocycle, as requested, is not allowed for the caller's role,
/// or cannot take part in a mesocycle.
/// </summary>
public sealed class InvalidMesocycleAssignmentException : ApplicationLayerException
{
    public InvalidMesocycleAssignmentException(string reason) : base(reason)
    { }
}
