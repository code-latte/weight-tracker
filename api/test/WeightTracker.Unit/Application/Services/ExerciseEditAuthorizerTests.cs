using FluentAssertions;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Application.Services;

public class ExerciseEditAuthorizerTests
{
    private static readonly Id OwnerId = Id.New();

    private static Exercise ACustomExercise()
        => Exercise.Rehydrate(Id.New(), null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, [], OwnerId);

    private static Exercise ASystemExercise()
        => Exercise.Rehydrate(Id.New(), null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, []);

    [Theory]
    [InlineData(Role.TRAINER)]
    [InlineData(Role.USER)]
    public void EnsureCanEdit_ShouldAllowTheOwner(Role role)
    {
        // Arrange
        var exercise = ACustomExercise();

        // Act
        var act = () => new ExerciseEditAuthorizer().EnsureCanEdit(exercise, OwnerId, role, "PUT /exercises/{id}");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanEdit_ShouldAllowAnAdminOnACustomExercise()
    {
        // Arrange
        var exercise = ACustomExercise();

        // Act
        var act = () => new ExerciseEditAuthorizer().EnsureCanEdit(exercise, Id.New(), Role.ADMIN, "PUT /exercises/{id}");

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(Role.TRAINER)]
    [InlineData(Role.USER)]
    public void EnsureCanEdit_ShouldRejectSomeoneWhoIsNotTheOwner(Role role)
    {
        // Arrange
        var exercise = ACustomExercise();

        // Act
        var act = () => new ExerciseEditAuthorizer().EnsureCanEdit(exercise, Id.New(), role, "PUT /exercises/{id}");

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Theory]
    [InlineData(Role.ADMIN)]
    [InlineData(Role.TRAINER)]
    [InlineData(Role.USER)]
    public void EnsureCanEdit_ShouldRejectASystemExerciseForEveryRole(Role role)
    {
        // Arrange
        var exercise = ASystemExercise();

        // Act
        var act = () => new ExerciseEditAuthorizer().EnsureCanEdit(exercise, Id.New(), role, "PUT /exercises/{id}");

        // Assert
        act.Should().Throw<SystemExerciseNotEditableException>();
    }
}
