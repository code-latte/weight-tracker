using FluentAssertions;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Workouts.Observations;
using Xunit;

namespace WeightTracker.Unit.Domain.Workouts.Observations;

public class ObservationEvidenceTests
{
    [Fact]
    public void With_ShouldKeepNumbersAndListsWithoutChangingTheOriginal()
    {
        // Arrange
        var original = ObservationEvidence.Empty;

        // Act
        var evidence = original.With("currentMaxKg", 130m).With("previousWeightsKg", [110m, 105m]);

        // Assert
        evidence.Number("currentMaxKg").Should().Be(130m);
        evidence.Numbers("previousWeightsKg").Should().Equal(110m, 105m);
        original.Values.Should().BeEmpty();
    }

    [Fact]
    public void With_ShouldRejectABlankName()
    {
        // Arrange
        // Act
        var act = () => ObservationEvidence.Empty.With(" ", 1m);

        // Assert
        act.Should().Throw<InvalidWorkoutObservationException>();
    }

    [Fact]
    public void With_ShouldRejectANameThatIsAlreadySet()
    {
        // Arrange
        var evidence = ObservationEvidence.Empty.With("currentMaxKg", 130m);

        // Act
        var act = () => evidence.With("currentMaxKg", 140m);

        // Assert
        act.Should().Throw<InvalidWorkoutObservationException>();
    }

    [Fact]
    public void Number_ShouldRejectAListOrAMissingName()
    {
        // Arrange
        var evidence = ObservationEvidence.Empty.With("weights", [1m, 2m]);

        // Act
        var onList = () => evidence.Number("weights");
        var onMissing = () => evidence.Number("other");

        // Assert
        onList.Should().Throw<InvalidWorkoutObservationException>();
        onMissing.Should().Throw<InvalidWorkoutObservationException>();
    }

    [Fact]
    public void Equals_ShouldCompareNamesNumbersAndListContents()
    {
        // Arrange
        var first = ObservationEvidence.Empty.With("a", 1m).With("list", [1m, 2m]);
        var same = ObservationEvidence.Empty.With("list", [1m, 2m]).With("a", 1m);
        var different = ObservationEvidence.Empty.With("a", 1m).With("list", [1m, 3m]);

        // Act
        // Assert
        first.Should().Be(same);
        first.Should().NotBe(different);
    }

    [Fact]
    public void From_ShouldRestoreStoredValues()
    {
        // Arrange
        var stored = new Dictionary<string, EvidenceValue>
        {
            ["currentMaxKg"] = new EvidenceValue.Single(130m),
            ["previousWeightsKg"] = new EvidenceValue.Many([110m, 105m]),
        };

        // Act
        var evidence = ObservationEvidence.From(stored);

        // Assert
        evidence.Number("currentMaxKg").Should().Be(130m);
        evidence.Numbers("previousWeightsKg").Should().Equal(110m, 105m);
    }
}
