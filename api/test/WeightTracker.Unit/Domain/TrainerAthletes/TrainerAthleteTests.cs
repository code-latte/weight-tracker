using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.TrainerAthletes;
using Xunit;

namespace WeightTracker.Unit.Domain.TrainerAthletes;

public class TrainerAthleteTests
{
    private static readonly Id TrainerId = Id.New();
    private static readonly Id AthleteId = Id.New();
    private static readonly DateTime StartedAt = new(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EndedAt = new(2026, 6, 30, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldStartAnActiveRelationshipWithoutEndDate()
    {
        // Arrange & Act
        var relationship = TrainerAthlete.Create(TrainerId, AthleteId, StartedAt);

        // Assert
        relationship.IsNew.Should().BeTrue();
        relationship.Id.Value.Should().NotBe(Guid.Empty);
        relationship.TrainerId.Should().Be(TrainerId);
        relationship.UserId.Should().Be(AthleteId);
        relationship.Status.Should().Be(TrainerAthleteStatus.ACTIVE);
        relationship.StartedAtUtc.Should().Be(StartedAt);
        relationship.FinishedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Rehydrate_ShouldRestoreEveryFieldAndNotBeNew()
    {
        // Arrange
        var id = Id.New();

        // Act
        var relationship = TrainerAthlete.Rehydrate(
            id, TrainerId, AthleteId, TrainerAthleteStatus.ENDED, StartedAt, EndedAt);

        // Assert
        relationship.IsNew.Should().BeFalse();
        relationship.Id.Should().Be(id);
        relationship.Status.Should().Be(TrainerAthleteStatus.ENDED);
        relationship.FinishedAtUtc.Should().Be(EndedAt);
    }

    [Fact]
    public void End_ShouldEndAnActiveRelationshipAndRecordWhen()
    {
        // Arrange
        var relationship = TrainerAthlete.Create(TrainerId, AthleteId, StartedAt);

        // Act
        relationship.End(EndedAt);

        // Assert
        relationship.Status.Should().Be(TrainerAthleteStatus.ENDED);
        relationship.FinishedAtUtc.Should().Be(EndedAt);
    }

    [Fact]
    public void End_ShouldRejectAnEndedRelationshipAndKeepTheFirstDate()
    {
        // Arrange
        var relationship = TrainerAthlete.Create(TrainerId, AthleteId, StartedAt);
        relationship.End(EndedAt);

        // Act
        var act = () => relationship.End(EndedAt.AddDays(1));

        // Assert
        act.Should().Throw<InvalidTrainerAthleteStatusTransitionException>();
        relationship.Status.Should().Be(TrainerAthleteStatus.ENDED);
        relationship.FinishedAtUtc.Should().Be(EndedAt);
    }

    [Fact]
    public void Rehydrate_ShouldRejectAnActiveRelationshipWithAnEndDate()
    {
        // Arrange & Act
        var act = () => TrainerAthlete.Rehydrate(
            Id.New(), TrainerId, AthleteId, TrainerAthleteStatus.ACTIVE, StartedAt, EndedAt);

        // Assert
        act.Should().Throw<InvalidTrainerAthleteStateException>();
    }

    [Fact]
    public void Rehydrate_ShouldRejectAnEndedRelationshipWithoutEndDate()
    {
        // Arrange & Act
        var act = () => TrainerAthlete.Rehydrate(
            Id.New(), TrainerId, AthleteId, TrainerAthleteStatus.ENDED, StartedAt, null);

        // Assert
        act.Should().Throw<InvalidTrainerAthleteStateException>();
    }

    [Fact]
    public void Rehydrate_ShouldRejectAnEndBeforeTheStart()
    {
        // Arrange & Act
        var act = () => TrainerAthlete.Rehydrate(
            Id.New(), TrainerId, AthleteId, TrainerAthleteStatus.ENDED, StartedAt, StartedAt.AddDays(-1));

        // Assert
        act.Should().Throw<InvalidTrainerAthleteStateException>();
    }
}
