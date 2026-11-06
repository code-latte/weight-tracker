using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

/// <summary>
/// Raised when an operation needs the trainer of a workout and the workout has none, such as saving it as a template.
/// </summary>
public sealed class WorkoutWithoutTrainerException : DomainException
{
    public WorkoutWithoutTrainerException(Id workoutId)
        : base($"Workout '{workoutId}' has no trainer")
    { }
}
