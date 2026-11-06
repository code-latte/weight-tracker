using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

/// <summary>
/// A workout together with its owner and its prescribing trainer, which is <c>null</c> when it has none, so callers
/// can show their names without looking them up.
/// </summary>
public sealed record WorkoutView(Workout Workout, User Athlete, User? Trainer);
