using FluentAssertions;
using Moq;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Workouts;
using WeightTracker.Domain.Auth;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Application.Workouts;

public class CreateWorkoutUseCaseTests
{
    private static readonly DateTime WorkoutDate = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private static readonly Id UserId = Id.New();

    private static readonly Id TrainerId = Id.New();

    private static readonly Id AdminId = Id.New();

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);
    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    private readonly Mock<IMesocycleRepository> _mesocycles = new(MockBehavior.Strict);

    private CreateWorkoutUseCase AUseCase() => new(
        _workouts.Object, new WorkoutAssignmentValidator(_users.Object),
        new MesocyclePlacementValidator(_mesocycles.Object, new MesocycleEditAuthorizer()), _unitOfWork.Object);

    private static User AUser(Id id, Role role, bool isActive = true) => User.Rehydrate(
        id, Email.From($"{id.Value}@weighttracker.test"), PersonName.From("Grace"), PersonName.From("Hopper"),
        new DateOnly(1985, 12, 9), role, PasswordHash.From("stored-hash"), WorkoutDate,
        isActive, isActive ? null : WorkoutDate.AddDays(1));

    private void GivenDeactivated(Id id, Role role)
        => _users.Setup(repository => repository.GetById(id, It.IsAny<CancellationToken>())).ReturnsAsync(AUser(id, role, isActive: false));

    private void GivenStored(Id id, Role role)
        => _users.Setup(repository => repository.GetById(id, It.IsAny<CancellationToken>())).ReturnsAsync(AUser(id, role));

    private void GivenNoUser(Id id)
        => _users.Setup(repository => repository.GetById(id, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

    private void GivenTrainerCheck(Id id, bool canTrain)
        => _users.Setup(repository => repository.GetById(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AUser(id, canTrain ? Role.TRAINER : Role.USER));

    private void GivenSaveWorks()
    {
        _workouts.Setup(repository => repository.Save(It.IsAny<Workout>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private void VerifyNothingSavedOrCommitted()
    {
        _workouts.Verify(repository => repository.Save(It.IsAny<Workout>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.VerifyNoOtherCalls();
    }

    private void VerifySavedAndCommittedOnce(Workout workout)
    {
        _workouts.Verify(repository => repository.Save(workout, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private void VerifyNoUserLookups() => _users.VerifyNoOtherCalls();

    // ---- USER

    [Fact]
    public async Task Execute_ShouldCreateTheWorkoutForTheCallerWhenAUserSendsNoIds()
    {
        // Arrange
        GivenSaveWorks();

        // Act
        var workout = await AUseCase().Execute(UserId, Role.USER, null, null, Name, null, WorkoutDate, null, null, CancellationToken.None);

        // Assert
        workout.UserId.Should().Be(UserId);
        workout.TrainerId.Should().BeNull();
        workout.WorkoutDate.Should().Be(WorkoutDate.Date);
        workout.Exercises.Should().BeEmpty();
        VerifySavedAndCommittedOnce(workout);
        VerifyNoUserLookups();
    }

    [Fact]
    public async Task Execute_ShouldKeepTheNameAndTheDescription()
    {
        // Arrange
        GivenSaveWorks();
        var description = WorkoutDescription.From("Focus on tempo");

        // Act
        var workout = await AUseCase().Execute(UserId, Role.USER, null, null, Name, description, WorkoutDate, null, null, CancellationToken.None);

        // Assert
        workout.Name.Should().Be(Name);
        workout.Description.Should().Be(description);
        VerifySavedAndCommittedOnce(workout);
    }

    [Fact]
    public async Task Execute_ShouldAcceptTheCallersOwnIdWhenAUserSendsIt()
    {
        // Arrange
        GivenSaveWorks();

        // Act
        var workout = await AUseCase().Execute(UserId, Role.USER, UserId, null, Name, null, WorkoutDate, null, null, CancellationToken.None);

        // Assert
        workout.UserId.Should().Be(UserId);
        workout.TrainerId.Should().BeNull();
        VerifySavedAndCommittedOnce(workout);
    }

    [Fact]
    public async Task Execute_ShouldRejectAUserNamingAnotherOwner()
    {
        // Arrange & Act
        var act = () => AUseCase().Execute(UserId, Role.USER, Id.New(), null, Name, null, WorkoutDate, null, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        VerifyNothingSavedOrCommitted();
        VerifyNoUserLookups();
    }

    [Fact]
    public async Task Execute_ShouldRejectAUserNamingATrainer()
    {
        // Arrange & Act
        var act = () => AUseCase().Execute(UserId, Role.USER, null, TrainerId, Name, null, WorkoutDate, null, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        VerifyNothingSavedOrCommitted();
        VerifyNoUserLookups();
    }

    // ---- TRAINER

    [Fact]
    public async Task Execute_ShouldPrescribeTheWorkoutToTheUserWhenATrainerCreatesIt()
    {
        // Arrange
        GivenStored(UserId, Role.USER);
        GivenSaveWorks();

        // Act
        var workout = await AUseCase().Execute(TrainerId, Role.TRAINER, UserId, null, Name, null, WorkoutDate, null, null, CancellationToken.None);

        // Assert
        workout.UserId.Should().Be(UserId);
        workout.TrainerId.Should().Be(TrainerId);
        VerifySavedAndCommittedOnce(workout);
    }

    [Fact]
    public async Task Execute_ShouldRejectATrainerWhoSendsNoOwner()
    {
        // Arrange & Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, null, null, Name, null, WorkoutDate, null, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        VerifyNothingSavedOrCommitted();
        VerifyNoUserLookups();
    }

    [Fact]
    public async Task Execute_ShouldRejectATrainerNamingThemselvesAsOwner()
    {
        // Arrange & Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, TrainerId, null, Name, null, WorkoutDate, null, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        VerifyNothingSavedOrCommitted();
        VerifyNoUserLookups();
    }

    [Fact]
    public async Task Execute_ShouldRejectATrainerNamingATrainer()
    {
        // Arrange & Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, UserId, Id.New(), Name, null, WorkoutDate, null, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        VerifyNothingSavedOrCommitted();
        VerifyNoUserLookups();
    }

    [Fact]
    public async Task Execute_ShouldRejectATrainerNamingAnUnknownOwner()
    {
        // Arrange
        GivenNoUser(UserId);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, UserId, null, Name, null, WorkoutDate, null, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Theory]
    [InlineData(Role.TRAINER)]
    [InlineData(Role.ADMIN)]
    public async Task Execute_ShouldRejectATrainerNamingAnOwnerWhoIsNotAUser(Role ownerRole)
    {
        // Arrange
        var ownerId = Id.New();
        GivenStored(ownerId, ownerRole);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, ownerId, null, Name, null, WorkoutDate, null, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingSavedOrCommitted();
    }

    // ---- ADMIN

    [Fact]
    public async Task Execute_ShouldCreateTheWorkoutWithoutTrainerWhenAnAdminSendsNone()
    {
        // Arrange
        GivenStored(UserId, Role.USER);
        GivenSaveWorks();

        // Act
        var workout = await AUseCase().Execute(AdminId, Role.ADMIN, UserId, null, Name, null, WorkoutDate, null, null, CancellationToken.None);

        // Assert
        workout.UserId.Should().Be(UserId);
        workout.TrainerId.Should().BeNull();
        VerifySavedAndCommittedOnce(workout);
    }

    [Fact]
    public async Task Execute_ShouldAssignTheTrainerWhenAnAdminSendsOneWhoCanTrain()
    {
        // Arrange
        GivenStored(UserId, Role.USER);
        GivenTrainerCheck(TrainerId, true);
        GivenSaveWorks();

        // Act
        var workout = await AUseCase().Execute(AdminId, Role.ADMIN, UserId, TrainerId, Name, null, WorkoutDate, null, null, CancellationToken.None);

        // Assert
        workout.UserId.Should().Be(UserId);
        workout.TrainerId.Should().Be(TrainerId);
        VerifySavedAndCommittedOnce(workout);
    }

    [Fact]
    public async Task Execute_ShouldRejectAnAdminWhoSendsNoOwner()
    {
        // Arrange & Act
        var act = () => AUseCase().Execute(AdminId, Role.ADMIN, null, null, Name, null, WorkoutDate, null, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        VerifyNothingSavedOrCommitted();
        VerifyNoUserLookups();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnAdminNamingThemselvesAsOwner()
    {
        // Arrange & Act
        var act = () => AUseCase().Execute(AdminId, Role.ADMIN, AdminId, null, Name, null, WorkoutDate, null, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        VerifyNothingSavedOrCommitted();
        VerifyNoUserLookups();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnAdminNamingAnUnknownOwner()
    {
        // Arrange
        GivenNoUser(UserId);

        // Act
        var act = () => AUseCase().Execute(AdminId, Role.ADMIN, UserId, null, Name, null, WorkoutDate, null, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnAdminNamingATrainerWhoCannotTrain()
    {
        // Arrange
        GivenStored(UserId, Role.USER);
        GivenTrainerCheck(TrainerId, false);

        // Act
        var act = () => AUseCase().Execute(AdminId, Role.ADMIN, UserId, TrainerId, Name, null, WorkoutDate, null, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldPropagateTheDomainErrorWhenTheTrainerIsTheOwner()
    {
        // Arrange
        GivenStored(TrainerId, Role.TRAINER);
        GivenTrainerCheck(TrainerId, true);

        // Act
        var act = () => AUseCase().Execute(AdminId, Role.ADMIN, TrainerId, TrainerId, Name, null, WorkoutDate, null, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<TrainerCannotBeWorkoutOwnerException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectATrainerPrescribingToADeactivatedUser()
    {
        // Arrange
        GivenDeactivated(UserId, Role.USER);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, UserId, null, Name, null, WorkoutDate, null, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnAdminCreatingAWorkoutForADeactivatedUser()
    {
        // Arrange
        GivenDeactivated(UserId, Role.USER);

        // Act
        var act = () => AUseCase().Execute(AdminId, Role.ADMIN, UserId, null, Name, null, WorkoutDate, null, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnAdminAssigningADeactivatedTrainer()
    {
        // Arrange
        GivenStored(UserId, Role.USER);
        GivenDeactivated(TrainerId, Role.TRAINER);

        // Act
        var act = () => AUseCase().Execute(AdminId, Role.ADMIN, UserId, TrainerId, Name, null, WorkoutDate, null, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldStillForbidATrainerNamingADeactivatedTrainerAsOwner()
    {
        // Arrange
        var otherTrainer = Id.New();
        GivenDeactivated(otherTrainer, Role.TRAINER);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, otherTrainer, null, Name, null, WorkoutDate, null, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingSavedOrCommitted();
    }

    // ---- Mesocycle placement

    private Mesocycle GivenMesocycle(Id trainerId, Id athleteId)
    {
        var mesocycle = Mesocycle.Rehydrate(
            Id.New(), trainerId, athleteId, MesocycleName.From("Block"), MesocycleObjective.From("Objective"),
            WorkoutDate, DurationInWeeks.From(4), []);
        _mesocycles.Setup(repository => repository.GetById(mesocycle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mesocycle);

        return mesocycle;
    }

    [Fact]
    public async Task Execute_ShouldCreateATrainersWorkoutPlacedInTheirMesocycle()
    {
        // Arrange
        GivenStored(UserId, Role.USER);
        GivenSaveWorks();
        var mesocycle = GivenMesocycle(TrainerId, UserId);

        // Act
        var workout = await AUseCase().Execute(
            TrainerId, Role.TRAINER, UserId, null, Name, null, WorkoutDate, mesocycle.Id, WeekNumber.From(3), CancellationToken.None);

        // Assert
        workout.MesocycleId.Should().Be(mesocycle.Id);
        workout.MesocycleWeekNumber.Should().Be(WeekNumber.From(3));
        VerifySavedAndCommittedOnce(workout);
    }

    [Fact]
    public async Task Execute_ShouldDateAPlacedWorkoutByItsWeekIgnoringTheSentDate()
    {
        // Arrange
        GivenStored(UserId, Role.USER);
        GivenSaveWorks();
        var mesocycle = GivenMesocycle(TrainerId, UserId);
        var sentDate = new DateTime(2026, 12, 25);

        // Act
        var workout = await AUseCase().Execute(
            TrainerId, Role.TRAINER, UserId, null, Name, null, sentDate, mesocycle.Id, WeekNumber.From(3), CancellationToken.None);

        // Assert
        workout.WorkoutDate.Should().Be(new DateTime(2026, 10, 14));
    }

    [Fact]
    public async Task Execute_ShouldCreateAPlacedWorkoutWithoutADate()
    {
        // Arrange
        GivenStored(UserId, Role.USER);
        GivenSaveWorks();
        var mesocycle = GivenMesocycle(TrainerId, UserId);

        // Act
        var workout = await AUseCase().Execute(
            TrainerId, Role.TRAINER, UserId, null, Name, null, null, mesocycle.Id, WeekNumber.From(1), CancellationToken.None);

        // Assert
        workout.WorkoutDate.Should().Be(new DateTime(2026, 9, 30));
        VerifySavedAndCommittedOnce(workout);
    }

    [Fact]
    public async Task Execute_ShouldRejectAStandaloneWorkoutWithoutADateBeforeAnyLookup()
    {
        // Arrange
        var callerId = TrainerId;

        // Act
        var act = () => AUseCase().Execute(
            callerId, Role.TRAINER, UserId, null, Name, null, null, null, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MissingWorkoutDateException>();
        VerifyNoUserLookups();
        _mesocycles.VerifyNoOtherCalls();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldCreateAStandaloneWorkoutWithoutPlacement()
    {
        // Arrange
        GivenSaveWorks();

        // Act
        var workout = await AUseCase().Execute(UserId, Role.USER, null, null, Name, null, WorkoutDate, null, null, CancellationToken.None);

        // Assert
        workout.MesocycleId.Should().BeNull();
        workout.MesocycleWeekNumber.Should().BeNull();
        workout.WorkoutDate.Should().Be(WorkoutDate);
        _mesocycles.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldRejectHalfAPlacementBeforeAnyLookup()
    {
        // Arrange
        var mesocycleId = Id.New();

        // Act
        var act = () => AUseCase().Execute(
            TrainerId, Role.TRAINER, UserId, null, Name, null, WorkoutDate, mesocycleId, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<IncompleteMesocyclePlacementException>();
        VerifyNoUserLookups();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRefuseAUserPlacingTheirOwnWorkout()
    {
        // Arrange
        var mesocycle = GivenMesocycle(TrainerId, UserId);

        // Act
        var act = () => AUseCase().Execute(
            UserId, Role.USER, null, null, Name, null, WorkoutDate, mesocycle.Id, WeekNumber.From(1), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAPlacementInAMesocycleOfAnotherAthlete()
    {
        // Arrange
        GivenStored(UserId, Role.USER);
        var mesocycle = GivenMesocycle(TrainerId, Id.New());

        // Act
        var act = () => AUseCase().Execute(
            TrainerId, Role.TRAINER, UserId, null, Name, null, WorkoutDate, mesocycle.Id, WeekNumber.From(1), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MesocycleAthleteMismatchException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnAdminPlacementWhoseTrainerDoesNotPrepareTheMesocycle()
    {
        // Arrange
        var otherTrainerId = Id.New();
        GivenStored(UserId, Role.USER);
        GivenTrainerCheck(otherTrainerId, canTrain: true);
        var mesocycle = GivenMesocycle(TrainerId, UserId);

        // Act
        var act = () => AUseCase().Execute(
            AdminId, Role.ADMIN, UserId, otherTrainerId, Name, null, WorkoutDate, mesocycle.Id, WeekNumber.From(1), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MesocycleTrainerMismatchException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAPlacementBeyondTheMesocyclesDuration()
    {
        // Arrange
        GivenStored(UserId, Role.USER);
        var mesocycle = GivenMesocycle(TrainerId, UserId);

        // Act
        var act = () => AUseCase().Execute(
            TrainerId, Role.TRAINER, UserId, null, Name, null, WorkoutDate, mesocycle.Id, WeekNumber.From(5), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MesocycleWeekOutOfRangeException>();
        VerifyNothingSavedOrCommitted();
    }
}
