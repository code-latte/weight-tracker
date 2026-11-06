namespace WeightTracker.Infra.Entities;

/// <summary>EF representation of the <c>WorkoutTemplateExercises</c> table.</summary>
/// <remarks>
/// <c>ExerciseId</c> stays a plain foreign key column. The Exercise aggregate is referenced by identifier only.
/// </remarks>
public class WorkoutTemplateExerciseEntity
{
    public Guid Id { get; set; }

    public Guid WorkoutTemplateId { get; set; }

    public Guid ExerciseId { get; set; }

    /// <summary>Preserves the order the aggregate holds its exercises in.</summary>
    public int Position { get; set; }

    /// <summary>Value of the <c>Effort</c> enum.</summary>
    public int Effort { get; set; }

    public bool PerSide { get; set; }

    public decimal? RestMinutes { get; set; }

    /// <summary>Superseries label, only unique together with <c>WorkoutTemplateId</c>. Null when the exercise is not grouped.</summary>
    public int? SuperSeriesId { get; set; }

    /// <summary>Navigation inside the WorkoutTemplate aggregate boundary.</summary>
    public List<WorkoutTemplateExerciseSetEntity> Sets { get; set; } = [];
}
