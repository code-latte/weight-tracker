using System.ComponentModel.DataAnnotations;

namespace WeightTracker.Api.Endpoints.Workouts.Dtos;

/// <summary>
/// Body of <c>POST /workouts/{workoutId}/exercises/{exerciseId}/set-records</c> and
/// <c>PUT /workouts/{workoutId}/exercises/{exerciseId}/set-records/{setRecordId}</c>.
/// Only structural validation lives here; the value rules belong to the domain. The registration time is set by the server.
/// </summary>
public sealed record AddOrUpdateSetRecordRequest
{
    /// <summary>
    /// Reps actually performed, at least 1. They may differ from the prescription.
    /// </summary>
    [Required]
    public int? Reps { get; init; }

    /// <summary>
    /// Weight lifted in kilograms, from 0 (bodyweight) with up to 2 decimals.
    /// </summary>
    [Required]
    public decimal? Weight { get; init; }
}
