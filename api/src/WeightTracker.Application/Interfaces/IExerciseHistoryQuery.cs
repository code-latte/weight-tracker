using WeightTracker.Application.UseCases.Users;
using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Interfaces;

/// <summary>
/// Read projection over the Workout tables: the latest finished sessions in which a user did an exercise. It never
/// loads or stages an aggregate, so it is not a repository and is never used before a write.
/// </summary>
public interface IExerciseHistoryQuery
{
    /// <summary>
    /// The user's latest finished workouts that hold set records for the exercise, newest first, each with the sets
    /// recorded for the exercise in it, in order. Only <c>COMPLETED</c> and <c>REVIEWED</c> workouts count. They are
    /// ordered by workout date then by workout id, both descending, and at most <paramref name="limit"/> are returned.
    /// </summary>
    /// <param name="userId">Owner of the workouts to read; workouts of anyone else never count.</param>
    /// <param name="exerciseId">Exercise whose sessions are read.</param>
    /// <param name="prescribedBy">When set, only workouts prescribed by that trainer count; <c>null</c> places no limit.</param>
    /// <param name="limit">Maximum number of sessions, already validated by the caller.</param>
    public Task<IReadOnlyList<ExerciseSessionView>> Get(Id userId, Id exerciseId, Id? prescribedBy, int limit, CancellationToken cancellationToken);
}
