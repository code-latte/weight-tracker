using System.ComponentModel.DataAnnotations;

namespace WeightTracker.Api.Endpoints.WorkoutTemplates.Dtos;

/// <summary>
/// Body of <c>PUT /workout-templates/{id}</c>. It replaces the name and the description; the trainer and the
/// exercises are not changed here.
/// </summary>
public sealed record UpdateWorkoutTemplateRequest
{
    /// <summary>
    /// Name the template is listed by. Trimmed; it cannot be blank or longer than 256 characters.
    /// </summary>
    [Required]
    public string? Name { get; init; }

    /// <summary>
    /// Optional free text, up to 1024 characters once trimmed. <c>null</c> or blank clears it.
    /// </summary>
    public string? Description { get; init; }
}
