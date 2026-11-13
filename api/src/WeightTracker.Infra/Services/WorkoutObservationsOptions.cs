using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Workouts.Observations;

namespace WeightTracker.Infra.Services;

/// <summary>
/// Settings of the <c>WorkoutObservations</c> configuration section: the thresholds of the observation rules and of the
/// highlight eligibility. They are initial product thresholds, so every value must be supplied. None has a default in code.
/// </summary>
public class WorkoutObservationsOptions
{
    public const string SectionName = "WorkoutObservations";

    public int? RepeatedMissCount { get; set; }

    public int? PlateauCount { get; set; }

    public int? TrendCount { get; set; }

    public decimal? SevereNonCompliancePercent { get; set; }

    public decimal? SignificantDeclinePercent { get; set; }

    public decimal? SignificantRecordPercent { get; set; }

    public decimal? ExceptionalProgressionPercent { get; set; }

    /// <exception cref="InvalidOperationException">When a value is missing or out of range.</exception>
    public ObservationThresholds ToThresholds()
    {
        try
        {
            return new ObservationThresholds(
                Required(RepeatedMissCount, nameof(RepeatedMissCount)),
                Required(PlateauCount, nameof(PlateauCount)),
                Required(TrendCount, nameof(TrendCount)),
                Required(SevereNonCompliancePercent, nameof(SevereNonCompliancePercent)),
                Required(SignificantDeclinePercent, nameof(SignificantDeclinePercent)),
                Required(SignificantRecordPercent, nameof(SignificantRecordPercent)),
                Required(ExceptionalProgressionPercent, nameof(ExceptionalProgressionPercent)));
        }
        catch (InvalidObservationThresholdsException exception)
        {
            throw new InvalidOperationException(
                $"{SectionName} is not valid: {exception.Message}. "
                    + $"Supply it through configuration or the {SectionName}__* environment variables.",
                exception);
        }
    }

    private static T Required<T>(T? value, string name) where T : struct
        => value ?? throw new InvalidOperationException(
            $"{SectionName}:{name} is required. Supply it through configuration or the {SectionName}__{name} environment variable.");
}
