namespace WeightTracker.Infra.Entities;

/// <summary>EF representation of the <c>FavoriteExercises</c> table.</summary>
public class ExerciseFavoriteEntity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid ExerciseId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
