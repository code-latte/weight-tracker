using FluentAssertions;
using Moq;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.WorkoutTemplates;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Application.WorkoutTemplates;

/// <summary>
/// Covers the use cases that change the exercises, order and superseries of a workout template.
/// </summary>
public class WorkoutTemplateContentUseCaseTests
{
    private static readonly Id TrainerId = Id.New();

    private static readonly SetPrescription ThreeByTen = new(3, new SetTarget.Reps(10));

    private static readonly SetPrescription FourByTen = new(4, new SetTarget.Reps(10));

    private readonly Mock<IWorkoutTemplateRepository> _templates = new(MockBehavior.Strict);
    private readonly Mock<IExerciseRepository> _exercises = new(MockBehavior.Strict);
    private readonly Mock<IWorkoutRepository> _workouts = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    private readonly Id _firstExerciseId = Id.New();
    private readonly Id _secondExerciseId = Id.New();
    private readonly WorkoutTemplate _template;

    public WorkoutTemplateContentUseCaseTests()
    {
        _template = WorkoutTemplate.Rehydrate(Id.New(), TrainerId, WorkoutName.From("Full body"), null,
        [
            WorkoutExercise.Rehydrate(Id.New(), _firstExerciseId, Effort.MID, [ThreeByTen]),
            WorkoutExercise.Rehydrate(Id.New(), _secondExerciseId, Effort.MID, [ThreeByTen]),
        ]);

        _templates.Setup(repository => repository.GetById(_template.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_template);
        _templates.Setup(repository => repository.Save(_template, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _workouts.Setup(repository => repository.GetAllByUserOrTrainerId(It.IsAny<Id>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
    }

    private Task<WorkoutExercise> Add(Id callerId, Role role, Id exerciseId, Id? templateId = null)
        => new AddWorkoutTemplateExerciseUseCase(
                _templates.Object, _exercises.Object, new WorkoutTemplateEditAuthorizer(),
                new ExerciseVisibility(new WorkoutVisibility(_workouts.Object)), _unitOfWork.Object)
            .Execute(callerId, role, templateId ?? _template.Id, exerciseId, Effort.HIGH, [ThreeByTen], true, TimeInMinutes.From(1m), CancellationToken.None);

    private Task<WorkoutExercise> Update(Id routeExerciseId, Id bodyExerciseId, IReadOnlyList<SetPrescription> sets)
        => new UpdateWorkoutTemplateExerciseUseCase(_templates.Object, new WorkoutTemplateEditAuthorizer(), _unitOfWork.Object)
            .Execute(TrainerId, Role.TRAINER, _template.Id, routeExerciseId, bodyExerciseId, Effort.HIGH, sets, false, null, CancellationToken.None);

    private Task Remove(Id callerId, Role role, Id exerciseId)
        => new RemoveWorkoutTemplateExerciseUseCase(_templates.Object, new WorkoutTemplateEditAuthorizer(), _unitOfWork.Object)
            .Execute(callerId, role, _template.Id, exerciseId, CancellationToken.None);

    private Task<WorkoutTemplate> Reorder(IReadOnlyList<Id> exerciseIds)
        => new ReorderWorkoutTemplateExercisesUseCase(_templates.Object, new WorkoutTemplateEditAuthorizer(), _unitOfWork.Object)
            .Execute(TrainerId, Role.TRAINER, _template.Id, exerciseIds, CancellationToken.None);

    private Task<SuperSeriesId> CreateSuperSeries(Id callerId, Role role)
        => new CreateWorkoutTemplateSuperSeriesUseCase(_templates.Object, new WorkoutTemplateEditAuthorizer(), _unitOfWork.Object)
            .Execute(callerId, role, _template.Id, _template.Exercises.Select(entry => entry.Id).ToList(), CancellationToken.None);

    private Task Break(int superSeriesId)
        => new BreakWorkoutTemplateSuperSeriesUseCase(_templates.Object, new WorkoutTemplateEditAuthorizer(), _unitOfWork.Object)
            .Execute(TrainerId, Role.TRAINER, _template.Id, superSeriesId, CancellationToken.None);

    private void GivenInCatalog(Exercise exercise)
        => _exercises.Setup(repository => repository.GetById(exercise.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exercise);

    private static Exercise ASystemExercise() => Exercise.Rehydrate(Id.New(), null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, []);

    private static Exercise ACustomExercise(Id ownerId, DateTime? archivedAtUtc = null)
        => Exercise.Rehydrate(Id.New(), null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, [], ownerId, archivedAtUtc);

    private void VerifyNothingSavedOrCommitted()
    {
        _templates.Verify(repository => repository.Save(It.IsAny<WorkoutTemplate>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Add_ShouldAppendASystemExerciseAndCommitOnce()
    {
        // Arrange
        var exercise = ASystemExercise();
        GivenInCatalog(exercise);

        // Act
        var entry = await Add(TrainerId, Role.TRAINER, exercise.Id);

        // Assert
        entry.ExerciseId.Should().Be(exercise.Id);
        entry.PerSide.Should().BeTrue();
        entry.Rest.Should().Be(TimeInMinutes.From(1m));
        _template.Exercises.Last().Id.Should().Be(entry.Id);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Add_ShouldRejectAnUnknownTemplate()
    {
        // Arrange
        var unknown = Id.New();
        _templates.Setup(repository => repository.GetById(unknown, It.IsAny<CancellationToken>())).ReturnsAsync((WorkoutTemplate?)null);

        // Act
        var act = () => Add(TrainerId, Role.TRAINER, Id.New(), unknown);

        // Assert
        await act.Should().ThrowAsync<WorkoutTemplateNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Add_ShouldHideTheTemplateFromAnotherTrainer()
    {
        // Arrange
        var exercise = ASystemExercise();
        GivenInCatalog(exercise);

        // Act
        var act = () => Add(Id.New(), Role.TRAINER, exercise.Id);

        // Assert
        await act.Should().ThrowAsync<WorkoutTemplateNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Add_ShouldRejectAnExerciseMissingFromTheCatalog()
    {
        // Arrange
        var missing = Id.New();
        _exercises.Setup(repository => repository.GetById(missing, It.IsAny<CancellationToken>())).ReturnsAsync((Exercise?)null);

        // Act
        var act = () => Add(TrainerId, Role.TRAINER, missing);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Add_ShouldTreatACustomExerciseTheCallerCannotSeeAsMissing()
    {
        // Arrange
        var custom = ACustomExercise(Id.New());
        GivenInCatalog(custom);

        // Act
        var act = () => Add(TrainerId, Role.TRAINER, custom.Id);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Add_ShouldAcceptACustomExerciseOwnedByTheTemplateTrainer()
    {
        // Arrange
        var custom = ACustomExercise(TrainerId);
        GivenInCatalog(custom);

        // Act
        var entry = await Add(TrainerId, Role.TRAINER, custom.Id);

        // Assert
        entry.ExerciseId.Should().Be(custom.Id);
    }

    [Fact]
    public async Task Add_ShouldRejectAnotherTrainersCustomExerciseForAnAdmin()
    {
        // Arrange
        var custom = ACustomExercise(Id.New());
        GivenInCatalog(custom);

        // Act
        var act = () => Add(Id.New(), Role.ADMIN, custom.Id);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotUsableInWorkoutException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Add_ShouldRejectAnArchivedCustomExercise()
    {
        // Arrange
        var custom = ACustomExercise(TrainerId, archivedAtUtc: new DateTime(2026, 11, 1));
        GivenInCatalog(custom);

        // Act
        var act = () => Add(TrainerId, Role.TRAINER, custom.Id);

        // Assert
        await act.Should().ThrowAsync<ExerciseArchivedException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Add_ShouldRejectAnExerciseAlreadyInTheTemplate()
    {
        // Arrange
        var exercise = Exercise.Rehydrate(_firstExerciseId, null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, []);
        GivenInCatalog(exercise);

        // Act
        var act = () => Add(TrainerId, Role.TRAINER, exercise.Id);

        // Assert
        await act.Should().ThrowAsync<ExerciseAlreadyInWorkoutException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Update_ShouldReplaceTheSetsOfTheEntry()
    {
        // Arrange
        var exerciseId = _firstExerciseId;

        // Act
        var entry = await Update(exerciseId, exerciseId, [FourByTen]);

        // Assert
        entry.Sets.Should().Equal(FourByTen);
        entry.Effort.Should().Be(Effort.HIGH);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_ShouldRejectBodyAndRouteNamingDifferentExercises()
    {
        // Arrange
        var routeExerciseId = _firstExerciseId;

        // Act
        var act = () => Update(routeExerciseId, _secondExerciseId, [FourByTen]);

        // Assert
        await act.Should().ThrowAsync<ExerciseIdMismatchException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Update_ShouldRejectAnExerciseNotInTheTemplate()
    {
        // Arrange
        var missing = Id.New();

        // Act
        var act = () => Update(missing, missing, [FourByTen]);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotInWorkoutException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Update_ShouldRejectANewTotalInsideASuperSeries()
    {
        // Arrange
        _template.CreateSuperSeries(_template.Exercises.Select(entry => entry.Id));

        // Act
        var act = () => Update(_firstExerciseId, _firstExerciseId, [FourByTen]);

        // Assert
        await act.Should().ThrowAsync<SuperSeriesSetCountMismatchException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Remove_ShouldDropTheExerciseForAnAdmin()
    {
        // Arrange
        var exerciseId = _firstExerciseId;

        // Act
        await Remove(Id.New(), Role.ADMIN, exerciseId);

        // Assert
        _template.Exercises.Select(entry => entry.ExerciseId).Should().Equal(_secondExerciseId);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Remove_ShouldRefuseARegularUser()
    {
        // Arrange
        var exerciseId = _firstExerciseId;

        // Act
        var act = () => Remove(Id.New(), Role.USER, exerciseId);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        _template.Exercises.Should().HaveCount(2);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Remove_ShouldRejectAnExerciseNotInTheTemplate()
    {
        // Arrange
        var missing = Id.New();

        // Act
        var act = () => Remove(TrainerId, Role.TRAINER, missing);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotInWorkoutException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Reorder_ShouldPutTheExercisesInTheGivenOrder()
    {
        // Arrange
        var order = new[] { _secondExerciseId, _firstExerciseId };

        // Act
        var template = await Reorder(order);

        // Assert
        template.Exercises.Select(entry => entry.ExerciseId).Should().Equal(order);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reorder_ShouldRejectAMissingExercise()
    {
        // Arrange
        var order = new[] { _secondExerciseId };

        // Act
        var act = () => Reorder(order);

        // Assert
        await act.Should().ThrowAsync<WorkoutExerciseOrderMismatchException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task CreateSuperSeries_ShouldGroupTheEntries()
    {
        // Arrange
        var trainerId = TrainerId;

        // Act
        var superSeriesId = await CreateSuperSeries(trainerId, Role.TRAINER);

        // Assert
        _template.Exercises.Should().OnlyContain(entry => entry.SuperSeriesId == superSeriesId);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateSuperSeries_ShouldRejectEntriesWithDifferentTotals()
    {
        // Arrange
        _template.UpdateExercise(_firstExerciseId, Effort.MID, [FourByTen]);

        // Act
        var act = () => CreateSuperSeries(TrainerId, Role.TRAINER);

        // Assert
        await act.Should().ThrowAsync<SuperSeriesSetCountMismatchException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task CreateSuperSeries_ShouldHideTheTemplateFromAnotherTrainer()
    {
        // Arrange
        var otherTrainerId = Id.New();

        // Act
        var act = () => CreateSuperSeries(otherTrainerId, Role.TRAINER);

        // Assert
        await act.Should().ThrowAsync<WorkoutTemplateNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Break_ShouldUngroupTheEntries()
    {
        // Arrange
        var superSeriesId = _template.CreateSuperSeries(_template.Exercises.Select(entry => entry.Id));

        // Act
        await Break(superSeriesId.Value);

        // Assert
        _template.Exercises.Should().OnlyContain(entry => entry.SuperSeriesId == null);
        _template.Exercises.Should().HaveCount(2);
    }

    [Fact]
    public async Task Break_ShouldRejectAnUnknownSuperSeries()
    {
        // Arrange
        var unknown = 7;

        // Act
        var act = () => Break(unknown);

        // Assert
        await act.Should().ThrowAsync<SuperSeriesNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }
}
