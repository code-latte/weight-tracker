using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Shared plumbing of the tests of the stats routes, which read finished workouts with their recorded sets. Every
/// workout is arranged through the API: an admin creates it, fills it and moves it through its statuses, and the
/// sets are recorded by its owner, so no row is written directly.
/// </summary>
public abstract class WorkoutStatsEndpointTest : WorkoutExerciseEndpointTest
{
    protected WorkoutStatsEndpointTest(ApiFactory factory) : base(factory)
    { }

    protected static readonly Guid User = SeedData.UserId;

    protected static readonly Guid Trainer = SeedData.TrainerId;

    protected static readonly Guid Press = SeedData.PressBancaId;

    protected static readonly Guid Squat = SeedData.SentadillaId;

    protected static readonly Guid Plank = SeedData.PlanchaId;

    /// <summary>One exercise of an arranged workout: how it is prescribed and the sets recorded for it, in order.</summary>
    protected sealed record Entry(Guid ExerciseId, object[] Sets, object[] Records);

    protected static object Rec(int reps, decimal? weight = null) => weight is null ? new { reps } : new { reps, weight };

    protected static object Timed(int durationSeconds, decimal? weight = null) => weight is null ? new { durationSeconds } : new { durationSeconds, weight };

    /// <summary>
    /// Creates a workout for the owner, fills it with the entries and brings it to the given status (<c>DRAFT</c>,
    /// <c>STARTED</c>, <c>COMPLETED</c> or <c>REVIEWED</c>), recording the sets while it is started: as the owner when
    /// the owner is the athlete, as an admin otherwise.
    /// </summary>
    protected async Task<Guid> GivenWorkout(string date, string status, Guid ownerId, Guid? trainerId, params Entry[] entries)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var created = await admin.PostAsJsonAsync("/workouts", new { userId = ownerId, trainerId, name = "Session", workoutDate = date });
        created.StatusCode.Should().Be(HttpStatusCode.Created, "the workout must be arranged first");
        var workoutId = (await created.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("id").GetGuid();

        foreach (var entry in entries)
        {
            var added = await admin.PostAsJsonAsync(ExercisesRoute(workoutId), Body(entry.ExerciseId, "MID", entry.Sets));
            added.IsSuccessStatusCode.Should().BeTrue("the exercise must be arranged first");
        }

        if (status == "DRAFT")
            return workoutId;

        await GivenWorkoutStatus(workoutId, "STARTED");
        // Only an admin or the athlete who owns the workout may record sets; a trainer never may, even on their own workout
        var recorder = await AuthenticatedClientFor(ownerId == User ? SeedCredentials.User : SeedCredentials.Admin);

        foreach (var entry in entries)
        {
            foreach (var record in entry.Records)
            {
                var recorded = await recorder.PostAsJsonAsync($"/workouts/{workoutId}/exercises/{entry.ExerciseId}/set-records", record);
                recorded.StatusCode.Should().Be(HttpStatusCode.OK, "the set record must be arranged first");
            }
        }

        if (status == "COMPLETED")
            await GivenWorkoutStatus(workoutId, "COMPLETED");

        if (status == "REVIEWED")
            await GivenReviewed(workoutId);

        return workoutId;
    }
}
