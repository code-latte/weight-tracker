namespace WeightTracker.Domain.Workouts.Observations;

/// <summary>
/// One piece of numeric evidence: a single number or a list of numbers.
/// </summary>
public abstract record EvidenceValue
{
    public sealed record Single(decimal Value) : EvidenceValue;

    public sealed record Many : EvidenceValue
    {
        public IReadOnlyList<decimal> Values { get; }

        public Many(IEnumerable<decimal> values)
        {
            Values = values.ToList();
        }

        public bool Equals(Many? other) => other is not null && Values.SequenceEqual(other.Values);

        public override int GetHashCode()
        {
            var hash = new HashCode();

            foreach (var value in Values)
                hash.Add(value);

            return hash.ToHashCode();
        }
    }
}
