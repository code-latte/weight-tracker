using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Application.UseCases.Exercises;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Application.Exercises;

public class GetExerciseByIdUseCaseTests
{
    private readonly Mock<IExerciseRepository> _exercises = new(MockBehavior.Strict);

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);

    private GetExerciseByIdUseCase AUseCase()
        => new(_exercises.Object, new ExerciseVisibility(new WorkoutVisibility(_workouts.Object)));

    private static Exercise ASystemExercise()
        => Exercise.Rehydrate(Id.New(), null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, []);

    private static Exercise ACustomExercise(Id ownerId)
        => Exercise.Rehydrate(Id.New(), null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, [], ownerId);

    private static AuthenticatedUser ACaller(Id id, Role role)
        => new(id, Email.From($"{id.Value}@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"), role);

    [Fact]
    public async Task Execute_ShouldReturnTheStoredSystemExerciseToAnAnonymousCaller()
    {
        // Arrange
        var stored = ASystemExercise();
        _exercises.Setup(repository => repository.GetById(stored.Id, It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        // Act
        var result = await AUseCase().Execute(stored.Id, null, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(stored);
    }

    [Fact]
    public async Task Execute_ShouldRejectAnUnknownExercise()
    {
        // Arrange
        var id = Id.New();
        _exercises.Setup(repository => repository.GetById(id, It.IsAny<CancellationToken>())).ReturnsAsync((Exercise?)null);

        // Act
        var act = () => AUseCase().Execute(id, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotFoundException>();
    }

    [Fact]
    public async Task Execute_ShouldReturnACustomExerciseToItsOwner()
    {
        // Arrange
        var ownerId = Id.New();
        var stored = ACustomExercise(ownerId);
        _exercises.Setup(repository => repository.GetById(stored.Id, It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        // Act
        var result = await AUseCase().Execute(stored.Id, ACaller(ownerId, Role.USER), CancellationToken.None);

        // Assert
        result.Should().BeSameAs(stored);
    }

    [Fact]
    public async Task Execute_ShouldHideACustomExerciseFromAnAnonymousCaller()
    {
        // Arrange
        var stored = ACustomExercise(Id.New());
        _exercises.Setup(repository => repository.GetById(stored.Id, It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        // Act
        var act = () => AUseCase().Execute(stored.Id, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotFoundException>();
    }
}
