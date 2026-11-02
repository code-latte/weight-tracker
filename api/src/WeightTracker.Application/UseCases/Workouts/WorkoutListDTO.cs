using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

/// <summary>
/// Workouts the caller may see, plus the exercises and the users (owners and trainers) they reference, keyed by identifier.
/// </summary>
/// <remarks>
/// The Workout aggregate references exercises and users by identifier only, so the read joins them here.
/// </remarks>
public sealed record WorkoutListDTO(
    IReadOnlyList<Workout> Workouts,
    IReadOnlyDictionary<Id, Exercise> Exercises,
    IReadOnlyDictionary<Id, User> Users);
