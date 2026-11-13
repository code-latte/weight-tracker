using WeightTracker.Application.UseCases.Users;

namespace WeightTracker.Api.Endpoints.Users.Dtos;

/// <param name="WorkoutId">Identifier of the finished workout of this session.</param>
/// <param name="WorkoutDate">Date of that workout.</param>
/// <param name="MaxWeight">Heaviest weight lifted in the exercise during the session, in kilograms. Null when no set had a weight.</param>
/// <param name="Volume">Sum of reps × kilograms over the sets of the session. Null when no set had both reps and a weight.</param>
/// <param name="EstimatedOneRepMax">Best estimated one-rep max of the session, <c>weight × (1 + reps / 30)</c> rounded to two decimals. Null when no set had both reps and a weight.</param>
public sealed record ExerciseHistorySessionResponse(
    Guid WorkoutId,
    DateOnly WorkoutDate,
    decimal? MaxWeight,
    decimal? Volume,
    decimal? EstimatedOneRepMax)
{
    public static ExerciseHistorySessionResponse From(ExerciseHistorySessionView view)
        => new(view.WorkoutId.Value, view.WorkoutDate, view.MaxWeight, view.Volume, view.EstimatedOneRepMax);
}
