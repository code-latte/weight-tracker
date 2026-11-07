namespace WeightTracker.Infra.Entities;

/// <summary>EF representation of the <c>SetRecords</c> table.</summary>
public class SetRecordEntity
{
    public Guid Id { get; set; }

    public Guid WorkoutExerciseId { get; set; }

    /// <summary>Preserves the order the aggregate holds the set records of one exercise in.</summary>
    public int Position { get; set; }

    public int Reps { get; set; }

    /// <summary>Kilograms.</summary>
    public decimal Weight { get; set; }

    public DateTime RegisterDatetimeUtc { get; set; }
}
