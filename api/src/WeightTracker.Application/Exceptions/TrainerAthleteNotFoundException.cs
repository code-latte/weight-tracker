using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when there is no active relationship between the given trainer and athlete.
/// </summary>
public sealed class TrainerAthleteNotFoundException : ApplicationLayerException
{
    public TrainerAthleteNotFoundException(Id trainerId, Id athleteId)
        : base($"No active relationship between trainer '{trainerId}' and athlete '{athleteId}' was found")
    { }
}
