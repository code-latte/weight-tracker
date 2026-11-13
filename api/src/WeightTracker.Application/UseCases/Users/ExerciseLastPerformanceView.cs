using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Users;

/// <summary>
/// What a user did the last time they finished a workout with an exercise: the date of that workout, the effort it
/// prescribed for the exercise, and the sets they recorded in it, in order.
/// </summary>
public sealed record ExerciseLastPerformanceView(DateOnly WorkoutDate, Effort Effort, IReadOnlyList<LastPerformanceSetView> Sets);

/// <summary>
/// One recorded set. Exactly one of <paramref name="Reps"/> and <paramref name="DurationSeconds"/> is set.
/// <paramref name="Weight"/> is <c>null</c> when no weight was tracked, which is different from a weight of 0.
/// </summary>
public sealed record LastPerformanceSetView(int? Reps, int? DurationSeconds, decimal? Weight);
