using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Integration.Bootstrap;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Shared plumbing of the workout tests: arranging a workout in a given status, and reading back what a workout
/// stores. Statuses are set through the API as an admin, who may skip the lifecycle, so no row is written directly.
/// Read helpers needed by more than one workout test class belong here rather than in each class.
/// </summary>
public abstract class WorkoutEndpointTest : IntegrationTest
{
    protected WorkoutEndpointTest(ApiFactory factory) : base(factory)
    { }

    protected async Task GivenWorkoutStatus(Guid workoutId, string status)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        var response = await admin.PutAsJsonAsync($"/workouts/{workoutId}/status", new { status });

        response.StatusCode.Should().Be(HttpStatusCode.OK, "the workout must be arranged in '{0}' first", status);
    }

    /// <summary>
    /// Completes a prescribed workout and reviews it as an admin, sending back the results time it currently has.
    /// </summary>
    protected async Task GivenReviewed(Guid workoutId)
    {
        await GivenWorkoutStatus(workoutId, "COMPLETED");
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var resultsUpdatedAt = await ResultsUpdatedAtOf(workoutId);

        var response = await admin.PutAsJsonAsync($"/workouts/{workoutId}/review", new { resultsUpdatedAt });

        response.StatusCode.Should().Be(HttpStatusCode.OK, "the workout must be arranged as reviewed first");
    }

    /// <summary>The <c>resultsUpdatedAt</c> of a workout as an admin reads it, <c>null</c> when its results never changed.</summary>
    protected async Task<DateTime?> ResultsUpdatedAtOf(Guid workoutId)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var workout = await admin.GetFromJsonAsync<JsonElement>($"/workouts/{workoutId}", JsonOptions);
        var value = workout.GetProperty("resultsUpdatedAt");

        return value.ValueKind == JsonValueKind.Null ? null : value.GetDateTime();
    }

    /// <summary>The <c>startedAt</c> of a workout as an admin reads it, <c>null</c> when it was never started.</summary>
    protected async Task<DateTime?> StartedAtOf(Guid workoutId)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var workout = await admin.GetFromJsonAsync<JsonElement>($"/workouts/{workoutId}", JsonOptions);
        var value = workout.GetProperty("startedAt");

        return value.ValueKind == JsonValueKind.Null ? null : value.GetDateTime();
    }

    /// <summary>The <c>completedAt</c> of a workout as an admin reads it, <c>null</c> when it was never completed.</summary>
    protected async Task<DateTime?> CompletedAtOf(Guid workoutId)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var workout = await admin.GetFromJsonAsync<JsonElement>($"/workouts/{workoutId}", JsonOptions);
        var value = workout.GetProperty("completedAt");

        return value.ValueKind == JsonValueKind.Null ? null : value.GetDateTime();
    }
}
