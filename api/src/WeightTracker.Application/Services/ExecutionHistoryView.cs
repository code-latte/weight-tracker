using WeightTracker.Domain.Common;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Workouts.Observations;

namespace WeightTracker.Application.Services;

/// <summary>
/// What the observation rules need to know besides the workout itself: the type of the mesocycle week it sits in and,
/// per exercise, what the athlete did in earlier sessions with the same trainer. An exercise they never did is left out.
/// </summary>
public sealed record ExecutionHistoryView(
    TrainingWeekType? WeekType,
    IReadOnlyDictionary<Id, ExerciseHistory> Exercises);
