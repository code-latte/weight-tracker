using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Workouts;

/// <summary>
/// Time actually held or spent in one set, in whole seconds.
/// </summary>
public sealed record DurationInSeconds
{
    public int Value { get; }

    private DurationInSeconds(int value)
    {
        Value = value;
    }

    /// <exception cref="InvalidDurationInSecondsException">When the value is lower than 1.</exception>
    public static DurationInSeconds From(int value)
    {
        if (value < 1)
            throw new InvalidDurationInSecondsException($"seconds must be at least 1, got {value}");

        return new DurationInSeconds(value);
    }

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
