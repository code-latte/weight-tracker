using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when an athlete who already has an active trainer would be linked to, or invited by, a trainer.
/// </summary>
public sealed class AthleteAlreadyHasTrainerException : ApplicationLayerException
{
    public AthleteAlreadyHasTrainerException(Id athleteId)
        : base($"Athlete '{athleteId}' already has an active trainer")
    { }
}
