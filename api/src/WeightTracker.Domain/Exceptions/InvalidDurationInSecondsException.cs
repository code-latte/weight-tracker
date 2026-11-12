namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidDurationInSecondsException : DomainException
{
    public InvalidDurationInSecondsException(string reason)
        : base($"Invalid duration in seconds: {reason}")
    { }
}
