using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Api.Endpoints.Workouts.Dtos;

/// <summary>
/// Body of <c>PUT /workouts/{workoutId}/status</c>. Only structural validation lives here;
/// which moves are allowed belongs to the domain and the application layer.
/// </summary>
public sealed record ChangeWorkoutStatusRequest
{
    /// <summary>
    /// Status to move the workout to: <c>DRAFT</c>, <c>PUBLISHED</c>, <c>STARTED</c>, <c>COMPLETED</c> or
    /// <c>REVIEWED</c>, or their number (1 to 5). <c>REVIEWED</c> is always refused here: a workout is
    /// reviewed through <c>PUT /workouts/{workoutId}/review</c>.
    /// </summary>
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter<WorkoutStatus>))]
    public WorkoutStatus? Status { get; init; }
}
