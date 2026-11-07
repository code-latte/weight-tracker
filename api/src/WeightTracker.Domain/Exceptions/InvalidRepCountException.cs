namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidRepCountException : DomainException
{
    public InvalidRepCountException(string reason)
        : base($"Invalid rep count: {reason}")
    { }
}
