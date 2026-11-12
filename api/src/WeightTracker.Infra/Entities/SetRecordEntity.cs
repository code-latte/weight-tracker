namespace WeightTracker.Infra.Entities;

/// <summary>EF representation of the <c>SetRecords</c> table.</summary>
public class SetRecordEntity
{
    public Guid Id { get; set; }

    public Guid WorkoutExerciseId { get; set; }

    /// <summary>Preserves the order the aggregate holds the set records of one exercise in.</summary>
    public int Position { get; set; }

    /// <summary>Reps done. Set for a rep-based exercise, null for a time-based one; exactly one of <see cref="Reps"/> and <see cref="DurationSeconds"/> is set.</summary>
    public int? Reps { get; set; }

    /// <summary>Seconds held. Set for a time-based exercise, null for a rep-based one.</summary>
    public int? DurationSeconds { get; set; }

    /// <summary>Kilograms. Null when the weight is not tracked.</summary>
    public decimal? Weight { get; set; }

    public DateTime RegisterDatetimeUtc { get; set; }
}
