using FluentAssertions;
using Moq;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Application.UseCases.Exercises;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Application.Exercises;

public class GetAllExercisesUseCaseTests
{
    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private readonly Mock<IExerciseRepository> _exercises = new(MockBehavior.Strict);

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);

    private GetAllExercisesUseCase AUseCase() => new(_exercises.Object, new WorkoutVisibility(_workouts.Object));

    private static Exercise ASystemExercise()
        => Exercise.Rehydrate(Id.New(), null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, []);

    private static Exercise ACustomExercise(Id ownerId)
        => Exercise.Rehydrate(Id.New(), null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, [], ownerId);

    private static AuthenticatedUser ACaller(Id id, Role role)
        => new(id, Email.From($"{id.Value}@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"), role);

    private static Workout AWorkoutUsing(Exercise exercise, Id ownerId, Id? trainerId, WorkoutStatus status)
        => Workout.Rehydrate(
            Id.New(), ownerId, trainerId, Name, null, new DateTime(2026, 10, 1), status,
            [WorkoutExercise.Rehydrate(Id.New(), exercise.Id, Effort.MID, [new SetPrescription(3, new SetTarget.Reps(10))])]);

    [Fact]
    public async Task Anonymous_ShouldGetTheSystemExercisesOnly()
    {
        // Arrange
        var system = ASystemExercise();
        _exercises.Setup(repository => repository.GetSystemAndOwnedBy(null, It.IsAny<CancellationToken>())).ReturnsAsync([system]);

        // Act
        var result = await AUseCase().Execute(null, CancellationToken.None);

        // Assert
        result.Should().Equal(system);
        _exercises.Verify(repository => repository.GetSystemAndOwnedBy(null, It.IsAny<CancellationToken>()), Times.Once);
        _exercises.VerifyNoOtherCalls();
        _workouts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Admin_ShouldGetEveryExercise()
    {
        // Arrange
        var all = new[] { ASystemExercise(), ACustomExercise(Id.New()) };
        _exercises.Setup(repository => repository.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(all);

        // Act
        var result = await AUseCase().Execute(ACaller(Id.New(), Role.ADMIN), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(all);
        _workouts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task User_ShouldGetSystemOwnAndTheExercisesOfTheirVisibleWorkouts()
    {
        // Arrange
        var callerId = Id.New();
        var trainerId = Id.New();
        var system = ASystemExercise();
        var own = ACustomExercise(callerId);
        var trainers = ACustomExercise(trainerId);
        _exercises.Setup(repository => repository.GetSystemAndOwnedBy(callerId, It.IsAny<CancellationToken>())).ReturnsAsync([system, own]);
        _workouts.Setup(repository => repository.GetAllByUserId(callerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([AWorkoutUsing(trainers, callerId, trainerId, WorkoutStatus.PUBLISHED), AWorkoutUsing(system, callerId, null, WorkoutStatus.DRAFT)]);
        _exercises.Setup(repository => repository.GetAllByIds(It.Is<IEnumerable<Id>>(ids => ids.SequenceEqual(new[] { trainers.Id })), It.IsAny<CancellationToken>()))
            .ReturnsAsync([trainers]);

        // Act
        var result = await AUseCase().Execute(ACaller(callerId, Role.USER), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo([system, own, trainers]);
    }

    [Fact]
    public async Task User_ShouldNotGetTheExerciseOfAHiddenPrescribedDraft()
    {
        // Arrange
        var callerId = Id.New();
        var trainerId = Id.New();
        var system = ASystemExercise();
        var hidden = ACustomExercise(trainerId);
        _exercises.Setup(repository => repository.GetSystemAndOwnedBy(callerId, It.IsAny<CancellationToken>())).ReturnsAsync([system]);
        _workouts.Setup(repository => repository.GetAllByUserId(callerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([AWorkoutUsing(hidden, callerId, trainerId, WorkoutStatus.DRAFT)]);

        // Act
        var result = await AUseCase().Execute(ACaller(callerId, Role.USER), CancellationToken.None);

        // Assert
        result.Should().Equal(system);
        _exercises.Verify(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Trainer_ShouldNotGetAnotherOwnersExerciseThatIsInNoVisibleWorkout()
    {
        // Arrange
        var callerId = Id.New();
        var system = ASystemExercise();
        _exercises.Setup(repository => repository.GetSystemAndOwnedBy(callerId, It.IsAny<CancellationToken>())).ReturnsAsync([system]);
        _workouts.Setup(repository => repository.GetAllByUserOrTrainerId(callerId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        // Act
        var result = await AUseCase().Execute(ACaller(callerId, Role.TRAINER), CancellationToken.None);

        // Assert
        result.Should().Equal(system);
    }

    [Fact]
    public async Task User_ShouldReadExercisesInAtMostThreeQueries()
    {
        // Arrange
        var callerId = Id.New();
        var trainerId = Id.New();
        var firstOfTrainer = ACustomExercise(trainerId);
        var secondOfTrainer = ACustomExercise(trainerId);
        _exercises.Setup(repository => repository.GetSystemAndOwnedBy(callerId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _workouts.Setup(repository => repository.GetAllByUserId(callerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                AWorkoutUsing(firstOfTrainer, callerId, trainerId, WorkoutStatus.PUBLISHED),
                AWorkoutUsing(firstOfTrainer, callerId, trainerId, WorkoutStatus.PUBLISHED),
                AWorkoutUsing(secondOfTrainer, callerId, trainerId, WorkoutStatus.PUBLISHED)]);
        _exercises.Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([firstOfTrainer, secondOfTrainer]);

        // Act
        await AUseCase().Execute(ACaller(callerId, Role.USER), CancellationToken.None);

        // Assert
        _exercises.Verify(repository => repository.GetAllByIds(
            It.Is<IEnumerable<Id>>(ids => ids.Count() == 2), It.IsAny<CancellationToken>()), Times.Once);
        _exercises.Verify(repository => repository.GetSystemAndOwnedBy(callerId, It.IsAny<CancellationToken>()), Times.Once);
        _workouts.Verify(repository => repository.GetAllByUserId(callerId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
