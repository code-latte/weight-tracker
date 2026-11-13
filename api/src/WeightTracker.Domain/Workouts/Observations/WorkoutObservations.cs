using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Workouts.Observations;

/// <summary>
/// Every observation generated when a workout was completed, and which of them are highlighted. There is one per
/// workout, identified by the workout's own identifier, and it is replaced as a whole when the workout is completed again.
/// </summary>
/// <remarks>
/// It exists as an aggregate because the rule that at most <see cref="MaxHighlights"/> observations are highlighted spans
/// all of them. It references the workout by identifier only.
/// </remarks>
public sealed class WorkoutObservations : AggregateRoot
{
    public const int MaxHighlights = 3;

    private readonly List<WorkoutObservation> _observations;

    /// <summary>
    /// Workout the observations are about. It is also the identifier of this aggregate.
    /// </summary>
    public Id WorkoutId => Id;

    public IReadOnlyList<WorkoutObservation> Observations => _observations.ToList();

    /// <summary>
    /// The highlighted observations, by rank.
    /// </summary>
    public IReadOnlyList<WorkoutObservation> Highlights
        => _observations.Where(observation => observation.IsHighlighted).OrderBy(observation => observation.HighlightRank).ToList();

    private WorkoutObservations(Id workoutId, IEnumerable<WorkoutObservation> observations, bool isNew)
    {
        Id = workoutId;
        IsNew = isNew;
        _observations = observations.ToList();
    }

    /// <summary>
    /// Creates the observations of a workout that has none yet. The first highlight gets rank 1, and so on.
    /// </summary>
    /// <param name="workoutId">Workout the observations are about</param>
    /// <param name="observations">Every generated observation, none highlighted yet</param>
    /// <param name="highlights">Identifiers of the observations to highlight, most important first</param>
    /// <exception cref="InvalidWorkoutObservationException">When the highlights or the observations break an invariant.</exception>
    public static WorkoutObservations Create(Id workoutId, IEnumerable<WorkoutObservation> observations, IReadOnlyList<Id> highlights)
    {
        var entries = observations.ToList();
        Highlight(entries, highlights);

        return new WorkoutObservations(workoutId, entries, isNew: true);
    }

    /// <summary>
    /// Restores stored observations, with the ranks they were stored with.
    /// </summary>
    /// <exception cref="InvalidWorkoutObservationException">When the stored observations break an invariant.</exception>
    public static WorkoutObservations Rehydrate(Id workoutId, IEnumerable<WorkoutObservation> observations)
    {
        var entries = observations.ToList();
        EnsureValid(entries);

        return new WorkoutObservations(workoutId, entries, isNew: false);
    }

    /// <summary>
    /// Replaces every observation and highlight, as when the workout is completed again. Nothing changes when the new ones are invalid.
    /// </summary>
    /// <param name="observations">Every generated observation, none highlighted yet</param>
    /// <param name="highlights">Identifiers of the observations to highlight, most important first</param>
    /// <exception cref="InvalidWorkoutObservationException">When the highlights or the observations break an invariant.</exception>
    public void Replace(IEnumerable<WorkoutObservation> observations, IReadOnlyList<Id> highlights)
    {
        var entries = observations.ToList();
        Highlight(entries, highlights);

        _observations.Clear();
        _observations.AddRange(entries);
    }

    private static void Highlight(List<WorkoutObservation> entries, IReadOnlyList<Id> highlights)
    {
        if (entries.Any(entry => entry.IsHighlighted))
            throw new InvalidWorkoutObservationException("observations must be given without a highlight rank");

        EnsureUnique(entries);

        if (highlights.Count > MaxHighlights)
            throw new InvalidWorkoutObservationException($"at most {MaxHighlights} observations can be highlighted, got {highlights.Count}");

        if (highlights.Distinct().Count() != highlights.Count)
            throw new InvalidWorkoutObservationException("an observation cannot be highlighted twice");

        var ranked = highlights
            .Select(id => entries.FirstOrDefault(entry => entry.Id == id)
                ?? throw new InvalidWorkoutObservationException($"highlighted observation {id} is not one of the observations"))
            .ToList();

        if (ranked.Any(entry => entry.Priority is null))
            throw new InvalidWorkoutObservationException("an observation without priority cannot be highlighted");

        for (var index = 0; index < ranked.Count; index++)
            ranked[index].Highlight(index + 1);
    }

    private static void EnsureValid(List<WorkoutObservation> entries)
    {
        EnsureUnique(entries);

        var ranks = entries.Where(entry => entry.IsHighlighted).ToList();

        if (ranks.Any(entry => entry.Priority is null))
            throw new InvalidWorkoutObservationException("an observation without priority cannot be highlighted");

        var expected = Enumerable.Range(1, ranks.Count);

        if (ranks.Count > MaxHighlights || ranks.Select(entry => entry.HighlightRank!.Value).Order().SequenceEqual(expected) == false)
            throw new InvalidWorkoutObservationException($"highlight ranks must be unique, start at 1 and be at most {MaxHighlights}");
    }

    private static void EnsureUnique(List<WorkoutObservation> entries)
    {
        if (entries.Select(entry => entry.Id).Distinct().Count() != entries.Count)
            throw new InvalidWorkoutObservationException("observation identifiers must be unique");

        if (entries.Select(entry => (entry.Code, entry.ExerciseId)).Distinct().Count() != entries.Count)
            throw new InvalidWorkoutObservationException("there can be only one observation per code and exercise");
    }
}
