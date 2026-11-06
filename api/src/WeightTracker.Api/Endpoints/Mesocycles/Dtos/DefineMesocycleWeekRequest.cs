using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using WeightTracker.Domain.Mesocycles;

namespace WeightTracker.Api.Endpoints.Mesocycles.Dtos;

/// <summary>
/// Body of <c>POST /mesocycles/{id}/weeks</c>. Only structural validation lives here; whether the week fits the
/// mesocycle is enforced by the domain.
/// </summary>
public sealed record DefineMesocycleWeekRequest
{
    /// <summary>
    /// Number of the week, from 1 to the duration of the mesocycle. It must not be defined yet.
    /// </summary>
    [Required]
    public int? WeekNumber { get; init; }

    /// <summary>
    /// Kind of training of the week: <c>ADAPTATION</c>, <c>ACCUMULATION</c>, <c>INTENSIFICATION</c>,
    /// <c>REALIZATION</c>, <c>DELOAD</c> or <c>RECOVERY</c>.
    /// </summary>
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter<TrainingWeekType>))]
    public TrainingWeekType? TrainingWeekType { get; init; }

    /// <summary>
    /// What the week is meant to achieve. Trimmed; it cannot be blank or longer than 4000 characters.
    /// </summary>
    [Required]
    public string? Objective { get; init; }
}
