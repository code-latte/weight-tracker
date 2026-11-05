namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidWeekNumberException : DomainException
{
    public InvalidWeekNumberException(int value)
        : base($"A week number must be 1 or greater, got {value}")
    { }
}
