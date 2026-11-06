using FluentAssertions;
using Moq;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Application.Services;

public class MesocycleViewLoaderTests
{
    private static readonly DateTime Now = new(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);

    private static User AStoredUser(string name, Role role) => User.Rehydrate(
        Id.New(), Email.From($"{name.ToLowerInvariant()}@weighttracker.test"), PersonName.From(name),
        PersonName.From("Surname"), new DateOnly(1988, 5, 12), role, PasswordHash.From("stored-hash"),
        Now.AddDays(-30), true, null);

    private static Mesocycle AMesocycle(User trainer, User athlete) => Mesocycle.Create(
        trainer.Id, athlete.Id, MesocycleName.From("Block"), MesocycleObjective.From("Objective"),
        new DateTime(2026, 11, 9), DurationInWeeks.From(4));

    private void GivenStored(params User[] users)
        => _users
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

    [Fact]
    public async Task Load_ShouldJoinEachMesocycleWithItsTrainerAndAthleteInOneLookup()
    {
        // Arrange
        var trainer = AStoredUser("Grace", Role.TRAINER);
        var ada = AStoredUser("Ada", Role.USER);
        var alan = AStoredUser("Alan", Role.USER);
        var mesocycles = new[] { AMesocycle(trainer, ada), AMesocycle(trainer, alan) };
        GivenStored(trainer, ada, alan);

        // Act
        var views = await new MesocycleViewLoader(_users.Object).Load(mesocycles, CancellationToken.None);

        // Assert
        views.Select(view => view.Mesocycle).Should().Equal(mesocycles);
        views.Select(view => view.Athlete).Should().Equal(ada, alan);
        views.Should().OnlyContain(view => view.Trainer == trainer);
        _users.Verify(
            repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Load_ShouldNotLookUpUsersForAnEmptyList()
    {
        // Arrange
        var loader = new MesocycleViewLoader(_users.Object);

        // Act
        var views = await loader.Load([], CancellationToken.None);

        // Assert
        views.Should().BeEmpty();
    }

    [Fact]
    public async Task Load_ShouldFailWhenAReferencedUserDoesNotExist()
    {
        // Arrange
        var trainer = AStoredUser("Grace", Role.TRAINER);
        var athlete = AStoredUser("Ada", Role.USER);
        var mesocycle = AMesocycle(trainer, athlete);
        GivenStored(trainer);

        // Act
        var act = () => new MesocycleViewLoader(_users.Object).Load(mesocycle, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
