namespace WeightTracker.Infra.Entities;

/// <summary>EF representation of the <c>workouts</c> table.</summary>
/// <remarks>
/// <c>UserId</c> and <c>TrainerId</c> stay plain foreign key columns: the User aggregate
/// is referenced by identifier only, never navigated into from here.
/// </remarks>
public class WorkoutEntity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid? TrainerId { get; set; }

    public DateOnly WorkoutDate { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Stored as the integer value of the domain <c>WorkoutStatus</c>.</summary>
    public int Status { get; set; }

    /// <summary>
    /// Mesocycle the workout is planned in. A plain foreign key column: the Mesocycle aggregate is referenced
    /// by identifier only. Null, together with <c>MesocycleWeekNumber</c>, for a standalone workout.
    /// </summary>
    public Guid? MesocycleId { get; set; }

    public int? MesocycleWeekNumber { get; set; }

    /// <summary>When the workout was reviewed, in UTC. Null, together with <c>ReviewedBy</c>, until it is reviewed.</summary>
    public DateTime? ReviewedAtUtc { get; set; }

    /// <summary>User who reviewed the workout. A plain foreign key column, like <c>UserId</c>.</summary>
    public Guid? ReviewedBy { get; set; }

    /// <summary>Last time the set records of the workout changed, in UTC.</summary>
    public DateTime? ResultsUpdatedAtUtc { get; set; }

    /// <summary>Last time the workout reached started, in UTC. Kept when it leaves started. Estimated for older workouts.</summary>
    public DateTime? StartedAtUtc { get; set; }

    /// <summary>Last time the workout reached completed, in UTC. Kept when it leaves completed.</summary>
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>
    /// Postgres <c>xmin</c> system column, used as an optimistic concurrency token. It changes on every write of the row.
    /// </summary>
    public uint Version { get; set; }

    /// <summary>Navigation inside the Workout aggregate boundary.</summary>
    public List<WorkoutExerciseEntity> WorkoutExercises { get; set; } = [];
}
