using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Application.UseCases.Users;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Application.Users;

public class GetMyExerciseLastPerformanceUseCaseTests
{
    private readonly Mock<IExerciseRepository> _exercises = new(MockBehavior.Strict);

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);

    private readonly Mock<IExerciseLastPerformanceQuery> _lastPerformance = new(MockBehavior.Strict);

    private GetMyExerciseLastPerformanceUseCase AUseCase()
        => new(_exercises.Object, new ExerciseVisibility(new WorkoutVisibility(_workouts.Object)), _lastPerformance.Object);

    private static Exercise ASystemExercise()
        => Exercise.Rehydrate(Id.New(), null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, []);

    private static Exercise ACustomExercise(Id ownerId)
        => Exercise.Rehydrate(Id.New(), null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, [], ownerId);

    private static AuthenticatedUser ACaller(Id id, Role role)
        => new(id, Email.From($"{id.Value}@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"), role);

    [Fact]
    public async Task Execute_ShouldReturnTheLastPerformanceOfTheCaller()
    {
        // Arrange
        var caller = ACaller(Id.New(), Role.USER);
        var exercise = ASystemExercise();
        var stored = new ExerciseLastPerformanceView(
            new DateOnly(2026, 11, 10),
            Effort.MID,
            [new LastPerformanceSetView(10, null, null), new LastPerformanceSetView(null, 45, 10m)]);
        _exercises.Setup(repository => repository.GetById(exercise.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exercise);
        _lastPerformance.Setup(query => query.Get(caller.Id, exercise.Id, It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        // Act
        var result = await AUseCase().Execute(exercise.Id, caller, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(stored);
    }

    [Fact]
    public async Task Execute_ShouldReturnNullWhenTheCallerNeverDidTheExercise()
    {
        // Arrange
        var caller = ACaller(Id.New(), Role.USER);
        var exercise = ASystemExercise();
        _exercises.Setup(repository => repository.GetById(exercise.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exercise);
        _lastPerformance.Setup(query => query.Get(caller.Id, exercise.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExerciseLastPerformanceView?)null);

        // Act
        var result = await AUseCase().Execute(exercise.Id, caller, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Execute_ShouldReturnTheLastPerformanceOfACustomExerciseToItsOwner()
    {
        // Arrange
        var caller = ACaller(Id.New(), Role.USER);
        var exercise = ACustomExercise(caller.Id);
        var stored = new ExerciseLastPerformanceView(new DateOnly(2026, 11, 10), Effort.LOW, [new LastPerformanceSetView(8, null, 20m)]);
        _exercises.Setup(repository => repository.GetById(exercise.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exercise);
        _lastPerformance.Setup(query => query.Get(caller.Id, exercise.Id, It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        // Act
        var result = await AUseCase().Execute(exercise.Id, caller, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(stored);
    }

    [Fact]
    public async Task Execute_ShouldRejectAnUnknownExerciseWithoutReadingTheLastPerformance()
    {
        // Arrange
        var caller = ACaller(Id.New(), Role.USER);
        var id = Id.New();
        _exercises.Setup(repository => repository.GetById(id, It.IsAny<CancellationToken>())).ReturnsAsync((Exercise?)null);

        // Act
        var act = () => AUseCase().Execute(id, caller, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotFoundException>();
        _lastPerformance.Verify(query => query.Get(It.IsAny<Id>(), It.IsAny<Id>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_ShouldRejectAHiddenCustomExerciseWithoutReadingTheLastPerformance()
    {
        // Arrange
        var caller = ACaller(Id.New(), Role.USER);
        var exercise = ACustomExercise(Id.New());
        _exercises.Setup(repository => repository.GetById(exercise.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exercise);
        _workouts.Setup(repository => repository.GetAllByUserId(caller.Id, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        // Act
        var act = () => AUseCase().Execute(exercise.Id, caller, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotFoundException>();
        _lastPerformance.Verify(query => query.Get(It.IsAny<Id>(), It.IsAny<Id>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
