using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts;
using WeightTracker.Infra.Exceptions;
using WeightTracker.Infra.Mappers;
using Xunit;

namespace WeightTracker.Unit.Infra.Mappers;

public class WorkoutTemplateMapperTests
{
    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private static readonly Id SentadillaId = Id.New();

    private static readonly Id PlanchaId = Id.New();

    private static WorkoutTemplate AFullyLoadedTemplate()
    {
        var template = WorkoutTemplate.Create(Id.New(), Name, WorkoutDescription.From("Focus on tempo"));
        template.AddExercise(SentadillaId, Effort.MIDHIGH,
        [
            new SetPrescription(5, new SetTarget.Reps(5)),
            new SetPrescription(2, new SetTarget.RepsRange(8, 12)),
        ], perSide: true, rest: TimeInMinutes.From(1.5m));
        template.AddExercise(PlanchaId, Effort.LOW,
        [
            new SetPrescription(3, new SetTarget.Duration(40)),
            new SetPrescription(4, new SetTarget.Minutes(TimeInMinutes.From(2m))),
        ]);
        template.AddExercise(Id.New(), Effort.HIGH, [new SetPrescription(7, new SetTarget.MaxReps())]);
        template.CreateSuperSeries(template.Exercises.Take(2).Select(entry => entry.Id));

        return template;
    }

    private static WorkoutTemplate RoundTrip(WorkoutTemplate template)
        => WorkoutTemplateMapper.MapToDomain(WorkoutTemplateMapper.MapToEntity(template));

    [Fact]
    public void RoundTrip_ShouldPreserveIdentityTrainerNameAndDescription()
    {
        // Arrange
        var original = AFullyLoadedTemplate();

        // Act
        var restored = RoundTrip(original);

        // Assert
        restored.Id.Should().Be(original.Id);
        restored.TrainerId.Should().Be(original.TrainerId);
        restored.Name.Should().Be(original.Name);
        restored.Description.Should().Be(original.Description);
        restored.IsNew.Should().BeFalse();
    }

    [Fact]
    public void RoundTrip_ShouldPreserveAnEmptyTemplateWithoutDescription()
    {
        // Arrange
        var original = WorkoutTemplate.Create(Id.New(), Name, null);

        // Act
        var restored = RoundTrip(original);

        // Assert
        restored.Description.Should().BeNull();
        restored.Exercises.Should().BeEmpty();
    }

    [Fact]
    public void RoundTrip_ShouldPreserveEveryEntryInOrderWithItsSets()
    {
        // Arrange
        var original = AFullyLoadedTemplate();

        // Act
        var restored = RoundTrip(original);

        // Assert
        restored.Exercises.Should().HaveCount(3);
        restored.Exercises.Zip(original.Exercises).Should().AllSatisfy(pair =>
        {
            var (copy, source) = pair;
            copy.Id.Should().Be(source.Id);
            copy.ExerciseId.Should().Be(source.ExerciseId);
            copy.Effort.Should().Be(source.Effort);
            copy.Sets.Should().Equal(source.Sets);
            copy.PerSide.Should().Be(source.PerSide);
            copy.Rest.Should().Be(source.Rest);
            copy.SuperSeriesId.Should().Be(source.SuperSeriesId);
        });
    }

    [Fact]
    public void MapToDomain_ShouldHonourStoredPositionsRatherThanRowOrder()
    {
        // Arrange
        var entity = WorkoutTemplateMapper.MapToEntity(AFullyLoadedTemplate());
        entity.Exercises.Reverse();
        foreach (var templateExercise in entity.Exercises)
            templateExercise.Sets.Reverse();

        // Act
        var restored = WorkoutTemplateMapper.MapToDomain(entity);

        // Assert
        restored.Exercises.Take(2).Select(entry => entry.ExerciseId).Should().Equal(SentadillaId, PlanchaId);
        restored.Exercises[0].Sets.Should().Equal(
            new SetPrescription(5, new SetTarget.Reps(5)),
            new SetPrescription(2, new SetTarget.RepsRange(8, 12)));
    }

    [Fact]
    public void MapToDomain_ShouldRejectAStoredSuperSeriesWithASingleMember()
    {
        // Arrange
        var entity = WorkoutTemplateMapper.MapToEntity(AFullyLoadedTemplate());
        entity.Exercises[1].SuperSeriesId = null;

        // Act
        var act = () => WorkoutTemplateMapper.MapToDomain(entity);

        // Assert
        act.Should().Throw<PersistenceMappingException>();
    }

    [Fact]
    public void MapToDomain_ShouldRejectAnUnknownStoredEffort()
    {
        // Arrange
        var entity = WorkoutTemplateMapper.MapToEntity(AFullyLoadedTemplate());
        entity.Exercises[0].Effort = 0;

        // Act
        var act = () => WorkoutTemplateMapper.MapToDomain(entity);

        // Assert
        act.Should().Throw<PersistenceMappingException>();
    }
}
