using WeightTracker.Api.Endpoints.Users.Dtos;
using WeightTracker.Application.UseCases.Workouts;

namespace WeightTracker.Api.Endpoints.Workouts.Dtos;

/// <param name="WorkoutId">Identifier of the summarised workout.</param>
/// <param name="SetsInTarget">Sets of the whole workout that met their target.</param>
/// <param name="Volume">Sum of reps × kilograms over the whole workout. Null when no set has both reps and a weight.</param>
/// <param name="Exercises">One entry per exercise of the workout, in workout order.</param>
public sealed record WorkoutSummaryResponse(
    Guid WorkoutId,
    SetsInTargetResponse SetsInTarget,
    decimal? Volume,
    IReadOnlyList<ExerciseSummaryResponse> Exercises)
{
    public static WorkoutSummaryResponse From(WorkoutSummaryView view)
        => new(
            view.WorkoutId.Value,
            SetsInTargetResponse.From(view.SetsInTarget),
            view.Volume,
            view.Exercises.Select(ExerciseSummaryResponse.From).ToList());
}

/// <param name="InTarget">Recorded sets that met their prescribed target.</param>
/// <param name="Evaluated">Recorded sets that were compared with a prescribed set. Recorded sets beyond the prescribed ones and prescribed sets without a record are not evaluated.</param>
public sealed record SetsInTargetResponse(int InTarget, int Evaluated)
{
    public static SetsInTargetResponse From(SetsInTargetView view)
        => new(view.InTarget, view.Evaluated);
}

/// <param name="WorkoutExerciseId">Identifier of this entry of the workout.</param>
/// <param name="ExerciseId">Identifier of the catalog exercise performed in this entry.</param>
/// <param name="SetsInTarget">Sets of this exercise that met their target.</param>
/// <param name="Volume">Sum of reps × kilograms over the sets of this exercise. Null when no set has both reps and a weight.</param>
/// <param name="TopSet">Heaviest set of this exercise in the workout, ties broken by most reps. Null when it has no set records.</param>
/// <param name="Previous">The previous execution of this exercise. Null when there is none the caller may see.</param>
public sealed record ExerciseSummaryResponse(
    Guid WorkoutExerciseId,
    Guid ExerciseId,
    SetsInTargetResponse SetsInTarget,
    decimal? Volume,
    LastPerformanceSetResponse? TopSet,
    PreviousExecutionResponse? Previous)
{
    public static ExerciseSummaryResponse From(ExerciseSummaryView view)
        => new(
            view.WorkoutExerciseId.Value,
            view.ExerciseId.Value,
            SetsInTargetResponse.From(view.SetsInTarget),
            view.Volume,
            view.TopSet is null ? null : LastPerformanceSetResponse.From(view.TopSet),
            view.Previous is null ? null : PreviousExecutionResponse.From(view.Previous));
}

/// <param name="WorkoutId">Identifier of the earlier finished workout where the owner did the exercise.</param>
/// <param name="WorkoutDate">Date of that workout.</param>
/// <param name="TopSet">Heaviest set of the exercise in that workout, ties broken by most reps.</param>
public sealed record PreviousExecutionResponse(Guid WorkoutId, DateOnly WorkoutDate, LastPerformanceSetResponse TopSet)
{
    public static PreviousExecutionResponse From(PreviousExecutionView view)
        => new(view.WorkoutId.Value, view.WorkoutDate, LastPerformanceSetResponse.From(view.TopSet));
}
