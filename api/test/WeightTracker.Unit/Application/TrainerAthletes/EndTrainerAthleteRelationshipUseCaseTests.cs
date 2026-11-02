using FluentAssertions;
using Moq;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.TrainerAthletes;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.TrainerAthletes;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Application.TrainerAthletes;

public class EndTrainerAthleteRelationshipUseCaseTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 10, 0, 0, DateTimeKind.Utc);

    private static readonly Id TrainerId = Id.New();
    private static readonly Id AthleteId = Id.New();

    private readonly Mock<ITrainerAthleteRepository> _trainerAthletes = new(MockBehavior.Strict);
    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    public EndTrainerAthleteRelationshipUseCaseTests()
    {
        _clock.Setup(clock => clock.UtcNow).Returns(Now);
        _users
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([AStoredUser(TrainerId, "Grace", Role.TRAINER), AStoredUser(AthleteId, "Ada", Role.USER)]);
    }

    private static User AStoredUser(Id id, string name, Role role) => User.Rehydrate(
        id, Email.From($"{name.ToLowerInvariant()}@weighttracker.test"), PersonName.From(name), PersonName.From("Lovelace"),
        new DateOnly(1988, 5, 12), role, PasswordHash.From("stored-hash"), Now.AddDays(-30), true, null);

    private static TrainerAthlete AnActiveRelationship()
        => TrainerAthlete.Rehydrate(Id.New(), TrainerId, AthleteId, TrainerAthleteStatus.ACTIVE, Now.AddDays(-30), null);

    private void GivenActive(TrainerAthlete? relationship)
        => _trainerAthletes
            .Setup(repository => repository.GetActiveByTrainerIdAndUserId(TrainerId, AthleteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(relationship);

    private void GivenSaveAndCommitSucceed(TrainerAthlete relationship)
    {
        _trainerAthletes.Setup(repository => repository.Save(relationship, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private EndTrainerAthleteRelationshipUseCase AUseCase()
        => new(_trainerAthletes.Object, new TrainerAthleteViewLoader(_users.Object), _clock.Object, _unitOfWork.Object);

    public static TheoryData<string> AllowedCallers() => new() { "admin", "trainer", "athlete" };

    private static (Id Id, Role Role) Caller(string who) => who switch
    {
        "admin" => (Id.New(), Role.ADMIN),
        "trainer" => (TrainerId, Role.TRAINER),
        _ => (AthleteId, Role.USER),
    };

    [Theory]
    [MemberData(nameof(AllowedCallers))]
    public async Task Execute_ShouldEndTheActiveRelationshipSaveAndCommitOnce(string who)
    {
        // Arrange
        var (callerId, callerRole) = Caller(who);
        var relationship = AnActiveRelationship();
        GivenActive(relationship);
        GivenSaveAndCommitSucceed(relationship);

        // Act
        var ended = await AUseCase().Execute(callerId, callerRole, TrainerId, AthleteId, CancellationToken.None);

        // Assert
        ended.Relationship.Status.Should().Be(TrainerAthleteStatus.ENDED);
        ended.Relationship.FinishedAtUtc.Should().Be(Now);
        ended.Trainer.Name.Value.Should().Be("Grace");
        ended.Athlete.Name.Value.Should().Be("Ada");
        _trainerAthletes.Verify(repository => repository.Save(relationship, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(Role.TRAINER)]
    [InlineData(Role.USER)]
    public async Task Execute_ShouldRejectAStrangerWithoutLookingTheRelationshipUp(Role callerRole)
    {
        // Arrange
        var strangerId = Id.New();

        // Act
        var act = () => AUseCase().Execute(strangerId, callerRole, TrainerId, AthleteId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        _trainerAthletes.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldRejectWhenThereIsNoActiveRelationship()
    {
        // Arrange
        GivenActive(null);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, TrainerId, AthleteId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<TrainerAthleteNotFoundException>();
        _trainerAthletes.Verify(
            repository => repository.Save(It.IsAny<TrainerAthlete>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.VerifyNoOtherCalls();
    }
}
