using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exercises;

/// <summary>
/// The name of an exercise in one language.
/// </summary>
/// <remarks>
/// Entity of the Exercise aggregate. It is only created, restored and replaced together with its
/// <see cref="Exercise"/>, which keeps at most one translation per language.
/// </remarks>
public sealed class ExerciseTranslation : Entity
{
    public LanguageCode LanguageCode { get; }

    public ExerciseName Name { get; }

    private ExerciseTranslation(Id id, LanguageCode languageCode, ExerciseName name)
    {
        this.Id = id;
        this.IsNew = false;
        LanguageCode = languageCode;
        Name = name;
    }

    /// <summary>
    /// Restores a translation stored in the catalog.
    /// </summary>
    /// <summary>
    /// Creates a translation that does not exist in the system yet.
    /// </summary>
    public static ExerciseTranslation Create(LanguageCode languageCode, ExerciseName name)
        => new(Id.New(), languageCode, name) { IsNew = true };

    public static ExerciseTranslation Rehydrate(Id id, LanguageCode languageCode, ExerciseName name)
        => new(id, languageCode, name);
}
