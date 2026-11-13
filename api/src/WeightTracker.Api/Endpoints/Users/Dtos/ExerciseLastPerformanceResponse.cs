using WeightTracker.Application.UseCases.Users;

namespace WeightTracker.Api.Endpoints.Users.Dtos;

/// <param name="WorkoutDate">Date of the caller's last finished workout with set records for the exercise.</param>
/// <param name="Effort">Effort that workout prescribed for the exercise: <c>LOW</c>, <c>MIDLOW</c>, <c>MID</c>, <c>MIDHIGH</c> or <c>HIGH</c>.</param>
/// <param name="Sets">Sets recorded for the exercise in that workout, in order.</param>
public sealed record ExerciseLastPerformanceResponse(DateOnly WorkoutDate, string Effort, IReadOnlyList<LastPerformanceSetResponse> Sets)
{
    public static ExerciseLastPerformanceResponse From(ExerciseLastPerformanceView view)
        => new(view.WorkoutDate, view.Effort.ToString(), view.Sets.Select(LastPerformanceSetResponse.From).ToList());
}

/// <param name="Reps">Reps performed. Set for an exercise prescribed by reps, null otherwise.</param>
/// <param name="DurationSeconds">Seconds held. Set for an exercise prescribed by duration or minutes, null otherwise.</param>
/// <param name="Weight">Weight lifted, in kilograms. Null when the weight was not tracked, which is different from 0.</param>
public sealed record LastPerformanceSetResponse(int? Reps, int? DurationSeconds, decimal? Weight)
{
    public static LastPerformanceSetResponse From(LastPerformanceSetView view)
        => new(view.Reps, view.DurationSeconds, view.Weight);
}
