using WeightTracker.Application.UseCases.Users;
using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Interfaces;

/// <summary>
/// Read projection over the Workout tables: what a user did the last time they finished a workout with an exercise.
/// It never loads or stages an aggregate, so it is not a repository and is never used before a write.
/// </summary>
public interface IExerciseLastPerformanceQuery
{
    /// <summary>
    /// The sets the user recorded for the exercise in their latest finished workout that holds any, with that workout's
    /// date and the effort it prescribed, or <c>null</c> when there is none. Only <c>COMPLETED</c> and <c>REVIEWED</c>
    /// workouts count. The latest is the one with the latest workout date, ties broken by the newest workout.
    /// </summary>
    public Task<ExerciseLastPerformanceView?> Get(Id userId, Id exerciseId, CancellationToken cancellationToken);
}
