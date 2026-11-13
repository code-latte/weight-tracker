namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when the number of sessions asked of an exercise history is outside the accepted range.
/// </summary>
public sealed class InvalidHistoryLimitException : ApplicationLayerException
{
    public InvalidHistoryLimitException(int limit, int min, int max)
        : base($"limit must be between {min} and {max}, got {limit}")
    { }
}
