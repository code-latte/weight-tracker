using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;

namespace WeightTracker.Integration.WorkoutTemplates;

/// <summary>
/// Shared plumbing of the workout template endpoint tests. The seeded trainer owns one template: Sentadilla and
/// Press Banca grouped in superseries 1 (4 sets each), then Plancha (3 sets). Extra users, templates and exercises
/// are arranged through the API, so no row is written directly and the shared seed stays unchanged.
/// </summary>
public abstract class WorkoutTemplateEndpointTest : IntegrationTest
{
    protected const string TemplatesRoute = "/workout-templates";

    protected static readonly string SeededTemplateRoute = $"{TemplatesRoute}/{SeedData.TrainerTemplateId}";

    protected WorkoutTemplateEndpointTest(ApiFactory factory) : base(factory)
    { }

    protected static async Task<JsonElement> Body(HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

    protected static IReadOnlyList<Guid> Ids(JsonElement list)
        => list.EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).ToList();

    protected static IReadOnlyList<Guid> ExerciseIds(JsonElement owner)
        => owner.GetProperty("exercises").EnumerateArray().Select(entry => entry.GetProperty("exerciseId").GetGuid()).ToList();

    protected static IReadOnlyList<int?> SuperSeries(JsonElement owner)
        => owner.GetProperty("exercises").EnumerateArray()
            .Select(entry => entry.GetProperty("superSeriesId").ValueKind == JsonValueKind.Null
                ? (int?)null
                : entry.GetProperty("superSeriesId").GetInt32())
            .ToList();

    protected static object Reps(int count, int target) => new { count, targetType = "Reps", target };

    protected static object AnExerciseBody(Guid exerciseId, object effort, params object[] sets)
        => new { exerciseId, effort, sets };

    protected static Task<HttpResponseMessage> Send(HttpClient client, string method, string route, object? body)
        => method switch
        {
            "GET" => client.GetAsync(route),
            "POST" => client.PostAsJsonAsync(route, body),
            "DELETE" => client.DeleteAsync(route),
            _ => client.PutAsJsonAsync(route, body),
        };

    /// <summary>
    /// Reads a template as the admin, who may read any of them.
    /// </summary>
    protected async Task<JsonElement> TemplateAsAdmin(Guid templateId)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var response = await admin.GetAsync($"{TemplatesRoute}/{templateId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "the template must still be readable by the admin");

        return await Body(response);
    }

    /// <summary>
    /// Reads a workout as the admin, who may read any of them.
    /// </summary>
    protected async Task<JsonElement> WorkoutAsAdmin(Guid workoutId)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var response = await admin.GetAsync($"/workouts/{workoutId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "the workout must still be readable by the admin");

        return await Body(response);
    }

    /// <summary>
    /// Number of templates of every trainer, as the admin sees them.
    /// </summary>
    protected async Task<int> TemplateCountAsAdmin()
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        return (await Body(await admin.GetAsync(TemplatesRoute))).GetArrayLength();
    }

    protected async Task SeededTemplateShouldBeUnchanged()
    {
        var template = await TemplateAsAdmin(SeedData.TrainerTemplateId);

        template.GetProperty("name").GetString().Should().Be(SeedData.TrainerTemplateName);
        ExerciseIds(template).Should().Equal(SeedData.SentadillaId, SeedData.PressBancaId, SeedData.PlanchaId);
        SuperSeries(template).Should().Equal(SeedData.SeededSuperSeries, SeedData.SeededSuperSeries, null);
    }

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
    /// Creates an empty template for a trainer through <c>POST /workout-templates</c> as the admin.
    /// </summary>
    protected async Task<Guid> GivenTemplateFor(Guid trainerId, string name = "Other template")
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        var response = await admin.PostAsJsonAsync(TemplatesRoute, new { name, trainerId });

        response.StatusCode.Should().Be(HttpStatusCode.Created, "the extra template must be arranged first");

        return (await Body(response)).GetProperty("id").GetGuid();
    }

    /// <summary>
    /// Creates a custom exercise owned by the caller through <c>POST /exercises</c>.
    /// </summary>
    protected static async Task<Guid> GivenCustomExercise(HttpClient owner)
    {
        var response = await owner.PostAsJsonAsync("/exercises", new { languageCode = "es", name = "Remo con banda", muscle = "MIDDLE_BACK", type = "STRENGTH" });

        response.StatusCode.Should().Be(HttpStatusCode.Created, "the custom exercise must be arranged first");

        return (await Body(response)).GetProperty("id").GetGuid();
    }

    protected async Task GivenDeactivated(Guid userId)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        var response = await admin.PutAsync($"/users/{userId}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "the user must be deactivated first");
    }

    /// <summary>
    /// Creates a standalone workout for the seeded user from the seeded template, as the seeded trainer.
    /// </summary>
    protected async Task<Guid> GivenWorkoutFromSeededTemplate()
    {
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        var response = await trainer.PostAsJsonAsync(
            $"{SeededTemplateRoute}/workouts", new { userId = SeedData.UserId, workoutDate = "2026-12-01" });

        response.StatusCode.Should().Be(HttpStatusCode.Created, "the workout must be created from the template first");

        return (await Body(response)).GetProperty("id").GetGuid();
    }
}
