using WeightTracker.Domain.TrainerAthletes;

namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidTrainerAthleteStatusTransitionException : DomainException
{
    public InvalidTrainerAthleteStatusTransitionException(TrainerAthleteStatus from, TrainerAthleteStatus to)
        : base($"A trainer–athlete relationship cannot go from '{from}' to '{to}'")
    { }
}
