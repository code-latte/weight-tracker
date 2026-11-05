namespace WeightTracker.Infra.Entities;

/// <summary>EF representation of the <c>Mesocycles</c> table.</summary>
/// <remarks>
/// <c>TrainerId</c> and <c>AthleteId</c> stay plain foreign key columns: the User aggregate
/// is referenced by identifier only, never navigated into from here.
/// </remarks>
public class MesocycleEntity
{
    public Guid Id { get; set; }

    public Guid TrainerId { get; set; }

    public Guid AthleteId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Objective { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public int DurationInWeeks { get; set; }

    /// <summary>Navigation inside the Mesocycle aggregate boundary.</summary>
    public List<MesocycleWeekEntity> Weeks { get; set; } = [];
}
