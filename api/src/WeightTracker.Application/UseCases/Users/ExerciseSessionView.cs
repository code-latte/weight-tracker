using WeightTracker.Domain.Common;

namespace WeightTracker.Application.UseCases.Users;

/// <summary>
/// One session of an exercise: a finished workout that holds sets of it, with the workout's date and the raw values of
/// those sets, in order. The use case turns them into domain values to compute the session's numbers.
/// </summary>
public sealed record ExerciseSessionView(Id WorkoutId, DateOnly WorkoutDate, IReadOnlyList<LastPerformanceSetView> Sets);
