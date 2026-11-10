using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

/// <summary>
/// A workout together with its owner, its prescribing trainer, which is <c>null</c> when it has none, the exercises its
/// entries reference, keyed by identifier, and the mesocycle it is placed in, which is <c>null</c> for a standalone
/// workout, so callers can show them without looking them up.
/// </summary>
public sealed record WorkoutView(
    Workout Workout,
    User Athlete,
    User? Trainer,
    IReadOnlyDictionary<Id, Exercise> Exercises,
    Mesocycle? Mesocycle);
