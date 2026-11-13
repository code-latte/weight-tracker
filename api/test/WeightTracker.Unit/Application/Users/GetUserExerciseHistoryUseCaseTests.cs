using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Users;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Application.Users;

public class GetUserExerciseHistoryUseCaseTests
{
    // Strict: a read the use case must not make fails the test
    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);

    private readonly Mock<IExerciseRepository> _exercises = new(MockBehavior.Strict);

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);

    private readonly Mock<IExerciseHistoryQuery> _history = new(MockBehavior.Strict);

    private GetUserExerciseHistoryUseCase AUseCase()
        => new(_users.Object, _exercises.Object, new ExerciseVisibility(new WorkoutVisibility(_workouts.Object)), _history.Object);

    private static Exercise ASystemExercise()
        => Exercise.Rehydrate(Id.New(), null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, []);

    private static Exercise ACustomExercise(Id ownerId)
        => Exercise.Rehydrate(Id.New(), null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, [], ownerId);

    private static User AUser(Id id) => User.Rehydrate(
        id, Email.From($"{id.Value}@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"),
        new DateOnly(1988, 5, 12), Role.USER, PasswordHash.From("stored-hash"), new DateTime(2026, 1, 1), true, null);

    private void GivenUser(Id userId)
        => _users.Setup(repository => repository.GetById(userId, It.IsAny<CancellationToken>())).ReturnsAsync(AUser(userId));

    private Exercise GivenExercise()
    {
        var exercise = ASystemExercise();
        _exercises.Setup(repository => repository.GetById(exercise.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exercise);

        return exercise;
    }

    /// <summary>The sessions the projection returns, capturing the trainer filter and the limit it was asked with.</summary>
    private (Func<Id?> PrescribedBy, Func<int> Limit) GivenSessions(Id userId, Exercise exercise, params ExerciseSessionView[] sessions)
    {
        Id? prescribedBy = Id.New();
        var limit = 0;
        _history
            .Setup(query => query.Get(userId, exercise.Id, It.IsAny<Id?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback((Id _, Id _, Id? requestedBy, int requestedLimit, CancellationToken _) =>
            {
                prescribedBy = requestedBy;
                limit = requestedLimit;
            })
            .ReturnsAsync(sessions);

        return (() => prescribedBy, () => limit);
    }

    private static ExerciseSessionView ASession(params LastPerformanceSetView[] sets)
        => new(Id.New(), new DateOnly(2026, 11, 10), sets);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(51)]
    public async Task Execute_ShouldRejectALimitOutsideTheRangeWithoutReadingAnything(int limit)
    {
        // Arrange
        var userId = Id.New();

        // Act
        var act = () => AUseCase().Execute(userId, Role.USER, userId, Id.New(), limit, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidHistoryLimitException>();
        _history.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public async Task Execute_ShouldPassAnAcceptedLimitToTheProjection(int limit)
    {
        // Arrange
        var userId = Id.New();
        GivenUser(userId);
        var exercise = GivenExercise();
        var asked = GivenSessions(userId, exercise);

        // Act
        await AUseCase().Execute(userId, Role.USER, userId, exercise.Id, limit, CancellationToken.None);

        // Assert
        asked.Limit().Should().Be(limit);
    }

    [Fact]
    public async Task Execute_ShouldUseTheDefaultLimitWhenNoneIsGiven()
    {
        // Arrange
        var userId = Id.New();
        GivenUser(userId);
        var exercise = GivenExercise();
        var asked = GivenSessions(userId, exercise);

        // Act
        await AUseCase().Execute(userId, Role.USER, userId, exercise.Id, null, CancellationToken.None);

        // Assert
        asked.Limit().Should().Be(10);
    }

    [Fact]
    public async Task Execute_ShouldRefuseAUserAskingForAnotherUserWithoutLookingAnythingUp()
    {
        // Arrange & Act
        var act = () => AUseCase().Execute(Id.New(), Role.USER, Id.New(), Id.New(), null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        _users.VerifyNoOtherCalls();
        _exercises.VerifyNoOtherCalls();
        _history.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldThrowWhenTheUserDoesNotExist()
    {
        // Arrange
        var userId = Id.New();
        _users.Setup(repository => repository.GetById(userId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.ADMIN, userId, Id.New(), null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
        _history.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldThrowWhenTheExerciseDoesNotExist()
    {
        // Arrange
        var userId = Id.New();
        var exerciseId = Id.New();
        GivenUser(userId);
        _exercises.Setup(repository => repository.GetById(exerciseId, It.IsAny<CancellationToken>())).ReturnsAsync((Exercise?)null);

        // Act
        var act = () => AUseCase().Execute(userId, Role.USER, userId, exerciseId, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotFoundException>();
        _history.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldThrowForACustomExerciseOfAnotherUserWithoutReadingTheHistory()
    {
        // Arrange
        var userId = Id.New();
        GivenUser(userId);
        var hidden = ACustomExercise(Id.New());
        _exercises.Setup(repository => repository.GetById(hidden.Id, It.IsAny<CancellationToken>())).ReturnsAsync(hidden);
        _workouts.Setup(repository => repository.GetAllByUserId(userId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        // Act
        var act = () => AUseCase().Execute(userId, Role.USER, userId, hidden.Id, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotFoundException>();
        _history.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldReturnAnEmptyListWhenThereAreNoSessions()
    {
        // Arrange
        var userId = Id.New();
        GivenUser(userId);
        var exercise = GivenExercise();
        GivenSessions(userId, exercise);

        // Act
        var result = await AUseCase().Execute(userId, Role.USER, userId, exercise.Id, null, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldNotScopeTheSessionsForAUserReadingThemselves()
    {
        // Arrange
        var userId = Id.New();
        GivenUser(userId);
        var exercise = GivenExercise();
        var asked = GivenSessions(userId, exercise);

        // Act
        await AUseCase().Execute(userId, Role.USER, userId, exercise.Id, null, CancellationToken.None);

        // Assert
        asked.PrescribedBy().Should().BeNull();
    }

    [Fact]
    public async Task Execute_ShouldNotScopeTheSessionsForATrainerReadingThemselves()
    {
        // Arrange
        var trainerId = Id.New();
        GivenUser(trainerId);
        var exercise = GivenExercise();
        var asked = GivenSessions(trainerId, exercise);

        // Act
        await AUseCase().Execute(trainerId, Role.TRAINER, trainerId, exercise.Id, null, CancellationToken.None);

        // Assert
        asked.PrescribedBy().Should().BeNull();
    }

    [Fact]
    public async Task Execute_ShouldScopeTheSessionsToTheTrainerWhenATrainerReadsAnotherUser()
    {
        // Arrange
        var trainerId = Id.New();
        var athleteId = Id.New();
        GivenUser(athleteId);
        var exercise = GivenExercise();
        var asked = GivenSessions(athleteId, exercise);

        // Act
        await AUseCase().Execute(trainerId, Role.TRAINER, athleteId, exercise.Id, null, CancellationToken.None);

        // Assert
        asked.PrescribedBy().Should().Be(trainerId);
    }

    [Fact]
    public async Task Execute_ShouldNotScopeTheSessionsForAnAdmin()
    {
        // Arrange
        var athleteId = Id.New();
        GivenUser(athleteId);
        var exercise = GivenExercise();
        var asked = GivenSessions(athleteId, exercise);

        // Act
        await AUseCase().Execute(Id.New(), Role.ADMIN, athleteId, exercise.Id, null, CancellationToken.None);

        // Assert
        asked.PrescribedBy().Should().BeNull();
    }

    [Fact]
    public async Task Execute_ShouldComputeTheMaxWeightVolumeAndEstimatedOneRepMaxOfEachSession()
    {
        // Arrange
        var userId = Id.New();
        GivenUser(userId);
        var exercise = GivenExercise();
        var newest = ASession(new LastPerformanceSetView(10, null, 60m), new LastPerformanceSetView(3, null, 70m), new LastPerformanceSetView(8, null, null));
        var oldest = ASession(new LastPerformanceSetView(10, null, 40m), new LastPerformanceSetView(8, null, 50m));
        GivenSessions(userId, exercise, newest, oldest);

        // Act
        var result = await AUseCase().Execute(userId, Role.USER, userId, exercise.Id, null, CancellationToken.None);

        // Assert
        result.Should().Equal(
            new ExerciseHistorySessionView(newest.WorkoutId, newest.WorkoutDate, 70m, 810m, 80m),
            new ExerciseHistorySessionView(oldest.WorkoutId, oldest.WorkoutDate, 50m, 800m, 63.33m));
    }

    [Fact]
    public async Task Execute_ShouldReturnNullNumbersForASessionWithoutWeight()
    {
        // Arrange
        var userId = Id.New();
        GivenUser(userId);
        var exercise = GivenExercise();
        var session = ASession(new LastPerformanceSetView(10, null, null), new LastPerformanceSetView(8, null, null));
        GivenSessions(userId, exercise, session);

        // Act
        var result = await AUseCase().Execute(userId, Role.USER, userId, exercise.Id, null, CancellationToken.None);

        // Assert
        result.Should().Equal(new ExerciseHistorySessionView(session.WorkoutId, session.WorkoutDate, null, null, null));
    }

    [Fact]
    public async Task Execute_ShouldKeepTheMaxWeightButNotVolumeOrEstimateForATimedSession()
    {
        // Arrange
        var userId = Id.New();
        GivenUser(userId);
        var exercise = GivenExercise();
        var session = ASession(new LastPerformanceSetView(null, 45, 10m), new LastPerformanceSetView(null, 40, 15m));
        GivenSessions(userId, exercise, session);

        // Act
        var result = await AUseCase().Execute(userId, Role.USER, userId, exercise.Id, null, CancellationToken.None);

        // Assert
        result.Should().Equal(new ExerciseHistorySessionView(session.WorkoutId, session.WorkoutDate, 15m, null, null));
    }

    [Fact]
    public async Task Execute_ShouldReturnAZeroVolumeAndMaxWeightForABodyweightSession()
    {
        // Arrange
        var userId = Id.New();
        GivenUser(userId);
        var exercise = GivenExercise();
        var session = ASession(new LastPerformanceSetView(12, null, 0m));
        GivenSessions(userId, exercise, session);

        // Act
        var result = await AUseCase().Execute(userId, Role.USER, userId, exercise.Id, null, CancellationToken.None);

        // Assert
        result.Should().Equal(new ExerciseHistorySessionView(session.WorkoutId, session.WorkoutDate, 0m, 0m, 0m));
    }
}
