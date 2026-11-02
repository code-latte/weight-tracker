using WeightTracker.Domain.AthleteInvitations;

namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidTrainerInvitationStatusTransitionException : DomainException
{
    public InvalidTrainerInvitationStatusTransitionException(TrainerInvitationStatus from, TrainerInvitationStatus to)
        : base($"A trainer invitation cannot go from '{from}' to '{to}'")
    { }
}
