namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidWeightException : DomainException
{
    public InvalidWeightException(string reason)
        : base($"Invalid weight: {reason}")
    { }
}
