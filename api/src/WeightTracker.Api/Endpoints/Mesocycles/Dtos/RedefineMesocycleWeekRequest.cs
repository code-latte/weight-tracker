using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using WeightTracker.Domain.Mesocycles;

namespace WeightTracker.Api.Endpoints.Mesocycles.Dtos;

/// <summary>
/// Body of <c>PUT /mesocycles/{id}/weeks/{weekNumber}</c>. It replaces the training type and objective of a
/// defined week; the week keeps its number.
/// </summary>
public sealed record RedefineMesocycleWeekRequest
{
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
