using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Workouts;

/// <summary>
/// Raised by <see cref="Workout"/> every time its status becomes completed, through the lifecycle or an admin override.
/// It carries only the workout identifier: whoever reacts reloads the workout.
/// </summary>
/// <param name="WorkoutId">Workout that was completed</param>
/// <param name="OccuredOnUtc">When the workout was completed, in UTC</param>
public sealed record WorkoutCompletedEvent(Id WorkoutId, DateTime OccuredOnUtc) : IDomainEvent;
