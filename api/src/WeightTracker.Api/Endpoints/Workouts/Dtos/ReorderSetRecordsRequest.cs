using System.ComponentModel.DataAnnotations;

namespace WeightTracker.Api.Endpoints.Workouts.Dtos;

/// <summary>
/// Body of <c>PUT /workouts/{workoutId}/exercises/{exerciseId}/set-records/order</c>. Only structural validation lives here;
/// that the list matches the set records of the exercise is enforced by the domain.
/// </summary>
public sealed record ReorderSetRecordsRequest
{
    /// <summary>
    /// Identifier of every set record of the exercise, each once, in the new order.
    /// </summary>
    [Required]
    public IReadOnlyList<Guid>? SetRecordIds { get; init; }
}
