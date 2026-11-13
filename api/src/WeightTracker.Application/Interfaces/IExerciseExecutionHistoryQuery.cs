using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Interfaces;

/// <summary>
/// Read projection over the Workout tables: the earlier sessions of several exercises, with their prescriptions and
/// recorded sets, plus the records over every comparable session. It never loads or stages an aggregate, so it is not a
/// repository. It feeds the generation of a new aggregate and is never used to change a workout.
/// </summary>
public interface IExerciseExecutionHistoryQuery
{
    /// <summary>
    /// For each exercise, up to <paramref name="depth"/> earlier sessions, newest first, that are <c>COMPLETED</c> or
    /// <c>REVIEWED</c>, hold at least one set record for it, belong to the owner, were prescribed by the trainer and come
    /// strictly before the anchor workout by date, ties broken by workout id. It also returns the records, which look at
    /// every such session and leave out the ones in light mesocycle weeks. The anchor workout itself never counts.
    /// </summary>
    /// <param name="ownerId">Athlete whose workouts are read; workouts of anyone else never count.</param>
    /// <param name="trainerId">Only workouts prescribed by this trainer count.</param>
    /// <param name="anchorWorkoutId">The workout being completed. Its week type is returned and its identifier breaks a tie on the date.</param>
    /// <param name="anchorDate">Workout date of the anchor workout.</param>
    /// <param name="exerciseIds">Exercises to look up.</param>
    /// <param name="depth">How many sessions to load per exercise.</param>
    public Task<ExecutionHistoryView> Get(
        Id ownerId,
        Id trainerId,
        Id anchorWorkoutId,
        DateOnly anchorDate,
        IReadOnlyCollection<Id> exerciseIds,
        int depth,
        CancellationToken cancellationToken);
}
