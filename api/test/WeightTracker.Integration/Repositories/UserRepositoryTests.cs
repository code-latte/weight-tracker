using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Repositories;

/// <summary>
/// Covers the lookups behind the email business identifier and the trainer eligibility
/// rule. Both are repository queries, so they are verified against the real database
/// rather than against a mock.
/// </summary>
public class UserRepositoryTests : IntegrationTest
{
    public UserRepositoryTests(ApiFactory factory) : base(factory)
    { }

    private Task<TResult> OnRepository<TResult>(Func<IUserRepository, Task<TResult>> action)
        => Factory.WithScope(services => action(services.GetRequiredService<IUserRepository>()));

    [Fact]
    public async Task GetByEmail_ShouldFindASeededUser()
    {
        // Arrange & Act
        var user = await OnRepository(repository => repository.GetByEmail(Email.From(SeedData.UserEmail), CancellationToken.None));

        // Assert
        user.Should().NotBeNull();
        user!.Id.Should().Be(Id.From(SeedData.UserId));
        user.Role.Should().Be(Role.USER);
        user.IsNew.Should().BeFalse();
    }

    [Fact]
    public async Task GetByEmail_ShouldBeCaseInsensitive()
    {
        // Arrange & Act
        var user = await OnRepository(repository => repository.GetByEmail(Email.From("USER@WeightTracker.TEST"), CancellationToken.None));

        // Assert
        user.Should().NotBeNull();
        user!.Id.Should().Be(Id.From(SeedData.UserId));
    }

    [Fact]
    public async Task GetByEmail_ShouldReturnNullForAnUnknownAddress()
    {
        // Arrange & Act
        var user = await OnRepository(
            repository => repository.GetByEmail(Email.From("nobody@weighttracker.test"), CancellationToken.None));

        // Assert
        user.Should().BeNull();
    }

    [Fact]
    public async Task ExistsByEmail_ShouldDetectTheUniqueBusinessIdentifier()
    {
        // Arrange & Act
        var exists = await OnRepository(repository => repository.ExistsByEmail(Email.From(SeedData.UserEmail), CancellationToken.None));

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByEmail_ShouldBeFalseForAnUnusedAddress()
    {
        // Arrange & Act
        var exists = await OnRepository(
            repository => repository.ExistsByEmail(Email.From("nobody@weighttracker.test"), CancellationToken.None));

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetById_ShouldRestoreTheWholeUser()
    {
        // Arrange & Act
        var user = await OnRepository(repository => repository.GetById(Id.From(SeedData.TrainerId), CancellationToken.None));

        // Assert
        user.Should().NotBeNull();
        user!.Email.Should().Be(Email.From(SeedData.TrainerEmail));
        user.Name.Should().Be(PersonName.From("Grace"));
        user.Surname.Should().Be(PersonName.From("Hopper"));
        user.Role.Should().Be(Role.TRAINER);
        user.CanTrain().Should().BeTrue();
        user.PasswordHash.Value.Should().NotBeNullOrWhiteSpace();
        user.PasswordHash.Value.Should().NotBe(
            SeedData.Password, "credentials are stored hashed, never in plain text");
    }

    [Fact]
    public async Task GetById_ShouldReturnNullForAnUnknownUser()
    {
        // Arrange & Act
        var user = await OnRepository(repository => repository.GetById(Id.New(), CancellationToken.None));

        // Assert
        user.Should().BeNull();
    }

    [Fact]
    public async Task GetAllByIds_ShouldReturnOnlyTheKnownRequestedUsers()
    {
        // Arrange
        var requested = new[] { Id.From(SeedData.UserId), Id.From(SeedData.AdminId), Id.New() };

        // Act
        var users = await OnRepository(repository => repository.GetAllByIds(requested, CancellationToken.None));

        // Assert
        users.Select(user => user.Id).Should().BeEquivalentTo([Id.From(SeedData.UserId), Id.From(SeedData.AdminId)]);
    }
}
