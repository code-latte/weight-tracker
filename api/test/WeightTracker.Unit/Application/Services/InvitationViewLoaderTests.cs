using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.AthleteInvitations;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Application.Services;

public class InvitationViewLoaderTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);

    private static User AStoredUser(string name, Role role) => User.Rehydrate(
        Id.New(), Email.From($"{name.ToLowerInvariant()}@weighttracker.test"), PersonName.From(name),
        PersonName.From("Surname"), new DateOnly(1988, 5, 12), role, PasswordHash.From("stored-hash"),
        Now.AddDays(-30), true, null);

    private static TrainerInvitation AnInvitation(Id trainerId, Id? userId) => TrainerInvitation.Create(
        trainerId, Email.From("invited@weighttracker.test"), userId, "token-hash", Now, Now.AddDays(7));

    private void GivenStored(params User[] users)
        => _users
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

    [Fact]
    public async Task Load_ShouldJoinEachInvitationWithItsTrainerAndInviteeInOneLookup()
    {
        // Arrange
        var trainer = AStoredUser("Grace", Role.TRAINER);
        var athlete = AStoredUser("Ada", Role.USER);
        var invitations = new[] { AnInvitation(trainer.Id, athlete.Id), AnInvitation(trainer.Id, null) };
        GivenStored(trainer, athlete);

        // Act
        var views = await new InvitationViewLoader(_users.Object).Load(invitations, CancellationToken.None);

        // Assert
        views.Select(view => view.Invitation).Should().Equal(invitations);
        views[0].Trainer.Should().BeSameAs(trainer);
        views[0].Invitee.Should().BeSameAs(athlete);
        views[1].Trainer.Should().BeSameAs(trainer);
        views[1].Invitee.Should().BeNull();
        _users.Verify(
            repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Load_ShouldRejectAnInvitationWhoseTrainerDoesNotExist()
    {
        // Arrange
        var invitation = AnInvitation(Id.New(), null);
        GivenStored();

        // Act
        var act = () => new InvitationViewLoader(_users.Object).Load(invitation, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
    }
}
