using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Shared plumbing of the tests of the set record routes. The seeded prescribed started workout belongs to the user,
/// is prescribed by the trainer and holds Press Banca at 3 x 10 reps. The seeded self-created pair workout also belongs
/// to the user and holds Plancha at 4 x 45 seconds, the time-based exercise. Set records are arranged through the API as
/// the user and read back as an admin, so what a rejected call leaves behind is checked independently of the caller.
/// </summary>
public abstract class SetRecordEndpointTest : WorkoutEndpointTest
{
    protected SetRecordEndpointTest(ApiFactory factory) : base(factory)
    { }

    protected static readonly Guid StartedWorkoutId = SeedData.PrescribedStartedWorkoutId;

    protected static readonly Guid StartedExerciseId = SeedData.PressBancaId;

    /// <summary>Workout holding the time-based exercise. It must be put in <c>STARTED</c> first with <see cref="WorkoutEndpointTest.GivenWorkoutStatus"/>.</summary>
    protected static readonly Guid TimedWorkoutId = SeedData.SelfCreatedPairWorkoutId;

    protected static readonly Guid TimedExerciseId = SeedData.PlanchaId;

    protected const int TimedExercisePrescribedSeconds = 45;

    protected static string SetRecordsRoute(Guid workoutId, Guid exerciseId)
        => $"/workouts/{workoutId}/exercises/{exerciseId}/set-records";

    protected static string SetRecordRoute(Guid workoutId, Guid exerciseId, Guid setRecordId)
        => $"{SetRecordsRoute(workoutId, exerciseId)}/{setRecordId}";

    protected static string SetRecordOrderRoute(Guid workoutId, Guid exerciseId)
        => $"{SetRecordsRoute(workoutId, exerciseId)}/order";

    protected static object SetRecordBody(int reps, decimal weight) => new { reps, weight };

    protected static object SetRecordBody(int reps) => new { reps };

    protected static object DurationBody(int durationSeconds) => new { durationSeconds };

    protected static object DurationBody(int durationSeconds, decimal weight) => new { durationSeconds, weight };

    protected static object Order(params Guid[] setRecordIds) => new { setRecordIds };

    protected sealed record StoredSetRecord(Guid Id, string Type, int? Reps, int? DurationSeconds, decimal? Weight, DateTime RegisteredAtUtc);

    /// <summary>Records the given rep sets on the started workout as its athlete and returns their ids in order.</summary>
    protected async Task<IReadOnlyList<Guid>> GivenRecordedSets(params (int Reps, decimal Weight)[] sets)
    {
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        foreach (var (reps, weight) in sets)
        {
            var response = await user.PostAsJsonAsync(SetRecordsRoute(StartedWorkoutId, StartedExerciseId), SetRecordBody(reps, weight));
            response.StatusCode.Should().Be(HttpStatusCode.OK, "the set record must be arranged first");
        }

        return (await SetRecordsOf(StartedWorkoutId, StartedExerciseId)).Select(record => record.Id).ToList();
    }

    /// <summary>Starts the timed workout and records the given timed sets on its time-based exercise as its athlete, returning their ids in order.</summary>
    protected async Task<IReadOnlyList<Guid>> GivenRecordedTimedSets(params (int Seconds, decimal? Weight)[] sets)
    {
        await GivenWorkoutStatus(TimedWorkoutId, "STARTED");
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        foreach (var (seconds, weight) in sets)
        {
            var body = weight is null ? DurationBody(seconds) : DurationBody(seconds, weight.Value);
            var response = await user.PostAsJsonAsync(SetRecordsRoute(TimedWorkoutId, TimedExerciseId), body);
            response.StatusCode.Should().Be(HttpStatusCode.OK, "the timed set record must be arranged first");
        }

        return (await SetRecordsOf(TimedWorkoutId, TimedExerciseId)).Select(record => record.Id).ToList();
    }

    /// <summary>The set records of one exercise as an admin reads them, in order.</summary>
    protected async Task<IReadOnlyList<StoredSetRecord>> SetRecordsOf(Guid workoutId, Guid exerciseId)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var workout = await admin.GetFromJsonAsync<JsonElement>($"/workouts/{workoutId}", JsonOptions);

        return SetRecordsIn(workout, exerciseId);
    }

    protected static IReadOnlyList<StoredSetRecord> SetRecordsIn(JsonElement workout, Guid exerciseId)
        => workout.GetProperty("exercises").EnumerateArray()
            .Single(entry => entry.GetProperty("exerciseId").GetGuid() == exerciseId)
            .GetProperty("setRecords").EnumerateArray()
            .Select(record => new StoredSetRecord(
                record.GetProperty("id").GetGuid(),
                record.GetProperty("type").GetString()!,
                NullableInt(record, "reps"),
                NullableInt(record, "durationSeconds"),
                NullableDecimal(record, "weight"),
                record.GetProperty("registeredAtUtc").GetDateTime()))
            .ToList();

    private static int? NullableInt(JsonElement record, string name)
        => record.GetProperty(name) is { ValueKind: not JsonValueKind.Null } value ? value.GetInt32() : null;

    private static decimal? NullableDecimal(JsonElement record, string name)
        => record.GetProperty(name) is { ValueKind: not JsonValueKind.Null } value ? value.GetDecimal() : null;
}
