namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidMesocycleObjectiveException : DomainException
{
    public InvalidMesocycleObjectiveException(string message)
        : base(message)
    { }
}
