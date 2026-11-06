namespace WeightTracker.Infra.Entities;

/// <summary>EF representation of the <c>WorkoutTemplateExerciseSets</c> table.</summary>
public class WorkoutTemplateExerciseSetEntity
{
    public Guid Id { get; set; }

    public Guid WorkoutTemplateExerciseId { get; set; }

    /// <summary>Preserves the order of the prescriptions of one exercise.</summary>
    public int Position { get; set; }

    public int Count { get; set; }

    /// <summary>Discriminator of the SetTarget hierarchy, as in <c>WorkoutExerciseSets</c>.</summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>Null for the target types that carry no value. The min for RepsRange.</summary>
    public int? TargetValue { get; set; }

    /// <summary>The max for RepsRange; null for every other target type.</summary>
    public int? TargetMaxValue { get; set; }

    public decimal? TargetMinutes { get; set; }
}
