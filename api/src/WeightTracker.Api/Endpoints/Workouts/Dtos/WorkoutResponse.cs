using System.Text.Json.Serialization;
using WeightTracker.Api.Endpoints.Exercises.Dtos;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Api.Endpoints.Workouts.Dtos;

/// <param name="MesocycleId">Mesocycle the workout is placed in, <c>null</c> for a standalone workout.</param>
/// <param name="MesocycleWeekNumber">Week of the mesocycle the workout is placed in, <c>null</c> for a standalone workout.</param>
/// <param name="CompletedAt">Last time the workout reached <c>COMPLETED</c>, in UTC, <c>null</c> until it is first completed. Kept when it leaves completed.</param>
/// <param name="ReviewedAt">When the workout was reviewed, in UTC, <c>null</c> until it is reviewed.</param>
/// <param name="ReviewedBy">User who reviewed the workout, <c>null</c> until it is reviewed.</param>
/// <param name="ResultsUpdatedAt">Last time its set records changed, in UTC, <c>null</c> while they never changed. Send it back when reviewing.</param>
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
    int? MesocycleWeekNumber,
    DateTime? CompletedAt,
    DateTime? ReviewedAt,
    Guid? ReviewedBy,
    DateTime? ResultsUpdatedAt)
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
            workout.MesocycleWeekNumber?.Value,
            workout.CompletedAt,
            workout.ReviewedAt,
            workout.ReviewedBy?.Value,
            workout.ResultsUpdatedAt);
}

/// <summary>
/// A single workout as read by id. Entries reference the catalog exercise by <c>ExerciseId</c> only:
/// the client already holds the catalog, so no exercise data is embedded.
/// </summary>
/// <param name="MesocycleId">Mesocycle the workout is placed in, <c>null</c> for a standalone workout.</param>
/// <param name="MesocycleWeekNumber">Week of the mesocycle the workout is placed in, <c>null</c> for a standalone workout.</param>
/// <param name="CompletedAt">Last time the workout reached <c>COMPLETED</c>, in UTC, <c>null</c> until it is first completed. Kept when it leaves completed.</param>
/// <param name="ReviewedAt">When the workout was reviewed, in UTC, <c>null</c> until it is reviewed.</param>
/// <param name="ReviewedBy">User who reviewed the workout, <c>null</c> until it is reviewed.</param>
/// <param name="ResultsUpdatedAt">Last time its set records changed, in UTC, <c>null</c> while they never changed. Send it back when reviewing.</param>
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
    int? MesocycleWeekNumber,
    DateTime? CompletedAt,
    DateTime? ReviewedAt,
    Guid? ReviewedBy,
    DateTime? ResultsUpdatedAt)
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
            workout.MesocycleWeekNumber?.Value,
            workout.CompletedAt,
            workout.ReviewedAt,
            workout.ReviewedBy?.Value,
            workout.ResultsUpdatedAt);
}

/// <param name="Id">Identifier of this entry of the workout.</param>
/// <param name="ExerciseId">Identifier of the catalog exercise performed in this entry.</param>
/// <param name="PerSide">Whether the targets of the sets are per side (1x8 reps means 8 on each side).</param>
/// <param name="RestMinutes">Rest between sets in minutes, <c>null</c> when none is prescribed.</param>
/// <param name="SuperSeriesId">Superseries this entry belongs to, <c>null</c> when it is performed on its own.</param>
/// <param name="SetRecords">Sets actually performed, in their order. Always empty for a workout template.</param>
public sealed record WorkoutExerciseDetailResponse(
    Guid Id,
    Guid ExerciseId,
    string Effort,
    bool PerSide,
    decimal? RestMinutes,
    IReadOnlyList<SetResponse> Sets,
    int? SuperSeriesId,
    IReadOnlyList<SetRecordResponse> SetRecords)
{
    public static WorkoutExerciseDetailResponse From(WorkoutExercise entry)
        => new(
            entry.Id.Value,
            entry.ExerciseId.Value,
            entry.Effort.ToString(),
            entry.PerSide,
            entry.Rest?.Value,
            entry.Sets.Select(SetResponse.From).ToList(),
            entry.SuperSeriesId?.Value,
            entry.SetRecords.Select(SetRecordResponse.From).ToList());
}

/// <param name="Id">Identifier of the set record, used to edit, remove or reorder it.</param>
/// <param name="Type">What the set measured: <c>Reps</c> or <c>Duration</c>. It follows how the exercise is prescribed.</param>
/// <param name="Reps">Reps actually performed. Set when <c>Type</c> is <c>Reps</c>, null otherwise.</param>
/// <param name="DurationSeconds">Seconds actually held. Set when <c>Type</c> is <c>Duration</c>, null otherwise.</param>
/// <param name="Weight">Weight lifted, in kilograms. Null when the weight is not tracked.</param>
/// <param name="RegisteredAtUtc">When the set was registered, in UTC.</param>
public sealed record SetRecordResponse(Guid Id, string Type, int? Reps, int? DurationSeconds, decimal? Weight, DateTime RegisteredAtUtc)
{
    public static SetRecordResponse From(SetRecord setRecord)
        => setRecord.Result switch
        {
            SetResult.Reps reps => new(setRecord.Id.Value, SetResultTypes.Reps, reps.Count.Value, null, setRecord.Weight?.Value, setRecord.RegisteredAtUtc),
            SetResult.Duration duration => new(setRecord.Id.Value, SetResultTypes.Duration, null, duration.Value.Value, setRecord.Weight?.Value, setRecord.RegisteredAtUtc),
            _ => throw new ArgumentOutOfRangeException(nameof(setRecord), setRecord.Result, "Unknown set result kind"),
        };
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
