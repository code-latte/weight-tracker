namespace WeightTracker.Infra.Entities;

/// <summary>EF representation of the <c>WorkoutTemplates</c> table.</summary>
/// <remarks>
/// <c>TrainerId</c> stays a plain foreign key column: the User aggregate is referenced by identifier only.
/// </remarks>
public class WorkoutTemplateEntity
{
    public Guid Id { get; set; }

    public Guid TrainerId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Navigation inside the WorkoutTemplate aggregate boundary.</summary>
    public List<WorkoutTemplateExerciseEntity> Exercises { get; set; } = [];
}
