using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

/// <summary>
/// Turns the optional reps and seconds of a set-record request into a <see cref="SetResult"/>, shared by the add and update use cases.
/// </summary>
internal static class SetResultInput
{
    /// <returns>The result, or null when neither reps nor seconds are given.</returns>
    /// <exception cref="InvalidRepCountException">When the reps are not valid.</exception>
    /// <exception cref="InvalidDurationInSecondsException">When the seconds are not valid.</exception>
    public static SetResult? ToResult(int? reps, int? durationSeconds)
    {
        if (reps is not null)
            return new SetResult.Reps(RepCount.From(reps.Value));

        if (durationSeconds is not null)
            return new SetResult.Duration(DurationInSeconds.From(durationSeconds.Value));

        return null;
    }
}
