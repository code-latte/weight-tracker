using FluentAssertions;
using WeightTracker.Domain.Workouts.Observations;
using Xunit;

namespace WeightTracker.Unit.Domain.Workouts.Observations;

public class ObservationCodeTests
{
    [Fact]
    public void Codes_ShouldBeTheReleasedPublicCodesInOrder()
    {
        // Arrange
        // Act
        var codes = Enum.GetNames<ObservationCode>();

        // Assert
        codes.Should().Equal(
            "WORKOUT_INCOMPLETE",
            "EXERCISE_SKIPPED",
            "REPS_BELOW_TARGET",
            "REPS_ABOVE_TARGET",
            "ALL_TARGETS_MET",
            "WEIGHT_INCREASE",
            "WEIGHT_DECREASE",
            "WEIGHT_PERSONAL_RECORD",
            "REPS_PERSONAL_RECORD",
            "VOLUME_INCREASE",
            "VOLUME_DECREASE",
            "REPS_INCREASE",
            "REPS_DECREASE",
            "REPEATED_TARGET_MISS",
            "WEIGHT_PLATEAU",
            "WEIGHT_UPTREND",
            "WEIGHT_DOWNTREND");
    }
}
