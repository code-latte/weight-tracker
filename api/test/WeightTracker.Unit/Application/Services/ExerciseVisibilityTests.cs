using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Application.Services;

public class ExerciseVisibilityTests
{
    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);

    private ExerciseVisibility AVisibility() => new(new WorkoutVisibility(_workouts.Object));

    private static AuthenticatedUser ACaller(Id id, Role role)
        => new(id, Email.From($"{id.Value}@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"), role);

    private static Exercise ACustomExercise(Id ownerId)
        => Exercise.Rehydrate(Id.New(), null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, [], ownerId);

    private static Exercise ASystemExercise()
        => Exercise.Rehydrate(Id.New(), null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, []);

    private static Workout AWorkoutUsing(Exercise exercise, Id ownerId, Id? trainerId, WorkoutStatus status)
        => Workout.Rehydrate(
            Id.New(), ownerId, trainerId, Name, null, new DateTime(2026, 10, 1), status,
            [WorkoutExercise.Rehydrate(Id.New(), exercise.Id, Effort.MID, [new SetPrescription(3, new SetTarget.Reps(10))])]);

    [Fact]
    public async Task SystemExercise_ShouldBeVisibleToAnAnonymousCallerWithoutReadingWorkouts()
    {
        // Arrange
        var exercise = ASystemExercise();

        // Act
        var visible = await AVisibility().IsVisible(exercise, null, CancellationToken.None);

        // Assert
        visible.Should().BeTrue();
        _workouts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CustomExercise_ShouldBeInvisibleToAnAnonymousCaller()
    {
        // Arrange
        var exercise = ACustomExercise(Id.New());

        // Act
        var visible = await AVisibility().IsVisible(exercise, null, CancellationToken.None);

        // Assert
        visible.Should().BeFalse();
    }

    [Theory]
    [InlineData(Role.USER)]
    [InlineData(Role.TRAINER)]
    public async Task CustomExercise_ShouldBeVisibleToItsOwnerWithoutReadingWorkouts(Role role)
    {
        // Arrange
        var ownerId = Id.New();
        var exercise = ACustomExercise(ownerId);

        // Act
        var visible = await AVisibility().IsVisible(exercise, ACaller(ownerId, role), CancellationToken.None);

        // Assert
        visible.Should().BeTrue();
        _workouts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CustomExercise_ShouldBeVisibleToAnAdminWithoutReadingWorkouts()
    {
        // Arrange
        var exercise = ACustomExercise(Id.New());

        // Act
        var visible = await AVisibility().IsVisible(exercise, ACaller(Id.New(), Role.ADMIN), CancellationToken.None);

        // Assert
        visible.Should().BeTrue();
        _workouts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CustomExercise_ShouldBeVisibleToAnAthleteWhoseVisibleWorkoutUsesIt()
    {
        // Arrange
        var trainerId = Id.New();
        var athleteId = Id.New();
        var exercise = ACustomExercise(trainerId);
        _workouts.Setup(repository => repository.GetAllByUserId(athleteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([AWorkoutUsing(exercise, athleteId, trainerId, WorkoutStatus.PUBLISHED)]);

        // Act
        var visible = await AVisibility().IsVisible(exercise, ACaller(athleteId, Role.USER), CancellationToken.None);

        // Assert
        visible.Should().BeTrue();
    }

    [Fact]
    public async Task CustomExercise_ShouldBeInvisibleToAnAthleteWhenOnlyAHiddenDraftUsesIt()
    {
        // Arrange
        var trainerId = Id.New();
        var athleteId = Id.New();
        var exercise = ACustomExercise(trainerId);
        _workouts.Setup(repository => repository.GetAllByUserId(athleteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([AWorkoutUsing(exercise, athleteId, trainerId, WorkoutStatus.DRAFT)]);

        // Act
        var visible = await AVisibility().IsVisible(exercise, ACaller(athleteId, Role.USER), CancellationToken.None);

        // Assert
        visible.Should().BeFalse();
    }

    [Fact]
    public async Task CustomExercise_ShouldBeInvisibleToACallerWhoseWorkoutsDoNotUseIt()
    {
        // Arrange
        var callerId = Id.New();
        var exercise = ACustomExercise(Id.New());
        _workouts.Setup(repository => repository.GetAllByUserOrTrainerId(callerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([AWorkoutUsing(ASystemExercise(), Id.New(), callerId, WorkoutStatus.PUBLISHED)]);

        // Act
        var visible = await AVisibility().IsVisible(exercise, ACaller(callerId, Role.TRAINER), CancellationToken.None);

        // Assert
        visible.Should().BeFalse();
    }

    [Fact]
    public async Task EnsureVisible_ShouldFailAsIfTheExerciseDidNotExist()
    {
        // Arrange
        var exercise = ACustomExercise(Id.New());

        // Act
        var act = async () => await AVisibility().EnsureVisible(exercise, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotFoundException>();
    }
}
