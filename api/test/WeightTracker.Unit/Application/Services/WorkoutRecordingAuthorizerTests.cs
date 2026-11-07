using FluentAssertions;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Application.Services;

public class WorkoutRecordingAuthorizerTests
{
    private const string Operation = "record sets";

    private static readonly DateTime WorkoutDate = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private static readonly Id OwnerId = Id.New();

    private static readonly Id TrainerId = Id.New();

    private readonly WorkoutRecordingAuthorizer _authorizer = new();

    private static Workout APrescribedWorkout() => Workout.Create(OwnerId, TrainerId, Name, null, WorkoutDate);

    private static Workout ASelfCreatedWorkout() => Workout.Create(OwnerId, null, Name, null, WorkoutDate);

    [Fact]
    public void EnsureCanRecord_ShouldAllowAnAdminOnAWorkoutTheyDoNotOwn()
    {
        // Arrange
        var workout = APrescribedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanRecord(workout, Id.New(), Role.ADMIN, Operation);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanRecord_ShouldAllowTheOwnerOfAPrescribedWorkout()
    {
        // Arrange
        var workout = APrescribedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanRecord(workout, OwnerId, Role.USER, Operation);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanRecord_ShouldAllowTheOwnerOfASelfCreatedWorkout()
    {
        // Arrange
        var workout = ASelfCreatedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanRecord(workout, OwnerId, Role.USER, Operation);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanRecord_ShouldRejectThePrescribingTrainer()
    {
        // Arrange
        var workout = APrescribedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanRecord(workout, TrainerId, Role.TRAINER, Operation);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Fact]
    public void EnsureCanRecord_ShouldRejectATrainerWhoOwnsASelfCreatedWorkout()
    {
        // Arrange
        var workout = ASelfCreatedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanRecord(workout, OwnerId, Role.TRAINER, Operation);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Fact]
    public void EnsureCanRecord_ShouldRejectAUserWhoDoesNotOwnTheWorkout()
    {
        // Arrange
        var workout = ASelfCreatedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanRecord(workout, Id.New(), Role.USER, Operation);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.COMPLETED)]
    public void EnsureCanRecord_ShouldNotCheckTheStatus(WorkoutStatus status)
    {
        // Arrange
        var workout = Workout.Rehydrate(Id.New(), OwnerId, null, Name, null, WorkoutDate, status, []);

        // Act
        var act = () => _authorizer.EnsureCanRecord(workout, OwnerId, Role.USER, Operation);

        // Assert
        act.Should().NotThrow();
    }
}
