using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Api.Endpoints.Workouts.Dtos;

/// <summary>
/// Body of <c>POST /workouts/{workoutId}/exercises</c> and <c>PUT /workouts/{workoutId}/exercises/{exerciseId}</c>.
/// Only structural validation lives here; the value rules of the sets belong to the domain.
/// </summary>
public sealed record AddOrUpdateExerciseRequest
{
    /// <summary>
    /// Catalog exercise. On <c>PUT</c> it must be the same one as in the route.
    /// </summary>
    [Required]
    public Guid? ExerciseId { get; init; }

    /// <summary>
    /// How hard the exercise must be performed: <c>LOW</c>, <c>MIDLOW</c>, <c>MID</c>, <c>MIDHIGH</c>
    /// or <c>HIGH</c>, or their number (1 to 5).
    /// </summary>
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter<Effort>))]
    public Effort? Effort { get; init; }

    /// <summary>
    /// Ordered set prescriptions. At least one is required.
    /// </summary>
    [Required]
    [MinLength(1)]
    public IReadOnlyList<SetPrescriptionRequest>? Sets { get; init; }

    /// <summary>
    /// When true the targets of the sets are per side (1x8 reps means 8 on each side). Defaults to false.
    /// </summary>
    public bool? PerSide { get; init; }

    /// <summary>
    /// Rest between sets in minutes, up to 2 decimals (1.5 is 1 minute and 30 seconds). Omit it for no rest.
    /// </summary>
    public decimal? RestMinutes { get; init; }

    public TimeInMinutes? ToRest() => RestMinutes is null ? null : TimeInMinutes.From(RestMinutes.Value);
}

public sealed record SetPrescriptionRequest
{
    /// <summary>Number of sets with this target.</summary>
    [Required]
    public int? Count { get; init; }

    /// <summary><c>Reps</c>, <c>Duration</c>, <c>MaxReps</c>, <c>RepsRange</c> or <c>Minutes</c>.</summary>
    [Required]
    [AllowedValues(SetTargetTypes.Reps, SetTargetTypes.Duration, SetTargetTypes.MaxReps, SetTargetTypes.RepsRange, SetTargetTypes.Minutes)]
    public string? TargetType { get; init; }

    /// <summary>
    /// Reps per set for <c>Reps</c>, seconds per set for <c>Duration</c>, minimum reps per set for
    /// <c>RepsRange</c>. Ignored for <c>MaxReps</c>.
    /// </summary>
    public int? Target { get; init; }

    /// <summary>
    /// Maximum reps per set for <c>RepsRange</c>. Ignored for every other target type.
    /// </summary>
    public int? MaxTarget { get; init; }

    /// <summary>
    /// Minutes per set for <c>Minutes</c>, up to 2 decimals (1.25 is 1 minute and 15 seconds). Ignored for every other target type.
    /// </summary>
    public decimal? TargetMinutes { get; init; }

    public SetPrescription ToDomain() => new(Count!.Value, TargetType switch
    {
        SetTargetTypes.Reps => new SetTarget.Reps(Target ?? 0),
        SetTargetTypes.Duration => new SetTarget.Duration(Target ?? 0),
        SetTargetTypes.RepsRange => new SetTarget.RepsRange(Target ?? 0, MaxTarget ?? 0),
        SetTargetTypes.Minutes => new SetTarget.Minutes(TimeInMinutes.From(TargetMinutes ?? 0)),
        _ => new SetTarget.MaxReps(),
    });
}
