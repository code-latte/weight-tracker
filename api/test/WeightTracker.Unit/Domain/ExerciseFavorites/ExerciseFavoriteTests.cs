using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.ExerciseFavorites;
using Xunit;

namespace WeightTracker.Unit.Domain.ExerciseFavorites;

public class ExerciseFavoriteTests
{
    private static readonly DateTime Now = new(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldBuildANewFavoriteWithAFreshId()
    {
        // Arrange
        var userId = Id.New();
        var exerciseId = Id.New();

        // Act
        var favorite = ExerciseFavorite.Create(userId, exerciseId, Now);

        // Assert
        favorite.IsNew.Should().BeTrue();
        favorite.Id.Should().NotBe(userId).And.NotBe(exerciseId);
        favorite.UserId.Should().Be(userId);
        favorite.ExerciseId.Should().Be(exerciseId);
        favorite.CreatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Create_ShouldGenerateADifferentIdEachTime()
    {
        // Arrange
        var userId = Id.New();
        var exerciseId = Id.New();

        // Act
        var first = ExerciseFavorite.Create(userId, exerciseId, Now);
        var second = ExerciseFavorite.Create(userId, exerciseId, Now);

        // Assert
        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public void Rehydrate_ShouldRestoreEveryFieldAndMarkTheFavoriteAsNotNew()
    {
        // Arrange
        var id = Id.New();
        var userId = Id.New();
        var exerciseId = Id.New();

        // Act
        var favorite = ExerciseFavorite.Rehydrate(id, userId, exerciseId, Now);

        // Assert
        favorite.IsNew.Should().BeFalse();
        favorite.Id.Should().Be(id);
        favorite.UserId.Should().Be(userId);
        favorite.ExerciseId.Should().Be(exerciseId);
        favorite.CreatedAtUtc.Should().Be(Now);
    }
}
