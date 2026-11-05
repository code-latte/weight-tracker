namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidMesocycleNameException : DomainException
{
    public InvalidMesocycleNameException(string message)
        : base(message)
    { }
}
