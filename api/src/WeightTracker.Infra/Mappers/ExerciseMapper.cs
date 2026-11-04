using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Exceptions;

namespace WeightTracker.Infra.Mappers;

/// <summary>
/// Maps the Exercise aggregate across the two tables it spans: <c>Exercises</c> and <c>ExerciseTranslations</c>.
/// </summary>
public static class ExerciseMapper
{
    public static ExerciseEntity MapToEntity(Exercise exercise)
    {
        var entity = new ExerciseEntity
        {
            Id = exercise.Id.Value,
            FreeExerciseDbId = exercise.FreeExerciseDbId?.Value,
            VideoUrl = exercise.VideoUrl?.Value,
            Muscle = (int)exercise.Muscle,
            Type = (int)exercise.Type,
            OwnerId = exercise.OwnerId?.Value,
            ArchivedAtUtc = exercise.ArchivedAtUtc is { } archivedAt
                ? DateTime.SpecifyKind(archivedAt, DateTimeKind.Utc)
                : null,
        };

        foreach (var translation in exercise.Translations)
        {
            entity.Translations.Add(new ExerciseTranslationEntity
            {
                Id = translation.Id.Value,
                ExerciseId = entity.Id,
                LanguageCode = translation.LanguageCode.Value,
                Name = translation.Name.Value,
            });
        }

        return entity;
    }

    /// <remarks>
    /// Translations carry no stored order, so they are restored sorted by language code to keep reads stable.
    /// </remarks>
    public static Exercise MapToDomain(ExerciseEntity entity)
    {
        try
        {
            return Exercise.Rehydrate(
                Id.From(entity.Id),
                entity.FreeExerciseDbId is null ? null : FreeExerciseDbId.From(entity.FreeExerciseDbId),
                entity.VideoUrl is null ? null : Url.From(entity.VideoUrl),
                MapMuscle(entity.Muscle),
                MapType(entity.Type),
                entity.Translations
                    .OrderBy(translation => translation.LanguageCode, StringComparer.Ordinal)
                    .Select(translation => ExerciseTranslation.Rehydrate(
                        Id.From(translation.Id),
                        LanguageCode.From(translation.LanguageCode),
                        ExerciseName.From(translation.Name)))
                    .ToList(),
                entity.OwnerId is null ? null : Id.From(entity.OwnerId.Value),
                entity.ArchivedAtUtc);
        }
        catch (DomainException exception)
        {
            throw new PersistenceMappingException(
                $"Stored exercise '{entity.Id}' could not be restored", exception);
        }
    }

    private static Muscles MapMuscle(int muscle)
    {
        if (Enum.IsDefined(typeof(Muscles), muscle) == false)
            throw new PersistenceMappingException($"'{muscle}' is not a known muscle");

        return (Muscles)muscle;
    }

    private static ExerciseTypes MapType(int type)
    {
        if (Enum.IsDefined(typeof(ExerciseTypes), type) == false)
            throw new PersistenceMappingException($"'{type}' is not a known exercise type");

        return (ExerciseTypes)type;
    }
}
