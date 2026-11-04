using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using WeightTracker.Domain.Exercises;

namespace WeightTracker.Api.Endpoints.Exercises.Dtos;

/// <summary>
/// Body of <c>POST /exercises</c> and <c>PUT /exercises/{id}</c>. Only structural validation lives here; the value
/// rules of the name, the language and the video belong to the domain.
/// </summary>
public record ExerciseRequest
{
    /// <summary>
    /// Language of <see cref="Name"/>, as a two letter code such as <c>es</c> or <c>en</c>. On creation it is the
    /// only translation. On update it replaces the name in that language, or adds it, and keeps the others.
    /// </summary>
    [Required]
    public string? LanguageCode { get; init; }

    /// <summary>
    /// Name of the exercise in <see cref="LanguageCode"/>.
    /// </summary>
    [Required]
    public string? Name { get; init; }

    /// <summary>
    /// Muscle the exercise works, by name (for example <c>MIDDLE_BACK</c>) or number. The body part is derived from it.
    /// </summary>
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter<Muscles>))]
    public Muscles? Muscle { get; init; }

    /// <summary>
    /// Kind of training: <c>STRENGTH</c>, <c>CARDIO</c> or <c>STRETCHING</c>, or their number.
    /// </summary>
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter<ExerciseTypes>))]
    public ExerciseTypes? Type { get; init; }

    /// <summary>
    /// Optional link to a video of the exercise.
    /// </summary>
    public string? VideoUrl { get; init; }
}
