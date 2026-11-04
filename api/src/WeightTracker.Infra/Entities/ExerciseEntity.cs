namespace WeightTracker.Infra.Entities;

/// <summary>EF representation of the <c>Exercises</c> table.</summary>
public class ExerciseEntity
{
    public Guid Id { get; set; }

    public string? FreeExerciseDbId { get; set; }

    public string? VideoUrl { get; set; }

    /// <summary>Value of the <c>Muscles</c> enum. The body part is derived from it and not stored.</summary>
    public int Muscle { get; set; }

    /// <summary>Value of the <c>ExerciseTypes</c> enum.</summary>
    public int Type { get; set; }

    public Guid? OwnerId { get; set; }

    public DateTime? ArchivedAtUtc { get; set; }

    /// <summary>Navigation inside the Exercise aggregate boundary.</summary>
    public List<ExerciseTranslationEntity> Translations { get; set; } = [];
}
