using FluentAssertions;
using Moq;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Application.UseCases.Exercises;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Application.Exercises;

/// <summary>
/// Covers the create, update and archive use cases of custom exercises.
/// </summary>
public class ExerciseWriteUseCaseTests
{
    private static readonly DateTime Now = new(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc);
    private static readonly LanguageCode Spanish = LanguageCode.From("es");
    private static readonly ExerciseName RemoConBanda = ExerciseName.From("Remo con banda");

    private readonly Mock<IExerciseRepository> _exercises = new(MockBehavior.Strict);
    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);
    private readonly Mock<IClock> _clock = new();

    public ExerciseWriteUseCaseTests()
    {
        _clock.SetupGet(clock => clock.UtcNow).Returns(Now);
    }

    private ExerciseVisibility AVisibility() => new(new WorkoutVisibility(_workouts.Object));

    private CreateExerciseUseCase ACreate() => new(_exercises.Object, _unitOfWork.Object);

    private UpdateExerciseUseCase AnUpdate()
        => new(_exercises.Object, AVisibility(), new ExerciseEditAuthorizer(), _unitOfWork.Object);

    private ArchiveExerciseUseCase AnArchive()
        => new(_exercises.Object, AVisibility(), new ExerciseEditAuthorizer(), _clock.Object, _unitOfWork.Object);

    private static AuthenticatedUser ACaller(Id id, Role role)
        => new(id, Email.From($"{id.Value}@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"), role);

    private static Exercise ACustomExercise(Id ownerId, DateTime? archivedAtUtc = null)
        => Exercise.Rehydrate(
            Id.New(), null, null, Muscles.MIDDLE_BACK, ExerciseTypes.STRENGTH,
            [ExerciseTranslation.Rehydrate(Id.New(), Spanish, RemoConBanda)], ownerId, archivedAtUtc);

    private static Exercise ASystemExercise()
        => Exercise.Rehydrate(Id.New(), null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, []);

    private void GivenStored(Exercise exercise)
        => _exercises.Setup(repository => repository.GetById(exercise.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exercise);

    private void GivenSaveAndCommitSucceed()
    {
        _exercises.Setup(repository => repository.Save(It.IsAny<Exercise>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private void VerifyNothingSavedOrCommitted()
    {
        _exercises.Verify(repository => repository.Save(It.IsAny<Exercise>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(Role.TRAINER)]
    [InlineData(Role.USER)]
    public async Task Create_ShouldSaveAnExerciseOwnedByTheCallerAndCommitOnce(Role role)
    {
        // Arrange
        var caller = ACaller(Id.New(), role);
        GivenSaveAndCommitSucceed();

        // Act
        var exercise = await ACreate().Execute(caller, Spanish, RemoConBanda, Muscles.MIDDLE_BACK, ExerciseTypes.STRENGTH, null, CancellationToken.None);

        // Assert
        exercise.OwnerId.Should().Be(caller.Id);
        exercise.Translations.Should().ContainSingle().Which.Name.Should().Be(RemoConBanda);
        _exercises.Verify(repository => repository.Save(exercise, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_ShouldRejectAnAdminBeforeWritingAnything()
    {
        // Arrange
        var caller = ACaller(Id.New(), Role.ADMIN);

        // Act
        var act = () => ACreate().Execute(caller, Spanish, RemoConBanda, Muscles.MIDDLE_BACK, ExerciseTypes.STRENGTH, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Create_ShouldSaveNothingWhenTheMuscleIsInvalid()
    {
        // Arrange
        var caller = ACaller(Id.New(), Role.TRAINER);

        // Act
        var act = () => ACreate().Execute(caller, Spanish, RemoConBanda, (Muscles)999, ExerciseTypes.STRENGTH, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidMuscleException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Update_ShouldReplaceTheDetailsAndCommitOnce()
    {
        // Arrange
        var caller = ACaller(Id.New(), Role.TRAINER);
        var exercise = ACustomExercise(caller.Id);
        GivenStored(exercise);
        GivenSaveAndCommitSucceed();

        // Act
        var result = await AnUpdate().Execute(caller, exercise.Id, Spanish, ExerciseName.From("Remo con goma"), Muscles.LATS, ExerciseTypes.CARDIO, null, CancellationToken.None);

        // Assert
        result.Muscle.Should().Be(Muscles.LATS);
        result.Translations.Single().Name.Value.Should().Be("Remo con goma");
        _exercises.Verify(repository => repository.Save(exercise, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_ShouldAllowAnAdminOnACustomExercise()
    {
        // Arrange
        var exercise = ACustomExercise(Id.New());
        GivenStored(exercise);
        GivenSaveAndCommitSucceed();

        // Act
        var result = await AnUpdate().Execute(ACaller(Id.New(), Role.ADMIN), exercise.Id, Spanish, RemoConBanda, Muscles.LATS, ExerciseTypes.STRENGTH, null, CancellationToken.None);

        // Assert
        result.OwnerId.Should().Be(exercise.OwnerId);
    }

    [Fact]
    public async Task Update_ShouldHideAnInvisibleExerciseBeforeCheckingPermissions()
    {
        // Arrange
        var caller = ACaller(Id.New(), Role.TRAINER);
        var exercise = ACustomExercise(Id.New());
        GivenStored(exercise);
        _workouts.Setup(repository => repository.GetAllByUserOrTrainerId(caller.Id, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        // Act
        var act = () => AnUpdate().Execute(caller, exercise.Id, Spanish, RemoConBanda, Muscles.LATS, ExerciseTypes.STRENGTH, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Update_ShouldRejectASystemExerciseEvenForAnAdmin()
    {
        // Arrange
        var exercise = ASystemExercise();
        GivenStored(exercise);

        // Act
        var act = () => AnUpdate().Execute(ACaller(Id.New(), Role.ADMIN), exercise.Id, Spanish, RemoConBanda, Muscles.LATS, ExerciseTypes.STRENGTH, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SystemExerciseNotEditableException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Update_ShouldRejectAnArchivedExercise()
    {
        // Arrange
        var caller = ACaller(Id.New(), Role.USER);
        var exercise = ACustomExercise(caller.Id, archivedAtUtc: Now);
        GivenStored(exercise);

        // Act
        var act = () => AnUpdate().Execute(caller, exercise.Id, Spanish, RemoConBanda, Muscles.LATS, ExerciseTypes.STRENGTH, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseArchivedException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Update_ShouldRejectAnUnknownExercise()
    {
        // Arrange
        var id = Id.New();
        _exercises.Setup(repository => repository.GetById(id, It.IsAny<CancellationToken>())).ReturnsAsync((Exercise?)null);

        // Act
        var act = () => AnUpdate().Execute(ACaller(Id.New(), Role.USER), id, Spanish, RemoConBanda, Muscles.LATS, ExerciseTypes.STRENGTH, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Update_ShouldRejectAVisibleExerciseOfSomeoneElseAsForbidden()
    {
        // Arrange
        var caller = ACaller(Id.New(), Role.USER);
        var trainerId = Id.New();
        var exercise = ACustomExercise(trainerId);
        GivenStored(exercise);
        _workouts.Setup(repository => repository.GetAllByUserId(caller.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([AWorkoutUsing(caller.Id, trainerId, exercise)]);

        // Act
        var act = () => AnUpdate().Execute(caller, exercise.Id, Spanish, RemoConBanda, Muscles.LATS, ExerciseTypes.STRENGTH, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Archive_ShouldRecordTheClockTimeAndCommitOnce()
    {
        // Arrange
        var caller = ACaller(Id.New(), Role.USER);
        var exercise = ACustomExercise(caller.Id);
        GivenStored(exercise);
        GivenSaveAndCommitSucceed();

        // Act
        await AnArchive().Execute(caller, exercise.Id, CancellationToken.None);

        // Assert
        exercise.ArchivedAtUtc.Should().Be(Now);
        _exercises.Verify(repository => repository.Save(exercise, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Archive_ShouldRejectAnAlreadyArchivedExercise()
    {
        // Arrange
        var caller = ACaller(Id.New(), Role.USER);
        var exercise = ACustomExercise(caller.Id, archivedAtUtc: Now.AddDays(-1));
        GivenStored(exercise);

        // Act
        var act = () => AnArchive().Execute(caller, exercise.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseArchivedException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Archive_ShouldRejectASystemExerciseEvenForAnAdmin()
    {
        // Arrange
        var exercise = ASystemExercise();
        GivenStored(exercise);

        // Act
        var act = () => AnArchive().Execute(ACaller(Id.New(), Role.ADMIN), exercise.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SystemExerciseNotEditableException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Archive_ShouldRejectSomeoneWhoIsNotTheOwnerWhenTheyCanSeeTheExercise()
    {
        // Arrange
        var caller = ACaller(Id.New(), Role.USER);
        var trainerId = Id.New();
        var exercise = ACustomExercise(trainerId);
        GivenStored(exercise);
        _workouts.Setup(repository => repository.GetAllByUserId(caller.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([AWorkoutUsing(caller.Id, trainerId, exercise)]);

        // Act
        var act = () => AnArchive().Execute(caller, exercise.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        exercise.IsArchived.Should().BeFalse();
        VerifyNothingSavedOrCommitted();
    }

    private static Workout AWorkoutUsing(Id ownerId, Id trainerId, Exercise exercise)
        => Workout.Rehydrate(
            Id.New(), ownerId, trainerId, WorkoutName.From("Leg day"), null, new DateTime(2026, 10, 1), WorkoutStatus.PUBLISHED,
            [WorkoutExercise.Rehydrate(Id.New(), exercise.Id, Effort.MID, [new SetPrescription(3, new SetTarget.Reps(10))])]);
}
