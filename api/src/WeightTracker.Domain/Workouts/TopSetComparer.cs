namespace WeightTracker.Domain.Workouts;

/// <summary>
/// One performed set as the top-set ranking sees it: what was measured, with which weight and at which position
/// among the sets of its exercise.
/// </summary>
public sealed record TopSetCandidate(SetResult Result, Weight? Weight, int Position);

/// <summary>
/// Ranks performed sets to find the top set of an exercise: the heaviest, ties broken by most reps, then longest
/// duration, then lowest position. A set without weight ranks below one at 0 kg. The better set sorts first.
/// </summary>
public sealed class TopSetComparer : IComparer<TopSetCandidate>
{
    public static readonly TopSetComparer Instance = new();

    /// <summary>
    /// The top set among the candidates, null when there are none.
    /// </summary>
    public static TopSetCandidate? Best(IEnumerable<TopSetCandidate> candidates)
        => candidates.Order(Instance).FirstOrDefault();

    public int Compare(TopSetCandidate? x, TopSetCandidate? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return 1;
        if (y is null) return -1;

        var byWeight = WeightOf(y).CompareTo(WeightOf(x));
        if (byWeight != 0) return byWeight;

        var byReps = RepsOf(y).CompareTo(RepsOf(x));
        if (byReps != 0) return byReps;

        var bySeconds = SecondsOf(y).CompareTo(SecondsOf(x));
        if (bySeconds != 0) return bySeconds;

        return x.Position.CompareTo(y.Position);
    }

    // A missing weight ranks below 0 kg, so it sorts under every real weight
    private static decimal WeightOf(TopSetCandidate set) => set.Weight?.Value ?? -1m;

    private static int RepsOf(TopSetCandidate set) => set.Result is SetResult.Reps reps ? reps.Count.Value : 0;

    private static int SecondsOf(TopSetCandidate set) => set.Result is SetResult.Duration duration ? duration.Value.Value : 0;
}
