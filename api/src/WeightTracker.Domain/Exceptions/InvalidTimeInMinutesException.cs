namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidTimeInMinutesException : DomainException
{
    public InvalidTimeInMinutesException(string reason)
        : base($"Invalid time in minutes: {reason}")
    { }
}
