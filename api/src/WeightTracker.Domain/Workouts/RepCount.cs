using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Workouts;

/// <summary>
/// Number of reps actually performed in one set.
/// </summary>
public sealed record RepCount
{
    public int Value { get; }

    private RepCount(int value)
    {
        Value = value;
    }

    /// <exception cref="InvalidRepCountException">When the value is lower than 1.</exception>
    public static RepCount From(int value)
    {
        if (value < 1)
            throw new InvalidRepCountException($"reps must be at least 1, got {value}");

        return new RepCount(value);
    }

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
