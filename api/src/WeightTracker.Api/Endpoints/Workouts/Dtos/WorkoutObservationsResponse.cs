using WeightTracker.Application.UseCases.Workouts;
using WeightTracker.Domain.Workouts.Observations;

namespace WeightTracker.Api.Endpoints.Workouts.Dtos;

/// <param name="Observations">Every observation generated when the workout was completed. Workout-level ones first, then by exercise position in the workout. Empty when none were generated.</param>
/// <param name="Highlights">At most 3 observations that deserve the trainer's attention, in rank order. Empty when none qualifies.</param>
public sealed record WorkoutObservationsResponse(
    IReadOnlyList<WorkoutObservationResponse> Observations,
    IReadOnlyList<WorkoutHighlightResponse> Highlights)
{
    public static WorkoutObservationsResponse From(WorkoutObservationsView view)
        => new(
            view.Observations.Select(WorkoutObservationResponse.From).ToList(),
            view.Highlights.Select(WorkoutHighlightResponse.From).ToList());
}

/// <param name="Id">Identifier of the observation, which highlights refer to.</param>
/// <param name="Code">Stable code of what the observation states, such as <c>WEIGHT_PERSONAL_RECORD</c>. Once released a code never changes meaning.</param>
/// <param name="Severity">What kind of fact it is: <c>INFO</c>, <c>WARNING</c> or <c>SUCCESS</c>.</param>
/// <param name="ExerciseId">Catalog exercise the observation is about. Null when it is about the whole workout.</param>
/// <param name="Priority">How much attention it deserves: <c>CRITICAL</c>, <c>HIGH</c>, <c>MEDIUM</c> or <c>LOW</c>. Null when it is not eligible to be highlighted.</param>
/// <param name="Data">The numbers the observation was calculated from, as named numbers or lists of numbers. Weights are in kilograms. The names depend on the code.</param>
public sealed record WorkoutObservationResponse(
    Guid Id,
    string Code,
    string Severity,
    Guid? ExerciseId,
    string? Priority,
    IReadOnlyDictionary<string, object> Data)
{
    public static WorkoutObservationResponse From(WorkoutObservationView view)
        => new(
            view.Id.Value,
            view.Code.ToString(),
            view.Severity.ToString(),
            view.ExerciseId?.Value,
            view.Priority?.ToString(),
            view.Data.Values.ToDictionary(
                pair => pair.Key,
                pair => pair.Value switch
                {
                    EvidenceValue.Single single => (object)single.Value,
                    EvidenceValue.Many many => many.Values,
                    _ => throw new InvalidOperationException($"unknown evidence kind for '{pair.Key}'"),
                }));
}

/// <param name="ObservationId">Identifier of the highlighted observation, one of the <c>observations</c>.</param>
/// <param name="Rank">Position among the highlights, starting at 1.</param>
/// <param name="Priority">Priority of the highlighted observation.</param>
public sealed record WorkoutHighlightResponse(Guid ObservationId, int Rank, string Priority)
{
    public static WorkoutHighlightResponse From(WorkoutHighlightView view)
        => new(view.ObservationId.Value, view.Rank, view.Priority.ToString());
}
