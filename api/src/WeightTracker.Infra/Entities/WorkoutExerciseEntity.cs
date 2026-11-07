namespace WeightTracker.Infra.Entities;

/// <summary>EF representation of the <c>workout_exercises</c> table.</summary>
/// <remarks>
/// <c>ExerciseId</c> stays a plain foreign key column. The Workout aggregate references the
/// Exercise aggregate by identifier only, so its rows are never loaded or navigated from here.
/// </remarks>
public class WorkoutExerciseEntity
{
    public Guid Id { get; set; }

    public Guid WorkoutId { get; set; }

    public Guid ExerciseId { get; set; }

    /// <summary>Preserves the order the aggregate holds its exercises in.</summary>
    public int Position { get; set; }

    /// <summary>Value of the <c>Effort</c> enum.</summary>
    public int Effort { get; set; }

    public bool PerSide { get; set; }

    public decimal? RestMinutes { get; set; }

    /// <summary>Superseries label, only unique together with <c>WorkoutId</c>. Null when the exercise is not grouped.</summary>
    public int? SuperSeriesId { get; set; }

    /// <summary>Navigation inside the Workout aggregate boundary.</summary>
    public List<WorkoutExerciseSetEntity> Sets { get; set; } = [];

    /// <summary>Navigation inside the Workout aggregate boundary.</summary>
    public List<SetRecordEntity> SetRecords { get; set; } = [];
}
