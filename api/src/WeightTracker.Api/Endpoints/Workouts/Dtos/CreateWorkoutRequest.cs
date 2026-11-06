using System.ComponentModel.DataAnnotations;

namespace WeightTracker.Api.Endpoints.Workouts.Dtos;

/// <summary>
/// Body of <c>POST /workouts</c>. Only structural validation lives here; which of
/// <c>userId</c> and <c>trainerId</c> each role may or must send is enforced by the use case.
/// </summary>
public sealed record CreateWorkoutRequest
{
    /// <summary>
    /// Owner of the workout. Optional for a user (it can only be themselves), required for a
    /// trainer or an admin, who cannot create workouts for themselves.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Trainer who prescribes the workout. Only an admin may send it, and never for a trainer
    /// who is the owner. A trainer who creates a workout is set as its trainer automatically.
    /// </summary>
    public Guid? TrainerId { get; init; }

    /// <summary>
    /// Name the workout is listed by. Trimmed; it cannot be blank or longer than 256 characters.
    /// </summary>
    [Required]
    public string? Name { get; init; }

    /// <summary>
    /// Optional free text, up to 1024 characters once trimmed. A blank value is stored as <c>null</c>.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Day of the workout, as <c>yyyy-MM-dd</c>. Time is not kept. Required for a standalone workout. Ignored when
    /// <c>mesocycleId</c> and <c>mesocycleWeekNumber</c> are sent: the workout then takes the Wednesday of its
    /// mesocycle week.
    /// </summary>
    public DateOnly? WorkoutDate { get; init; }

    /// <summary>
    /// Mesocycle to place the workout in. Sent together with <c>mesocycleWeekNumber</c>, or both omitted for a
    /// standalone workout. Only the mesocycle's trainer or an admin may place a workout, and the workout must belong
    /// to the mesocycle's athlete.
    /// </summary>
    public Guid? MesocycleId { get; init; }

    /// <summary>
    /// Week of the mesocycle to place the workout in, from 1 to its duration. The week does not need to be defined.
    /// </summary>
    public int? MesocycleWeekNumber { get; init; }
}
