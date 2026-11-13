using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Covers when observations are generated and what they say, driven only through the API: workouts are arranged with
/// <see cref="WorkoutStatsEndpointTest.GivenWorkout"/>, completed through <c>PUT /workouts/{id}/status</c> and read
/// through <c>GET /workouts/{id}/observations</c>. The rules themselves are covered by unit tests; here a few scenarios
/// prove the wiring from the completion to the stored result.
/// </summary>
public class WorkoutObservationsGenerationTests : WorkoutStatsEndpointTest
{
    public WorkoutObservationsGenerationTests(ApiFactory factory) : base(factory)
    { }

    private static readonly object[] ThreeOfTen = [Reps(3, 10)];

    private static object[] RecordsAt(decimal weight) => [Rec(10, weight), Rec(10, weight), Rec(10, weight)];

    private async Task<HttpResponseMessage> Complete(string email, Guid workoutId)
    {
        var client = await AuthenticatedClientFor(email);

        return await client.PutAsJsonAsync($"/workouts/{workoutId}/status", new { status = "COMPLETED" });
    }

    private async Task<JsonElement> ObservationsOf(Guid workoutId, string email = SeedCredentials.Admin)
    {
        var client = await AuthenticatedClientFor(email);
        var response = await client.GetAsync($"/workouts/{workoutId}/observations");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
    }

    private static IEnumerable<JsonElement> Observations(JsonElement body) => body.GetProperty("observations").EnumerateArray();

    private static string[] CodesOf(JsonElement body) => Observations(body).Select(observation => observation.GetProperty("code").GetString()!).ToArray();

    private static JsonElement ObservationWith(JsonElement body, string code, Guid? exerciseId = null)
        => Observations(body).Single(observation =>
            observation.GetProperty("code").GetString() == code
            && (exerciseId is null
                ? observation.GetProperty("exerciseId").ValueKind == JsonValueKind.Null
                : observation.GetProperty("exerciseId").GetGuid() == exerciseId));

    private async Task<Guid> GivenCompletedPrescribed(string date, Guid exerciseId, decimal top)
        => await GivenWorkout(date, "COMPLETED", User, Trainer, new Entry(exerciseId, ThreeOfTen, RecordsAt(top)));

