using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.TrainerAthletes;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Application.Services;

public class TrainerAthleteViewLoaderTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);

    private static User AStoredUser(string name, Role role) => User.Rehydrate(
        Id.New(), Email.From($"{name.ToLowerInvariant()}@weighttracker.test"), PersonName.From(name),
        PersonName.From("Surname"), new DateOnly(1988, 5, 12), role, PasswordHash.From("stored-hash"),
        Now.AddDays(-30), true, null);

    private void GivenStored(params User[] users)
        => _users
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

    [Fact]
    public async Task Load_ShouldJoinEachRelationshipWithItsTrainerAndAthleteInOneLookup()
    {
        // Arrange
        var trainer = AStoredUser("Grace", Role.TRAINER);
        var ada = AStoredUser("Ada", Role.USER);
        var alan = AStoredUser("Alan", Role.USER);
        var relationships = new[]
        {
            TrainerAthlete.Create(trainer.Id, ada.Id, Now), TrainerAthlete.Create(trainer.Id, alan.Id, Now),
        };
        GivenStored(trainer, ada, alan);

        // Act
        var views = await new TrainerAthleteViewLoader(_users.Object).Load(relationships, CancellationToken.None);

        // Assert
        views.Select(view => view.Relationship).Should().Equal(relationships);
        views.Select(view => view.Athlete).Should().Equal(ada, alan);
        views.Should().OnlyContain(view => view.Trainer == trainer);
        _users.Verify(
            repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Load_ShouldRejectARelationshipWhoseAthleteDoesNotExist()
    {
        // Arrange
        var trainer = AStoredUser("Grace", Role.TRAINER);
        var relationship = TrainerAthlete.Create(trainer.Id, Id.New(), Now);
        GivenStored(trainer);

        // Act
        var act = () => new TrainerAthleteViewLoader(_users.Object).Load(relationship, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
    }
}
