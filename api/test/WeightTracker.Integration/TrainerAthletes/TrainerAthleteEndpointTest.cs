using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Persistence;
using WeightTracker.Integration.Bootstrap;

namespace WeightTracker.Integration.TrainerAthletes;

/// <summary>
/// Shared arrangements for the trainer–athlete and invitation endpoint tests. Extra users and relationships are
/// created through the API as an administrator, never written directly, so every caller still authenticates
/// through <c>POST /auth/login</c>.
/// </summary>
public abstract class TrainerAthleteEndpointTest : IntegrationTest
{
    protected TrainerAthleteEndpointTest(ApiFactory factory) : base(factory)
    { }

    /// <summary>Creates a user with the seeded password, so they can log in, and returns their id.</summary>
    protected async Task<Guid> CreateUser(string email, Role role, string? name = null, string? surname = null)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        var response = await admin.PostAsJsonAsync(
            "/users", new { email, password = SeedCredentials.Password, role = (int)role, name, surname });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        return body.GetProperty("id").GetGuid();
    }

    protected async Task Deactivate(Guid userId)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        (await admin.PutAsync($"/users/{userId}/deactivate", null)).EnsureSuccessStatusCode();
    }

    /// <summary>Links an athlete to a trainer through the admin route.</summary>
    protected async Task Link(Guid trainerId, Guid athleteId)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        (await admin.PostAsJsonAsync($"/trainer-athletes/{trainerId}", new { athleteId })).EnsureSuccessStatusCode();
    }

    protected Task<List<TrainerAthleteEntity>> StoredRelationships(Guid trainerId, Guid athleteId)
        => Factory.WithScope(services => services.GetRequiredService<WeightTrackerDbContext>()
            .TrainerAthletes.AsNoTracking()
            .Where(relationship => relationship.TrainerId == trainerId && relationship.UserId == athleteId)
            .ToListAsync());

    protected Task<int> StoredRelationshipCount()
        => Factory.WithScope(services => services.GetRequiredService<WeightTrackerDbContext>()
            .TrainerAthletes.AsNoTracking().CountAsync());

    protected static async Task<JsonElement> Body(HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

    protected static async Task<List<JsonElement>> Items(HttpResponseMessage response)
        => (await Body(response)).EnumerateArray().ToList();

    protected static void ShouldHaveExactly(JsonElement element, params string[] properties)
        => element.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(properties);
}
