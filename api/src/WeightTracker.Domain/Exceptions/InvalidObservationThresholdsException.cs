namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidObservationThresholdsException : DomainException
{
    public InvalidObservationThresholdsException(string reason)
        : base($"Invalid observation thresholds: {reason}")
    { }
}
