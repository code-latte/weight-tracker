namespace WeightTracker.Infra.Entities;

/// <summary>EF representation of the <c>TrainerAthletes</c> table.</summary>
public class TrainerAthleteEntity
{
    public Guid Id { get; set; }

    public Guid TrainerId { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Stored as the integer value of the domain <c>TrainerAthleteStatus</c>.</summary>
    public int Status { get; set; }

    public DateTime StartedAtUtc { get; set; }

    public DateTime? FinishedAtUtc { get; set; }
}
