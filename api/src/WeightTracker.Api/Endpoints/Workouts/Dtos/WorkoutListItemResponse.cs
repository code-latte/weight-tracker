using WeightTracker.Api.Endpoints.Users.Dtos;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Api.Endpoints.Workouts.Dtos;

/// <summary>
/// A workout as <c>GET /workouts</c> lists it: the <see cref="WorkoutResponse"/> fields plus the public profiles of
/// its athlete and of its prescribing trainer, which is <c>null</c> when the workout has none.
/// </summary>
/// <param name="Athlete">Owner of the workout, the user whose id is <paramref name="UserId"/>.</param>
/// <param name="Trainer">Prescribing trainer, the user whose id is <paramref name="TrainerId"/>, or <c>null</c>.</param>
/// <param name="MesocycleId">Mesocycle the workout is placed in, <c>null</c> for a standalone workout.</param>
/// <param name="MesocycleWeekNumber">Week of the mesocycle the workout is placed in, <c>null</c> for a standalone workout.</param>
/// <param name="CompletedAt">Last time the workout reached <c>COMPLETED</c>, in UTC, <c>null</c> until it is first completed. Kept when it leaves completed.</param>
/// <param name="ReviewedAt">When the workout was reviewed, in UTC, <c>null</c> until it is reviewed.</param>
/// <param name="ReviewedBy">User who reviewed the workout, <c>null</c> until it is reviewed.</param>
/// <param name="ResultsUpdatedAt">Last time its set records changed, in UTC, <c>null</c> while they never changed. Send it back when reviewing.</param>
public sealed record WorkoutListItemResponse(
    Guid Id,
    string Name,
    string? Description,
    DateOnly WorkoutDate,
    Guid UserId,
    Guid? TrainerId,
    string Status,
    IReadOnlyList<WorkoutExerciseResponse> Exercises,
    UserProfileResponse Athlete,
    UserProfileResponse? Trainer,
    Guid? MesocycleId,
    int? MesocycleWeekNumber,
    DateTime? CompletedAt,
    DateTime? ReviewedAt,
    Guid? ReviewedBy,
    DateTime? ResultsUpdatedAt)
{
    /// <param name="workout">Workout to project.</param>
    /// <param name="exercises">Exercises referenced by the workout, keyed by identifier.</param>
    /// <param name="users">Owner and trainer of the workout, keyed by identifier.</param>
    public static WorkoutListItemResponse From(
        Workout workout,
        IReadOnlyDictionary<Id, Exercise> exercises,
        IReadOnlyDictionary<Id, User> users)
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
            UserProfileResponse.From(users[workout.UserId]),
            workout.TrainerId is null ? null : UserProfileResponse.From(users[workout.TrainerId]),
            workout.MesocycleId?.Value,
            workout.MesocycleWeekNumber?.Value,
            workout.CompletedAt,
            workout.ReviewedAt,
            workout.ReviewedBy?.Value,
            workout.ResultsUpdatedAt);
}
