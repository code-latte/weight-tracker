using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when a custom exercise is added to a workout whose editor of record is not the exercise's owner.
/// </summary>
public sealed class ExerciseNotUsableInWorkoutException : ApplicationLayerException
{
    public ExerciseNotUsableInWorkoutException(Id exerciseId, Id workoutId)
        : base($"Exercise '{exerciseId}' belongs to another user and cannot be added to workout '{workoutId}'")
    { }
}
