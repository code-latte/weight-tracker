using System.Text.Json.Serialization;
using WeightTracker.Api.Endpoints.Exercises.Dtos;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Api.Endpoints.Workouts.Dtos;

/// <param name="MesocycleId">Mesocycle the workout is placed in, <c>null</c> for a standalone workout.</param>
/// <param name="MesocycleWeekNumber">Week of the mesocycle the workout is placed in, <c>null</c> for a standalone workout.</param>
public sealed record WorkoutResponse(
    Guid Id,
    string Name,
    string? Description,
    DateOnly WorkoutDate,
    Guid UserId,
    Guid? TrainerId,
    string Status,
    IReadOnlyList<WorkoutExerciseResponse> Exercises,
    Guid? MesocycleId,
    int? MesocycleWeekNumber)
{
    /// <param name="workout">Workout to project.</param>
    /// <param name="exercises">Exercises referenced by the workout, keyed by identifier.</param>
    public static WorkoutResponse From(Workout workout, IReadOnlyDictionary<Id, Exercise> exercises)
        => new(
            workout.Id.Value,
            workout.Name.Value,
            workout.Description?.Value,
            DateOnly.FromDateTime(workout.WorkoutDate),
            workout.UserId.Value,
            workout.TrainerId?.Value,
            workout.Status.ToString(),
            workout.Exercises
                .Select(entry => WorkoutExerciseResponse.From(entry, exercises[entry.ExerciseId]))
                .ToList(),
            workout.MesocycleId?.Value,
            workout.MesocycleWeekNumber?.Value);
}

/// <summary>
/// A single workout as read by id. Entries reference the catalog exercise by <c>ExerciseId</c> only:
/// the client already holds the catalog, so no exercise data is embedded.
/// </summary>
/// <param name="MesocycleId">Mesocycle the workout is placed in, <c>null</c> for a standalone workout.</param>
/// <param name="MesocycleWeekNumber">Week of the mesocycle the workout is placed in, <c>null</c> for a standalone workout.</param>
public sealed record WorkoutDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    DateOnly WorkoutDate,
    Guid UserId,
    Guid? TrainerId,
    string Status,
    IReadOnlyList<WorkoutExerciseDetailResponse> Exercises,
    Guid? MesocycleId,
    int? MesocycleWeekNumber)
{
    public static WorkoutDetailResponse From(Workout workout)
        => new(
            workout.Id.Value,
            workout.Name.Value,
            workout.Description?.Value,
            DateOnly.FromDateTime(workout.WorkoutDate),
            workout.UserId.Value,
            workout.TrainerId?.Value,
            workout.Status.ToString(),
            workout.Exercises.Select(WorkoutExerciseDetailResponse.From).ToList(),
            workout.MesocycleId?.Value,
            workout.MesocycleWeekNumber?.Value);
}

/// <param name="Id">Identifier of this entry of the workout.</param>
/// <param name="ExerciseId">Identifier of the catalog exercise performed in this entry.</param>
/// <param name="PerSide">Whether the targets of the sets are per side (1x8 reps means 8 on each side).</param>
/// <param name="RestMinutes">Rest between sets in minutes, <c>null</c> when none is prescribed.</param>
/// <param name="SuperSeriesId">Superseries this entry belongs to, <c>null</c> when it is performed on its own.</param>
public sealed record WorkoutExerciseDetailResponse(
    Guid Id,
    Guid ExerciseId,
    string Effort,
    bool PerSide,
    decimal? RestMinutes,
    IReadOnlyList<SetResponse> Sets,
    int? SuperSeriesId)
{
    public static WorkoutExerciseDetailResponse From(WorkoutExercise entry)
        => new(
            entry.Id.Value,
            entry.ExerciseId.Value,
            entry.Effort.ToString(),
            entry.PerSide,
            entry.Rest?.Value,
            entry.Sets.Select(SetResponse.From).ToList(),
            entry.SuperSeriesId?.Value);
}

/// <param name="Id">Identifier of this entry of the workout, used to address it (for example to group it in a superseries).</param>
/// <param name="ExerciseId">Identifier of the catalog exercise performed in this entry.</param>
/// <param name="Translations">Names of the exercise, one per language.</param>
/// <param name="BodyPart">Body part derived from <paramref name="Muscle"/>.</param>
/// <param name="Type">Kind of training of the exercise, independent of <paramref name="Muscle"/>.</param>
/// <param name="PerSide">Whether the targets of the sets are per side (1x8 reps means 8 on each side).</param>
/// <param name="RestMinutes">Rest between sets in minutes, <c>null</c> when none is prescribed.</param>
/// <param name="SuperSeriesId">Superseries this entry belongs to, <c>null</c> when it is performed on its own.</param>
public sealed record WorkoutExerciseResponse(
    Guid Id,
    Guid ExerciseId,
    IReadOnlyList<ExerciseTranslationResponse> Translations,
    string Muscle,
    string BodyPart,
    string Type,
    string Effort,
    bool PerSide,
    decimal? RestMinutes,
    IReadOnlyList<SetResponse> Sets,
    int? SuperSeriesId)
{
    public static WorkoutExerciseResponse From(WorkoutExercise entry, Exercise exercise)
        => new(
            entry.Id.Value,
            exercise.Id.Value,
            exercise.Translations.Select(ExerciseTranslationResponse.From).ToList(),
            exercise.Muscle.ToString(),
            exercise.BodyPart.ToString(),
            exercise.Type.ToString(),
            entry.Effort.ToString(),
            entry.PerSide,
            entry.Rest?.Value,
            entry.Sets.Select(SetResponse.From).ToList(),
            entry.SuperSeriesId?.Value);
}

public sealed record SetResponse(int Count, SetTargetResponse Target)
{
    public static SetResponse From(SetPrescription prescription)
        => new(prescription.count, SetTargetResponse.From(prescription.target));
}

/// <summary>
/// Discriminated projection of <see cref="SetTarget"/>.
/// </summary>
public sealed record SetTargetResponse(string Type)
{
    /// <summary>Omitted entirely for <c>MaxReps</c>, which carries no value.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Value { get; init; }

    /// <summary>Upper bound of a <c>RepsRange</c>, whose lower bound is <see cref="Value"/>. Omitted for every other type.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxValue { get; init; }

    /// <summary>Minutes of a <c>Minutes</c> target, with up to two decimals. Omitted for every other type.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? Minutes { get; init; }

    public static SetTargetResponse From(SetTarget target) => target switch
    {
        SetTarget.Reps reps => new SetTargetResponse(SetTargetTypes.Reps) { Value = reps.value },
        SetTarget.Duration duration => new SetTargetResponse(SetTargetTypes.Duration) { Value = duration.value },
        SetTarget.RepsRange range => new SetTargetResponse(SetTargetTypes.RepsRange) { Value = range.min, MaxValue = range.max },
        SetTarget.Minutes minutes => new SetTargetResponse(SetTargetTypes.Minutes) { Minutes = minutes.value.Value },
        _ => new SetTargetResponse(SetTargetTypes.MaxReps),
    };
}
