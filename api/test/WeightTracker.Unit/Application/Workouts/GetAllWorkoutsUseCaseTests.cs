using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Workouts;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Application.Workouts;

public class GetAllWorkoutsUseCaseTests
{
    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);

    private readonly Mock<IExerciseRepository> _exercises = new(MockBehavior.Strict);

    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);

    private GetAllWorkoutsUseCase AUseCase() => new(new WorkoutVisibility(_workouts.Object), _exercises.Object, _users.Object);

    private static Workout AWorkout(Id ownerId, params Id[] exerciseIds) =>
        Workout.Rehydrate(
            Id.New(), ownerId, null, Name, null, new DateTime(2026, 10, 1), WorkoutStatus.DRAFT,
            exerciseIds.Select(exerciseId => WorkoutExercise.Rehydrate(
                Id.New(), exerciseId, Effort.MID, [new SetPrescription(3, new SetTarget.Reps(10))])));

    private static Exercise AnExercise(Id id) =>
        Exercise.Rehydrate(id, null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, []);

    private void NoExercisesStored() =>
        _exercises
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

    private static User AUser(Id id) => User.Rehydrate(
        id, Email.From($"{id.Value}@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"),
        new DateOnly(1988, 5, 12), Role.USER, PasswordHash.From("stored-hash"), new DateTime(2026, 1, 1), true, null);

    /// <summary>Every requested user exists, which the foreign keys guarantee outside tests.</summary>
    private void EveryUserStored() =>
        _users
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Id> ids, CancellationToken _) => ids.Select(AUser).ToList());

    [Fact]
    public async Task Admin_ShouldSeeEveryWorkout()
    {
        // Arrange
        var callerId = Id.New();
        var all = new[] { AWorkout(Id.New()), AWorkout(Id.New()) };
        _workouts.Setup(repository => repository.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(all);
        NoExercisesStored();
        EveryUserStored();

        // Act
        var result = await AUseCase().Execute(callerId, Role.ADMIN, CancellationToken.None);

        // Assert
        result.Workouts.Should().BeEquivalentTo(all);
        _workouts.Verify(repository => repository.GetAll(It.IsAny<CancellationToken>()), Times.Once);
        _workouts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Trainer_ShouldSeeOwnedAndTrainedWorkouts()
    {
        // Arrange
        var callerId = Id.New();
        var visible = new[] { AWorkout(callerId) };
        _workouts.Setup(repository => repository.GetAllByUserOrTrainerId(callerId, It.IsAny<CancellationToken>())).ReturnsAsync(visible);
        NoExercisesStored();
        EveryUserStored();

        // Act
        var result = await AUseCase().Execute(callerId, Role.TRAINER, CancellationToken.None);

        // Assert
        result.Workouts.Should().BeEquivalentTo(visible);
        _workouts.Verify(repository => repository.GetAllByUserOrTrainerId(callerId, It.IsAny<CancellationToken>()), Times.Once);
        _workouts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task User_ShouldOnlySeeOwnedWorkouts()
    {
        // Arrange
        var callerId = Id.New();
        var owned = new[] { AWorkout(callerId) };
        _workouts.Setup(repository => repository.GetAllByUserId(callerId, It.IsAny<CancellationToken>())).ReturnsAsync(owned);
        NoExercisesStored();
        EveryUserStored();

        // Act
        var result = await AUseCase().Execute(callerId, Role.USER, CancellationToken.None);

        // Assert
        result.Workouts.Should().BeEquivalentTo(owned);
        _workouts.Verify(repository => repository.GetAllByUserId(callerId, It.IsAny<CancellationToken>()), Times.Once);
        _workouts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UnknownRole_ShouldBeRejected()
    {
        // Arrange & Act
        var act = async () => await AUseCase().Execute(Id.New(), (Role)99, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        _workouts.VerifyNoOtherCalls();
        _exercises.VerifyNoOtherCalls();
        _users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldLoadEachReferencedExerciseOnce()
    {
        // Arrange
        var callerId = Id.New();
        var pressBanca = Id.New();
        var sentadilla = Id.New();
        var owned = new[] { AWorkout(callerId, pressBanca, sentadilla), AWorkout(callerId, pressBanca) };
        _workouts.Setup(repository => repository.GetAllByUserId(callerId, It.IsAny<CancellationToken>())).ReturnsAsync(owned);
        NoExercisesStored();
        EveryUserStored();

        // Act
        await AUseCase().Execute(callerId, Role.USER, CancellationToken.None);

        // Assert
        _exercises.Verify(repository => repository.GetAllByIds(
            It.Is<IEnumerable<Id>>(ids => ids.Count() == 2 && ids.Contains(pressBanca) && ids.Contains(sentadilla)),
            It.IsAny<CancellationToken>()), Times.Once);
        _exercises.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldReturnTheReferencedExercisesById()
    {
        // Arrange
        var callerId = Id.New();
        var pressBancaId = Id.New();
        var pressBanca = AnExercise(pressBancaId);
        _workouts.Setup(repository => repository.GetAllByUserId(callerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([AWorkout(callerId, pressBancaId)]);
        _exercises
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([pressBanca]);
        EveryUserStored();

        // Act
        var result = await AUseCase().Execute(callerId, Role.USER, CancellationToken.None);

        // Assert
        result.Exercises.Should().ContainSingle();
        result.Exercises[pressBancaId].Should().BeSameAs(pressBanca);
    }

    private static Workout APrescribedWorkout(Id ownerId, Id trainerId, WorkoutStatus status, params Id[] exerciseIds) =>
        Workout.Rehydrate(
            Id.New(), ownerId, trainerId, Name, null, new DateTime(2026, 10, 1), status,
            exerciseIds.Select(exerciseId => WorkoutExercise.Rehydrate(
                Id.New(), exerciseId, Effort.MID, [new SetPrescription(3, new SetTarget.Reps(10))])));

    [Fact]
    public async Task User_ShouldNotSeeThePrescribedDraftsTheyOwn()
    {
        // Arrange
        var callerId = Id.New();
        var trainerId = Id.New();
        var draft = APrescribedWorkout(callerId, trainerId, WorkoutStatus.DRAFT);
        var published = APrescribedWorkout(callerId, trainerId, WorkoutStatus.PUBLISHED);
        var selfPlannedDraft = AWorkout(callerId);
        _workouts.Setup(repository => repository.GetAllByUserId(callerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([draft, published, selfPlannedDraft]);
        NoExercisesStored();
        EveryUserStored();

        // Act
        var result = await AUseCase().Execute(callerId, Role.USER, CancellationToken.None);

        // Assert
        result.Workouts.Should().BeEquivalentTo([published, selfPlannedDraft]);
        result.Workouts.Should().NotContain(draft);
    }

    [Fact]
    public async Task Trainer_ShouldSeeTheDraftsTheyPrescribed()
    {
        // Arrange
        var callerId = Id.New();
        var draft = APrescribedWorkout(Id.New(), callerId, WorkoutStatus.DRAFT);
        _workouts.Setup(repository => repository.GetAllByUserOrTrainerId(callerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([draft]);
        NoExercisesStored();
        EveryUserStored();

        // Act
        var result = await AUseCase().Execute(callerId, Role.TRAINER, CancellationToken.None);

        // Assert
        result.Workouts.Should().BeEquivalentTo([draft]);
    }

    [Fact]
    public async Task Trainer_ShouldNotSeeAPrescribedDraftOfWhichTheyAreTheAthlete()
    {
        // Arrange
        var callerId = Id.New();
        var prescribedToThem = APrescribedWorkout(callerId, Id.New(), WorkoutStatus.DRAFT);
        var prescribedByThem = APrescribedWorkout(Id.New(), callerId, WorkoutStatus.DRAFT);
        _workouts.Setup(repository => repository.GetAllByUserOrTrainerId(callerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([prescribedToThem, prescribedByThem]);
        NoExercisesStored();
        EveryUserStored();

        // Act
        var result = await AUseCase().Execute(callerId, Role.TRAINER, CancellationToken.None);

        // Assert
        result.Workouts.Should().BeEquivalentTo([prescribedByThem]);
        result.Workouts.Should().NotContain(prescribedToThem);
    }

    [Fact]
    public async Task Admin_ShouldSeeEveryDraft()
    {
        // Arrange
        var draft = APrescribedWorkout(Id.New(), Id.New(), WorkoutStatus.DRAFT);
        _workouts.Setup(repository => repository.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([draft]);
        NoExercisesStored();
        EveryUserStored();

        // Act
        var result = await AUseCase().Execute(Id.New(), Role.ADMIN, CancellationToken.None);

        // Assert
        result.Workouts.Should().BeEquivalentTo([draft]);
    }

    [Fact]
    public async Task User_ShouldNotLoadTheExercisesOfAHiddenDraft()
    {
        // Arrange
        var callerId = Id.New();
        var hiddenExercise = Id.New();
        var draft = APrescribedWorkout(callerId, Id.New(), WorkoutStatus.DRAFT, hiddenExercise);
        _workouts.Setup(repository => repository.GetAllByUserId(callerId, It.IsAny<CancellationToken>())).ReturnsAsync([draft]);
        NoExercisesStored();
        EveryUserStored();

        // Act
        await AUseCase().Execute(callerId, Role.USER, CancellationToken.None);

        // Assert
        _exercises.Verify(repository => repository.GetAllByIds(
            It.Is<IEnumerable<Id>>(ids => ids.Contains(hiddenExercise) == false),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ShouldLoadEachReferencedOwnerAndTrainerOnce()
    {
        // Arrange
        var athlete = Id.New();
        var otherAthlete = Id.New();
        var trainer = Id.New();
        var all = new[]
        {
            AWorkout(athlete),
            APrescribedWorkout(athlete, trainer, WorkoutStatus.PUBLISHED),
            APrescribedWorkout(otherAthlete, trainer, WorkoutStatus.DRAFT),
        };
        _workouts.Setup(repository => repository.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(all);
        NoExercisesStored();
        EveryUserStored();

        // Act
        await AUseCase().Execute(Id.New(), Role.ADMIN, CancellationToken.None);

        // Assert
        _users.Verify(repository => repository.GetAllByIds(
            It.Is<IEnumerable<Id>>(ids => ids.Count() == 3 && ids.Contains(athlete) && ids.Contains(otherAthlete) && ids.Contains(trainer)),
            It.IsAny<CancellationToken>()), Times.Once);
        _users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldNotAskForATrainerOfASelfPlannedWorkout()
    {
        // Arrange
        var callerId = Id.New();
        _workouts.Setup(repository => repository.GetAllByUserId(callerId, It.IsAny<CancellationToken>())).ReturnsAsync([AWorkout(callerId)]);
        NoExercisesStored();
        EveryUserStored();

        // Act
        await AUseCase().Execute(callerId, Role.USER, CancellationToken.None);

        // Assert
        _users.Verify(repository => repository.GetAllByIds(
            It.Is<IEnumerable<Id>>(ids => ids.SequenceEqual(new[] { callerId })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EmptyList_ShouldReadUsersWithAnEmptyIdSet()
    {
        // Arrange
        var callerId = Id.New();
        _workouts.Setup(repository => repository.GetAllByUserId(callerId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        NoExercisesStored();
        EveryUserStored();

        // Act
        var result = await AUseCase().Execute(callerId, Role.USER, CancellationToken.None);

        // Assert
        result.Users.Should().BeEmpty();
        _users.Verify(repository => repository.GetAllByIds(
            It.Is<IEnumerable<Id>>(ids => ids.Any() == false),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ShouldReturnTheReferencedUsersById()
    {
        // Arrange
        var callerId = Id.New();
        var trainerId = Id.New();
        _workouts.Setup(repository => repository.GetAllByUserId(callerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([APrescribedWorkout(callerId, trainerId, WorkoutStatus.PUBLISHED)]);
        NoExercisesStored();
        EveryUserStored();

        // Act
        var result = await AUseCase().Execute(callerId, Role.USER, CancellationToken.None);

        // Assert
        result.Users.Keys.Should().BeEquivalentTo([callerId, trainerId]);
        result.Users[trainerId].Id.Should().Be(trainerId);
    }

    [Fact]
    public async Task MissingUser_ShouldFailInsteadOfReturningAPartialItem()
    {
        // Arrange
        var callerId = Id.New();
        var trainerId = Id.New();
        _workouts.Setup(repository => repository.GetAllByUserId(callerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([APrescribedWorkout(callerId, trainerId, WorkoutStatus.PUBLISHED)]);
        NoExercisesStored();
        _users
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([AUser(callerId)]);

        // Act
        var act = async () => await AUseCase().Execute(callerId, Role.USER, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
