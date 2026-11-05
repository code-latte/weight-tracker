using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Common;

/// <summary>
/// Position of a week inside a mesocycle, starting at 1.
/// </summary>
/// <remarks>
/// Shared by the Mesocycles and Workouts areas so a workout can point to a mesocycle week without depending on
/// the Mesocycle aggregate. The upper bound depends on each mesocycle and is checked there.
/// </remarks>
public sealed record WeekNumber
{
    public const int MinValue = 1;

    public int Value { get; }

    private WeekNumber(int value)
    {
        Value = value;
    }

    /// <exception cref="InvalidWeekNumberException">When the value is lower than 1.</exception>
    public static WeekNumber From(int value)
    {
        if (value < MinValue)
            throw new InvalidWeekNumberException(value);

        return new WeekNumber(value);
    }

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
