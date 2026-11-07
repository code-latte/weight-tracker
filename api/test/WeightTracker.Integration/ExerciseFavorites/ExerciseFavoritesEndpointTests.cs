using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using WeightTracker.Integration.Exercises;
using Xunit;

namespace WeightTracker.Integration.ExerciseFavorites;

/// <summary>
/// Covers <c>PUT</c>, <c>DELETE</c> and <c>GET</c> of <c>/exercise-favorites</c>. Every route needs a token, any
/// role may keep favorites, and a caller only ever reaches their own.
/// </summary>
public class ExerciseFavoritesEndpointTests : ExerciseEndpointTest
{
    public ExerciseFavoritesEndpointTests(ApiFactory factory) : base(factory)
    { }

    private static HttpRequestMessage RequestFor(string method)
        => method switch
        {
            "PUT" => new HttpRequestMessage(HttpMethod.Put, $"/exercise-favorites/{SeedData.SentadillaId}"),
            "DELETE" => new HttpRequestMessage(HttpMethod.Delete, $"/exercise-favorites/{SeedData.SentadillaId}"),
            _ => new HttpRequestMessage(HttpMethod.Get, "/exercise-favorites"),
        };

    private static Task<HttpResponseMessage> Mark(HttpClient client, Guid exerciseId)
        => client.PutAsync($"/exercise-favorites/{exerciseId}", null);

    private static async Task<JsonElement> Favorites(HttpClient client)
        => await client.GetFromJsonAsync<JsonElement>("/exercise-favorites", JsonOptions);

