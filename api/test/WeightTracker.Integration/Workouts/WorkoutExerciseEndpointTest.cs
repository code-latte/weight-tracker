using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Persistence;
using WeightTracker.Integration.Bootstrap;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Shared plumbing of the tests of the workout exercise routes. Reads go through the API as an admin;
/// only the set rows, which no response exposes, are read from the database.
/// </summary>
public abstract class WorkoutExerciseEndpointTest : WorkoutEndpointTest
{
    protected WorkoutExerciseEndpointTest(ApiFactory factory) : base(factory)
    { }

    protected static string ExercisesRoute(Guid workoutId) => $"/workouts/{workoutId}/exercises";

    protected static string ExerciseRoute(Guid workoutId, Guid exerciseId) => $"/workouts/{workoutId}/exercises/{exerciseId}";

    protected static object Body(Guid exerciseId, object effort, params object[] sets)
        => new { exerciseId, effort, sets };

    /// <summary>A body that also carries the per-side flag and the rest. Either is left out when null.</summary>
    protected static object BodyWith(Guid exerciseId, object effort, bool? perSide, decimal? restMinutes, params object[] sets)
        => new { exerciseId, effort, sets, perSide, restMinutes };

    protected static object MinutesSet(int count, decimal? targetMinutes)
        => new { count, targetType = "Minutes", targetMinutes };

    protected static object Sets(int count, string targetType, int? target = null)
        => new { count, targetType, target };

    protected static object Reps(int count, int reps) => Sets(count, "Reps", reps);

    protected static object RepsRange(int count, int? min, int? max)
        => new { count, targetType = "RepsRange", target = min, maxTarget = max };

    /// <summary>The workout as an admin reads it, so what a rejected call leaves behind is checked independently of the caller.</summary>
    protected async Task<JsonElement> WorkoutAsAdmin(Guid workoutId)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        return await admin.GetFromJsonAsync<JsonElement>($"/workouts/{workoutId}", JsonOptions);
    }

    protected async Task<IReadOnlyList<JsonElement>> EntriesOf(Guid workoutId)
        => (await WorkoutAsAdmin(workoutId)).GetProperty("exercises").EnumerateArray().ToList();

    protected static IReadOnlyList<Guid> ExerciseIds(IEnumerable<JsonElement> entries)
        => entries.Select(entry => entry.GetProperty("exerciseId").GetGuid()).ToList();

    protected static IReadOnlyList<(int Count, string Type, int? Value)> SetsOf(JsonElement entry)
        => entry.GetProperty("sets").EnumerateArray()
            .Select(set =>
            {
                var target = set.GetProperty("target");
                return (set.GetProperty("count").GetInt32(),
                    target.GetProperty("type").GetString()!,
                    target.TryGetProperty("value", out var value) ? value.GetInt32() : (int?)null);
            })
            .ToList();

    /// <summary>The <c>maxValue</c> of every set target of one entry, null where it is omitted.</summary>
    protected static IReadOnlyList<int?> MaxValuesOf(JsonElement entry)
        => entry.GetProperty("sets").EnumerateArray()
            .Select(set => set.GetProperty("target").TryGetProperty("maxValue", out var maxValue) ? maxValue.GetInt32() : (int?)null)
            .ToList();

    /// <summary>The <c>minutes</c> of every set target of one entry, null where it is omitted.</summary>
    protected static IReadOnlyList<decimal?> MinutesOf(JsonElement entry)
        => entry.GetProperty("sets").EnumerateArray()
            .Select(set => set.GetProperty("target").TryGetProperty("minutes", out var minutes) ? minutes.GetDecimal() : (decimal?)null)
            .ToList();

    /// <summary>Ids of the stored set rows of one workout entry, in position order.</summary>
    protected Task<IReadOnlyList<Guid>> StoredSetRowIds(Guid workoutExerciseId)
        => Factory.WithScope<IReadOnlyList<Guid>>(async services =>
        {
            var context = services.GetRequiredService<WeightTrackerDbContext>();

            return await context.WorkoutExerciseSets.AsNoTracking()
                .Where(set => set.WorkoutExerciseId == workoutExerciseId)
                .OrderBy(set => set.Position)
                .Select(set => set.Id)
                .ToListAsync();
        });

    protected Task<int> StoredEntryCount(Guid workoutExerciseId)
        => Factory.WithScope(services => services.GetRequiredService<WeightTrackerDbContext>()
            .WorkoutExercises.AsNoTracking().CountAsync(entry => entry.Id == workoutExerciseId));
}
