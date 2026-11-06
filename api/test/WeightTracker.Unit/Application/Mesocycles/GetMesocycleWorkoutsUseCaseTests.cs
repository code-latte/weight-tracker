using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Mesocycles;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;
using static WeightTracker.Unit.Application.Mesocycles.MesocycleTestUsers;

namespace WeightTracker.Unit.Application.Mesocycles;

public class GetMesocycleWorkoutsUseCaseTests
{
    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private readonly Mock<IMesocycleRepository> _mesocycles = new(MockBehavior.Strict);

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);

    private readonly Mock<IExerciseRepository> _exercises = new();

    private readonly User _trainer = AStoredUser(Role.TRAINER);

    private readonly User _athlete = AStoredUser(Role.USER);

    public GetMesocycleWorkoutsUseCaseTests()
    {
        _exercises
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Exercise>());
    }

    private GetMesocycleWorkoutsUseCase AUseCase()
        => new(
            _mesocycles.Object, new MesocycleVisibility(_mesocycles.Object),
            _workouts.Object, new WorkoutVisibility(_workouts.Object),
            new WorkoutListLoader(_exercises.Object, UsersHolding(_trainer, _athlete).Object));

    private Mesocycle Stored()
    {
        var mesocycle = AStoredMesocycle(_trainer.Id, _athlete.Id);
        _mesocycles.Setup(repository => repository.GetById(mesocycle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mesocycle);

        return mesocycle;
    }

    private Workout APlacedWorkout(
        Mesocycle mesocycle, int week, DateTime date, Id? trainerId, WorkoutStatus status = WorkoutStatus.PUBLISHED)
        => Workout.Rehydrate(
            Id.New(), _athlete.Id, trainerId, Name, null, date, status, [], mesocycle.Id, WeekNumber.From(week));

    private void Placed(Mesocycle mesocycle, params Workout[] workouts)
        => _workouts.Setup(repository => repository.GetAllByMesocycleId(mesocycle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workouts);

    [Fact]
    public async Task UnknownMesocycle_ShouldBeNotFoundBeforeReadingWorkouts()
    {
        // Arrange
        var unknown = Id.New();
        _mesocycles.Setup(repository => repository.GetById(unknown, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mesocycle?)null);

        // Act
        var act = async () => await AUseCase().Execute(Id.New(), Role.ADMIN, unknown, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MesocycleNotFoundException>();
        _workouts.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(Role.TRAINER)]
    [InlineData(Role.USER)]
    public async Task CallerOutsideTheMesocycle_ShouldBeRefusedBeforeReadingWorkouts(Role role)
    {
        // Arrange
        var mesocycle = Stored();

        // Act
        var act = async () => await AUseCase().Execute(Id.New(), role, mesocycle.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        _workouts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Athlete_ShouldSeeThePublishedWorkoutsButNotAPrescribedDraft()
    {
        // Arrange
        var mesocycle = Stored();
        var published = APlacedWorkout(mesocycle, 1, new DateTime(2026, 11, 11), _trainer.Id);
        var draft = APlacedWorkout(mesocycle, 2, new DateTime(2026, 11, 18), _trainer.Id, WorkoutStatus.DRAFT);
        Placed(mesocycle, published, draft);

        // Act
        var result = await AUseCase().Execute(_athlete.Id, Role.USER, mesocycle.Id, CancellationToken.None);

        // Assert
        result.Workouts.Should().Equal(published);
    }

    [Fact]
    public async Task Trainer_ShouldSeeWhatTheyPrescribeIncludingDraftsButNotATrainerlessWorkout()
    {
        // Arrange
        var mesocycle = Stored();
        var draft = APlacedWorkout(mesocycle, 1, new DateTime(2026, 11, 11), _trainer.Id, WorkoutStatus.DRAFT);
        var trainerless = APlacedWorkout(mesocycle, 2, new DateTime(2026, 11, 18), null);
        Placed(mesocycle, draft, trainerless);

        // Act
        var result = await AUseCase().Execute(_trainer.Id, Role.TRAINER, mesocycle.Id, CancellationToken.None);

        // Assert
        result.Workouts.Should().Equal(draft);
    }

    [Fact]
    public async Task Admin_ShouldSeeEveryPlacedWorkoutWithItsPeople()
    {
        // Arrange
        var mesocycle = Stored();
        var draft = APlacedWorkout(mesocycle, 1, new DateTime(2026, 11, 11), _trainer.Id, WorkoutStatus.DRAFT);
        var trainerless = APlacedWorkout(mesocycle, 2, new DateTime(2026, 11, 18), null);
        Placed(mesocycle, draft, trainerless);

        // Act
        var result = await AUseCase().Execute(Id.New(), Role.ADMIN, mesocycle.Id, CancellationToken.None);

        // Assert
        result.Workouts.Should().BeEquivalentTo([draft, trainerless]);
        result.Users.Keys.Should().BeEquivalentTo([_trainer.Id, _athlete.Id]);
    }

    [Fact]
    public async Task MesocycleWithoutWorkouts_ShouldReturnAnEmptyList()
    {
        // Arrange
        var mesocycle = Stored();
        Placed(mesocycle);

        // Act
        var result = await AUseCase().Execute(_athlete.Id, Role.USER, mesocycle.Id, CancellationToken.None);

        // Assert
        result.Workouts.Should().BeEmpty();
    }

    [Fact]
    public async Task Workouts_ShouldBeOrderedByWeekAndThenByDate()
    {
        // Arrange
        var mesocycle = Stored();
        var secondWeek = APlacedWorkout(mesocycle, 2, new DateTime(2026, 11, 16), _trainer.Id);
        var firstWeekLater = APlacedWorkout(mesocycle, 1, new DateTime(2026, 11, 13), _trainer.Id);
        var firstWeekEarlier = APlacedWorkout(mesocycle, 1, new DateTime(2026, 11, 10), _trainer.Id);
        Placed(mesocycle, secondWeek, firstWeekLater, firstWeekEarlier);

        // Act
        var result = await AUseCase().Execute(Id.New(), Role.ADMIN, mesocycle.Id, CancellationToken.None);

        // Assert
        result.Workouts.Should().Equal(firstWeekEarlier, firstWeekLater, secondWeek);
    }
}
