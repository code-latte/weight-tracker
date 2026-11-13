namespace WeightTracker.Domain.Workouts.Observations;

/// <summary>
/// What kind of fact an observation is. It is independent of <see cref="HighlightPriority"/>, which says how much
/// attention the fact deserves.
/// </summary>
public enum ObservationSeverity
{
    INFO = 1,
    WARNING = 2,
    SUCCESS = 3,
}
