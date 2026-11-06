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

public class UpdateWorkoutUseCaseTests
{
    private static readonly DateTime WorkoutDate = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private static readonly DateTime NewDate = new(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc);
    private static readonly WorkoutName NewName = WorkoutName.From("Push day");
    private static readonly WorkoutDescription? NewDescription = WorkoutDescription.From("Heavy presses");

    private static readonly Id OwnerId = Id.New();

    private static readonly Id TrainerId = Id.New();

    private static readonly Id AdminId = Id.New();

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);
    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    private static Workout APrescribedWorkout(WorkoutStatus status = WorkoutStatus.DRAFT)
        => Workout.Rehydrate(Id.New(), OwnerId, TrainerId, Name, null, WorkoutDate, status, []);

    private static Workout ASelfPlannedWorkout(WorkoutStatus status = WorkoutStatus.DRAFT)
        => Workout.Rehydrate(Id.New(), OwnerId, null, Name, null, WorkoutDate, status, []);

    private static User AUser(Id id, Role role, bool isActive = true) => User.Rehydrate(
        id, Email.From($"{id.Value}@weighttracker.test"), PersonName.From("Grace"), PersonName.From("Hopper"),
        new DateOnly(1985, 12, 9), role, PasswordHash.From("stored-hash"), WorkoutDate,
        isActive, isActive ? null : WorkoutDate.AddDays(1));

    private void GivenDeactivated(Id id, Role role)
        => _users.Setup(repository => repository.GetById(id, It.IsAny<CancellationToken>())).ReturnsAsync(AUser(id, role, isActive: false));

    private void GivenStored(Workout workout)
        => _workouts.Setup(repository => repository.GetById(workout.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workout);

    private void GivenUser(Id id, Role role)
        => _users.Setup(repository => repository.GetById(id, It.IsAny<CancellationToken>())).ReturnsAsync(AUser(id, role));

    private void GivenNoUser(Id id)
        => _users.Setup(repository => repository.GetById(id, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

    private void GivenTrainerCheck(Id id, bool canTrain)
        => _users.Setup(repository => repository.GetById(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AUser(id, canTrain ? Role.TRAINER : Role.USER));

    private void GivenSaveAndCommitSucceed(Workout workout)
    {
        _workouts.Setup(repository => repository.Save(workout, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private void VerifySavedAndCommittedOnce(Workout workout)
    {
        _workouts.Verify(repository => repository.Save(workout, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private void VerifyNothingSavedOrCommitted()
    {
        _workouts.Verify(repository => repository.Save(It.IsAny<Workout>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.VerifyNoOtherCalls();
    }

    private static void ShouldBeUnchanged(Workout workout, Id ownerId, Id? trainerId)
    {
        workout.Name.Should().Be(Name);
        workout.Description.Should().BeNull();
        workout.WorkoutDate.Should().Be(WorkoutDate);
        workout.UserId.Should().Be(ownerId);
        workout.TrainerId.Should().Be(trainerId);
    }

    private readonly Mock<IMesocycleRepository> _mesocycles = new(MockBehavior.Strict);

    private UpdateWorkoutUseCase AUseCase()
        => new(
            _workouts.Object, new WorkoutEditAuthorizer(), new WorkoutAssignmentValidator(_users.Object),
            new MesocyclePlacementValidator(_mesocycles.Object, new MesocycleEditAuthorizer()), _unitOfWork.Object);

    private Task<Workout> Update(Id callerId, Role callerRole, Workout workout, Id userId, Id? trainerId)
        => AUseCase().Execute(callerId, callerRole, workout.Id, NewName, NewDescription, NewDate, userId, trainerId, null, null, CancellationToken.None);

    // ---- Not found

    [Fact]
    public async Task Execute_ShouldRejectAnUnknownWorkoutWithoutSavingOrCommitting()
    {
        // Arrange
        var workoutId = Id.New();
        _workouts.Setup(repository => repository.GetById(workoutId, It.IsAny<CancellationToken>())).ReturnsAsync((Workout?)null);

        // Act
        var act = () => AUseCase().Execute(AdminId, Role.ADMIN, workoutId, NewName, NewDescription, NewDate, OwnerId, null, null, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    // ---- Details

    [Fact]
    public async Task Execute_ShouldLetTheOwnerOfASelfPlannedWorkoutUpdateItsDetails()
    {
        // Arrange
        var workout = ASelfPlannedWorkout();
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var updated = await Update(OwnerId, Role.USER, workout, OwnerId, null);

        // Assert
        updated.Name.Should().Be(NewName);
        updated.Description.Should().Be(NewDescription);
        updated.WorkoutDate.Should().Be(NewDate);
        VerifySavedAndCommittedOnce(workout);
        _users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldLetThePrescribingTrainerUpdateTheDetails()
    {
        // Arrange
        var workout = APrescribedWorkout(WorkoutStatus.PUBLISHED);
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var updated = await Update(TrainerId, Role.TRAINER, workout, OwnerId, TrainerId);

        // Assert
        updated.Name.Should().Be(NewName);
        updated.Status.Should().Be(WorkoutStatus.PUBLISHED);
        VerifySavedAndCommittedOnce(workout);
    }

    [Fact]
    public async Task Execute_ShouldLetAnAdminUpdateTheDetailsOfAnyWorkout()
    {
        // Arrange
        var workout = APrescribedWorkout();
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var updated = await Update(AdminId, Role.ADMIN, workout, OwnerId, TrainerId);

        // Assert
        updated.Name.Should().Be(NewName);
        updated.UserId.Should().Be(OwnerId);
        updated.TrainerId.Should().Be(TrainerId);
        VerifySavedAndCommittedOnce(workout);
        _users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldRejectTheOwnerOfAPrescribedWorkoutAndChangeNothing()
    {
        // Arrange
        var workout = APrescribedWorkout();
        GivenStored(workout);

        // Act
        var act = () => Update(OwnerId, Role.USER, workout, OwnerId, TrainerId);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        ShouldBeUnchanged(workout, OwnerId, TrainerId);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnUnrelatedCallerAndChangeNothing()
    {
        // Arrange
        var workout = ASelfPlannedWorkout();
        GivenStored(workout);

        // Act
        var act = () => Update(Id.New(), Role.TRAINER, workout, OwnerId, null);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        ShouldBeUnchanged(workout, OwnerId, null);
        VerifyNothingSavedOrCommitted();
    }

    // ---- Lock

    [Theory]
    [InlineData(WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.FINISHED)]
    public async Task Execute_ShouldRejectANonAdminOnALockedWorkoutAndChangeNothing(WorkoutStatus status)
    {
        // Arrange
        var workout = APrescribedWorkout(status);
        GivenStored(workout);

        // Act
        var act = () => Update(TrainerId, Role.TRAINER, workout, OwnerId, TrainerId);

        // Assert
        await act.Should().ThrowAsync<WorkoutLockedException>();
        ShouldBeUnchanged(workout, OwnerId, TrainerId);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldLetAnAdminUpdateAFinishedWorkout()
    {
        // Arrange
        var workout = APrescribedWorkout(WorkoutStatus.FINISHED);
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var updated = await Update(AdminId, Role.ADMIN, workout, OwnerId, TrainerId);

        // Assert
        updated.Name.Should().Be(NewName);
        updated.Status.Should().Be(WorkoutStatus.FINISHED);
        VerifySavedAndCommittedOnce(workout);
    }

    // ---- Reassignment by non-admins

    [Fact]
    public async Task Execute_ShouldRejectATrainerWhoChangesTheOwner()
    {
        // Arrange
        var workout = APrescribedWorkout();
        GivenStored(workout);

        // Act
        var act = () => Update(TrainerId, Role.TRAINER, workout, Id.New(), TrainerId);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        ShouldBeUnchanged(workout, OwnerId, TrainerId);
        VerifyNothingSavedOrCommitted();
        _users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnOwnerWhoAddsATrainer()
    {
        // Arrange
        var workout = ASelfPlannedWorkout();
        GivenStored(workout);

        // Act
        var act = () => Update(OwnerId, Role.USER, workout, OwnerId, TrainerId);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        ShouldBeUnchanged(workout, OwnerId, null);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectATrainerWhoDropsThemselvesAsTrainer()
    {
        // Arrange
        var workout = APrescribedWorkout();
        GivenStored(workout);

        // Act
        var act = () => Update(TrainerId, Role.TRAINER, workout, OwnerId, null);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        ShouldBeUnchanged(workout, OwnerId, TrainerId);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectANonAdminReassignmentOfALockedWorkoutAsForbidden()
    {
        // Arrange
        var workout = APrescribedWorkout(WorkoutStatus.STARTED);
        GivenStored(workout);

        // Act
        var act = () => Update(TrainerId, Role.TRAINER, workout, Id.New(), TrainerId);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingSavedOrCommitted();
    }

    // ---- Reassignment by admins

    [Fact]
    public async Task Execute_ShouldLetAnAdminChangeTheOwner()
    {
        // Arrange
        var workout = ASelfPlannedWorkout();
        var newOwner = Id.New();
        GivenStored(workout);
        GivenUser(newOwner, Role.USER);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var updated = await Update(AdminId, Role.ADMIN, workout, newOwner, null);

        // Assert
        updated.UserId.Should().Be(newOwner);
        updated.TrainerId.Should().BeNull();
        updated.Name.Should().Be(NewName);
        VerifySavedAndCommittedOnce(workout);
    }

    [Fact]
    public async Task Execute_ShouldLetAnAdminAssignATrainer()
    {
        // Arrange
        var workout = ASelfPlannedWorkout();
        GivenStored(workout);
        GivenTrainerCheck(TrainerId, true);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var updated = await Update(AdminId, Role.ADMIN, workout, OwnerId, TrainerId);

        // Assert
        updated.TrainerId.Should().Be(TrainerId);
        VerifySavedAndCommittedOnce(workout);
    }

    [Fact]
    public async Task Execute_ShouldLetAnAdminRemoveTheTrainer()
    {
        // Arrange
        var workout = APrescribedWorkout();
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var updated = await Update(AdminId, Role.ADMIN, workout, OwnerId, null);

        // Assert
        updated.TrainerId.Should().BeNull();
        VerifySavedAndCommittedOnce(workout);
        _users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnAdminWhoMakesThemselvesTheOwner()
    {
        // Arrange
        var workout = ASelfPlannedWorkout();
        GivenStored(workout);

        // Act
        var act = () => Update(AdminId, Role.ADMIN, workout, AdminId, null);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        ShouldBeUnchanged(workout, OwnerId, null);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnUnknownNewOwner()
    {
        // Arrange
        var workout = ASelfPlannedWorkout();
        var newOwner = Id.New();
        GivenStored(workout);
        GivenNoUser(newOwner);

        // Act
        var act = () => Update(AdminId, Role.ADMIN, workout, newOwner, null);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
        ShouldBeUnchanged(workout, OwnerId, null);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectANewTrainerWhoCannotTrain()
    {
        // Arrange
        var workout = ASelfPlannedWorkout();
        var notATrainer = Id.New();
        GivenStored(workout);
        GivenTrainerCheck(notATrainer, false);

        // Act
        var act = () => Update(AdminId, Role.ADMIN, workout, OwnerId, notATrainer);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        ShouldBeUnchanged(workout, OwnerId, null);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectATrainerWhoIsTheOwner()
    {
        // Arrange
        var workout = APrescribedWorkout();
        GivenStored(workout);
        GivenUser(TrainerId, Role.TRAINER);

        // Act
        var act = () => Update(AdminId, Role.ADMIN, workout, TrainerId, TrainerId);

        // Assert
        await act.Should().ThrowAsync<TrainerCannotBeWorkoutOwnerException>();
        ShouldBeUnchanged(workout, OwnerId, TrainerId);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnAdminReassigningToADeactivatedOwner()
    {
        // Arrange
        var workout = ASelfPlannedWorkout();
        var newOwner = Id.New();
        GivenStored(workout);
        GivenDeactivated(newOwner, Role.USER);

        // Act
        var act = () => Update(AdminId, Role.ADMIN, workout, newOwner, null);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        ShouldBeUnchanged(workout, OwnerId, null);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnAdminAssigningADeactivatedTrainer()
    {
        // Arrange
        var workout = ASelfPlannedWorkout();
        GivenStored(workout);
        GivenDeactivated(TrainerId, Role.TRAINER);

        // Act
        var act = () => Update(AdminId, Role.ADMIN, workout, OwnerId, TrainerId);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        ShouldBeUnchanged(workout, OwnerId, null);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldNotCheckAnUnchangedOwnerOrTrainer()
    {
        // Arrange
        var workout = APrescribedWorkout();
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var updated = await Update(AdminId, Role.ADMIN, workout, OwnerId, TrainerId);

        // Assert
        updated.Name.Should().Be(NewName);
        VerifySavedAndCommittedOnce(workout);
        _users.VerifyNoOtherCalls();
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

    private static Workout APlacedWorkout(Id mesocycleId, int weekNumber, WorkoutStatus status = WorkoutStatus.PUBLISHED)
        => Workout.Rehydrate(
            Id.New(), OwnerId, TrainerId, Name, null, WorkoutDate, status, [], mesocycleId, WeekNumber.From(weekNumber));

    private Task<Workout> Place(Id callerId, Role callerRole, Workout workout, Id userId, Id? trainerId, Id? mesocycleId, int? weekNumber)
        => AUseCase().Execute(
            callerId, callerRole, workout.Id, NewName, NewDescription, NewDate, userId, trainerId, mesocycleId,
            weekNumber is null ? null : WeekNumber.From(weekNumber.Value), CancellationToken.None);

    [Fact]
    public async Task Execute_ShouldMoveAPlacedWorkoutToAnotherWeek()
    {
        // Arrange
        var mesocycle = GivenMesocycle(TrainerId, OwnerId);
        var workout = APlacedWorkout(mesocycle.Id, 1);
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var updated = await Place(TrainerId, Role.TRAINER, workout, OwnerId, TrainerId, mesocycle.Id, 3);

        // Assert
        updated.MesocycleId.Should().Be(mesocycle.Id);
        updated.MesocycleWeekNumber.Should().Be(WeekNumber.From(3));
        updated.WorkoutDate.Should().Be(new DateTime(2026, 10, 14));
        VerifySavedAndCommittedOnce(workout);
    }

    [Fact]
    public async Task Execute_ShouldRedateAnUnchangedPlacementByItsWeek()
    {
        // Arrange
        var mesocycle = GivenMesocycle(TrainerId, OwnerId);
        var workout = APlacedWorkout(mesocycle.Id, 1);
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var updated = await Place(TrainerId, Role.TRAINER, workout, OwnerId, TrainerId, mesocycle.Id, 1);

        // Assert
        updated.WorkoutDate.Should().Be(new DateTime(2026, 9, 30));
        updated.MesocycleWeekNumber.Should().Be(WeekNumber.From(1));
        VerifySavedAndCommittedOnce(workout);
    }

    [Fact]
    public async Task Execute_ShouldKeepAPlacedWorkoutWithoutASentDate()
    {
        // Arrange
        var mesocycle = GivenMesocycle(TrainerId, OwnerId);
        var workout = APlacedWorkout(mesocycle.Id, 2);
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var updated = await AUseCase().Execute(
            TrainerId, Role.TRAINER, workout.Id, NewName, NewDescription, null, OwnerId, TrainerId, mesocycle.Id,
            WeekNumber.From(2), CancellationToken.None);

        // Assert
        updated.WorkoutDate.Should().Be(new DateTime(2026, 10, 7));
        VerifySavedAndCommittedOnce(workout);
    }

    [Fact]
    public async Task Execute_ShouldRejectAStandaloneUpdateWithoutADateBeforeLoadingTheWorkout()
    {
        // Arrange
        var workout = APlacedWorkout(Id.New(), 1);

        // Act
        var act = () => AUseCase().Execute(
            TrainerId, Role.TRAINER, workout.Id, NewName, NewDescription, null, OwnerId, TrainerId, null, null,
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<MissingWorkoutDateException>();
        _workouts.VerifyNoOtherCalls();
        workout.MesocycleWeekNumber.Should().Be(WeekNumber.From(1));
        workout.WorkoutDate.Should().Be(WorkoutDate);
    }

    [Fact]
    public async Task Execute_ShouldUnlinkWhenThePlacementIsOmittedWithoutLookingUpTheMesocycle()
    {
        // Arrange
        var workout = APlacedWorkout(Id.New(), 1);
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var updated = await Place(TrainerId, Role.TRAINER, workout, OwnerId, TrainerId, null, null);

        // Assert
        updated.MesocycleId.Should().BeNull();
        updated.MesocycleWeekNumber.Should().BeNull();
        updated.WorkoutDate.Should().Be(NewDate);
        _mesocycles.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldAcceptTheUnchangedPlacementOfAStartedWorkout()
    {
        // Arrange
        var mesocycle = GivenMesocycle(TrainerId, OwnerId);
        var workout = APlacedWorkout(mesocycle.Id, 2, WorkoutStatus.STARTED);
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var updated = await Place(AdminId, Role.ADMIN, workout, OwnerId, TrainerId, mesocycle.Id, 2);

        // Assert
        updated.Name.Should().Be(NewName);
        updated.MesocycleWeekNumber.Should().Be(WeekNumber.From(2));
        updated.WorkoutDate.Should().Be(new DateTime(2026, 10, 7));
        VerifySavedAndCommittedOnce(workout);
    }

    [Fact]
    public async Task Execute_ShouldRefuseMovingAStartedWorkoutEvenForAnAdmin()
    {
        // Arrange
        var mesocycle = GivenMesocycle(TrainerId, OwnerId);
        var workout = APlacedWorkout(mesocycle.Id, 2, WorkoutStatus.STARTED);
        GivenStored(workout);

        // Act
        var act = () => Place(AdminId, Role.ADMIN, workout, OwnerId, TrainerId, mesocycle.Id, 3);

        // Assert
        await act.Should().ThrowAsync<WorkoutPlacementLockedException>();
        workout.MesocycleWeekNumber.Should().Be(WeekNumber.From(2));
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAReassignmentThatBreaksAKeptPlacement()
    {
        // Arrange
        var newOwnerId = Id.New();
        var mesocycle = GivenMesocycle(TrainerId, OwnerId);
        var workout = APlacedWorkout(mesocycle.Id, 1);
        GivenStored(workout);
        GivenUser(newOwnerId, Role.USER);

        // Act
        var act = () => Place(AdminId, Role.ADMIN, workout, newOwnerId, TrainerId, mesocycle.Id, 1);

        // Assert
        await act.Should().ThrowAsync<MesocycleAthleteMismatchException>();
        ShouldBeUnchanged(workout, OwnerId, TrainerId);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRefuseAnOwnerPlacingTheirSelfPlannedWorkout()
    {
        // Arrange
        var mesocycle = GivenMesocycle(TrainerId, OwnerId);
        var workout = ASelfPlannedWorkout();
        GivenStored(workout);

        // Act
        var act = () => Place(OwnerId, Role.USER, workout, OwnerId, null, mesocycle.Id, 1);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectHalfAPlacementBeforeLoadingTheWorkout()
    {
        // Arrange
        var workout = APrescribedWorkout();

        // Act
        var act = () => Place(TrainerId, Role.TRAINER, workout, OwnerId, TrainerId, null, 2);

        // Assert
        await act.Should().ThrowAsync<IncompleteMesocyclePlacementException>();
        _workouts.VerifyNoOtherCalls();
    }
}
