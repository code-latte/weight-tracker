using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Application.Common;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.TrainerAthletes;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Repositories;

/// <summary>
/// Covers the active-relationship queries and the database guarantee that an athlete has at most one active trainer.
/// </summary>
public class TrainerAthleteRepositoryTests : IntegrationTest
{
    public TrainerAthleteRepositoryTests(ApiFactory factory) : base(factory)
    { }

    private Task<TResult> OnRepository<TResult>(Func<ITrainerAthleteRepository, Task<TResult>> action)
        => Factory.WithScope(services => action(services.GetRequiredService<ITrainerAthleteRepository>()));

    private Task<bool> Write(Func<ITrainerAthleteRepository, Task> action)
        => Factory.WithScope(async services =>
        {
            await action(services.GetRequiredService<ITrainerAthleteRepository>());
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

            return true;
        });

    [Fact]
    public async Task GetActiveByTrainerIdAndUserId_ShouldReturnTheSeededActiveRelationshipOnly()
    {
        // Arrange & Act
        var relationship = await OnRepository(repository => repository.GetActiveByTrainerIdAndUserId(
            Id.From(SeedData.TrainerId), Id.From(SeedData.UserId), CancellationToken.None));

        // Assert
        relationship.Should().NotBeNull();
        relationship!.Id.Should().Be(Id.From(SeedData.ActiveTrainerAthleteId));
        relationship.Status.Should().Be(TrainerAthleteStatus.ACTIVE);
        relationship.IsNew.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsActiveByUserId_ShouldOnlyCountActiveRelationships()
    {
        // Arrange
        await Write(async repository =>
        {
            var active = await repository.GetActiveByTrainerIdAndUserId(Id.From(SeedData.TrainerId), Id.From(SeedData.UserId), CancellationToken.None);
            active!.End(SeedData.CreatedAt.AddDays(1));
            await repository.Save(active, CancellationToken.None);
        });

        // Act
        var exists = await OnRepository(repository => repository.ExistsActiveByUserId(
            Id.From(SeedData.UserId), CancellationToken.None));

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllActiveByTrainerId_ShouldExcludeEndedRelationships()
    {
        // Arrange & Act
        var relationships = await OnRepository(repository => repository.GetAllActiveByTrainerId(
            Id.From(SeedData.TrainerId), CancellationToken.None));

        // Assert
        relationships.Select(relationship => relationship.Id)
            .Should().BeEquivalentTo([Id.From(SeedData.ActiveTrainerAthleteId)]);
    }

    [Fact]
    public async Task GetAllActiveByUserIds_ShouldReturnOnlyActiveRelationshipsOfTheGivenAthletes()
    {
        // Arrange
        var userIds = new[] { Id.From(SeedData.UserId), Id.From(SeedData.TrainerId), Id.From(SeedData.AdminId) };

        // Act
        var relationships = await OnRepository(repository => repository.GetAllActiveByUserIds(
            userIds, CancellationToken.None));

        // Assert
        relationships.Select(relationship => relationship.Id)
            .Should().BeEquivalentTo([Id.From(SeedData.ActiveTrainerAthleteId)]);
    }

    [Fact]
    public async Task GetAllActiveByUserIds_ShouldIgnoreAthletesNotAsked()
    {
        // Arrange
        var userIds = new[] { Id.From(SeedData.TrainerId), Id.From(SeedData.AdminId) };

        // Act
        var relationships = await OnRepository(repository => repository.GetAllActiveByUserIds(
            userIds, CancellationToken.None));

        // Assert
        relationships.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllActiveByUserIds_ShouldIgnoreEndedRelationships()
    {
        // Arrange
        await Write(async repository =>
        {
            var active = await repository.GetActiveByTrainerIdAndUserId(Id.From(SeedData.TrainerId), Id.From(SeedData.UserId), CancellationToken.None);
            active!.End(SeedData.CreatedAt.AddDays(1));
            await repository.Save(active, CancellationToken.None);
        });

        // Act
        var relationships = await OnRepository(repository => repository.GetAllActiveByUserIds(
            [Id.From(SeedData.UserId)], CancellationToken.None));

        // Assert
        relationships.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllActiveByUserIds_ShouldReturnEmptyForNoAthletes()
    {
        // Arrange & Act
        var relationships = await OnRepository(repository => repository.GetAllActiveByUserIds(
            [], CancellationToken.None));

        // Assert
        relationships.Should().BeEmpty();
    }

    [Fact]
    public async Task Save_ShouldPersistTheEndOfARelationship()
    {
        // Arrange
        var endedAt = SeedData.CreatedAt.AddDays(5);

        // Act
        await Write(async repository =>
        {
            var active = await repository.GetActiveByTrainerIdAndUserId(Id.From(SeedData.TrainerId), Id.From(SeedData.UserId), CancellationToken.None);
            active!.End(endedAt);
            await repository.Save(active, CancellationToken.None);
        });

        // Assert
        var stillActive = await OnRepository(repository => repository.GetActiveByTrainerIdAndUserId(
            Id.From(SeedData.TrainerId), Id.From(SeedData.UserId), CancellationToken.None));
        stillActive.Should().BeNull();
    }

    [Fact]
    public async Task Save_ShouldRejectASecondActiveRelationshipForTheSameAthlete()
    {
        // Arrange
        var duplicate = TrainerAthlete.Create(Id.From(SeedData.AdminId), Id.From(SeedData.UserId), SeedData.CreatedAt);

        // Act
        var act = () => Write(repository => repository.Save(duplicate, CancellationToken.None));

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
