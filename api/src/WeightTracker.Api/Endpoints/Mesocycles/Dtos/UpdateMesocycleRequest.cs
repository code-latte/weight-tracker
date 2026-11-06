using System.ComponentModel.DataAnnotations;

namespace WeightTracker.Api.Endpoints.Mesocycles.Dtos;

/// <summary>
/// Body of <c>PUT /mesocycles/{id}</c>. It replaces the name, objective, start date and duration; trainer,
/// athlete and weeks are not changed here. Only structural validation lives here.
/// </summary>
public sealed record UpdateMesocycleRequest
{
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
    /// How many weeks the mesocycle lasts, from 1 to 52. It cannot drop below the highest defined week.
    /// </summary>
    [Required]
    public int? DurationInWeeks { get; init; }
}
