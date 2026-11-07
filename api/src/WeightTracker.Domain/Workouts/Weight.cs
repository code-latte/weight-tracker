using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Workouts;

/// <summary>
/// Weight lifted in kilograms, with up to two decimals. Zero means bodyweight.
/// </summary>
public sealed record Weight
{
    /// <summary>
    /// The numeric(6,2) column that stores weights must stay aligned with this value.
    /// </summary>
    public const decimal MaxValue = 9999.99m;

    public decimal Value { get; }

    private Weight(decimal value)
    {
        Value = value;
    }

    /// <exception cref="InvalidWeightException">When the value is negative, is above the maximum or has more than 2 decimals.</exception>
    public static Weight From(decimal value)
    {
        if (value < 0)
            throw new InvalidWeightException($"weight cannot be negative, got {value}");

        if (value > MaxValue)
            throw new InvalidWeightException($"weight cannot be greater than {MaxValue}, got {value}");

        if (decimal.Round(value, 2) != value)
            throw new InvalidWeightException($"weight allows at most 2 decimals, got {value}");

        return new Weight(value);
    }

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
