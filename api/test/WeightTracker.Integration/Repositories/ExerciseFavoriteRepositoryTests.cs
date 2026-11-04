using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Application.Common;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.ExerciseFavorites;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Repositories;

public class ExerciseFavoriteRepositoryTests : IntegrationTest
{
    public ExerciseFavoriteRepositoryTests(ApiFactory factory) : base(factory)
    { }

    private static readonly Id User = Id.From(SeedData.UserId);

    private static readonly Id Trainer = Id.From(SeedData.TrainerId);

    private static readonly Id Sentadilla = Id.From(SeedData.SentadillaId);

    private static readonly Id Plancha = Id.From(SeedData.PlanchaId);

    private static readonly DateTime Start = new(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc);

    private Task<bool> Write(Func<IExerciseFavoriteRepository, Task> action)
        => Factory.WithScope(async services =>
        {
            await action(services.GetRequiredService<IExerciseFavoriteRepository>());
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

            return true;
        });

    private Task<ExerciseFavorite?> Read(Id userId, Id exerciseId)
        => Factory.WithScope(services => services.GetRequiredService<IExerciseFavoriteRepository>()
            .GetByUserAndExercise(userId, exerciseId, CancellationToken.None));

    private Task<IEnumerable<ExerciseFavorite>> ReadAll(Id userId)
        => Factory.WithScope(services => services.GetRequiredService<IExerciseFavoriteRepository>()
            .GetAllByUser(userId, CancellationToken.None));

    [Fact]
    public async Task Save_ShouldPersistAFavoriteThatCanBeReadByUserAndExercise()
    {
        // Arrange
        var favorite = ExerciseFavorite.Create(User, Sentadilla, Start);
        await Write(repository => repository.Save(favorite, CancellationToken.None));

        // Act
        var stored = await Read(User, Sentadilla);

        // Assert
        stored!.Id.Should().Be(favorite.Id);
        stored.CreatedAtUtc.Should().Be(Start);
        stored.IsNew.Should().BeFalse();
    }

    [Fact]
    public async Task GetByUserAndExercise_ShouldReturnNothingForAnExerciseThatIsNotAFavorite()
    {
        // Arrange
        await Write(repository => repository.Save(ExerciseFavorite.Create(User, Sentadilla, Start), CancellationToken.None));

        // Act
        var stored = await Read(User, Plancha);

        // Assert
        stored.Should().BeNull();
    }

    [Fact]
    public async Task GetByUserAndExercise_ShouldNotReturnAnotherUsersFavorite()
    {
        // Arrange
        await Write(repository => repository.Save(ExerciseFavorite.Create(Trainer, Sentadilla, Start), CancellationToken.None));

        // Act
        var stored = await Read(User, Sentadilla);

        // Assert
        stored.Should().BeNull();
    }

    [Fact]
    public async Task GetAllByUser_ShouldReturnTheNewestFirstAndExcludeOtherUsers()
    {
        // Arrange
        var older = ExerciseFavorite.Create(User, Sentadilla, Start);
        var newer = ExerciseFavorite.Create(User, Plancha, Start.AddMinutes(5));
        var other = ExerciseFavorite.Create(Trainer, Sentadilla, Start.AddMinutes(10));
        await Write(async repository =>
        {
            await repository.Save(older, CancellationToken.None);
            await repository.Save(newer, CancellationToken.None);
            await repository.Save(other, CancellationToken.None);
        });

        // Act
        var favorites = await ReadAll(User);

        // Assert
        favorites.Select(favorite => favorite.Id).Should().Equal(newer.Id, older.Id);
    }

    [Fact]
    public async Task GetAllByUser_ShouldReturnNothingForAUserWithoutFavorites()
    {
        // Arrange & Act
        var favorites = await ReadAll(User);

        // Assert
        favorites.Should().BeEmpty();
    }

    [Fact]
    public async Task Remove_ShouldDeleteTheStoredFavoriteAndKeepTheOthers()
    {
        // Arrange
        var removed = ExerciseFavorite.Create(User, Sentadilla, Start);
        var kept = ExerciseFavorite.Create(User, Plancha, Start.AddMinutes(1));
        await Write(async repository =>
        {
            await repository.Save(removed, CancellationToken.None);
            await repository.Save(kept, CancellationToken.None);
        });
        var stored = await Read(User, Sentadilla);
        await Write(repository => repository.Remove(stored!, CancellationToken.None));

        // Act
        var favorites = await ReadAll(User);

        // Assert
        favorites.Select(favorite => favorite.Id).Should().Equal(kept.Id);
    }

    [Fact]
    public async Task Save_ShouldLeaveAStoredFavoriteUntouched()
    {
        // Arrange
        var favorite = ExerciseFavorite.Create(User, Sentadilla, Start);
        await Write(repository => repository.Save(favorite, CancellationToken.None));
        var stored = await Read(User, Sentadilla);
        await Write(repository => repository.Save(stored!, CancellationToken.None));

        // Act
        var favorites = await ReadAll(User);

        // Assert
        favorites.Should().ContainSingle().Which.CreatedAtUtc.Should().Be(Start);
    }
}
