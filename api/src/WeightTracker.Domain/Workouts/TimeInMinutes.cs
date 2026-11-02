using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Workouts;

/// <summary>
/// Amount of time in minutes, with up to two decimals (1.25 is 1 minute and 15 seconds).
/// Used by the minutes set target and by the rest of a workout exercise.
/// </summary>
public sealed record TimeInMinutes
{
    /// <summary>
    /// The numeric(5,2) columns that store minutes must stay aligned with this value.
    /// </summary>
    public const decimal MaxValue = 999.99m;

    public decimal Value { get; }

    private TimeInMinutes(decimal value)
    {
        Value = value;
    }

    /// <exception cref="InvalidTimeInMinutesException">When the value is not greater than 0, is above the maximum or has more than 2 decimals.</exception>
    public static TimeInMinutes From(decimal value)
    {
        if (value <= 0)
            throw new InvalidTimeInMinutesException($"minutes must be greater than 0, got {value}");

        if (value > MaxValue)
            throw new InvalidTimeInMinutesException($"minutes cannot be greater than {MaxValue}, got {value}");

        if (decimal.Round(value, 2) != value)
            throw new InvalidTimeInMinutesException($"minutes allow at most 2 decimals, got {value}");

        return new TimeInMinutes(value);
    }

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
