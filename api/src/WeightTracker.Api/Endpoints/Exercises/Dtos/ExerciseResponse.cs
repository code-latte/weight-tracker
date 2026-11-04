using WeightTracker.Domain.Exercises;

namespace WeightTracker.Api.Endpoints.Exercises.Dtos;

/// <param name="OwnerId">User who created the exercise, <c>null</c> for a system exercise.</param>
/// <param name="ArchivedAtUtc">When the owner archived the exercise, <c>null</c> while it can still be added to workouts.</param>
public sealed record ExerciseResponse(
    Guid Id,
    string? FreeExerciseDbId,
    string? VideoUrl,
    string Muscle,
    string BodyPart,
    string Type,
    IReadOnlyList<ExerciseTranslationResponse> Translations,
    Guid? OwnerId,
    DateTime? ArchivedAtUtc)
{
    public static ExerciseResponse From(Exercise exercise)
        => new(
            exercise.Id.Value,
            exercise.FreeExerciseDbId?.Value,
            exercise.VideoUrl?.Value,
            exercise.Muscle.ToString(),
            exercise.BodyPart.ToString(),
            exercise.Type.ToString(),
            exercise.Translations.Select(ExerciseTranslationResponse.From).ToList(),
            exercise.OwnerId?.Value,
            exercise.ArchivedAtUtc);
}

public sealed record ExerciseTranslationResponse(string LanguageCode, string Name)
{
    public static ExerciseTranslationResponse From(ExerciseTranslation translation)
        => new(translation.LanguageCode.Value, translation.Name.Value);
}
