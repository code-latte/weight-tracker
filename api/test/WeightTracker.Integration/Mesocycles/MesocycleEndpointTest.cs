using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;

namespace WeightTracker.Integration.Mesocycles;

/// <summary>
/// Shared plumbing of the mesocycle endpoint tests. Extra users and mesocycles are arranged through the API as
/// the admin, so no row is written directly and the shared seed stays unchanged.
/// </summary>
public abstract class MesocycleEndpointTest : IntegrationTest
{
    protected static readonly string SeededMesocycleRoute = $"/mesocycles/{SeedData.MesocycleId}";

    protected MesocycleEndpointTest(ApiFactory factory) : base(factory)
    { }

    protected static async Task<JsonElement> Body(HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

    protected static IReadOnlyList<Guid> Ids(JsonElement list)
        => list.EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).ToList();

    protected static object AMesocycleBody(Guid athleteId, Guid? trainerId = null, string startDate = "2026-11-09", int durationInWeeks = 4)
        => new
        {
            trainerId,
            athleteId,
            name = "Strength block",
            objective = "Peak strength",
            startDate,
            durationInWeeks,
        };

    /// <summary>
    /// Creates a user through <c>POST /users</c> and returns their id and email.
    /// </summary>
    protected async Task<(Guid Id, string Email)> GivenUser(Role role)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var email = $"{Guid.NewGuid():N}@weighttracker.test";

        var response = await admin.PostAsJsonAsync("/users", new
        {
            email,
            password = SeedCredentials.Password,
            role = (int)role,
            name = "Extra",
            surname = "User",
            dateBirth = "1990-01-01",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created, "the extra user must be arranged first");

        return ((await Body(response)).GetProperty("id").GetGuid(), email);
    }

    /// <summary>
    /// Creates a mesocycle through <c>POST /mesocycles</c> as the admin, who needs no trainer–athlete relationship.
    /// </summary>
    protected async Task<Guid> GivenMesocycle(Guid trainerId, Guid athleteId, string startDate = "2026-11-09")
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        var response = await admin.PostAsJsonAsync("/mesocycles", AMesocycleBody(athleteId, trainerId, startDate));

        response.StatusCode.Should().Be(HttpStatusCode.Created, "the extra mesocycle must be arranged first");

        return (await Body(response)).GetProperty("id").GetGuid();
    }

    /// <summary>
    /// Ends the seeded relationship between the trainer and the user, as the trainer.
    /// </summary>
    protected async Task GivenSeededRelationshipEnded()
    {
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        var response = await trainer.PutAsJsonAsync(
            $"/trainer-athletes/{SeedData.TrainerId}/end", new { athleteId = SeedData.UserId });

        response.StatusCode.Should().Be(HttpStatusCode.OK, "the relationship must be ended first");
    }

    protected async Task<JsonElement> SeededMesocycle()
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        return await Body(await admin.GetAsync(SeededMesocycleRoute));
    }
}
