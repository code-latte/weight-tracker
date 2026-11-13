namespace WeightTracker.Domain.Workouts.Observations;

/// <summary>
/// One performed set as the observation rules see it: what was measured and, when tracked, with which weight.
/// </summary>
public sealed record ExecutionSet(SetResult Result, Weight? Weight);
