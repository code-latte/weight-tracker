using System.ComponentModel.DataAnnotations;

namespace WeightTracker.Api.Endpoints.WorkoutTemplates.Dtos;

/// <summary>
/// Body of <c>POST /workout-templates/{id}/workouts</c>. The trainer of the new workout is always the template's
/// trainer, so the body carries none. Only structural validation lives here.
/// </summary>
public sealed record CreateWorkoutFromTemplateRequest
{
    /// <summary>
    /// Regular user the workout is for.
    /// </summary>
    [Required]
    public Guid? UserId { get; init; }

    /// <summary>
    /// Day of the workout, as <c>yyyy-MM-dd</c>. Required for a standalone workout. Ignored when
    /// <c>mesocycleId</c> and <c>mesocycleWeekNumber</c> are sent: the workout then takes the date of its mesocycle week.
    /// </summary>
    public DateOnly? WorkoutDate { get; init; }

    /// <summary>
    /// Mesocycle to place the workout in, sent together with <c>mesocycleWeekNumber</c>. The mesocycle must be
    /// prepared by the template's trainer for <c>userId</c>.
    /// </summary>
    public Guid? MesocycleId { get; init; }

    /// <summary>
    /// Week of the mesocycle to place the workout in, from 1 to its duration.
    /// </summary>
    public int? MesocycleWeekNumber { get; init; }
}
