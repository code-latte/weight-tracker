using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Exercises;

/// <summary>
/// An exercise that workouts are built from: either a system exercise of the shared catalog or a custom exercise
/// owned by a trainer or an athlete.
/// </summary>
/// <remarks>
/// System exercises have no owner. They are maintained in the database (migrations or direct edits), and
/// <see cref="UpdateDetails"/> and <see cref="Archive"/> reject them. Custom exercises are created and changed
/// through the application, and are archived instead of deleted because workouts reference them.
/// </remarks>
public sealed class Exercise : AggregateRoot
{
    private readonly List<ExerciseTranslation> _translations;

    /// <summary>
    /// Entry of the free exercise dataset this exercise comes from, null when it has none.
    /// </summary>
    public FreeExerciseDbId? FreeExerciseDbId { get; }

    /// <summary>
    /// User who created the exercise, null for a system exercise. Immutable.
    /// </summary>
    public Id? OwnerId { get; }

    /// <summary>
    /// When the owner archived the exercise, null while it can still be added to workouts.
    /// </summary>
    public DateTime? ArchivedAtUtc { get; private set; }

    public bool IsSystem => OwnerId is null;

    public bool IsArchived => ArchivedAtUtc is not null;

    public Url? VideoUrl { get; private set; }

    public Muscles Muscle { get; private set; }

    /// <summary>
    /// Body part the muscle belongs to. Always derived from <see cref="Muscle"/>, never stored.
    /// </summary>
    public BodyParts BodyPart => MuscleBodyParts.BodyPartOf(Muscle);

    /// <summary>
    /// Kind of training the exercise belongs to. Stored, and not related to <see cref="Muscle"/> or <see cref="BodyPart"/>.
    /// </summary>
    public ExerciseTypes Type { get; private set; }

    /// <summary>
    /// Snapshot of the names of the exercise, at most one per language.
    /// </summary>
    public IReadOnlyList<ExerciseTranslation> Translations => _translations.ToList();

    private Exercise(
        Id id,
        FreeExerciseDbId? freeExerciseDbId,
        Url? videoUrl,
        Muscles muscle,
        ExerciseTypes type,
        IEnumerable<ExerciseTranslation> translations,
        Id? ownerId,
        DateTime? archivedAtUtc)
    {
        var entries = translations.ToList();

        var duplicate = entries
            .GroupBy(translation => translation.LanguageCode)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
            throw new DuplicateExerciseTranslationException(duplicate.Key.Value);

        this.Id = id;
        this.IsNew = false;
        FreeExerciseDbId = freeExerciseDbId;
        VideoUrl = videoUrl;
        Muscle = EnsureDefined(muscle); // QUADRICEPS, CHEST, etc...
        Type = EnsureDefined(type);
        _translations = entries;
        OwnerId = ownerId;
        ArchivedAtUtc = archivedAtUtc;
    }

    /// <summary>
    /// Creates a custom exercise that does not exist in the system yet, owned by the given user and named in a
    /// single language. It never comes from the free exercise dataset.
    /// </summary>
    /// <exception cref="InvalidMuscleException">When the muscle is not a defined value.</exception>
    /// <exception cref="InvalidExerciseTypeException">When the type is not a defined value.</exception>
    public static Exercise Create(Id ownerId, ExerciseTranslation translation, Muscles muscle, ExerciseTypes type, Url? videoUrl)
        => new(Id.New(), null, videoUrl, muscle, type, [translation], ownerId, null) { IsNew = true };

    /// <summary>
    /// Restores an exercise already stored in the system.
    /// </summary>
    /// <exception cref="InvalidMuscleException">When the muscle is not a defined value.</exception>
    /// <exception cref="InvalidExerciseTypeException">When the type is not a defined value.</exception>
    /// <exception cref="DuplicateExerciseTranslationException">When two translations share a language.</exception>
    public static Exercise Rehydrate(
        Id id,
        FreeExerciseDbId? freeExerciseDbId,
        Url? videoUrl,
        Muscles muscle,
        ExerciseTypes type,
        IEnumerable<ExerciseTranslation> translations,
        Id? ownerId = null,
        DateTime? archivedAtUtc = null)
        => new(id, freeExerciseDbId, videoUrl, muscle, type, translations, ownerId, archivedAtUtc);

    /// <summary>
    /// Replaces the muscle, the type and the video, and sets the name in one language: it replaces that
    /// translation when it exists and adds it otherwise. Translations in other languages are kept. Nothing
    /// changes when an input is invalid.
    /// </summary>
    /// <exception cref="SystemExerciseNotEditableException">When the exercise is a system exercise.</exception>
    /// <exception cref="ExerciseArchivedException">When the exercise is archived.</exception>
    /// <exception cref="InvalidMuscleException">When the muscle is not a defined value.</exception>
    /// <exception cref="InvalidExerciseTypeException">When the type is not a defined value.</exception>
    public void UpdateDetails(ExerciseTranslation translation, Muscles muscle, ExerciseTypes type, Url? videoUrl)
    {
        EnsureEditable();

        var validMuscle = EnsureDefined(muscle);
        var validType = EnsureDefined(type);

        Muscle = validMuscle;
        Type = validType;
        VideoUrl = videoUrl;
        _translations.RemoveAll(existing => existing.LanguageCode == translation.LanguageCode);
        _translations.Add(translation);
    }

    /// <summary>
    /// Takes the exercise out of use for new workouts and records when. Workouts that already use it keep it.
    /// </summary>
    /// <exception cref="SystemExerciseNotEditableException">When the exercise is a system exercise.</exception>
    /// <exception cref="ExerciseArchivedException">When the exercise is already archived.</exception>
    public void Archive(DateTime nowUtc)
    {
        EnsureEditable();

        ArchivedAtUtc = nowUtc;
    }

    private void EnsureEditable()
    {
        if (IsSystem)
            throw new SystemExerciseNotEditableException(Id);

        if (IsArchived)
            throw new ExerciseArchivedException(Id);
    }

    private static Muscles EnsureDefined(Muscles muscle)
    {
        if (Enum.IsDefined(muscle) == false)
            throw new InvalidMuscleException((int)muscle);

        return muscle;
    }

    private static ExerciseTypes EnsureDefined(ExerciseTypes type)
    {
        if (Enum.IsDefined(type) == false)
            throw new InvalidExerciseTypeException((int)type);

        return type;
    }
}
