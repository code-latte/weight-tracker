using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when no workout template has the given id, and also when the template belongs to another trainer:
/// a template is private, so its existence is never confirmed to whoever cannot see it.
/// </summary>
public sealed class WorkoutTemplateNotFoundException : ApplicationLayerException
{
    public WorkoutTemplateNotFoundException(Id templateId) : base($"Workout template '{templateId}' was not found")
    { }
}
