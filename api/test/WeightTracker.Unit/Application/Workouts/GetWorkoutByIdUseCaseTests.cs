using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Workouts;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Application.Workouts;

public class GetWorkoutByIdUseCaseTests
{
    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);

    private readonly Mock<IUserRepository> _users = new();

    private readonly Mock<IExerciseRepository> _exercises = new();

    private readonly Mock<IMesocycleRepository> _mesocycles = new(MockBehavior.Strict);

    public GetWorkoutByIdUseCaseTests()
    {
        // Every requested user and exercise exists, which the foreign keys guarantee outside tests
        _users
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Id> ids, CancellationToken _) => ids.Select(AUser).ToList());
        _exercises
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Id> ids, CancellationToken _) => ids.Select(AnExercise).ToList());
    }

    private GetWorkoutByIdUseCase AUseCase() => new(
        _workouts.Object, new WorkoutVisibility(_workouts.Object), _users.Object, _exercises.Object, _mesocycles.Object);

    private static User AUser(Id id) => User.Rehydrate(
        id, Email.From($"{id.Value}@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"),
        new DateOnly(1988, 5, 12), Role.USER, PasswordHash.From("stored-hash"), new DateTime(2026, 1, 1), true, null);

    private static Exercise AnExercise(Id id) =>
        Exercise.Rehydrate(id, null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, []);

    private static WorkoutExercise AnEntry(Id exerciseId) =>
        WorkoutExercise.Rehydrate(Id.New(), exerciseId, Effort.MID, [new SetPrescription(3, new SetTarget.Reps(10))]);

    private Workout StoredWorkout(Id ownerId, Id? trainerId = null, WorkoutStatus status = WorkoutStatus.PUBLISHED)
        => Stored(Workout.Rehydrate(Id.New(), ownerId, trainerId, Name, null, new DateTime(2026, 10, 1), status, []));

    private Workout StoredWorkoutWith(params Id[] exerciseIds)
        => Stored(Workout.Rehydrate(
            Id.New(), Id.New(), null, Name, null, new DateTime(2026, 10, 1), WorkoutStatus.PUBLISHED,
            exerciseIds.Select(AnEntry)));

    private Workout StoredPlacedWorkout(Id ownerId, Id trainerId, Id mesocycleId)
        => Stored(Workout.Rehydrate(
            Id.New(), ownerId, trainerId, Name, null, new DateTime(2026, 10, 1), WorkoutStatus.PUBLISHED, [],
            mesocycleId, WeekNumber.From(1)));

    private Mesocycle StoredMesocycle(Id trainerId, Id athleteId)
    {
        var mesocycle = Mesocycle.Rehydrate(
            Id.New(), trainerId, athleteId, MesocycleName.From("Block"), MesocycleObjective.From("Objective"),
            new DateTime(2026, 10, 1), DurationInWeeks.From(4),
            [MesocycleWeek.Rehydrate(Id.New(), WeekNumber.From(1), TrainingWeekType.ADAPTATION, MesocycleObjective.From("Adapt"))]);
        _mesocycles
            .Setup(repository => repository.GetById(mesocycle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mesocycle);

        return mesocycle;
    }

    private Workout Stored(Workout workout)
    {
        _workouts
            .Setup(repository => repository.GetById(workout.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workout);

        return workout;
    }

    [Fact]
    public async Task Admin_ShouldReadAnyWorkout()
    {
        // Arrange
        var workout = StoredWorkout(Id.New(), Id.New());

        // Act
        var result = await AUseCase().Execute(Id.New(), Role.ADMIN, workout.Id, CancellationToken.None);

        // Assert
        result.Workout.Should().BeSameAs(workout);
    }

    [Fact]
    public async Task Trainer_ShouldReadAWorkoutTheyOwn()
    {
        // Arrange
        var callerId = Id.New();
        var workout = StoredWorkout(callerId);

        // Act
        var result = await AUseCase().Execute(callerId, Role.TRAINER, workout.Id, CancellationToken.None);

        // Assert
        result.Workout.Should().BeSameAs(workout);
    }

    [Fact]
    public async Task Trainer_ShouldReadAWorkoutTheyPrescribed()
    {
        // Arrange
        var callerId = Id.New();
        var workout = StoredWorkout(Id.New(), callerId);

        // Act
        var result = await AUseCase().Execute(callerId, Role.TRAINER, workout.Id, CancellationToken.None);

        // Assert
        result.Workout.Should().BeSameAs(workout);
    }

    [Fact]
    public async Task Trainer_ShouldNotReadAnUnrelatedWorkout()
    {
        // Arrange
        var workout = StoredWorkout(Id.New(), Id.New());

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.TRAINER, workout.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
    }

    [Fact]
    public async Task User_ShouldReadAWorkoutTheyOwn()
    {
        // Arrange
        var callerId = Id.New();
        var workout = StoredWorkout(callerId, Id.New());

        // Act
        var result = await AUseCase().Execute(callerId, Role.USER, workout.Id, CancellationToken.None);

        // Assert
        result.Workout.Should().BeSameAs(workout);
    }

    [Fact]
    public async Task User_ShouldNotReadAnotherUsersWorkout()
    {
        // Arrange
        var callerId = Id.New();
        var workout = StoredWorkout(Id.New(), callerId);

        // Act
        var act = () => AUseCase().Execute(callerId, Role.USER, workout.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
    }

    [Fact]
    public async Task ShouldFailWhenTheWorkoutDoesNotExist()
    {
        // Arrange
        var workoutId = Id.New();
        _workouts
            .Setup(repository => repository.GetById(workoutId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workout?)null);

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.ADMIN, workoutId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutNotFoundException>();
    }

    [Fact]
    public async Task User_ShouldNotReadAPrescribedDraftTheyOwn()
    {
        // Arrange
        var callerId = Id.New();
        var workout = StoredWorkout(callerId, Id.New(), WorkoutStatus.DRAFT);

        // Act
        var act = () => AUseCase().Execute(callerId, Role.USER, workout.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
    }

    [Fact]
    public async Task Trainer_ShouldNotReadAPrescribedDraftOfWhichTheyAreTheAthlete()
    {
        // Arrange
        var callerId = Id.New();
        var workout = StoredWorkout(callerId, Id.New(), WorkoutStatus.DRAFT);

        // Act
        var act = () => AUseCase().Execute(callerId, Role.TRAINER, workout.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
    }

    [Fact]
    public async Task User_ShouldReadASelfPlannedDraft()
    {
        // Arrange
        var callerId = Id.New();
        var workout = StoredWorkout(callerId, null, WorkoutStatus.DRAFT);

        // Act
        var result = await AUseCase().Execute(callerId, Role.USER, workout.Id, CancellationToken.None);

        // Assert
        result.Workout.Should().BeSameAs(workout);
    }

    [Fact]
    public async Task Trainer_ShouldReadTheDraftTheyPrescribed()
    {
        // Arrange
        var callerId = Id.New();
        var workout = StoredWorkout(Id.New(), callerId, WorkoutStatus.DRAFT);

        // Act
        var result = await AUseCase().Execute(callerId, Role.TRAINER, workout.Id, CancellationToken.None);

        // Assert
        result.Workout.Should().BeSameAs(workout);
    }

    [Fact]
    public async Task Admin_ShouldReadAPrescribedDraft()
    {
        // Arrange
        var workout = StoredWorkout(Id.New(), Id.New(), WorkoutStatus.DRAFT);

        // Act
        var result = await AUseCase().Execute(Id.New(), Role.ADMIN, workout.Id, CancellationToken.None);

        // Assert
        result.Workout.Should().BeSameAs(workout);
    }

    [Theory]
    [InlineData(WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.COMPLETED)]
    public async Task User_ShouldReadAPrescribedWorkoutOnceItIsPublished(WorkoutStatus status)
    {
        // Arrange
        var callerId = Id.New();
        var workout = StoredWorkout(callerId, Id.New(), status);

        // Act
        var result = await AUseCase().Execute(callerId, Role.USER, workout.Id, CancellationToken.None);

        // Assert
        result.Workout.Should().BeSameAs(workout);
    }

    [Fact]
    public async Task PrescribedWorkout_ShouldComeWithItsAthleteAndTrainer()
    {
        // Arrange
        var ownerId = Id.New();
        var trainerId = Id.New();
        var workout = StoredWorkout(ownerId, trainerId);

        // Act
        var result = await AUseCase().Execute(Id.New(), Role.ADMIN, workout.Id, CancellationToken.None);

        // Assert
        result.Athlete.Id.Should().Be(ownerId);
        result.Trainer!.Id.Should().Be(trainerId);
    }

    [Fact]
    public async Task SelfPlannedWorkout_ShouldComeWithItsAthleteAndNoTrainer()
    {
        // Arrange
        var ownerId = Id.New();
        var workout = StoredWorkout(ownerId);

        // Act
        var result = await AUseCase().Execute(ownerId, Role.USER, workout.Id, CancellationToken.None);

        // Assert
        result.Athlete.Id.Should().Be(ownerId);
        result.Trainer.Should().BeNull();
        _users.Verify(repository => repository.GetAllByIds(
            It.Is<IEnumerable<Id>>(ids => ids.SequenceEqual(new[] { ownerId })), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MissingUser_ShouldFailInsteadOfReturningAPartialWorkout()
    {
        // Arrange
        var workout = StoredWorkout(Id.New(), Id.New());
        _users
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var act = async () => await AUseCase().Execute(Id.New(), Role.ADMIN, workout.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RefusedRead_ShouldNotLookUpAnybody()
    {
        // Arrange
        var workout = StoredWorkout(Id.New(), Id.New());

        // Act
        var act = async () => await AUseCase().Execute(Id.New(), Role.USER, workout.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        _users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Workout_ShouldComeWithOnlyTheExercisesItsEntriesReference()
    {
        // Arrange
        var benchId = Id.New();
        var squatId = Id.New();
        var workout = StoredWorkoutWith(benchId, squatId);

        // Act
        var result = await AUseCase().Execute(Id.New(), Role.ADMIN, workout.Id, CancellationToken.None);

        // Assert
        result.Exercises.Keys.Should().BeEquivalentTo(new[] { benchId, squatId });
        _exercises.Verify(repository => repository.GetAllByIds(
            It.Is<IEnumerable<Id>>(ids => ids.SequenceEqual(new[] { benchId, squatId })), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PlacedWorkout_ShouldComeWithItsMesocycle()
    {
        // Arrange
        var ownerId = Id.New();
        var trainerId = Id.New();
        var mesocycle = StoredMesocycle(trainerId, ownerId);
        var workout = StoredPlacedWorkout(ownerId, trainerId, mesocycle.Id);

        // Act
        var result = await AUseCase().Execute(ownerId, Role.USER, workout.Id, CancellationToken.None);

        // Assert
        result.Mesocycle.Should().BeSameAs(mesocycle);
    }

    [Fact]
    public async Task StandaloneWorkout_ShouldComeWithoutAMesocycleAndNotLookOneUp()
    {
        // Arrange
        var ownerId = Id.New();
        var workout = StoredWorkout(ownerId);

        // Act
        var result = await AUseCase().Execute(ownerId, Role.USER, workout.Id, CancellationToken.None);

        // Assert
        result.Mesocycle.Should().BeNull();
        _mesocycles.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RefusedRead_ShouldNotLookUpExercisesOrTheMesocycle()
    {
        // Arrange
        var mesocycle = StoredMesocycle(Id.New(), Id.New());
        var workout = StoredPlacedWorkout(mesocycle.AthleteId, mesocycle.TrainerId, mesocycle.Id);

        // Act
        var act = async () => await AUseCase().Execute(Id.New(), Role.USER, workout.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        _exercises.VerifyNoOtherCalls();
        _mesocycles.Verify(repository => repository.GetById(It.IsAny<Id>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MissingWorkout_ShouldNotLookUpExercisesOrAMesocycle()
    {
        // Arrange
        var workoutId = Id.New();
        _workouts
            .Setup(repository => repository.GetById(workoutId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workout?)null);

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.ADMIN, workoutId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutNotFoundException>();
        _exercises.VerifyNoOtherCalls();
        _mesocycles.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MissingExercise_ShouldFailInsteadOfReturningAPartialWorkout()
    {
        // Arrange
        var workout = StoredWorkoutWith(Id.New());
        _exercises
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var act = async () => await AUseCase().Execute(Id.New(), Role.ADMIN, workout.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task MissingMesocycle_ShouldFailInsteadOfReturningAPartialWorkout()
    {
        // Arrange
        var mesocycleId = Id.New();
        var workout = StoredPlacedWorkout(Id.New(), Id.New(), mesocycleId);
        _mesocycles
            .Setup(repository => repository.GetById(mesocycleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mesocycle?)null);

        // Act
        var act = async () => await AUseCase().Execute(Id.New(), Role.ADMIN, workout.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
