using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Mesocycles;

/// <summary>
/// Name the mesocycle is listed and recognised by. It is not unique.
/// </summary>
public sealed record MesocycleName
{
    /// <summary>
    /// The Mesocycles.Name column length must stay aligned with this value.
    /// </summary>
    public const int MaxLength = 256;

    public string Value { get; }

    private MesocycleName(string value)
    {
        Value = value;
    }

    /// <exception cref="InvalidMesocycleNameException">When the value is blank or too long once trimmed.</exception>
    public static MesocycleName From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidMesocycleNameException("The mesocycle name is required");

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            throw new InvalidMesocycleNameException($"The mesocycle name cannot be longer than {MaxLength} characters");

        return new MesocycleName(trimmed);
    }

    public override string ToString() => Value;
}
