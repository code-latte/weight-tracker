namespace WeightTracker.Domain.Workouts.Observations;

/// <summary>
/// How much attention an observation deserves, most urgent first. An observation without priority is not eligible
/// to be highlighted.
/// </summary>
public enum HighlightPriority
{
    CRITICAL = 1,
    HIGH = 2,
    MEDIUM = 3,
    LOW = 4,
}
