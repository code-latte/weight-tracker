using FluentAssertions;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Application.Services;

public class WorkoutTemplateEditAuthorizerTests
{
    private static readonly Id TrainerId = Id.New();

    private static WorkoutTemplate ATemplate() => WorkoutTemplate.Create(TrainerId, WorkoutName.From("Full body"), null);

    [Fact]
    public void Admin_ShouldActOnAnyTemplate()
    {
        // Arrange
        var template = ATemplate();

        // Act
        var act = () => new WorkoutTemplateEditAuthorizer().EnsureCanEdit(template, Id.New(), Role.ADMIN, "test");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Trainer_ShouldActOnTheirTemplate()
    {
        // Arrange
        var template = ATemplate();

        // Act
        var act = () => new WorkoutTemplateEditAuthorizer().EnsureCanEdit(template, TrainerId, Role.TRAINER, "test");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AnotherTrainer_ShouldNotFindTheTemplate()
    {
        // Arrange
        var template = ATemplate();

        // Act
        var act = () => new WorkoutTemplateEditAuthorizer().EnsureCanEdit(template, Id.New(), Role.TRAINER, "test");

        // Assert
        act.Should().Throw<WorkoutTemplateNotFoundException>();
    }

    [Fact]
    public void RegularUser_ShouldBeRefused()
    {
        // Arrange
        var template = ATemplate();

        // Act
        var act = () => new WorkoutTemplateEditAuthorizer().EnsureCanEdit(template, Id.New(), Role.USER, "test");

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }
}
