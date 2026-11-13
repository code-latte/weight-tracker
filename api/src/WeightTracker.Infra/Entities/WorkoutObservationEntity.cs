namespace WeightTracker.Infra.Entities;

/// <summary>EF representation of the <c>WorkoutObservations</c> table: one observation of one workout.</summary>
public class WorkoutObservationEntity
{
    public Guid Id { get; set; }

    public Guid WorkoutId { get; set; }

    /// <summary>Null when the observation is about the whole workout.</summary>
    public Guid? ExerciseId { get; set; }

    /// <summary>Name of the observation code.</summary>
    public string Code { get; set; } = null!;

    public int Severity { get; set; }

    /// <summary>Null when the observation is not eligible to be highlighted.</summary>
    public int? Priority { get; set; }

    /// <summary>Position among the highlights, null when not highlighted.</summary>
    public int? HighlightRank { get; set; }

    /// <summary>The numbers the observation was calculated from, as a flat JSON object.</summary>
    public string Data { get; set; } = "{}";

    public DateTime CreatedAt { get; set; }
}
