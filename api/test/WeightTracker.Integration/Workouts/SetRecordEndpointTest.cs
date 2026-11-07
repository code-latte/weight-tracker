using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Shared plumbing of the tests of the set record routes. The seeded prescribed started workout belongs to the user,
/// is prescribed by the trainer and holds Press Banca at 3 x 10 reps. Set records are arranged through the API as the
/// user and read back as an admin, so what a rejected call leaves behind is checked independently of the caller.
/// </summary>
public abstract class SetRecordEndpointTest : WorkoutEndpointTest
{
    protected SetRecordEndpointTest(ApiFactory factory) : base(factory)
    { }

    protected static readonly Guid StartedWorkoutId = SeedData.PrescribedStartedWorkoutId;

    protected static readonly Guid StartedExerciseId = SeedData.PressBancaId;

    protected static string SetRecordsRoute(Guid workoutId, Guid exerciseId)
        => $"/workouts/{workoutId}/exercises/{exerciseId}/set-records";

    protected static string SetRecordRoute(Guid workoutId, Guid exerciseId, Guid setRecordId)
        => $"{SetRecordsRoute(workoutId, exerciseId)}/{setRecordId}";

    protected static string SetRecordOrderRoute(Guid workoutId, Guid exerciseId)
        => $"{SetRecordsRoute(workoutId, exerciseId)}/order";

    protected static object SetRecordBody(int reps, decimal weight) => new { reps, weight };

    protected static object Order(params Guid[] setRecordIds) => new { setRecordIds };

    protected sealed record StoredSetRecord(Guid Id, int Reps, decimal Weight, DateTime RegisteredAtUtc);

    /// <summary>Records the given sets on the started workout as its athlete and returns their ids in order.</summary>
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
                record.GetProperty("reps").GetInt32(),
                record.GetProperty("weight").GetDecimal(),
                record.GetProperty("registeredAtUtc").GetDateTime()))
            .ToList();
}
