using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Workouts.Observations;

/// <summary>
/// The numbers an observation was calculated from: a flat set of named values, each a number or a list of numbers.
/// Weights are in kilograms. It is stored as JSON, so the names are part of the public contract of each code.
/// </summary>
public sealed record ObservationEvidence
{
    private readonly Dictionary<string, EvidenceValue> _values;

    public static readonly ObservationEvidence Empty = new([]);

    public IReadOnlyDictionary<string, EvidenceValue> Values => _values;

    private ObservationEvidence(Dictionary<string, EvidenceValue> values)
    {
        _values = values;
    }

    /// <summary>
    /// A copy with one more number.
    /// </summary>
    /// <exception cref="InvalidWorkoutObservationException">When the name is blank or already present.</exception>
    public ObservationEvidence With(string name, decimal value) => Add(name, new EvidenceValue.Single(value));

    /// <summary>
    /// A copy with one more list of numbers.
    /// </summary>
    /// <exception cref="InvalidWorkoutObservationException">When the name is blank or already present.</exception>
    public ObservationEvidence With(string name, IEnumerable<decimal> values) => Add(name, new EvidenceValue.Many(values));

    /// <summary>
    /// The number stored under the name.
    /// </summary>
    /// <exception cref="InvalidWorkoutObservationException">When the name is absent or holds a list.</exception>
    public decimal Number(string name)
        => _values.GetValueOrDefault(name) is EvidenceValue.Single single
            ? single.Value
            : throw new InvalidWorkoutObservationException($"evidence '{name}' is not a number");

    /// <summary>
    /// The list stored under the name.
    /// </summary>
    /// <exception cref="InvalidWorkoutObservationException">When the name is absent or holds a single number.</exception>
    public IReadOnlyList<decimal> Numbers(string name)
        => _values.GetValueOrDefault(name) is EvidenceValue.Many many
            ? many.Values
            : throw new InvalidWorkoutObservationException($"evidence '{name}' is not a list");

    public bool Equals(ObservationEvidence? other)
        => other is not null
            && _values.Count == other._values.Count
            && _values.All(pair => other._values.TryGetValue(pair.Key, out var value) && value.Equals(pair.Value));

    public override int GetHashCode() => _values.Count;

    private ObservationEvidence Add(string name, EvidenceValue value)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidWorkoutObservationException("evidence name cannot be blank");

        if (_values.ContainsKey(name))
            throw new InvalidWorkoutObservationException($"evidence '{name}' is already set");

        return new ObservationEvidence(new Dictionary<string, EvidenceValue>(_values) { [name] = value });
    }

    /// <summary>
    /// Restores stored evidence as it is.
    /// </summary>
    /// <exception cref="InvalidWorkoutObservationException">When a name is blank.</exception>
    public static ObservationEvidence From(IEnumerable<KeyValuePair<string, EvidenceValue>> values)
    {
        var evidence = Empty;

        foreach (var (name, value) in values)
            evidence = evidence.Add(name, value);

        return evidence;
    }
}
