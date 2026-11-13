using FluentAssertions;
using Moq;
using WeightTracker.Application.Common;
using WeightTracker.Application.EventHandlers;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts;
using WeightTracker.Domain.Workouts.Observations;
using Xunit;

namespace WeightTracker.Unit.Application.EventHandlers;

public class GenerateWorkoutObservationsOnCompletedTests
{
    [Fact]
    public async Task HandleAsync_ShouldGenerateTheObservationsOfTheCompletedWorkout()
    {
        // Arrange
        var workoutId = Id.New();
        var workouts = new Mock<IWorkoutRepository>(MockBehavior.Strict);
        workouts.Setup(repository => repository.GetById(workoutId, It.IsAny<CancellationToken>())).ReturnsAsync((Workout?)null);
        var thresholds = new ObservationThresholds(3, 4, 3, 70, 15, 5, 10);
        var service = new WorkoutObservationService(
            workouts.Object,
            Mock.Of<IExerciseExecutionHistoryQuery>(),
            Mock.Of<IWorkoutObservationsRepository>(),
            new WorkoutObservationGenerator(thresholds),
            thresholds,
            Mock.Of<IUnitOfWork>(),
            Mock.Of<IClock>());
        var handler = new GenerateWorkoutObservationsOnCompleted(service);

        // Act
        var act = () => handler.HandleAsync(new WorkoutCompletedEvent(workoutId, DateTime.UtcNow), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WeightTracker.Application.Exceptions.WorkoutNotFoundException>();
        workouts.Verify(repository => repository.GetById(workoutId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
