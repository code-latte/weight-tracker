namespace WeightTracker.Infra.Entities;

/// <summary>EF representation of the <c>workout_exercise_sets</c> table.</summary>
public class WorkoutExerciseSetEntity
{
    public Guid Id { get; set; }

    public Guid WorkoutExerciseId { get; set; }

    /// <summary>Preserves the order of the prescriptions of one exercise.</summary>
    public int Position { get; set; }

    public int Count { get; set; }

    /// <summary>Discriminator of the SetTarget hierarchy: Reps, Duration, MaxReps or RepsRange.</summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>Null only for MaxReps, which carries no value. The min for RepsRange.</summary>
    public int? TargetValue { get; set; }

    /// <summary>The max for RepsRange; null for every other target type.</summary>
    public int? TargetMaxValue { get; set; }

    public decimal? TargetMinutes { get; set; }
}
