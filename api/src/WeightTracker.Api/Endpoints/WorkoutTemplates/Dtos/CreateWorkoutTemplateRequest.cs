using System.ComponentModel.DataAnnotations;

namespace WeightTracker.Api.Endpoints.WorkoutTemplates.Dtos;

/// <summary>
/// Body of <c>POST /workout-templates</c>. Only structural validation lives here; who may own the template is
/// enforced by the use case.
/// </summary>
public sealed record CreateWorkoutTemplateRequest
{
    /// <summary>
    /// Name the template is listed by. Trimmed; it cannot be blank or longer than 256 characters.
    /// </summary>
    [Required]
    public string? Name { get; init; }

    /// <summary>
    /// Optional free text, up to 1024 characters once trimmed. <c>null</c> or blank leaves it empty.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Trainer who owns the template. Required for an admin, who never owns a template; a trainer must omit it.
    /// </summary>
    public Guid? TrainerId { get; init; }
}
