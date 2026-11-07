using FluentAssertions;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Application.Services;

public class WorkoutStatusAuthorizerTests
{
    private const string Operation = "change workout status";

    private static readonly DateTime WorkoutDate = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private static readonly Id AthleteId = Id.New();

    private static readonly Id TrainerId = Id.New();

    private readonly WorkoutStatusAuthorizer _authorizer = new();

    private static Workout APrescribedWorkoutIn(WorkoutStatus status)
        => Workout.Rehydrate(Id.New(), AthleteId, TrainerId, Name, null, WorkoutDate, status, []);

    private static Workout ASelfPlannedWorkoutIn(WorkoutStatus status)
        => Workout.Rehydrate(Id.New(), AthleteId, null, Name, null, WorkoutDate, status, []);

    private void Ensure(Workout workout, Id callerId, Role role, WorkoutStatus target)
        => _authorizer.EnsureCanChange(workout, callerId, role, target, Operation);

    [Theory]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.COMPLETED)]
    [InlineData(WorkoutStatus.COMPLETED, WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.STARTED, WorkoutStatus.STARTED)]
    public void Admin_ShouldBeAllowedAnyMove(WorkoutStatus from, WorkoutStatus to)
    {
        // Arrange
        var workout = APrescribedWorkoutIn(from);

        // Act
        var act = () => Ensure(workout, Id.New(), Role.ADMIN, to);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.PUBLISHED, WorkoutStatus.DRAFT)]
    public void PrescribingTrainer_ShouldBeAllowedToPublishAndUnpublish(WorkoutStatus from, WorkoutStatus to)
    {
        // Arrange
        var workout = APrescribedWorkoutIn(from);

        // Act
        var act = () => Ensure(workout, TrainerId, Role.TRAINER, to);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(WorkoutStatus.PUBLISHED, WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.STARTED, WorkoutStatus.COMPLETED)]
    public void PrescribingTrainer_ShouldBeRefusedTheMovesThatBelongToTheAthlete(WorkoutStatus from, WorkoutStatus to)
    {
        // Arrange
        var workout = APrescribedWorkoutIn(from);

        // Act
        var act = () => Ensure(workout, TrainerId, Role.TRAINER, to);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.COMPLETED)]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.DRAFT)]
    public void PrescribingTrainer_ShouldLeaveAMoveOutsideTheLifecycleToTheAggregate(WorkoutStatus from, WorkoutStatus to)
    {
        // Arrange
        var workout = APrescribedWorkoutIn(from);

        // Act
        var act = () => Ensure(workout, TrainerId, Role.TRAINER, to);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(WorkoutStatus.PUBLISHED, WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.STARTED, WorkoutStatus.COMPLETED)]
    public void AthleteOfAPrescribedWorkout_ShouldBeAllowedToMoveItForward(WorkoutStatus from, WorkoutStatus to)
    {
        // Arrange
        var workout = APrescribedWorkoutIn(from);

        // Act
        var act = () => Ensure(workout, AthleteId, Role.USER, to);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AthleteOfAPrescribedWorkout_ShouldBeRefusedToUnpublish()
    {
        // Arrange
        var workout = APrescribedWorkoutIn(WorkoutStatus.PUBLISHED);

        // Act
        var act = () => Ensure(workout, AthleteId, Role.USER, WorkoutStatus.DRAFT);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Theory]
    [InlineData(WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.COMPLETED)]
    [InlineData(WorkoutStatus.DRAFT)]
    public void AthleteOfAPrescribedDraft_ShouldBeRefusedWhateverTheTarget(WorkoutStatus target)
    {
        // Arrange
        var workout = APrescribedWorkoutIn(WorkoutStatus.DRAFT);

        // Act
        var act = () => Ensure(workout, AthleteId, Role.USER, target);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Fact]
    public void AthleteOfAPrescribedWorkout_ShouldLeaveAMoveOutsideTheLifecycleToTheAggregate()
    {
        // Arrange
        var workout = APrescribedWorkoutIn(WorkoutStatus.STARTED);

        // Act
        var act = () => Ensure(workout, AthleteId, Role.USER, WorkoutStatus.PUBLISHED);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.PUBLISHED, WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.PUBLISHED, WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.STARTED, WorkoutStatus.COMPLETED)]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.COMPLETED)]
    public void AthleteOfASelfPlannedWorkout_ShouldBeAllowedEveryMove(WorkoutStatus from, WorkoutStatus to)
    {
        // Arrange
        var workout = ASelfPlannedWorkoutIn(from);

        // Act
        var act = () => Ensure(workout, AthleteId, Role.USER, to);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(Role.TRAINER)]
    [InlineData(Role.USER)]
    public void UnrelatedCaller_ShouldBeRefusedEvenForAMoveInsideTheLifecycle(Role role)
    {
        // Arrange
        var workout = APrescribedWorkoutIn(WorkoutStatus.DRAFT);

        // Act
        var act = () => Ensure(workout, Id.New(), role, WorkoutStatus.PUBLISHED);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Fact]
    public void TrainerUnrelatedToASelfPlannedWorkout_ShouldBeRefused()
    {
        // Arrange
        var workout = ASelfPlannedWorkoutIn(WorkoutStatus.DRAFT);

        // Act
        var act = () => Ensure(workout, TrainerId, Role.TRAINER, WorkoutStatus.PUBLISHED);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    private const string ReviewOperation = "review workout";

    [Theory]
    [InlineData(WorkoutStatus.COMPLETED, WorkoutStatus.REVIEWED)]
    [InlineData(WorkoutStatus.REVIEWED, WorkoutStatus.COMPLETED)]
    public void EnsureCanChange_ShouldLeaveReviewedMovesOnTheStatusRouteToTheAggregate(WorkoutStatus from, WorkoutStatus to)
    {
        // Arrange
        var workout = Workout.Rehydrate(Id.New(), AthleteId, TrainerId, Name, null, WorkoutDate, from, [],
            reviewedAt: from == WorkoutStatus.REVIEWED ? WorkoutDate : null,
            reviewedBy: from == WorkoutStatus.REVIEWED ? TrainerId : null);

        // Act
        var act = () => _authorizer.EnsureCanChange(workout, TrainerId, Role.TRAINER, to, Operation);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanReview_ShouldAllowThePrescribingTrainer()
    {
        // Arrange
        var workout = APrescribedWorkoutIn(WorkoutStatus.COMPLETED);

        // Act
        var act = () => _authorizer.EnsureCanReview(workout, TrainerId, Role.TRAINER, ReviewOperation);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EnsureCanReview_ShouldAllowAnAdminWhateverTheWorkout(bool prescribed)
    {
        // Arrange
        var workout = prescribed ? APrescribedWorkoutIn(WorkoutStatus.COMPLETED) : ASelfPlannedWorkoutIn(WorkoutStatus.COMPLETED);

        // Act
        var act = () => _authorizer.EnsureCanReview(workout, Id.New(), Role.ADMIN, ReviewOperation);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanReview_ShouldRejectTheAthlete()
    {
        // Arrange
        var workout = APrescribedWorkoutIn(WorkoutStatus.COMPLETED);

        // Act
        var act = () => _authorizer.EnsureCanReview(workout, AthleteId, Role.USER, ReviewOperation);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Fact]
    public void EnsureCanReview_ShouldRejectATrainerWhoDidNotPrescribeTheWorkout()
    {
        // Arrange
        var workout = APrescribedWorkoutIn(WorkoutStatus.COMPLETED);

        // Act
        var act = () => _authorizer.EnsureCanReview(workout, Id.New(), Role.TRAINER, ReviewOperation);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Fact]
    public void EnsureCanReview_ShouldRejectTheAthleteOfAWorkoutWithoutTrainer()
    {
        // Arrange
        var workout = ASelfPlannedWorkoutIn(WorkoutStatus.COMPLETED);

        // Act
        var act = () => _authorizer.EnsureCanReview(workout, AthleteId, Role.USER, ReviewOperation);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }
}
