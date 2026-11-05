using FluentAssertions;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Mesocycles;
using Xunit;

namespace WeightTracker.Unit.Domain.Mesocycles;

public class MesocycleNameTests
{
    [Fact]
    public void From_ShouldTrimTheValue()
    {
        // Arrange & Act
        var value = MesocycleName.From("  Hypertrophy block  ");

        // Assert
        value.Value.Should().Be("Hypertrophy block");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_ShouldRejectBlankValues(string? candidate)
    {
        // Arrange & Act
        var act = () => MesocycleName.From(candidate);

        // Assert
        act.Should().Throw<InvalidMesocycleNameException>();
    }

    [Fact]
    public void From_ShouldAcceptMaxLength()
    {
        // Arrange
        var candidate = new string('a', MesocycleName.MaxLength);

        // Act
        var value = MesocycleName.From(candidate);

        // Assert
        value.Value.Should().HaveLength(256);
    }

    [Fact]
    public void From_ShouldRejectValuesLongerThanMaxLength()
    {
        // Arrange
        var candidate = new string('a', MesocycleName.MaxLength + 1);

        // Act
        var act = () => MesocycleName.From(candidate);

        // Assert
        act.Should().Throw<InvalidMesocycleNameException>();
    }

    [Fact]
    public void From_ShouldMeasureLengthAfterTrimming()
    {
        // Arrange
        var candidate = $"  {new string('a', MesocycleName.MaxLength)}  ";

        // Act
        var value = MesocycleName.From(candidate);

        // Assert
        value.Value.Should().HaveLength(256);
    }
}
