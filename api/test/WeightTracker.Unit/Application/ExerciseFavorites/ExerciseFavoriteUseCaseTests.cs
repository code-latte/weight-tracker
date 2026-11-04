using FluentAssertions;
using Moq;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.ExerciseFavorites;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.ExerciseFavorites;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Application.ExerciseFavorites;

/// <summary>
/// Covers the mark, unmark and list use cases of favorite exercises.
/// </summary>
public class ExerciseFavoriteUseCaseTests
{
    private static readonly DateTime Now = new(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IExerciseRepository> _exercises = new(MockBehavior.Strict);
    private readonly Mock<IExerciseFavoriteRepository> _favorites = new(MockBehavior.Strict);
    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);
    private readonly Mock<IClock> _clock = new();

    public ExerciseFavoriteUseCaseTests()
    {
        _clock.SetupGet(clock => clock.UtcNow).Returns(Now);
    }

    private ExerciseVisibility AVisibility() => new(new WorkoutVisibility(_workouts.Object));

    private MarkExerciseFavoriteUseCase AMark()
        => new(_exercises.Object, _favorites.Object, AVisibility(), _clock.Object, _unitOfWork.Object);

    private UnmarkExerciseFavoriteUseCase AnUnmark()
        => new(_exercises.Object, _favorites.Object, AVisibility(), _unitOfWork.Object);

    private GetExerciseFavoritesUseCase AList() => new(_favorites.Object);

    private static Exercise ASystemExercise()
        => Exercise.Rehydrate(Id.New(), null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, []);

    private static Exercise ACustomExercise(Id ownerId, DateTime? archivedAtUtc = null)
        => Exercise.Rehydrate(Id.New(), null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, [], ownerId, archivedAtUtc);

