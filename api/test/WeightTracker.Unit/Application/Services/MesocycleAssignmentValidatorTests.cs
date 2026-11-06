using FluentAssertions;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using Xunit;
using static WeightTracker.Unit.Application.Mesocycles.MesocycleTestUsers;

namespace WeightTracker.Unit.Application.Services;

public class MesocycleAssignmentValidatorTests
{
    private static MesocycleAssignmentValidator AValidator(params User[] users) => new(UsersHolding(users).Object);

    [Fact]
    public async Task EnsureAthlete_ShouldAcceptAnActiveRegularUser()
    {
        // Arrange
        var athlete = AStoredUser(Role.USER);

        // Act
        var act = () => AValidator(athlete).EnsureAthlete(athlete.Id, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureAthlete_ShouldNotFindAnUnknownUser()
    {
        // Arrange
        var unknown = Id.New();

        // Act
        var act = () => AValidator().EnsureAthlete(unknown, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Theory]
    [InlineData(Role.TRAINER)]
    [InlineData(Role.ADMIN)]
    public async Task EnsureAthlete_ShouldRejectAUserWhoIsNotARegularUser(Role role)
    {
        // Arrange
        var user = AStoredUser(role);

        // Act
        var act = () => AValidator(user).EnsureAthlete(user.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidMesocycleAssignmentException>();
    }

    [Fact]
    public async Task EnsureAthlete_ShouldRejectADeactivatedUser()
    {
        // Arrange
        var athlete = AStoredUser(Role.USER, isActive: false);

        // Act
        var act = () => AValidator(athlete).EnsureAthlete(athlete.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidMesocycleAssignmentException>();
    }

    [Fact]
    public async Task EnsureTrainer_ShouldAcceptAnActiveTrainer()
    {
        // Arrange
        var trainer = AStoredUser(Role.TRAINER);

        // Act
        var act = () => AValidator(trainer).EnsureTrainer(trainer.Id, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(Role.ADMIN)]
    [InlineData(Role.USER)]
    public async Task EnsureTrainer_ShouldRejectAUserWhoIsNotATrainer(Role role)
    {
        // Arrange
        var user = AStoredUser(role);

        // Act
        var act = () => AValidator(user).EnsureTrainer(user.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidMesocycleAssignmentException>();
    }

    [Fact]
    public async Task EnsureTrainer_ShouldRejectADeactivatedTrainer()
    {
        // Arrange
        var trainer = AStoredUser(Role.TRAINER, isActive: false);

        // Act
        var act = () => AValidator(trainer).EnsureTrainer(trainer.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidMesocycleAssignmentException>();
    }

    [Fact]
    public async Task EnsureTrainer_ShouldRejectAnUnknownUser()
    {
        // Arrange
        var unknown = Id.New();

        // Act
        var act = () => AValidator().EnsureTrainer(unknown, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidMesocycleAssignmentException>();
    }
}
