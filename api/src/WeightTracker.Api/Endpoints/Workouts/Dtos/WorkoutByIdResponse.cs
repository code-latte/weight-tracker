using WeightTracker.Api.Endpoints.Users.Dtos;
using WeightTracker.Application.UseCases.Workouts;

namespace WeightTracker.Api.Endpoints.Workouts.Dtos;

/// <summary>
/// A workout as <c>GET /workouts/{id}</c> reads it: the <see cref="WorkoutDetailResponse"/> fields plus the public
/// profiles of its athlete and of its prescribing trainer, which is <c>null</c> when the workout has none. Entries
/// reference the catalog exercise by <c>ExerciseId</c> only.
/// </summary>
/// <param name="MesocycleId">Mesocycle the workout is placed in, <c>null</c> for a standalone workout.</param>
/// <param name="MesocycleWeekNumber">Week of the mesocycle the workout is placed in, <c>null</c> for a standalone workout.</param>
/// <param name="Athlete">Owner of the workout, the user whose id is <paramref name="UserId"/>.</param>
/// <param name="Trainer">Prescribing trainer, the user whose id is <paramref name="TrainerId"/>, or <c>null</c>.</param>
public sealed record WorkoutByIdResponse(
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
    UserProfileResponse Athlete,
    UserProfileResponse? Trainer)
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
            workout.Exercises.Select(WorkoutExerciseDetailResponse.From).ToList(),
            workout.MesocycleId?.Value,
            workout.MesocycleWeekNumber?.Value,
            UserProfileResponse.From(view.Athlete),
            view.Trainer is null ? null : UserProfileResponse.From(view.Trainer));
    }
}
