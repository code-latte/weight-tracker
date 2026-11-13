using WeightTracker.Application.UseCases.Workouts;
using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Interfaces;

/// <summary>
/// Read projection over the Workout tables: the previous time a user did each of several exercises, before a given
/// workout. It never loads or stages an aggregate, so it is not a repository and is never used before a write.
/// </summary>
public interface IExercisePreviousExecutionQuery
{
    /// <summary>
    /// For each exercise, the sets the owner recorded in their latest finished workout that holds any and comes strictly
    /// before the anchor workout, ordered by workout date then by workout id. Only <c>COMPLETED</c> and <c>REVIEWED</c>
    /// workouts count. Exercises with no such workout are left out of the result.
    /// </summary>
    /// <param name="ownerId">Owner of the workouts to read; workouts of anyone else never count.</param>
    /// <param name="exerciseIds">Exercises to look up.</param>
    /// <param name="beforeDate">Workout date of the anchor workout.</param>
    /// <param name="beforeWorkoutId">Identifier of the anchor workout, which breaks a tie on the date.</param>
    /// <param name="prescribedBy">When set, only workouts prescribed by that trainer count; <c>null</c> places no limit.</param>
    public Task<IReadOnlyList<ExercisePreviousExecutionView>> Get(
        Id ownerId,
        IReadOnlyCollection<Id> exerciseIds,
        DateOnly beforeDate,
        Id beforeWorkoutId,
        Id? prescribedBy,
        CancellationToken cancellationToken);
}
