using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Workouts;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Application.Workouts;

public class GetWorkoutByIdUseCaseTests
{
    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);

    private readonly Mock<IUserRepository> _users = new();

    public GetWorkoutByIdUseCaseTests()
    {
        // Every requested user exists, which the foreign keys guarantee outside tests
        _users
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Id> ids, CancellationToken _) => ids.Select(AUser).ToList());
    }

    private GetWorkoutByIdUseCase AUseCase() => new(_workouts.Object, new WorkoutVisibility(_workouts.Object), _users.Object);

    private static User AUser(Id id) => User.Rehydrate(
        id, Email.From($"{id.Value}@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"),
        new DateOnly(1988, 5, 12), Role.USER, PasswordHash.From("stored-hash"), new DateTime(2026, 1, 1), true, null);

    private Workout StoredWorkout(Id ownerId, Id? trainerId = null, WorkoutStatus status = WorkoutStatus.PUBLISHED)
    {
        var workout = Workout.Rehydrate(Id.New(), ownerId, trainerId, Name, null, new DateTime(2026, 10, 1), status, []);
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
    [InlineData(WorkoutStatus.FINISHED)]
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
}
