using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Mesocycles;

/// <summary>
/// How many weeks a mesocycle lasts.
/// </summary>
public sealed record DurationInWeeks
{
    public const int MinValue = 1;

    public const int MaxValue = 52;

    public int Value { get; }

    private DurationInWeeks(int value)
    {
        Value = value;
    }

    /// <exception cref="InvalidDurationInWeeksException">When the value is outside 1 to 52.</exception>
    public static DurationInWeeks From(int value)
    {
        if (value < MinValue || value > MaxValue)
            throw new InvalidDurationInWeeksException(value, MinValue, MaxValue);

        return new DurationInWeeks(value);
    }

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
