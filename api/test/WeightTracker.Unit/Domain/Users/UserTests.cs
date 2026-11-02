using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Domain.Users;

public class UserTests
{
    private static readonly DateOnly Birth = new(1988, 5, 12);
    private static readonly DateTime CreatedAt = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DeactivatedAt = new(2026, 10, 20, 18, 30, 0, DateTimeKind.Utc);

    private static User AnActiveUser() => User.Create(
        Email.From("user@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"),
        Birth, Role.USER, PasswordHash.From("hash"), CreatedAt);

    [Fact]
    public void Create_ShouldMarkTheUserAsNew()
    {
        // Arrange & Act
        var user = User.Create(
            Email.From("user@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"),
            Birth, Role.USER, PasswordHash.From("hash"), CreatedAt);

        // Assert
        user.IsNew.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldGenerateAnIdentifier()
    {
        // Arrange & Act
        var user = User.Create(
            Email.From("user@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"),
            Birth, Role.USER, PasswordHash.From("hash"), CreatedAt);

        // Assert
        user.Id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_ShouldKeepEveryProvidedField()
    {
        // Arrange
        var email = Email.From("trainer@weighttracker.test");

        // Act
        var user = User.Create(
            email, PersonName.From("Ada"), PersonName.From("Lovelace"),
            Birth, Role.TRAINER, PasswordHash.From("hashed-password"), CreatedAt);

        // Assert
        user.Email.Should().Be(email);
        user.Name.Should().Be(PersonName.From("Ada"));
        user.Surname.Should().Be(PersonName.From("Lovelace"));
        user.DateBirth.Should().Be(Birth);
        user.Role.Should().Be(Role.TRAINER);
        user.PasswordHash.Should().Be(PasswordHash.From("hashed-password"));
        user.CreatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void Rehydrate_ShouldNotMarkTheUserAsNew()
    {
        // Arrange & Act
        var user = User.Rehydrate(
            Id.New(), Email.From("user@weighttracker.test"), PersonName.From("Ada"),
            PersonName.From("Lovelace"), Birth, Role.USER, PasswordHash.From("hash"), CreatedAt, true, null);

        // Assert
        user.IsNew.Should().BeFalse();
    }

    [Fact]
    public void Rehydrate_ShouldRestoreEveryField()
    {
        // Arrange
        var id = Id.New();
        var email = Email.From("admin@weighttracker.test");

        // Act
        var user = User.Rehydrate(
            id, email, PersonName.From("Grace"), PersonName.From("Hopper"),
            Birth, Role.ADMIN, PasswordHash.From("stored-hash"), CreatedAt, false, DeactivatedAt);

        // Assert
        user.Id.Should().Be(id);
        user.Email.Should().Be(email);
        user.Name.Should().Be(PersonName.From("Grace"));
        user.Surname.Should().Be(PersonName.From("Hopper"));
        user.DateBirth.Should().Be(Birth);
        user.Role.Should().Be(Role.ADMIN);
        user.PasswordHash.Should().Be(PasswordHash.From("stored-hash"));
        user.CreatedAt.Should().Be(CreatedAt);
        user.IsActive.Should().BeFalse();
        user.DeactivatedAtUtc.Should().Be(DeactivatedAt);
    }

    [Theory]
    [InlineData(Role.TRAINER, true)]
    [InlineData(Role.ADMIN, true)]
    [InlineData(Role.USER, false)]
    public void CanTrain_ShouldOnlyAllowTrainersAndAdmins(Role role, bool expected)
    {
        // Arrange
        var user = User.Create(
            Email.From("user@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"),
            Birth, role, PasswordHash.From("hash"), CreatedAt);

        // Act
        var canTrain = user.CanTrain();

        // Assert
        canTrain.Should().Be(expected);
    }

    [Fact]
    public void UpdateProfile_ShouldReplaceNameAndSurname()
    {
        // Arrange
        var user = User.Create(
            Email.From("user@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"),
            Birth, Role.USER, PasswordHash.From("hash"), CreatedAt);

        // Act
        user.UpdateProfile(PersonName.From("Grace"), PersonName.From("Hopper"));

        // Assert
        user.Name.Should().Be(PersonName.From("Grace"));
        user.Surname.Should().Be(PersonName.From("Hopper"));
    }

    [Fact]
    public void UpdateProfile_ShouldLeaveEveryOtherFieldUntouched()
    {
        // Arrange
        var id = Id.New();
        var email = Email.From("trainer@weighttracker.test");
        var user = User.Rehydrate(
            id, email, PersonName.From("Ada"), PersonName.From("Lovelace"),
            Birth, Role.TRAINER, PasswordHash.From("stored-hash"), CreatedAt, true, null);

        // Act
        user.UpdateProfile(PersonName.From("Grace"), PersonName.From("Hopper"));

        // Assert
        user.Id.Should().Be(id);
        user.Email.Should().Be(email);
        user.Role.Should().Be(Role.TRAINER);
        user.DateBirth.Should().Be(Birth);
        user.PasswordHash.Should().Be(PasswordHash.From("stored-hash"));
        user.CreatedAt.Should().Be(CreatedAt);
        user.IsNew.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldStartActive()
    {
        // Arrange & Act
        var user = AnActiveUser();

        // Assert
        user.IsActive.Should().BeTrue();
        user.DeactivatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Deactivate_ShouldSwitchTheUserOffAndRecordWhen()
    {
        // Arrange
        var user = AnActiveUser();

        // Act
        user.Deactivate(DeactivatedAt);

        // Assert
        user.IsActive.Should().BeFalse();
        user.DeactivatedAtUtc.Should().Be(DeactivatedAt);
    }

    [Fact]
    public void Deactivate_ShouldRejectAnAlreadyDeactivatedUserAndKeepTheDate()
    {
        // Arrange
        var user = AnActiveUser();
        user.Deactivate(DeactivatedAt);

        // Act
        var act = () => user.Deactivate(DeactivatedAt.AddDays(1));

        // Assert
        act.Should().Throw<UserAlreadyDeactivatedException>();
        user.DeactivatedAtUtc.Should().Be(DeactivatedAt);
    }

    [Fact]
    public void Activate_ShouldRejectAnActiveUser()
    {
        // Arrange
        var user = AnActiveUser();

        // Act
        var act = () => user.Activate();

        // Assert
        act.Should().Throw<UserAlreadyActiveException>();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_ShouldSwitchADeactivatedUserBackOnAndClearTheDate()
    {
        // Arrange
        var user = AnActiveUser();
        user.Deactivate(DeactivatedAt);

        // Act
        user.Activate();

        // Assert
        user.IsActive.Should().BeTrue();
        user.DeactivatedAtUtc.Should().BeNull();
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void CanBeAssignedWorkouts_ShouldFollowTheActivationState(bool deactivated, bool expected)
    {
        // Arrange
        var user = AnActiveUser();
        if (deactivated)
            user.Deactivate(DeactivatedAt);

        // Act
        var canBeAssigned = user.CanBeAssignedWorkouts();

        // Assert
        canBeAssigned.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Rehydrate_ShouldRejectAnInconsistentActivationState(bool isActive, bool hasDeactivationDate)
    {
        // Arrange
        DateTime? deactivatedAt = hasDeactivationDate ? DeactivatedAt : null;

        // Act
        var act = () => User.Rehydrate(
            Id.New(), Email.From("user@weighttracker.test"), PersonName.From("Ada"),
            PersonName.From("Lovelace"), Birth, Role.USER, PasswordHash.From("hash"), CreatedAt, isActive, deactivatedAt);

        // Assert
        act.Should().Throw<InvalidUserActivationStateException>();
    }

    [Theory]
    [InlineData(Role.TRAINER, false, true)]
    [InlineData(Role.TRAINER, true, false)]
    [InlineData(Role.ADMIN, false, false)]
    [InlineData(Role.USER, false, false)]
    public void CanBeTrainer_ShouldOnlyAllowActiveTrainers(Role role, bool deactivated, bool expected)
    {
        // Arrange
        var user = User.Create(
            Email.From("user@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"),
            Birth, role, PasswordHash.From("hash"), CreatedAt);
        if (deactivated)
            user.Deactivate(DeactivatedAt);

        // Act
        var canBeTrainer = user.CanBeTrainer();

        // Assert
        canBeTrainer.Should().Be(expected);
    }

    [Theory]
    [InlineData(Role.USER, false, true)]
    [InlineData(Role.USER, true, false)]
    [InlineData(Role.TRAINER, false, false)]
    [InlineData(Role.ADMIN, false, false)]
    public void CanBeAthlete_ShouldOnlyAllowActiveUsers(Role role, bool deactivated, bool expected)
    {
        // Arrange
        var user = User.Create(
            Email.From("user@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"),
            Birth, role, PasswordHash.From("hash"), CreatedAt);
        if (deactivated)
            user.Deactivate(DeactivatedAt);

        // Act
        var canBeAthlete = user.CanBeAthlete();

        // Assert
        canBeAthlete.Should().Be(expected);
    }
}
