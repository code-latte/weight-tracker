namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when the trainer of a new workout template, as requested, is not allowed for the caller's role,
/// or cannot own a template.
/// </summary>
public sealed class InvalidWorkoutTemplateTrainerException : ApplicationLayerException
{
    public InvalidWorkoutTemplateTrainerException(string reason) : base(reason)
    { }
}
