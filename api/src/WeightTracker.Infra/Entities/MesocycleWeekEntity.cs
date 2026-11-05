namespace WeightTracker.Infra.Entities;

/// <summary>EF representation of the <c>MesocycleWeeks</c> table.</summary>
public class MesocycleWeekEntity
{
    public Guid Id { get; set; }

    public Guid MesocycleId { get; set; }

    /// <summary>Unique together with <c>MesocycleId</c>.</summary>
    public int WeekNumber { get; set; }

    /// <summary>Value of the <c>TrainingWeekType</c> enum.</summary>
    public int TrainingWeekType { get; set; }

    public string Objective { get; set; } = string.Empty;
}
