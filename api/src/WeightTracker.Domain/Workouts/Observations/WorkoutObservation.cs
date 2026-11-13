using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Workouts.Observations;

/// <summary>
/// One measurable fact about a completed workout, with the numbers it was calculated from. It states a fact, never a
/// diagnosis or a recommendation.
/// </summary>
/// <remarks>
/// Entity of the WorkoutObservations aggregate. It is created and highlighted only through its root.
/// </remarks>
public sealed class WorkoutObservation : Entity
{
    /// <summary>
    /// Exercise the fact is about. Null when it is about the whole workout.
    /// </summary>
    public Id? ExerciseId { get; }

    public ObservationCode Code { get; }

    public ObservationSeverity Severity { get; }

    /// <summary>
    /// How much attention it deserves. Null when it is not eligible to be highlighted.
    /// </summary>
    public HighlightPriority? Priority { get; }

    /// <summary>
    /// Position among the highlights, starting at 1. Null when it is not highlighted.
    /// </summary>
    public int? HighlightRank { get; private set; }

    public bool IsHighlighted => HighlightRank is not null;

    public ObservationEvidence Evidence { get; }

    /// <summary>
    /// When the observation was generated, in UTC.
    /// </summary>
    public DateTime CreatedAt { get; }

    private WorkoutObservation(
        Id id,
        Id? exerciseId,
        ObservationCode code,
        ObservationSeverity severity,
        HighlightPriority? priority,
        int? highlightRank,
        ObservationEvidence evidence,
        DateTime createdAt,
        bool isNew)
    {
        if (Enum.IsDefined(code) == false)
            throw new InvalidWorkoutObservationException($"code {(int)code} is not defined");

        if (Enum.IsDefined(severity) == false)
            throw new InvalidWorkoutObservationException($"severity {(int)severity} is not defined");

        if (priority is not null && Enum.IsDefined(priority.Value) == false)
            throw new InvalidWorkoutObservationException($"priority {(int)priority.Value} is not defined");

        Id = id;
        IsNew = isNew;
        ExerciseId = exerciseId;
        Code = code;
        Severity = severity;
        Priority = priority;
        HighlightRank = highlightRank;
        Evidence = evidence;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Creates an observation that does not exist in the system yet, not highlighted.
    /// </summary>
    /// <exception cref="InvalidWorkoutObservationException">When the code, severity or priority is not a defined value.</exception>
    public static WorkoutObservation Create(
        Id? exerciseId,
        ObservationCode code,
        ObservationSeverity severity,
        HighlightPriority? priority,
        ObservationEvidence evidence,
        DateTime createdAt)
        => new(Id.New(), exerciseId, code, severity, priority, highlightRank: null, evidence, createdAt, isNew: true);

    /// <summary>
    /// Restores an observation already stored in the system.
    /// </summary>
    /// <exception cref="InvalidWorkoutObservationException">When the code, severity or priority is not a defined value.</exception>
    public static WorkoutObservation Rehydrate(
        Id id,
        Id? exerciseId,
        ObservationCode code,
        ObservationSeverity severity,
        HighlightPriority? priority,
        int? highlightRank,
        ObservationEvidence evidence,
        DateTime createdAt)
        => new(id, exerciseId, code, severity, priority, highlightRank, evidence, createdAt, isNew: false);

    internal void Highlight(int rank) => HighlightRank = rank;
}