    private void GivenStored(Exercise exercise)
        => _exercises.Setup(repository => repository.GetById(exercise.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exercise);

    private void GivenNoFavorite(Id userId, Id exerciseId)
        => _favorites.Setup(repository => repository.GetByUserAndExercise(userId, exerciseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExerciseFavorite?)null);

    private void GivenFavorite(ExerciseFavorite favorite)
        => _favorites.Setup(repository => repository.GetByUserAndExercise(favorite.UserId, favorite.ExerciseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(favorite);

    private void GivenSaveAndCommitSucceed()
    {
        _favorites.Setup(repository => repository.Save(It.IsAny<ExerciseFavorite>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private void VerifyNothingWritten()
    {
        _favorites.Verify(repository => repository.Save(It.IsAny<ExerciseFavorite>(), It.IsAny<CancellationToken>()), Times.Never);
        _favorites.Verify(repository => repository.Remove(It.IsAny<ExerciseFavorite>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(Role.USER)]
    [InlineData(Role.TRAINER)]
    [InlineData(Role.ADMIN)]
    public async Task Mark_ShouldStoreANewFavoriteForAnyRoleAndCommitOnce(Role role)
    {
        // Arrange
        var callerId = Id.New();
        var exercise = ASystemExercise();
        GivenStored(exercise);
        GivenNoFavorite(callerId, exercise.Id);
        GivenSaveAndCommitSucceed();

        // Act
        var favorite = await AMark().Execute(callerId, role, exercise.Id, CancellationToken.None);

        // Assert
        favorite.UserId.Should().Be(callerId);
        favorite.ExerciseId.Should().Be(exercise.Id);
        favorite.CreatedAtUtc.Should().Be(Now);
        _favorites.Verify(repository => repository.Save(favorite, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Mark_ShouldReturnTheStoredFavoriteWithoutSavingWhenItAlreadyExists()
    {
        // Arrange
        var callerId = Id.New();
        var exercise = ASystemExercise();
        var stored = ExerciseFavorite.Rehydrate(Id.New(), callerId, exercise.Id, Now.AddDays(-2));
        GivenStored(exercise);
        GivenFavorite(stored);

        // Act
        var favorite = await AMark().Execute(callerId, Role.USER, exercise.Id, CancellationToken.None);

        // Assert
        favorite.Should().BeSameAs(stored);
        favorite.CreatedAtUtc.Should().Be(Now.AddDays(-2));
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Mark_ShouldRejectAnUnknownExercise()
    {
        // Arrange
        var exerciseId = Id.New();
        _exercises.Setup(repository => repository.GetById(exerciseId, It.IsAny<CancellationToken>())).ReturnsAsync((Exercise?)null);

        // Act
        var act = () => AMark().Execute(Id.New(), Role.USER, exerciseId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotFoundException>();
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Mark_ShouldTreatACustomExerciseTheCallerCannotSeeAsMissing()
    {
        // Arrange
        var callerId = Id.New();
        var exercise = ACustomExercise(Id.New());
        GivenStored(exercise);
        _workouts.Setup(repository => repository.GetAllByUserOrTrainerId(callerId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        // Act
        var act = () => AMark().Execute(callerId, Role.TRAINER, exercise.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotFoundException>();
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Mark_ShouldAcceptTheOwnersCustomExercise()
    {
        // Arrange
        var callerId = Id.New();
        var exercise = ACustomExercise(callerId);
        GivenStored(exercise);
        GivenNoFavorite(callerId, exercise.Id);
        GivenSaveAndCommitSucceed();

        // Act
        var favorite = await AMark().Execute(callerId, Role.TRAINER, exercise.Id, CancellationToken.None);

        // Assert
        favorite.ExerciseId.Should().Be(exercise.Id);
    }

    [Fact]
    public async Task Mark_ShouldRejectAnArchivedExerciseEvenWhenItIsAlreadyAFavorite()
    {
        // Arrange
        var callerId = Id.New();
        var exercise = ACustomExercise(callerId, archivedAtUtc: Now.AddDays(-1));
        GivenStored(exercise);

        // Act
        var act = () => AMark().Execute(callerId, Role.USER, exercise.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseArchivedException>();
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Unmark_ShouldRemoveTheFavoriteAndCommitOnce()
    {
        // Arrange
        var callerId = Id.New();
        var exercise = ASystemExercise();
        var stored = ExerciseFavorite.Rehydrate(Id.New(), callerId, exercise.Id, Now);
        GivenStored(exercise);
        GivenFavorite(stored);
        _favorites.Setup(repository => repository.Remove(stored, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await AnUnmark().Execute(callerId, Role.USER, exercise.Id, CancellationToken.None);

        // Assert
        _favorites.Verify(repository => repository.Remove(stored, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Unmark_ShouldDoNothingWhenTheExerciseIsNotAFavorite()
    {
        // Arrange
        var callerId = Id.New();
        var exercise = ASystemExercise();
        GivenStored(exercise);
        GivenNoFavorite(callerId, exercise.Id);

        // Act
        await AnUnmark().Execute(callerId, Role.USER, exercise.Id, CancellationToken.None);

        // Assert
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Unmark_ShouldStillRemoveTheFavoriteOfAnArchivedExercise()
    {
        // Arrange
        var callerId = Id.New();
        var exercise = ACustomExercise(callerId, archivedAtUtc: Now.AddDays(-1));
        var stored = ExerciseFavorite.Rehydrate(Id.New(), callerId, exercise.Id, Now.AddDays(-3));
        GivenStored(exercise);
        GivenFavorite(stored);
        _favorites.Setup(repository => repository.Remove(stored, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await AnUnmark().Execute(callerId, Role.USER, exercise.Id, CancellationToken.None);

        // Assert
        _favorites.Verify(repository => repository.Remove(stored, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Unmark_ShouldRejectAnUnknownExercise()
    {
        // Arrange
        var exerciseId = Id.New();
        _exercises.Setup(repository => repository.GetById(exerciseId, It.IsAny<CancellationToken>())).ReturnsAsync((Exercise?)null);

        // Act
        var act = () => AnUnmark().Execute(Id.New(), Role.USER, exerciseId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotFoundException>();
        VerifyNothingWritten();
    }

    [Fact]
    public async Task Unmark_ShouldTreatACustomExerciseTheCallerCannotSeeAsMissing()
    {
        // Arrange
        var callerId = Id.New();
        var exercise = ACustomExercise(Id.New());
        GivenStored(exercise);
        _workouts.Setup(repository => repository.GetAllByUserId(callerId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        // Act
        var act = () => AnUnmark().Execute(callerId, Role.USER, exercise.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotFoundException>();
        VerifyNothingWritten();
    }

    [Fact]
    public async Task List_ShouldReturnTheRepositoryOrderForTheCaller()
    {
        // Arrange
        var callerId = Id.New();
        var newer = ExerciseFavorite.Rehydrate(Id.New(), callerId, Id.New(), Now);
        var older = ExerciseFavorite.Rehydrate(Id.New(), callerId, Id.New(), Now.AddDays(-1));
        _favorites.Setup(repository => repository.GetAllByUser(callerId, It.IsAny<CancellationToken>())).ReturnsAsync([newer, older]);

        // Act
        var favorites = await AList().Execute(callerId, CancellationToken.None);

        // Assert
        favorites.Should().Equal(newer, older);
    }
}
