namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidTrainingWeekTypeException : DomainException
{
    public InvalidTrainingWeekTypeException(int value)
        : base($"'{value}' is not a known training week type")
    { }
}
