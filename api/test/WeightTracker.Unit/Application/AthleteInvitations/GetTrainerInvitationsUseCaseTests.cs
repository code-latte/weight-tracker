using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.AthleteInvitations;
using WeightTracker.Domain.AthleteInvitations;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Application.AthleteInvitations;

public class GetTrainerInvitationsUseCaseTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ITrainerInvitationRepository> _invitations = new(MockBehavior.Strict);
    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);

    private GetTrainerInvitationsUseCase AUseCase() => new(_invitations.Object, new InvitationViewLoader(_users.Object));

    private static User AStoredTrainer(Id id) => User.Rehydrate(
        id, Email.From($"{id}@weighttracker.test"), PersonName.From("Grace"), PersonName.From("Hopper"),
        new DateOnly(1985, 12, 9), Role.TRAINER, PasswordHash.From("stored-hash"), Now.AddDays(-30), true, null);

    private void GivenStoredTrainers(params Id[] ids)
        => _users
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ids.Select(AStoredTrainer).ToList());

    private static TrainerInvitation AnInvitation(Id trainerId) => TrainerInvitation.Create(
        trainerId, Email.From("athlete@weighttracker.test"), null, "token-hash", Now, Now.AddDays(7));

    [Fact]
    public async Task Admin_ShouldSeeEveryInvitation()
    {
        // Arrange
        var all = new[] { AnInvitation(Id.New()), AnInvitation(Id.New()) };
        GivenStoredTrainers(all[0].TrainerId, all[1].TrainerId);
        _invitations.Setup(repository => repository.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(all);

        // Act
        var result = await AUseCase().Execute(Id.New(), Role.ADMIN, CancellationToken.None);

        // Assert
        result.Select(view => view.Invitation).Should().Equal(all);
        result.Should().OnlyContain(view => view.Trainer.Name.Value == "Grace" && view.Invitee == null);
        _invitations.Verify(repository => repository.GetAll(It.IsAny<CancellationToken>()), Times.Once);
        _invitations.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Trainer_ShouldSeeOnlyTheirOwnInvitations()
    {
        // Arrange
        var trainerId = Id.New();
        var own = new[] { AnInvitation(trainerId) };
        GivenStoredTrainers(trainerId);
        _invitations
            .Setup(repository => repository.GetAllByTrainerId(trainerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(own);

        // Act
        var result = await AUseCase().Execute(trainerId, Role.TRAINER, CancellationToken.None);

        // Assert
        result.Select(view => view.Invitation).Should().Equal(own);
        result.Single().Trainer.Id.Should().Be(trainerId);
        _invitations.Verify(repository => repository.GetAllByTrainerId(trainerId, It.IsAny<CancellationToken>()), Times.Once);
        _invitations.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task User_ShouldBeRejectedAsUnauthorized()
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        var act = async () => await useCase.Execute(Id.New(), Role.USER, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        _invitations.VerifyNoOtherCalls();
    }
}
