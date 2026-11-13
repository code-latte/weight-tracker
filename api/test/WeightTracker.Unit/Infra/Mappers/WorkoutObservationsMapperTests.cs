using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts.Observations;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Exceptions;
using WeightTracker.Infra.Mappers;
using Xunit;

namespace WeightTracker.Unit.Infra.Mappers;

public class WorkoutObservationsMapperTests
{
    private static readonly DateTime CreatedAt = new(2026, 11, 13, 10, 0, 0, DateTimeKind.Utc);

    private static WorkoutObservations ObservationsWithEvidence()
    {
        var record = WorkoutObservation.Create(
            Id.New(), ObservationCode.WEIGHT_PERSONAL_RECORD, ObservationSeverity.SUCCESS, HighlightPriority.MEDIUM,
            ObservationEvidence.Empty.With("previousMaxKg", 120m).With("currentMaxKg", 130m).With("changePercent", 8.33m), CreatedAt);

        var trend = WorkoutObservation.Create(
            Id.New(), ObservationCode.WEIGHT_DOWNTREND, ObservationSeverity.INFO, HighlightPriority.HIGH,
            ObservationEvidence.Empty.With("previousWeightsKg", [110m, 105m, 100m]).With("currentWeightKg", 95m), CreatedAt);

        var incomplete = WorkoutObservation.Create(
            null, ObservationCode.WORKOUT_INCOMPLETE, ObservationSeverity.WARNING, null, ObservationEvidence.Empty, CreatedAt);

        return WorkoutObservations.Create(Id.New(), [record, trend, incomplete], [trend.Id, record.Id]);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveEveryObservationAndHighlight()
    {
        // Arrange
        var original = ObservationsWithEvidence();

        // Act
        var restored = WorkoutObservationsMapper.MapToDomain(original.WorkoutId, WorkoutObservationsMapper.MapToEntities(original));

        // Assert
        restored.WorkoutId.Should().Be(original.WorkoutId);
        restored.IsNew.Should().BeFalse();
        restored.Observations.Should().HaveCount(3);

        foreach (var expected in original.Observations)
        {
            var actual = restored.Observations.Single(observation => observation.Id == expected.Id);
            actual.ExerciseId.Should().Be(expected.ExerciseId);
            actual.Code.Should().Be(expected.Code);
            actual.Severity.Should().Be(expected.Severity);
            actual.Priority.Should().Be(expected.Priority);
            actual.HighlightRank.Should().Be(expected.HighlightRank);
            actual.Evidence.Should().Be(expected.Evidence);
            actual.CreatedAt.Should().Be(expected.CreatedAt);
        }

        restored.Highlights.Select(observation => observation.Id).Should().Equal(original.Highlights.Select(observation => observation.Id));
    }

    [Fact]
    public void MapToEntities_ShouldStoreTheEvidenceAsAFlatJsonObject()
    {
        // Arrange
        var original = ObservationsWithEvidence();

        // Act
        var entities = WorkoutObservationsMapper.MapToEntities(original);

        // Assert
        var record = entities.Single(entity => entity.Code == "WEIGHT_PERSONAL_RECORD");
        record.WorkoutId.Should().Be(original.WorkoutId.Value);
        record.Severity.Should().Be((int)ObservationSeverity.SUCCESS);
        record.Priority.Should().Be((int)HighlightPriority.MEDIUM);
        record.HighlightRank.Should().Be(2);
        record.Data.Should().Contain("\"previousMaxKg\":120").And.Contain("\"currentMaxKg\":130").And.Contain("\"changePercent\":8.33");

        entities.Single(entity => entity.Code == "WEIGHT_DOWNTREND").Data.Should().Contain("\"previousWeightsKg\":[110,105,100]");
        entities.Single(entity => entity.Code == "WORKOUT_INCOMPLETE").Data.Should().Be("{}");
        entities.Single(entity => entity.Code == "WORKOUT_INCOMPLETE").ExerciseId.Should().BeNull();
    }

    private static WorkoutObservationEntity Stored(
        string code = "WEIGHT_INCREASE", int severity = 1, int? priority = null, int? rank = null, string data = "{}")
        => new()
        {
            Id = Guid.NewGuid(),
            WorkoutId = Guid.NewGuid(),
            Code = code,
            Severity = severity,
            Priority = priority,
            HighlightRank = rank,
            Data = data,
            CreatedAt = CreatedAt,
        };

    [Fact]
    public void MapToDomain_ShouldRejectAnUnknownCodeAsAPersistenceFault()
    {
        // Arrange
        var entity = Stored(code: "NOT_A_CODE");

        // Act
        var act = () => WorkoutObservationsMapper.MapToDomain(Id.New(), [entity]);

        // Assert
        act.Should().Throw<PersistenceMappingException>();
    }

    [Theory]
    [InlineData("[1,2]")]
    [InlineData("{\"weights\":\"heavy\"}")]
    [InlineData("{\"weights\":[1,\"a\"]}")]
    [InlineData("not json")]
    public void MapToDomain_ShouldRejectCorruptEvidenceAsAPersistenceFault(string data)
    {
        // Arrange
        var entity = Stored(data: data);

        // Act
        var act = () => WorkoutObservationsMapper.MapToDomain(Id.New(), [entity]);

        // Assert
        act.Should().Throw<PersistenceMappingException>();
    }

    [Fact]
    public void MapToDomain_ShouldRejectAnUndefinedSeverityAsAPersistenceFault()
    {
        // Arrange
        var entity = Stored(severity: 99);

        // Act
        var act = () => WorkoutObservationsMapper.MapToDomain(Id.New(), [entity]);

        // Assert
        act.Should().Throw<PersistenceMappingException>();
    }

    [Fact]
    public void MapToDomain_ShouldRejectInconsistentHighlightRanksAsAPersistenceFault()
    {
        // Arrange
        var entity = Stored(priority: 2, rank: 2);

        // Act
        var act = () => WorkoutObservationsMapper.MapToDomain(Id.New(), [entity]);

        // Assert
        act.Should().Throw<PersistenceMappingException>();
    }
}
