using WeightTracker.Application.UseCases.Users;
using WeightTracker.Domain.Common;

namespace WeightTracker.Application.UseCases.Workouts;

/// <summary>
/// The previous execution of one exercise: the finished workout where its owner did it before the one being read, with
/// that workout's date and the sets recorded for the exercise in it, in order. The values are the raw recorded ones;
/// the use case turns them into domain values to rank them.
/// </summary>
public sealed record ExercisePreviousExecutionView(Id ExerciseId, Id WorkoutId, DateOnly WorkoutDate, IReadOnlyList<LastPerformanceSetView> Sets);
