namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidDurationInWeeksException : DomainException
{
    public InvalidDurationInWeeksException(int value, int min, int max)
        : base($"A mesocycle must last between {min} and {max} weeks, got {value}")
    { }
}
