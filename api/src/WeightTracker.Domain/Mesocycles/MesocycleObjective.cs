using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Mesocycles;

/// <summary>
/// What a mesocycle, or one of its weeks, is meant to achieve. Always required: it is a core part of the plan.
/// </summary>
public sealed record MesocycleObjective
{
    /// <summary>
    /// The Mesocycles.Objective and MesocycleWeeks.Objective column lengths must stay aligned with this value.
    /// </summary>
    public const int MaxLength = 4000;

    public string Value { get; }

    private MesocycleObjective(string value)
    {
        Value = value;
    }

    /// <exception cref="InvalidMesocycleObjectiveException">When the value is blank or too long once trimmed.</exception>
    public static MesocycleObjective From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidMesocycleObjectiveException("The objective is required");

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            throw new InvalidMesocycleObjectiveException($"The objective cannot be longer than {MaxLength} characters");

        return new MesocycleObjective(trimmed);
    }

    public override string ToString() => Value;
}
