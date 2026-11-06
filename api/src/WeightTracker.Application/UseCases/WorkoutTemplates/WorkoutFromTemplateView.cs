using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.WorkoutTemplates;

/// <summary>
/// A workout just created from a template, with the catalog exercises its copied entries reference.
/// </summary>
public sealed record WorkoutFromTemplateView(Workout Workout, IReadOnlyDictionary<Id, Exercise> Exercises);
