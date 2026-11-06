namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when a request names only one of the mesocycle and the week a workout is placed in.
/// </summary>
public sealed class IncompleteMesocyclePlacementException : ApplicationLayerException
{
    public IncompleteMesocyclePlacementException()
        : base("The mesocycle and the week number of a workout must be sent together, or both omitted")
    { }
}