    private static IReadOnlyList<Guid> ExerciseIdsOf(JsonElement favorites)
        => favorites.EnumerateArray().Select(favorite => favorite.GetProperty("exerciseId").GetGuid()).ToList();

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("GET")]
    public async Task Routes_ShouldRejectAnonymousCallers(string method)
    {
        // Arrange
        var request = RequestFor(method);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("GET")]
    public async Task Routes_ShouldRejectAGarbageToken(string method)
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");
        var request = RequestFor(method);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("GET")]
    public async Task Routes_ShouldRejectATokenWhoseUserNoLongerExists(string method)
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);
        (await Mark(client, SeedData.PlanchaId)).EnsureSuccessStatusCode();
        await Factory.RemoveUser(SeedCredentials.User);
        var request = RequestFor(method);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldMarkASystemExerciseForAnAthlete()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await Mark(user, SeedData.SentadillaId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var favorite = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        favorite.GetProperty("exerciseId").GetGuid().Should().Be(SeedData.SentadillaId);
        favorite.GetProperty("createdAtUtc").ValueKind.Should().Be(JsonValueKind.String);
        ExerciseIdsOf(await Favorites(user)).Should().Equal(SeedData.SentadillaId);
    }

    [Fact]
    public async Task Put_ShouldLetATrainerMarkTheirOwnCustomExercise()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var custom = await CreateExercise(trainer);

        // Act
        var response = await Mark(trainer, custom);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ExerciseIdsOf(await Favorites(trainer)).Should().Equal(custom);
    }

    [Fact]
    public async Task Put_ShouldLetAnAdminMarkAnyExercise()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var custom = await CreateExercise(user);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await Mark(admin, custom);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Put_ShouldKeepTheOriginalDateAndStoreASingleFavoriteWhenMarkedTwice()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var first = await (await Mark(user, SeedData.SentadillaId)).Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        // Act
        var secondResponse = await Mark(user, SeedData.SentadillaId);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var second = await secondResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        second.GetProperty("createdAtUtc").GetDateTime().Should().Be(first.GetProperty("createdAtUtc").GetDateTime());
        ExerciseIdsOf(await Favorites(user)).Should().Equal(SeedData.SentadillaId);
    }

    [Fact]
    public async Task Put_ShouldRejectAnUnknownExercise()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await Mark(user, Id.New().Value);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ExerciseIdsOf(await Favorites(user)).Should().BeEmpty();
    }

    [Fact]
    public async Task Put_ShouldTreatACustomExerciseTheCallerCannotSeeAsMissing()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var custom = await CreateExercise(user);
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await Mark(trainer, custom);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ExerciseIdsOf(await Favorites(trainer)).Should().BeEmpty();
    }

    [Fact]
    public async Task Put_ShouldLetAnAthleteMarkTheirTrainersExerciseThroughAPublishedWorkout()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var custom = await CreateExercise(trainer);
        await AddToPublishedWorkout(trainer, SeedData.TrainedWorkoutId, custom);
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await Mark(user, custom);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Put_ShouldRejectAnArchivedExercise()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var custom = await CreateExercise(user);
        (await user.DeleteAsync($"/exercises/{custom}")).EnsureSuccessStatusCode();

        // Act
        var response = await Mark(user, custom);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ExerciseIdsOf(await Favorites(user)).Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_ShouldRemoveTheFavorite()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        (await Mark(user, SeedData.SentadillaId)).EnsureSuccessStatusCode();

        // Act
        var response = await user.DeleteAsync($"/exercise-favorites/{SeedData.SentadillaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        ExerciseIdsOf(await Favorites(user)).Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_ShouldSucceedWhenTheExerciseIsNotAFavorite()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.DeleteAsync($"/exercise-favorites/{SeedData.SentadillaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_ShouldKeepTheFavoritesOfOtherExercises()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        (await Mark(user, SeedData.SentadillaId)).EnsureSuccessStatusCode();
        (await Mark(user, SeedData.PlanchaId)).EnsureSuccessStatusCode();

        // Act
        var response = await user.DeleteAsync($"/exercise-favorites/{SeedData.SentadillaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        ExerciseIdsOf(await Favorites(user)).Should().Equal(SeedData.PlanchaId);
    }

    [Fact]
    public async Task Delete_ShouldRejectAnUnknownExercise()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.DeleteAsync($"/exercise-favorites/{Id.New().Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldLetAnOwnerRemoveAFavoriteWhoseExerciseTheyArchived()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var custom = await CreateExercise(user);
        (await Mark(user, custom)).EnsureSuccessStatusCode();
        (await user.DeleteAsync($"/exercises/{custom}")).EnsureSuccessStatusCode();

        // Act
        var response = await user.DeleteAsync($"/exercise-favorites/{custom}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        ExerciseIdsOf(await Favorites(user)).Should().BeEmpty();
    }

    [Fact]
    public async Task Get_ShouldListTheNewestFavoriteFirst()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        (await Mark(user, SeedData.SentadillaId)).EnsureSuccessStatusCode();
        (await Mark(user, SeedData.PlanchaId)).EnsureSuccessStatusCode();

        // Act
        var favorites = await Favorites(user);

        // Assert
        ExerciseIdsOf(favorites).Should().Equal(SeedData.PlanchaId, SeedData.SentadillaId);
        favorites.EnumerateArray().First().EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo(["exerciseId", "createdAtUtc"]);
    }

    [Fact]
    public async Task Get_ShouldReturnAnEmptyListWhenNothingWasMarked()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var favorites = await Favorites(user);

        // Assert
        favorites.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Get_ShouldExcludeTheFavoritesOfOtherUsers()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        (await Mark(trainer, SeedData.PlanchaId)).EnsureSuccessStatusCode();
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        (await Mark(user, SeedData.SentadillaId)).EnsureSuccessStatusCode();

        // Act
        var favorites = await Favorites(user);

        // Assert
        ExerciseIdsOf(favorites).Should().Equal(SeedData.SentadillaId);
    }

    [Fact]
    public async Task Get_ShouldShowAnAdminOnlyTheirOwnFavorites()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        (await Mark(user, SeedData.SentadillaId)).EnsureSuccessStatusCode();
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var favorites = await Favorites(admin);

        // Assert
        favorites.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Get_ShouldKeepListingAFavoriteWhoseExerciseWasArchived()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var custom = await CreateExercise(user);
        (await Mark(user, custom)).EnsureSuccessStatusCode();
        (await user.DeleteAsync($"/exercises/{custom}")).EnsureSuccessStatusCode();

        // Act
        var favorites = await Favorites(user);

        // Assert
        ExerciseIdsOf(favorites).Should().Equal(custom);
    }
}
