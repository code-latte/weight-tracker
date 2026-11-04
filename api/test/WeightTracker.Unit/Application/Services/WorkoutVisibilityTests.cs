using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Application.Services;

public class WorkoutVisibilityTests
{
    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);

    private WorkoutVisibility AVisibility() => new(_workouts.Object);

    private static Workout AWorkout(Id ownerId, Id? trainerId = null, WorkoutStatus status = WorkoutStatus.DRAFT)
        => Workout.Rehydrate(Id.New(), ownerId, trainerId, Name, null, new DateTime(2026, 10, 1), status, []);

    [Fact]
    public async Task Admin_ShouldSeeEveryWorkoutIncludingDrafts()
    {
        // Arrange
        var all = new[] { AWorkout(Id.New(), Id.New()), AWorkout(Id.New()) };
        _workouts.Setup(repository => repository.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(all);

        // Act
        var visible = await AVisibility().VisibleTo(Id.New(), Role.ADMIN, CancellationToken.None);

        // Assert
        visible.Should().BeEquivalentTo(all);
    }

    [Fact]
    public async Task Trainer_ShouldSeeWhatTheyPrescribeButNotAPrescribedDraftOfWhichTheyAreTheAthlete()
    {
        // Arrange
        var callerId = Id.New();
        var prescribedByThem = AWorkout(Id.New(), callerId);
        var prescribedToThem = AWorkout(callerId, Id.New());
        var ownTrainerless = AWorkout(callerId);
        _workouts.Setup(repository => repository.GetAllByUserOrTrainerId(callerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([prescribedByThem, prescribedToThem, ownTrainerless]);

        // Act
        var visible = await AVisibility().VisibleTo(callerId, Role.TRAINER, CancellationToken.None);

        // Assert
        visible.Should().BeEquivalentTo([prescribedByThem, ownTrainerless]);
        visible.Should().NotContain(prescribedToThem);
    }

    [Fact]
    public async Task User_ShouldNotSeeAPrescribedDraftButSeesItOncePublished()
    {
        // Arrange
        var callerId = Id.New();
        var trainerId = Id.New();
        var draft = AWorkout(callerId, trainerId);
        var published = AWorkout(callerId, trainerId, WorkoutStatus.PUBLISHED);
        _workouts.Setup(repository => repository.GetAllByUserId(callerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([draft, published]);

        // Act
        var visible = await AVisibility().VisibleTo(callerId, Role.USER, CancellationToken.None);

        // Assert
        visible.Should().BeEquivalentTo([published]);
    }

    [Fact]
    public async Task UnknownRole_ShouldBeRejectedBeforeAnyRead()
    {
        // Arrange & Act
        var act = async () => await AVisibility().VisibleTo(Id.New(), (Role)99, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        _workouts.VerifyNoOtherCalls();
    }
}
