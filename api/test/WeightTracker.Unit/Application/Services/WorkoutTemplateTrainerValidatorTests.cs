using FluentAssertions;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using Xunit;
using static WeightTracker.Unit.Application.Mesocycles.MesocycleTestUsers;

namespace WeightTracker.Unit.Application.Services;

public class WorkoutTemplateTrainerValidatorTests
{
    private static WorkoutTemplateTrainerValidator AValidator(params User[] users) => new(UsersHolding(users).Object);

    [Fact]
    public async Task EnsureCanOwnTemplates_ShouldAcceptAnActiveTrainer()
    {
        // Arrange
        var trainer = AStoredUser(Role.TRAINER);

        // Act
        var act = () => AValidator(trainer).EnsureCanOwnTemplates(trainer.Id, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(Role.ADMIN)]
    [InlineData(Role.USER)]
    public async Task EnsureCanOwnTemplates_ShouldRejectAUserWhoIsNotATrainer(Role role)
    {
        // Arrange
        var user = AStoredUser(role);

        // Act
        var act = () => AValidator(user).EnsureCanOwnTemplates(user.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutTemplateTrainerException>();
    }

    [Fact]
    public async Task EnsureCanOwnTemplates_ShouldRejectADeactivatedTrainer()
    {
        // Arrange
        var trainer = AStoredUser(Role.TRAINER, isActive: false);

        // Act
        var act = () => AValidator(trainer).EnsureCanOwnTemplates(trainer.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutTemplateTrainerException>();
    }

    [Fact]
    public async Task EnsureCanOwnTemplates_ShouldRejectAnUnknownUser()
    {
        // Arrange
        var unknown = Id.New();

        // Act
        var act = () => AValidator().EnsureCanOwnTemplates(unknown, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutTemplateTrainerException>();
    }
}
