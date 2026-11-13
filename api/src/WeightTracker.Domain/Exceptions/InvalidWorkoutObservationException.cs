namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidWorkoutObservationException : DomainException
{
    public InvalidWorkoutObservationException(string reason)
        : base($"Invalid workout observation: {reason}")
    { }
}
