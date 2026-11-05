using FluentAssertions;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Mesocycles;
using Xunit;

namespace WeightTracker.Unit.Domain.Mesocycles;

public class MesocycleObjectiveTests
{
    [Fact]
    public void From_ShouldTrimTheValue()
    {
        // Arrange & Act
        var value = MesocycleObjective.From("  Build work capacity  ");

        // Assert
        value.Value.Should().Be("Build work capacity");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_ShouldRejectBlankValues(string? candidate)
    {
        // Arrange & Act
        var act = () => MesocycleObjective.From(candidate);

        // Assert
        act.Should().Throw<InvalidMesocycleObjectiveException>();
    }

    [Fact]
    public void From_ShouldAcceptMaxLength()
    {
        // Arrange
        var candidate = new string('a', MesocycleObjective.MaxLength);

        // Act
        var value = MesocycleObjective.From(candidate);

        // Assert
        value.Value.Should().HaveLength(4000);
    }

    [Fact]
    public void From_ShouldRejectValuesLongerThanMaxLength()
    {
        // Arrange
        var candidate = new string('a', MesocycleObjective.MaxLength + 1);

        // Act
        var act = () => MesocycleObjective.From(candidate);

        // Assert
        act.Should().Throw<InvalidMesocycleObjectiveException>();
    }

    [Fact]
    public void From_ShouldMeasureLengthAfterTrimming()
    {
        // Arrange
        var candidate = $"  {new string('a', MesocycleObjective.MaxLength)}  ";

        // Act
        var value = MesocycleObjective.From(candidate);

        // Assert
        value.Value.Should().HaveLength(4000);
    }
}
