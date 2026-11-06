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
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;
using static WeightTracker.Unit.Application.Mesocycles.MesocycleTestUsers;

namespace WeightTracker.Unit.Application.WorkoutTemplates;

public class CreateWorkoutFromTemplateUseCaseTests
{
    private static readonly DateTime WorkoutDate = new(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IWorkoutTemplateRepository> _templates = new(MockBehavior.Strict);
    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);
    private readonly Mock<IExerciseRepository> _exercises = new(MockBehavior.Strict);
    private readonly Mock<IMesocycleRepository> _mesocycles = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    private readonly User _trainer = AStoredUser(Role.TRAINER);
    private readonly User _otherTrainer = AStoredUser(Role.TRAINER);
    private readonly User _admin = AStoredUser(Role.ADMIN);
    private readonly User _athlete = AStoredUser(Role.USER);
    private readonly User _deactivatedAthlete = AStoredUser(Role.USER, isActive: false);
    private readonly User _deactivatedTrainer = AStoredUser(Role.TRAINER, isActive: false);

    private readonly Exercise _sentadilla = Exercise.Rehydrate(Id.New(), null, null, Muscles.QUADRICEPS, ExerciseTypes.STRENGTH, []);
    private readonly Exercise _plancha = Exercise.Rehydrate(Id.New(), null, null, Muscles.ABDOMINALS, ExerciseTypes.STRENGTH, []);

    private readonly WorkoutTemplate _template;

