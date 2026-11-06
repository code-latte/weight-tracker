using System.ComponentModel.DataAnnotations;

namespace WeightTracker.Api.Endpoints.Workouts.Dtos;

/// <summary>
/// Body of <c>PUT /workouts/{id}</c>. It replaces every editable property, so a client sends back what it
/// read from <c>GET /workouts/{id}</c> with its changes. Only structural validation lives here; who may
/// change what is enforced by the use case.
/// </summary>
public sealed record UpdateWorkoutRequest
{
    /// <summary>
    /// Name the workout is listed by. Trimmed; it cannot be blank or longer than 256 characters.
    /// </summary>
    [Required]
    public string? Name { get; init; }

    /// <summary>
    /// Optional free text, up to 1024 characters once trimmed. <c>null</c> or blank clears it.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Day of the workout, as <c>yyyy-MM-dd</c>. Time is not kept. Required for a standalone workout. Ignored when
    /// <c>mesocycleId</c> and <c>mesocycleWeekNumber</c> are sent: the workout then takes the Wednesday of its
    /// mesocycle week.
    /// </summary>
    public DateOnly? WorkoutDate { get; init; }

    /// <summary>
    /// Owner of the workout. Only an admin may change it; everybody else must send the current one.
    /// </summary>
    [Required]
    public Guid? UserId { get; init; }

    /// <summary>
    /// Trainer of the workout, <c>null</c> for none. Only an admin may change it; everybody else must send
    /// the current one, so omitting it on a prescribed workout is rejected.
    /// </summary>
    public Guid? TrainerId { get; init; }

    /// <summary>
    /// Mesocycle the workout is placed in. Sent together with <c>mesocycleWeekNumber</c>; omitting both takes the
    /// workout out of its mesocycle. Send back what <c>GET /workouts/{id}</c> returned to keep the placement. Only the mesocycle's trainer or an admin may place a workout, and the workout must belong
    /// to the mesocycle's athlete.
    /// </summary>
    public Guid? MesocycleId { get; init; }

    /// <summary>
    /// Week of the mesocycle to place the workout in, from 1 to its duration. The week does not need to be defined.
    /// </summary>
    public int? MesocycleWeekNumber { get; init; }
}
