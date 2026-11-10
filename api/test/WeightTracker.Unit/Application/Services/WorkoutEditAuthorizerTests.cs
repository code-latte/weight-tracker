using FluentAssertions;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Application.Services;

public class WorkoutEditAuthorizerTests
{
    private const string Operation = "edit workout";

    private static readonly DateTime WorkoutDate = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private static readonly Id OwnerId = Id.New();

    private static readonly Id TrainerId = Id.New();

    private readonly WorkoutEditAuthorizer _authorizer = new();

    private static Workout APrescribedWorkout() => Workout.Create(OwnerId, TrainerId, Name, null, WorkoutDate);

    private static Workout ASelfCreatedWorkout() => Workout.Create(OwnerId, null, Name, null, WorkoutDate);

    [Fact]
    public void EnsureCanEdit_ShouldAllowAnAdminOnAPrescribedWorkout()
    {
        // Arrange
        var workout = APrescribedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, Id.New(), Role.ADMIN, Operation);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanEdit_ShouldAllowAnAdminOnASelfCreatedWorkout()
    {
        // Arrange
        var workout = ASelfCreatedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, Id.New(), Role.ADMIN, Operation);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanEdit_ShouldAllowThePrescribingTrainer()
    {
        // Arrange
        var workout = APrescribedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, TrainerId, Role.TRAINER, Operation);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanEdit_ShouldRejectTheOwnerOfAPrescribedWorkout()
    {
        // Arrange
        var workout = APrescribedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, OwnerId, Role.USER, Operation);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Fact]
    public void EnsureCanEdit_ShouldRejectAnotherTrainerOnAPrescribedWorkout()
    {
        // Arrange
        var workout = APrescribedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, Id.New(), Role.TRAINER, Operation);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Fact]
    public void EnsureCanEdit_ShouldAllowAUserWhoOwnsASelfCreatedWorkout()
    {
        // Arrange
        var workout = ASelfCreatedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, OwnerId, Role.USER, Operation);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanEdit_ShouldAllowATrainerWhoOwnsASelfCreatedWorkout()
    {
        // Arrange
        var workout = ASelfCreatedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, OwnerId, Role.TRAINER, Operation);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanEdit_ShouldRejectATrainerWhoDoesNotOwnASelfCreatedWorkout()
    {
        // Arrange
        var workout = ASelfCreatedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, TrainerId, Role.TRAINER, Operation);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Fact]
    public void EnsureCanEdit_ShouldRejectAUserWhoDoesNotOwnASelfCreatedWorkout()
    {
        // Arrange
        var workout = ASelfCreatedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, Id.New(), Role.USER, Operation);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    private static Workout AWorkoutIn(WorkoutStatus status, Id? trainerId)
        => Workout.Rehydrate(Id.New(), OwnerId, trainerId, Name, null, WorkoutDate, status, [],
            reviewedAt: status == WorkoutStatus.REVIEWED ? WorkoutDate : null,
            reviewedBy: status == WorkoutStatus.REVIEWED ? trainerId : null);

    [Theory]
    [InlineData(WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.COMPLETED)]
    [InlineData(WorkoutStatus.REVIEWED)]
    public void EnsureCanEdit_ShouldRejectThePrescribingTrainerOnceTheWorkoutIsNoLongerADraft(WorkoutStatus status)
    {
        // Arrange
        var workout = AWorkoutIn(status, TrainerId);

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, TrainerId, Role.TRAINER, Operation);

        // Assert
        act.Should().Throw<WorkoutLockedException>();
    }

    [Theory]
    [InlineData(WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.COMPLETED)]
    public void EnsureCanEdit_ShouldRejectTheOwnerOfASelfCreatedWorkoutOnceItIsNoLongerADraft(WorkoutStatus status)
    {
        // Arrange
        var workout = AWorkoutIn(status, null);

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, OwnerId, Role.USER, Operation);

        // Assert
        act.Should().Throw<WorkoutLockedException>();
    }

    [Theory]
    [InlineData(WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.COMPLETED)]
    public void EnsureCanEdit_ShouldAllowAnAdminOnALockedWorkout(WorkoutStatus status)
    {
        // Arrange
        var workout = AWorkoutIn(status, TrainerId);

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, Id.New(), Role.ADMIN, Operation);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanEdit_ShouldRejectAnAdminOnAReviewedWorkout()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.REVIEWED, TrainerId);

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, Id.New(), Role.ADMIN, Operation);

        // Assert
        act.Should().Throw<WorkoutLockedException>();
    }

    [Fact]
    public void EnsureCanEdit_ShouldRefuseACallerWhoMayNotEditAReviewedWorkoutWithoutRevealingTheLock()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.REVIEWED, TrainerId);

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, OwnerId, Role.USER, Operation);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Fact]
    public void EnsureCanEdit_ShouldRefuseACallerWhoMayNotEditALockedWorkoutWithoutRevealingTheLock()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.STARTED, TrainerId);

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, OwnerId, Role.USER, Operation);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Fact]
    public void EnsureCanEdit_ShouldKeepAllowingTheTrainerWhileTheWorkoutIsADraft()
    {
        // Arrange
        var workout = AWorkoutIn(WorkoutStatus.DRAFT, TrainerId);

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, TrainerId, Role.TRAINER, Operation);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.COMPLETED)]
    [InlineData(WorkoutStatus.REVIEWED)]
    public void EnsureCanModify_ShouldAllowAnAdminWhateverTheStatus(WorkoutStatus status)
    {
        // Arrange
        var workout = AWorkoutIn(status, TrainerId);

        // Act
        var act = () => _authorizer.EnsureCanModify(workout, Id.New(), Role.ADMIN, Operation);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.COMPLETED)]
    [InlineData(WorkoutStatus.REVIEWED)]
    public void EnsureCanModify_ShouldAllowThePrescribingTrainerWhateverTheStatus(WorkoutStatus status)
    {
        // Arrange
        var workout = AWorkoutIn(status, TrainerId);

        // Act
        var act = () => _authorizer.EnsureCanModify(workout, TrainerId, Role.TRAINER, Operation);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.COMPLETED)]
    public void EnsureCanModify_ShouldAllowTheOwnerOfASelfCreatedWorkoutWhateverTheStatus(WorkoutStatus status)
    {
        // Arrange
        var workout = AWorkoutIn(status, null);

        // Act
        var act = () => _authorizer.EnsureCanModify(workout, OwnerId, Role.USER, Operation);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanModify_ShouldRejectTheOwnerOfAPrescribedWorkout()
    {
        // Arrange
        var workout = APrescribedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanModify(workout, OwnerId, Role.USER, Operation);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Fact]
    public void EnsureCanModify_ShouldRejectAnotherTrainerOnAPrescribedWorkout()
    {
        // Arrange
        var workout = APrescribedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanModify(workout, Id.New(), Role.TRAINER, Operation);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Fact]
    public void EnsureCanModify_ShouldRejectAUserWhoDoesNotOwnASelfCreatedWorkout()
    {
        // Arrange
        var workout = ASelfCreatedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanModify(workout, Id.New(), Role.USER, Operation);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }
}
