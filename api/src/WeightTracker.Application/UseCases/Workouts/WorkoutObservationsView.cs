using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts.Observations;

namespace WeightTracker.Application.UseCases.Workouts;

/// <summary>
/// The observations generated when a workout was completed and which of them are highlighted. Both lists are empty when
/// none were generated. Observations come workout-level first, then by exercise position in the workout, then in code order.
/// </summary>
/// <param name="Observations">Every observation.</param>
/// <param name="Highlights">At most three, in rank order, each referring to an observation of the first list.</param>
public sealed record WorkoutObservationsView(
    IReadOnlyList<WorkoutObservationView> Observations,
    IReadOnlyList<WorkoutHighlightView> Highlights);

/// <summary>
/// One observation. <paramref name="ExerciseId"/> is <c>null</c> for one about the whole workout and
/// <paramref name="Priority"/> when it is not eligible to be highlighted.
/// </summary>
public sealed record WorkoutObservationView(
    Id Id,
    ObservationCode Code,
    ObservationSeverity Severity,
    Id? ExerciseId,
    HighlightPriority? Priority,
    ObservationEvidence Data);

/// <summary>
/// A highlighted observation: which one, its position among the highlights from 1, and its priority.
/// </summary>
public sealed record WorkoutHighlightView(Id ObservationId, int Rank, HighlightPriority Priority);
