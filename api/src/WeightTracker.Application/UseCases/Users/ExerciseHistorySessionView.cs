using WeightTracker.Domain.Common;

namespace WeightTracker.Application.UseCases.Users;

/// <summary>
/// One finished session of an exercise: the workout and date it was done, the heaviest weight lifted, the volume
/// (reps × kg) and the best estimated one-rep max. Each number is <c>null</c> when no set of the session has reps and
/// a weight, or no weight at all for the max weight.
/// </summary>
public sealed record ExerciseHistorySessionView(
    Id WorkoutId,
    DateOnly WorkoutDate,
    decimal? MaxWeight,
    decimal? Volume,
    decimal? EstimatedOneRepMax);
