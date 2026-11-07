namespace WeightTracker.Api.Endpoints.Workouts.Dtos;

/// <summary>
/// Body of <c>PUT /workouts/{workoutId}/review</c>. Only structural validation lives here;
/// whether the workout can be reviewed belongs to the domain and the application layer.
/// </summary>
public sealed record ReviewWorkoutRequest
{
    /// <summary>
    /// <c>resultsUpdatedAt</c> of the workout as the reviewer read it, <c>null</c> when it had none.
    /// The review is refused when the results changed afterwards.
    /// </summary>
    public DateTime? ResultsUpdatedAt { get; init; }
}
