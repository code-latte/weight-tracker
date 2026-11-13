using WeightTracker.Application.UseCases.Users;
using WeightTracker.Domain.Common;

namespace WeightTracker.Application.UseCases.Workouts;

/// <summary>
/// The stats of one workout: the sets that met their target, the volume lifted and, per exercise in workout order,
/// the top set against the one of its previous execution. A volume is <c>null</c> when no set contributed to it.
/// </summary>
public sealed record WorkoutSummaryView(
    Id WorkoutId,
    SetsInTargetView SetsInTarget,
    decimal? Volume,
    IReadOnlyList<ExerciseSummaryView> Exercises);

/// <summary>
/// How many recorded sets met their target out of the ones that could be evaluated.
/// </summary>
public sealed record SetsInTargetView(int InTarget, int Evaluated);

/// <summary>
/// The stats of one exercise of a workout. <paramref name="TopSet"/> is <c>null</c> when it has no set records and
/// <paramref name="Previous"/> when the owner never did it in an earlier finished workout the caller may see.
/// </summary>
public sealed record ExerciseSummaryView(
    Id WorkoutExerciseId,
    Id ExerciseId,
    SetsInTargetView SetsInTarget,
    decimal? Volume,
    LastPerformanceSetView? TopSet,
    PreviousExecutionView? Previous);

/// <summary>
/// The previous execution of an exercise: the earlier finished workout, its date and its top set.
/// </summary>
public sealed record PreviousExecutionView(Id WorkoutId, DateOnly WorkoutDate, LastPerformanceSetView TopSet);
