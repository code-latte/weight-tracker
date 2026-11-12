using System.ComponentModel.DataAnnotations;

namespace WeightTracker.Api.Endpoints.Workouts.Dtos;

/// <summary>
/// Body of <c>POST /workouts/{workoutId}/exercises/{exerciseId}/set-records</c> and
/// <c>PUT /workouts/{workoutId}/exercises/{exerciseId}/set-records/{setRecordId}</c>.
/// Only structural validation lives here; the value rules belong to the domain. The registration time is set by the server.
/// </summary>
/// <remarks>
/// A set holds either <c>reps</c> or <c>durationSeconds</c>, according to how the exercise is prescribed, never both.
/// On <c>POST</c>, sending neither means the set was done as prescribed and the server copies the value of the next prescribed set.
/// On <c>PUT</c>, one of them is required. <c>weight</c> is optional in both cases.
/// </remarks>
public sealed record AddOrUpdateSetRecordRequest : IValidatableObject
{
    /// <summary>
    /// Reps actually performed, at least 1, for an exercise prescribed by reps. They may differ from the prescription.
    /// </summary>
    public int? Reps { get; init; }

    /// <summary>
    /// Seconds actually held, at least 1, for an exercise prescribed by duration or minutes.
    /// </summary>
    public int? DurationSeconds { get; init; }

    /// <summary>
    /// Weight lifted in kilograms, from 0 (bodyweight) with up to 2 decimals. Omit it when the weight is not tracked.
    /// </summary>
    public decimal? Weight { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Reps is not null && DurationSeconds is not null)
            yield return new ValidationResult("A set record holds either reps or durationSeconds, not both.", [nameof(Reps), nameof(DurationSeconds)]);
    }
}