    [Fact]
    public async Task Complete_ShouldGenerateTheObservationsWhenTheAthleteCompletesAPrescribedWorkout()
    {
        // Arrange
        var workoutId = await GivenWorkout("2026-11-05", "STARTED", User, Trainer, new Entry(Press, ThreeOfTen, RecordsAt(50m)));
        (await ObservationsOf(workoutId)).GetProperty("observations").GetArrayLength().Should().Be(0);

        // Act
        var response = await Complete(SeedCredentials.User, workoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CodesOf(await ObservationsOf(workoutId, SeedCredentials.User)).Should().Equal("ALL_TARGETS_MET");
    }

    [Fact]
    public async Task Complete_ShouldGenerateTheObservationsWhenAnAdminCompletesAPrescribedWorkout()
    {
        // Arrange
        // Act
        var workoutId = await GivenWorkout("2026-11-05", "COMPLETED", User, Trainer,
            new Entry(Press, ThreeOfTen, [Rec(10, 50m), Rec(6, 50m)]));

        // Assert
        var body = await ObservationsOf(workoutId, SeedCredentials.Trainer);
        CodesOf(body).Should().BeEquivalentTo("WORKOUT_INCOMPLETE", "REPS_BELOW_TARGET");
        var incomplete = ObservationWith(body, "WORKOUT_INCOMPLETE");
        incomplete.GetProperty("severity").GetString().Should().Be("WARNING");
        incomplete.GetProperty("data").GetProperty("recordedSets").GetDecimal().Should().Be(2);
        incomplete.GetProperty("data").GetProperty("prescribedSets").GetDecimal().Should().Be(3);
        incomplete.GetProperty("priority").GetString().Should().Be("CRITICAL");
        body.GetProperty("highlights").EnumerateArray().Should().ContainSingle()
            .Which.GetProperty("observationId").GetGuid().Should().Be(incomplete.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Complete_ShouldGenerateNothingForAWorkoutWithoutTrainer()
    {
        // Arrange
        // Act
        var workoutId = await GivenWorkout("2026-11-05", "COMPLETED", User, null, new Entry(Press, ThreeOfTen, RecordsAt(50m)));

        // Assert
        var body = await ObservationsOf(workoutId, SeedCredentials.User);
        body.GetProperty("observations").GetArrayLength().Should().Be(0);
        body.GetProperty("highlights").GetArrayLength().Should().Be(0);
    }

    [Theory]
    [InlineData("DRAFT")]
    [InlineData("STARTED")]
    public async Task Observations_ShouldNotExistBeforeTheWorkoutIsCompleted(string status)
    {
        // Arrange
        // Act
        var workoutId = await GivenWorkout("2026-11-05", status, User, Trainer, new Entry(Press, ThreeOfTen, RecordsAt(50m)));

        // Assert
        (await ObservationsOf(workoutId)).GetProperty("observations").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Observations_ShouldNotChangeWhenASetRecordIsCorrectedAfterCompletion()
    {
        // Arrange
        var workoutId = await GivenWorkout("2026-11-05", "COMPLETED", User, Trainer, new Entry(Press, ThreeOfTen, RecordsAt(50m)));
        var before = CodesOf(await ObservationsOf(workoutId));
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var detail = await admin.GetFromJsonAsync<JsonElement>($"/workouts/{workoutId}", JsonOptions);
        var recordId = (await EntriesOf(workoutId))[0].GetProperty("setRecords")[0].GetProperty("id").GetGuid();
        detail.GetProperty("status").GetString().Should().Be("COMPLETED");

        // Act
        var corrected = await admin.PutAsJsonAsync(
            $"/workouts/{workoutId}/exercises/{Press}/set-records/{recordId}", new { reps = 4, weight = 50m });

        // Assert
        corrected.StatusCode.Should().Be(HttpStatusCode.OK);
        CodesOf(await ObservationsOf(workoutId)).Should().Equal(before);
    }

    [Fact]
    public async Task Complete_ShouldReplaceTheObservationsWhenAnAdminCompletesTheWorkoutAgain()
    {
        // Arrange
        var workoutId = await GivenWorkout("2026-11-05", "COMPLETED", User, Trainer, new Entry(Press, ThreeOfTen, RecordsAt(50m)));
        var first = await ObservationsOf(workoutId);
        var firstIds = Observations(first).Select(observation => observation.GetProperty("id").GetGuid()).ToList();
        CodesOf(first).Should().Equal("ALL_TARGETS_MET");

        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        (await admin.PutAsJsonAsync($"/workouts/{workoutId}/status", new { status = "STARTED" })).StatusCode.Should().Be(HttpStatusCode.OK);
        var recordId = (await EntriesOf(workoutId))[0].GetProperty("setRecords")[0].GetProperty("id").GetGuid();
        (await admin.PutAsJsonAsync($"/workouts/{workoutId}/exercises/{Press}/set-records/{recordId}", new { reps = 4, weight = 50m }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await Complete(SeedCredentials.Admin, workoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var second = await ObservationsOf(workoutId);
        CodesOf(second).Should().Equal("REPS_BELOW_TARGET");
        Observations(second).Select(observation => observation.GetProperty("id").GetGuid()).Should().NotIntersectWith(firstIds);
    }

    [Fact]
    public async Task Complete_ShouldOnlyReportComplianceForAWorkoutPlacedInADeloadWeek()
    {
        // Arrange
        await GivenCompletedPrescribed("2026-10-05", Press, 100m);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var created = await admin.PostAsJsonAsync("/workouts", new
        {
            userId = User,
            trainerId = Trainer,
            name = "Deload day",
            workoutDate = "2026-10-21",
            mesocycleId = SeedData.MesocycleId,
            mesocycleWeekNumber = 4,
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var workoutId = (await created.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("id").GetGuid();
        (await admin.PostAsJsonAsync(ExercisesRoute(workoutId), Body(Press, "MID", ThreeOfTen))).IsSuccessStatusCode.Should().BeTrue();
        await GivenWorkoutStatus(workoutId, "STARTED");

        foreach (var record in RecordsAt(60m))
            (await admin.PostAsJsonAsync($"/workouts/{workoutId}/exercises/{Press}/set-records", record)).StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        await GivenWorkoutStatus(workoutId, "COMPLETED");

        // Assert
        CodesOf(await ObservationsOf(workoutId)).Should().Equal("ALL_TARGETS_MET");
    }

    [Fact]
    public async Task Complete_ShouldReportADowntrendAndHighlightItAfterThreeLowerSessionsInARow()
    {
        // Arrange
        await GivenCompletedPrescribed("2026-11-01", Press, 110m);
        await GivenCompletedPrescribed("2026-11-02", Press, 105m);
        await GivenCompletedPrescribed("2026-11-03", Press, 100m);

        // Act
        var workoutId = await GivenCompletedPrescribed("2026-11-04", Press, 95m);

        // Assert
        var body = await ObservationsOf(workoutId, SeedCredentials.Trainer);
        var trend = ObservationWith(body, "WEIGHT_DOWNTREND", Press);
        trend.GetProperty("data").GetProperty("previousWeightsKg").EnumerateArray().Select(weight => weight.GetDecimal())
            .Should().Equal(110m, 105m, 100m);
        trend.GetProperty("data").GetProperty("currentWeightKg").GetDecimal().Should().Be(95m);
        trend.GetProperty("data").GetProperty("consecutiveDecreases").GetDecimal().Should().Be(3);
        trend.GetProperty("priority").GetString().Should().Be("HIGH");
        body.GetProperty("highlights").EnumerateArray().Should().ContainSingle()
            .Which.GetProperty("observationId").GetGuid().Should().Be(trend.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Complete_ShouldReportAWeightRecordWithItsEvidence()
    {
        // Arrange
        await GivenCompletedPrescribed("2026-11-01", Press, 120m);
        await GivenCompletedPrescribed("2026-11-02", Press, 100m);

        // Act
        var workoutId = await GivenCompletedPrescribed("2026-11-03", Press, 130m);

        // Assert
        var record = ObservationWith(await ObservationsOf(workoutId), "WEIGHT_PERSONAL_RECORD", Press);
        record.GetProperty("severity").GetString().Should().Be("SUCCESS");
        record.GetProperty("data").GetProperty("previousMaxKg").GetDecimal().Should().Be(120m);
        record.GetProperty("data").GetProperty("currentMaxKg").GetDecimal().Should().Be(130m);
        record.GetProperty("data").GetProperty("changePercent").GetDecimal().Should().Be(8.33m);
        record.GetProperty("priority").GetString().Should().Be("MEDIUM");
    }

    [Fact]
    public async Task Complete_ShouldCompareOnlyWithEarlierSessionsOfTheSameTrainer()
    {
        // Arrange
        await GivenWorkout("2026-11-01", "COMPLETED", User, null, new Entry(Press, ThreeOfTen, RecordsAt(100m)));

        // Act
        var workoutId = await GivenCompletedPrescribed("2026-11-03", Press, 130m);

        // Assert
        CodesOf(await ObservationsOf(workoutId)).Should().Equal("ALL_TARGETS_MET");
    }

    [Fact]
    public async Task Complete_ShouldNeverCompareAWorkoutWithItself()
    {
        // Arrange
        // Act
        var workoutId = await GivenCompletedPrescribed("2026-11-03", Press, 130m);

        // Assert
        CodesOf(await ObservationsOf(workoutId)).Should().NotContain(code => code.StartsWith("WEIGHT_") || code.StartsWith("VOLUME_"));
    }

    [Fact]
    public async Task Complete_ShouldNotUseLaterOrUnfinishedSessionsAsHistory()
    {
        // Arrange
        await GivenCompletedPrescribed("2026-11-10", Press, 60m);
        await GivenWorkout("2026-11-01", "STARTED", User, Trainer, new Entry(Press, ThreeOfTen, RecordsAt(60m)));

        // Act
        var workoutId = await GivenCompletedPrescribed("2026-11-03", Press, 130m);

        // Assert
        CodesOf(await ObservationsOf(workoutId)).Should().Equal("ALL_TARGETS_MET");
    }
}