    public CreateWorkoutFromTemplateUseCaseTests()
    {
        _template = TemplateOf(_trainer.Id);

        GivenStored(_template);
        _workouts.Setup(repository => repository.Save(It.IsAny<Workout>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _exercises.Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([_sentadilla, _plancha]);
    }

    private WorkoutTemplate TemplateOf(Id trainerId)
    {
        var template = WorkoutTemplate.Create(trainerId, WorkoutName.From("Full body"), WorkoutDescription.From("Compound first"));
        template.AddExercise(_sentadilla.Id, Effort.HIGH, [new SetPrescription(4, new SetTarget.Reps(8))], perSide: true);
        template.AddExercise(_plancha.Id, Effort.LOW, [new SetPrescription(4, new SetTarget.Duration(40))]);
        template.CreateSuperSeries(template.Exercises.Select(entry => entry.Id));

        return template;
    }

    private void GivenStored(WorkoutTemplate template)
        => _templates.Setup(repository => repository.GetById(template.Id, It.IsAny<CancellationToken>())).ReturnsAsync(template);

    private Mesocycle GivenMesocycle(Id trainerId, Id athleteId)
    {
        var mesocycle = AStoredMesocycle(trainerId, athleteId);
        _mesocycles.Setup(repository => repository.GetById(mesocycle.Id, It.IsAny<CancellationToken>())).ReturnsAsync(mesocycle);

        return mesocycle;
    }

    private Task<WorkoutFromTemplateView> Execute(
        Id callerId,
        Role role,
        Id? userId,
        DateTime? workoutDate = null,
        Id? mesocycleId = null,
        WeekNumber? weekNumber = null,
        WorkoutTemplate? template = null)
        => new CreateWorkoutFromTemplateUseCase(
                _templates.Object, _workouts.Object, _exercises.Object, new WorkoutTemplateEditAuthorizer(),
                new WorkoutAssignmentValidator(
                    UsersHolding(_trainer, _otherTrainer, _admin, _athlete, _deactivatedAthlete, _deactivatedTrainer).Object),
                new MesocyclePlacementValidator(_mesocycles.Object, new MesocycleEditAuthorizer()), _unitOfWork.Object)
            .Execute(callerId, role, (template ?? _template).Id, userId, workoutDate, mesocycleId, weekNumber, CancellationToken.None);

    private void VerifyNothingSaved()
    {
        _workouts.Verify(repository => repository.Save(It.IsAny<Workout>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_ShouldCreateAStandaloneDraftCopyPrescribedByTheTemplateTrainer()
    {
        // Arrange
        var trainerId = _trainer.Id;

        // Act
        var view = await Execute(trainerId, Role.TRAINER, _athlete.Id, WorkoutDate);

        // Assert
        var workout = view.Workout;
        workout.UserId.Should().Be(_athlete.Id);
        workout.TrainerId.Should().Be(trainerId);
        workout.Status.Should().Be(WorkoutStatus.DRAFT);
        workout.WorkoutDate.Should().Be(WorkoutDate);
        workout.MesocycleId.Should().BeNull();
        workout.Name.Should().Be(_template.Name);
        workout.Exercises.Select(entry => entry.ExerciseId).Should().Equal(_sentadilla.Id, _plancha.Id);
        workout.Exercises.Should().OnlyContain(entry => entry.SuperSeriesId != null);
        view.Exercises.Keys.Should().BeEquivalentTo([_sentadilla.Id, _plancha.Id]);
        _workouts.Verify(repository => repository.Save(workout, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _templates.Verify(repository => repository.Save(It.IsAny<WorkoutTemplate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_ShouldKeepTheTemplateTrainerWhenAnAdminCreatesIt()
    {
        // Arrange
        var adminId = _admin.Id;

        // Act
        var view = await Execute(adminId, Role.ADMIN, _athlete.Id, WorkoutDate);

        // Assert
        view.Workout.TrainerId.Should().Be(_trainer.Id);
    }

    [Fact]
    public async Task Execute_ShouldPlaceTheWorkoutInTheWeekAndDateItByTheWeek()
    {
        // Arrange
        var mesocycle = GivenMesocycle(_trainer.Id, _athlete.Id);
        var week = WeekNumber.From(2);

        // Act
        var view = await Execute(_trainer.Id, Role.TRAINER, _athlete.Id, null, mesocycle.Id, week);

        // Assert
        view.Workout.MesocycleId.Should().Be(mesocycle.Id);
        view.Workout.MesocycleWeekNumber.Should().Be(week);
        view.Workout.WorkoutDate.Should().Be(mesocycle.DateOfWeek(week));
    }

    [Fact]
    public async Task Execute_ShouldRejectAMesocycleOfAnotherTrainer()
    {
        // Arrange
        var mesocycle = GivenMesocycle(_otherTrainer.Id, _athlete.Id);

        // Act
        var act = () => Execute(_admin.Id, Role.ADMIN, _athlete.Id, null, mesocycle.Id, WeekNumber.From(1));

        // Assert
        await act.Should().ThrowAsync<MesocycleTrainerMismatchException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Execute_ShouldRejectAMesocycleOfAnotherAthlete()
    {
        // Arrange
        var mesocycle = GivenMesocycle(_trainer.Id, Id.New());

        // Act
        var act = () => Execute(_trainer.Id, Role.TRAINER, _athlete.Id, null, mesocycle.Id, WeekNumber.From(1));

        // Assert
        await act.Should().ThrowAsync<MesocycleAthleteMismatchException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Execute_ShouldRejectHalfAPlacement()
    {
        // Arrange
        var mesocycleId = Id.New();

        // Act
        var act = () => Execute(_trainer.Id, Role.TRAINER, _athlete.Id, WorkoutDate, mesocycleId);

        // Assert
        await act.Should().ThrowAsync<IncompleteMesocyclePlacementException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Execute_ShouldRequireADateWithoutPlacement()
    {
        // Arrange
        var athleteId = _athlete.Id;

        // Act
        var act = () => Execute(_trainer.Id, Role.TRAINER, athleteId);

        // Assert
        await act.Should().ThrowAsync<MissingWorkoutDateException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnUnknownTemplate()
    {
        // Arrange
        var unknown = WorkoutTemplate.Create(_trainer.Id, WorkoutName.From("Unknown"), null);
        _templates.Setup(repository => repository.GetById(unknown.Id, It.IsAny<CancellationToken>())).ReturnsAsync((WorkoutTemplate?)null);

        // Act
        var act = () => Execute(_trainer.Id, Role.TRAINER, _athlete.Id, WorkoutDate, template: unknown);

        // Assert
        await act.Should().ThrowAsync<WorkoutTemplateNotFoundException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Execute_ShouldHideTheTemplateFromAnotherTrainer()
    {
        // Arrange
        var otherTrainerId = _otherTrainer.Id;

        // Act
        var act = () => Execute(otherTrainerId, Role.TRAINER, _athlete.Id, WorkoutDate);

        // Assert
        await act.Should().ThrowAsync<WorkoutTemplateNotFoundException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Execute_ShouldRefuseARegularUser()
    {
        // Arrange
        var athleteId = _athlete.Id;

        // Act
        var act = () => Execute(athleteId, Role.USER, athleteId, WorkoutDate);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Execute_ShouldRequireTheUser()
    {
        // Arrange
        var trainerId = _trainer.Id;

        // Act
        var act = () => Execute(trainerId, Role.TRAINER, null, WorkoutDate);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnUnknownUser()
    {
        // Arrange
        var unknownId = Id.New();

        // Act
        var act = () => Execute(_trainer.Id, Role.TRAINER, unknownId, WorkoutDate);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Execute_ShouldForbidATrainerNamingAnotherTrainer()
    {
        // Arrange
        var otherTrainerId = _otherTrainer.Id;

        // Act
        var act = () => Execute(_trainer.Id, Role.TRAINER, otherTrainerId, WorkoutDate);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnAdminNamingATrainer()
    {
        // Arrange
        var otherTrainerId = _otherTrainer.Id;

        // Act
        var act = () => Execute(_admin.Id, Role.ADMIN, otherTrainerId, WorkoutDate);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Execute_ShouldRejectADeactivatedAthlete()
    {
        // Arrange
        var deactivatedId = _deactivatedAthlete.Id;

        // Act
        var act = () => Execute(_trainer.Id, Role.TRAINER, deactivatedId, WorkoutDate);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Execute_ShouldRejectATemplateOfADeactivatedTrainer()
    {
        // Arrange
        var template = TemplateOf(_deactivatedTrainer.Id);
        GivenStored(template);

        // Act
        var act = () => Execute(_admin.Id, Role.ADMIN, _athlete.Id, WorkoutDate, template: template);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        VerifyNothingSaved();
    }
}
