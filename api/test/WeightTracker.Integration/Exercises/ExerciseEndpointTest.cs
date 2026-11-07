using System.Net.Http.Json;
using System.Text.Json;
using WeightTracker.Integration.Bootstrap;

namespace WeightTracker.Integration.Exercises;

/// <summary>
/// Shared plumbing of the tests of the exercise routes. Custom exercises are created and attached to workouts
/// through the API, as the seeded dataset only holds the system catalog.
/// </summary>
public abstract class ExerciseEndpointTest : IntegrationTest
{
    protected ExerciseEndpointTest(ApiFactory factory) : base(factory)
    { }

    protected static object ExerciseBody(
        string name = "Remo con banda",
        string languageCode = "es",
        string muscle = "MIDDLE_BACK",
        string type = "STRENGTH",
        string? videoUrl = null)
        => new { languageCode, name, muscle, type, videoUrl };

    /// <summary>Creates a custom exercise as the given caller and returns its identifier.</summary>
    protected async Task<Guid> CreateExercise(HttpClient client, object? body = null)
    {
        var response = await client.PostAsJsonAsync("/exercises", body ?? ExerciseBody());

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("id").GetGuid();
    }

    /// <summary>Adds the exercise to the workout as the given caller, with one set of ten reps.</summary>
    protected async Task AddToWorkout(HttpClient client, Guid workoutId, Guid exerciseId)
    {
        var body = new { exerciseId, effort = "MID", sets = new[] { new { count = 1, targetType = "Reps", target = 10 } } };

        var response = await client.PostAsJsonAsync($"/workouts/{workoutId}/exercises", body);

        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Adds the exercise to a published workout the trainer prescribes. Only a draft can be edited, so the trainer
    /// unpublishes it, adds the exercise and publishes it again, all through the API.
    /// </summary>
    protected async Task AddToPublishedWorkout(HttpClient trainer, Guid workoutId, Guid exerciseId)
    {
        (await trainer.PutAsJsonAsync($"/workouts/{workoutId}/status", new { status = "DRAFT" })).EnsureSuccessStatusCode();
        await AddToWorkout(trainer, workoutId, exerciseId);
        (await trainer.PutAsJsonAsync($"/workouts/{workoutId}/status", new { status = "PUBLISHED" })).EnsureSuccessStatusCode();
    }

    protected async Task<JsonElement> ExercisesSeenBy(HttpClient client)
        => await client.GetFromJsonAsync<JsonElement>("/exercises", JsonOptions);

    protected static IReadOnlyList<Guid> IdsOf(JsonElement exercises)
        => exercises.EnumerateArray().Select(exercise => exercise.GetProperty("id").GetGuid()).ToList();
}
