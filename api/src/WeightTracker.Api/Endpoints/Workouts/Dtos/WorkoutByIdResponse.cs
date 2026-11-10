using WeightTracker.Api.Endpoints.Exercises.Dtos;
using WeightTracker.Api.Endpoints.Mesocycles.Dtos;
using WeightTracker.Api.Endpoints.Users.Dtos;
using WeightTracker.Application.UseCases.Workouts;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Api.Endpoints.Workouts.Dtos;

/// <summary>
/// A workout as <c>GET /workouts/{id}</c> reads it: the <see cref="WorkoutDetailResponse"/> fields plus the public
/// profiles of its athlete and of its prescribing trainer, which is <c>null</c> when the workout has none, the catalog
/// data of the exercise of every entry and the mesocycle it is placed in, so the workout can be shown without further
/// requests.
/// </summary>
/// <param name="MesocycleId">Mesocycle the workout is placed in, <c>null</c> for a standalone workout.</param>
/// <param name="MesocycleWeekNumber">Week of the mesocycle the workout is placed in, <c>null</c> for a standalone workout.</param>
/// <param name="CompletedAt">Last time the workout reached <c>COMPLETED</c>, in UTC, <c>null</c> until it is first completed. Kept when it leaves completed.</param>
/// <param name="ReviewedAt">When the workout was reviewed, in UTC, <c>null</c> until it is reviewed.</param>
/// <param name="ReviewedBy">User who reviewed the workout, <c>null</c> until it is reviewed.</param>
/// <param name="ResultsUpdatedAt">Last time its set records changed, in UTC, <c>null</c> while they never changed. Send it back when reviewing.</param>
/// <param name="Athlete">Owner of the workout, the user whose id is <paramref name="UserId"/>.</param>
/// <param name="Trainer">Prescribing trainer, the user whose id is <paramref name="TrainerId"/>, or <c>null</c>.</param>
/// <param name="Mesocycle">Mesocycle whose id is <paramref name="MesocycleId"/>, with its weeks, or <c>null</c> for a standalone workout.</param>
public sealed record WorkoutByIdResponse(
    Guid Id,
    string Name,
    string? Description,
    DateOnly WorkoutDate,
    Guid UserId,
    Guid? TrainerId,
    string Status,
    IReadOnlyList<WorkoutByIdExerciseResponse> Exercises,
    Guid? MesocycleId,
    int? MesocycleWeekNumber,
    DateTime? CompletedAt,
    DateTime? ReviewedAt,
    Guid? ReviewedBy,
    DateTime? ResultsUpdatedAt,
    UserProfileResponse Athlete,
    UserProfileResponse? Trainer,
    WorkoutMesocycleResponse? Mesocycle)
{
    public static WorkoutByIdResponse From(WorkoutView view)
    {
        var workout = view.Workout;

        return new(
            workout.Id.Value,
            workout.Name.Value,
            workout.Description?.Value,
            DateOnly.FromDateTime(workout.WorkoutDate),
            workout.UserId.Value,
            workout.TrainerId?.Value,
            workout.Status.ToString(),
            workout.Exercises
                .Select(entry => WorkoutByIdExerciseResponse.From(entry, view.Exercises[entry.ExerciseId]))
                .ToList(),
            workout.MesocycleId?.Value,
            workout.MesocycleWeekNumber?.Value,
            workout.CompletedAt,
            workout.ReviewedAt,
            workout.ReviewedBy?.Value,
            workout.ResultsUpdatedAt,
            UserProfileResponse.From(view.Athlete),
            view.Trainer is null ? null : UserProfileResponse.From(view.Trainer),
            view.Mesocycle is null ? null : WorkoutMesocycleResponse.From(view.Mesocycle));
    }
}

/// <summary>
/// An entry as <c>GET /workouts/{id}</c> reads it: the <see cref="WorkoutExerciseDetailResponse"/> fields plus the
/// catalog data of its exercise, as <see cref="WorkoutExerciseResponse"/> carries it.
/// </summary>
/// <param name="Id">Identifier of this entry of the workout.</param>
/// <param name="ExerciseId">Identifier of the catalog exercise performed in this entry.</param>
/// <param name="Translations">Names of the exercise, one per language.</param>
/// <param name="VideoUrl">Link to a video showing the exercise, <c>null</c> when it has none.</param>
/// <param name="BodyPart">Body part derived from <paramref name="Muscle"/>.</param>
/// <param name="Type">Kind of training of the exercise, independent of <paramref name="Muscle"/>.</param>
/// <param name="PerSide">Whether the targets of the sets are per side (1x8 reps means 8 on each side).</param>
/// <param name="RestMinutes">Rest between sets in minutes, <c>null</c> when none is prescribed.</param>
/// <param name="SuperSeriesId">Superseries this entry belongs to, <c>null</c> when it is performed on its own.</param>
/// <param name="SetRecords">Sets actually performed, in their order.</param>
public sealed record WorkoutByIdExerciseResponse(
    Guid Id,
    Guid ExerciseId,
    IReadOnlyList<ExerciseTranslationResponse> Translations,
    string? VideoUrl,
    string Muscle,
    string BodyPart,
    string Type,
    string Effort,
    bool PerSide,
    decimal? RestMinutes,
    IReadOnlyList<SetResponse> Sets,
    int? SuperSeriesId,
    IReadOnlyList<SetRecordResponse> SetRecords)
{
    public static WorkoutByIdExerciseResponse From(WorkoutExercise entry, Exercise exercise)
        => new(
            entry.Id.Value,
            exercise.Id.Value,
            exercise.Translations.Select(ExerciseTranslationResponse.From).ToList(),
            exercise.VideoUrl?.Value,
            exercise.Muscle.ToString(),
            exercise.BodyPart.ToString(),
            exercise.Type.ToString(),
            entry.Effort.ToString(),
            entry.PerSide,
            entry.Rest?.Value,
            entry.Sets.Select(SetResponse.From).ToList(),
            entry.SuperSeriesId?.Value,
            entry.SetRecords.Select(SetRecordResponse.From).ToList());
}

/// <summary>
/// The mesocycle a workout is placed in, with its defined weeks ordered by week number. Its trainer and athlete are
/// left out, since the workout already carries them.
/// </summary>
public sealed record WorkoutMesocycleResponse(
    Guid Id,
    string Name,
    string Objective,
    DateOnly StartDate,
    int DurationInWeeks,
    IReadOnlyList<MesocycleWeekResponse> Weeks)
{
    public static WorkoutMesocycleResponse From(Mesocycle mesocycle)
        => new(
            mesocycle.Id.Value,
            mesocycle.Name.Value,
            mesocycle.Objective.Value,
            DateOnly.FromDateTime(mesocycle.StartDate),
            mesocycle.DurationInWeeks.Value,
            mesocycle.Weeks.Select(MesocycleWeekResponse.From).ToList());
}
