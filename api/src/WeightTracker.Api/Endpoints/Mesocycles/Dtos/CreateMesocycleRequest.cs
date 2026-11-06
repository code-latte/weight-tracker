using System.ComponentModel.DataAnnotations;

namespace WeightTracker.Api.Endpoints.Mesocycles.Dtos;

/// <summary>
/// Body of <c>POST /mesocycles</c>. Only structural validation lives here; which of <c>trainerId</c> and
/// <c>athleteId</c> each role may or must send, and whether they can take part, is enforced by the use case.
/// </summary>
public sealed record CreateMesocycleRequest
{
    /// <summary>
    /// Trainer who prepares the mesocycle. Required for an admin; a trainer must not send it, since they are set
    /// as the trainer automatically.
    /// </summary>
    public Guid? TrainerId { get; init; }

    /// <summary>
    /// Athlete the mesocycle is for. Must be an active regular user; a trainer must actively train them.
    /// </summary>
    [Required]
    public Guid? AthleteId { get; init; }

    /// <summary>
    /// Name the mesocycle is listed by. Trimmed; it cannot be blank or longer than 256 characters.
    /// </summary>
    [Required]
    public string? Name { get; init; }

    /// <summary>
    /// What the mesocycle is meant to achieve. Trimmed; it cannot be blank or longer than 4000 characters.
    /// </summary>
    [Required]
    public string? Objective { get; init; }

    /// <summary>
    /// First day of the mesocycle, as <c>yyyy-MM-dd</c>. Any day of the week is valid.
    /// </summary>
    [Required]
    public DateOnly? StartDate { get; init; }

    /// <summary>
    /// How many weeks the mesocycle lasts, from 1 to 52.
    /// </summary>
    [Required]
    public int? DurationInWeeks { get; init; }
}
