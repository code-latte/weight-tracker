using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Covers <c>GET /workouts/{workoutId}/summary</c>: the auth matrix, the visibility rule, the computed stats and the
/// previous execution. Workouts are arranged through the API with <see cref="WorkoutStatsEndpointTest.GivenWorkout"/>.
/// </summary>
public class WorkoutSummaryEndpointTests : WorkoutStatsEndpointTest
{
    public WorkoutSummaryEndpointTests(ApiFactory factory) : base(factory)
    { }

    private async Task<HttpResponseMessage> Get(string email, Guid workoutId)
    {
        var client = await AuthenticatedClientFor(email);

        return await client.GetAsync($"/workouts/{workoutId}/summary");
    }

    private async Task<JsonElement> SummaryOf(string email, Guid workoutId)
    {
        var response = await Get(email, workoutId);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
    }

    private static JsonElement ExerciseOf(JsonElement summary, Guid exerciseId)
        => summary.GetProperty("exercises").EnumerateArray().Single(entry => entry.GetProperty("exerciseId").GetGuid() == exerciseId);

    private static (int InTarget, int Evaluated) SetsInTarget(JsonElement element)
        => (element.GetProperty("setsInTarget").GetProperty("inTarget").GetInt32(), element.GetProperty("setsInTarget").GetProperty("evaluated").GetInt32());

    private static decimal? Decimal(JsonElement element, string name)
        => element.GetProperty(name) is { ValueKind: not JsonValueKind.Null } value ? value.GetDecimal() : null;

    private static (int? Reps, int? Seconds, decimal? Weight)? SetOf(JsonElement element, string name)
    {
        var set = element.GetProperty(name);

        if (set.ValueKind == JsonValueKind.Null)
            return null;

        return (
            set.GetProperty("reps") is { ValueKind: not JsonValueKind.Null } reps ? reps.GetInt32() : null,
            set.GetProperty("durationSeconds") is { ValueKind: not JsonValueKind.Null } seconds ? seconds.GetInt32() : null,
            Decimal(set, "weight"));
    }

    private static (int? Reps, int? Seconds, decimal? Weight)? Expected(int? reps, int? seconds, decimal? weight) => (reps, seconds, weight);

    [Fact]
    public async Task Get_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await Client.GetAsync($"/workouts/{SeedData.OwnedWorkoutId}/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.GetAsync($"/workouts/{SeedData.OwnedWorkoutId}/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFoundForAnUnknownWorkout()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.Admin, Guid.NewGuid());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFoundForAMalformedWorkoutId()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.GetAsync("/workouts/not-a-guid/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ShouldRefuseAUserTheWorkoutOfAnotherUser()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.User, SeedData.TrainerOwnWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_ShouldRefuseATrainerAWorkoutThatIsNeitherTheirsNorPrescribedByThem()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.Trainer, SeedData.OwnedWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_ShouldRefuseAnAthleteAPrescribedDraftTheirTrainerHasNotPublished()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.User, SeedData.PrescribedDraftWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_ShouldAllowTheOwner()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.User, SeedData.OwnedWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_ShouldAllowTheTrainerWhoPrescribedTheWorkout()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.Trainer, SeedData.TrainedWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_ShouldAllowAnAdmin()
    {
        // Arrange & Act
        var response = await Get(SeedCredentials.Admin, SeedData.OwnedWorkoutId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_ShouldCountTheSetsInTargetOfEachTargetKind()
    {
        // Arrange
        var workoutId = await GivenWorkout("2026-11-10", "COMPLETED", User, null,
            new Entry(Press, [Reps(1, 10), RepsRange(1, 8, 12), Sets(1, "MaxReps")], [Rec(9, 40m), Rec(14, 40m), Rec(3, 40m)]));

        // Act
        var summary = await SummaryOf(SeedCredentials.User, workoutId);

        // Assert
        summary.GetProperty("workoutId").GetGuid().Should().Be(workoutId);
        SetsInTarget(summary).Should().Be((2, 3));
        SetsInTarget(ExerciseOf(summary, Press)).Should().Be((2, 3));
    }

    [Fact]
    public async Task Get_ShouldCountATimedSetInTarget()
    {
        // Arrange
        var workoutId = await GivenWorkout("2026-11-10", "COMPLETED", User, null,
            new Entry(Plank, [Sets(2, "Duration", 45)], [Timed(44), Timed(50, 10m)]));

        // Act
        var summary = await SummaryOf(SeedCredentials.User, workoutId);

        // Assert
        SetsInTarget(ExerciseOf(summary, Plank)).Should().Be((1, 2));
        Decimal(ExerciseOf(summary, Plank), "volume").Should().BeNull();
    }

    [Fact]
    public async Task Get_ShouldSumTheVolumeOfEachExerciseAndOfTheWorkout()
    {
        // Arrange
        var workoutId = await GivenWorkout("2026-11-10", "COMPLETED", User, null,
            new Entry(Press, [Reps(2, 10)], [Rec(10, 40m), Rec(8, 50m)]),
            new Entry(Squat, [Reps(1, 5)], [Rec(5, 100m)]));

        // Act
        var summary = await SummaryOf(SeedCredentials.User, workoutId);

        // Assert
        Decimal(ExerciseOf(summary, Press), "volume").Should().Be(800m);
        Decimal(ExerciseOf(summary, Squat), "volume").Should().Be(500m);
        Decimal(summary, "volume").Should().Be(1300m);
    }

    [Fact]
    public async Task Get_ShouldReturnANullVolumeWithoutWeightAndAZeroVolumeForABodyweightSet()
    {
        // Arrange
        var workoutId = await GivenWorkout("2026-11-10", "COMPLETED", User, null,
            new Entry(Press, [Reps(1, 12)], [Rec(12, 0m)]),
            new Entry(Squat, [Reps(1, 10)], [Rec(10)]));

        // Act
        var summary = await SummaryOf(SeedCredentials.User, workoutId);

        // Assert
        Decimal(ExerciseOf(summary, Press), "volume").Should().Be(0m);
        Decimal(ExerciseOf(summary, Squat), "volume").Should().BeNull();
        Decimal(summary, "volume").Should().Be(0m);
    }

    [Fact]
    public async Task Get_ShouldReturnTheHeaviestSetAsTheTopSet()
    {
        // Arrange
        var workoutId = await GivenWorkout("2026-11-10", "COMPLETED", User, null,
            new Entry(Press, [Reps(3, 10)], [Rec(10, 40m), Rec(6, 50m), Rec(8, 45m)]));

        // Act
        var summary = await SummaryOf(SeedCredentials.User, workoutId);

        // Assert
        SetOf(ExerciseOf(summary, Press), "topSet").Should().Be(Expected(6, null, 50m));
    }

    [Fact]
    public async Task Get_ShouldSummariseAWorkoutInProgressWithoutRecordsAsEmpty()
    {
        // Arrange
        var workoutId = await GivenWorkout("2026-11-10", "STARTED", User, null,
            new Entry(Press, [Reps(2, 10)], [Rec(10, 40m)]),
            new Entry(Squat, [Reps(2, 10)], []));

        // Act
        var summary = await SummaryOf(SeedCredentials.User, workoutId);

        // Assert
        SetsInTarget(ExerciseOf(summary, Press)).Should().Be((1, 1));
        var untouched = ExerciseOf(summary, Squat);
        SetsInTarget(untouched).Should().Be((0, 0));
        Decimal(untouched, "volume").Should().BeNull();
        SetOf(untouched, "topSet").Should().BeNull();
        SetsInTarget(summary).Should().Be((1, 1));
    }

    [Fact]
    public async Task Get_ShouldReturnTheExercisesInWorkoutOrder()
    {
        // Arrange
        var workoutId = await GivenWorkout("2026-11-10", "DRAFT", User, null,
            new Entry(Squat, [Reps(1, 10)], []),
            new Entry(Press, [Reps(1, 10)], []),
            new Entry(Plank, [Sets(1, "Duration", 30)], []));

        // Act
        var summary = await SummaryOf(SeedCredentials.User, workoutId);

        // Assert
        summary.GetProperty("exercises").EnumerateArray().Select(entry => entry.GetProperty("exerciseId").GetGuid())
            .Should().Equal(Squat, Press, Plank);
    }

    [Fact]
    public async Task Get_ShouldReturnTheLatestEarlierFinishedWorkoutAsThePreviousExecution()
    {
        // Arrange
        await GivenWorkout("2026-11-03", "COMPLETED", User, null, new Entry(Press, [Reps(1, 5)], [Rec(5, 50m)]));
        var latestEarlier = await GivenWorkout("2026-11-06", "COMPLETED", User, null, new Entry(Press, [Reps(2, 8)], [Rec(8, 60m), Rec(6, 62.5m)]));
        var current = await GivenWorkout("2026-11-10", "COMPLETED", User, null, new Entry(Press, [Reps(1, 10)], [Rec(10, 70m)]));

        // Act
        var summary = await SummaryOf(SeedCredentials.User, current);

        // Assert
        var previous = ExerciseOf(summary, Press).GetProperty("previous");
        previous.GetProperty("workoutId").GetGuid().Should().Be(latestEarlier);
        previous.GetProperty("workoutDate").GetString().Should().Be("2026-11-06");
        SetOf(previous, "topSet").Should().Be(Expected(6, null, 62.5m));
    }

    [Fact]
    public async Task Get_ShouldCountAReviewedWorkoutAsThePreviousExecution()
    {
        // Arrange
        var reviewed = await GivenWorkout("2026-11-06", "REVIEWED", User, Trainer, new Entry(Press, [Reps(1, 8)], [Rec(8, 60m)]));
        var current = await GivenWorkout("2026-11-10", "COMPLETED", User, null, new Entry(Press, [Reps(1, 10)], [Rec(10, 70m)]));

        // Act
        var summary = await SummaryOf(SeedCredentials.User, current);

        // Assert
        ExerciseOf(summary, Press).GetProperty("previous").GetProperty("workoutId").GetGuid().Should().Be(reviewed);
    }

    [Fact]
    public async Task Get_ShouldIgnoreLaterUnfinishedAndOtherUsersWorkoutsAsPreviousExecutions()
    {
        // Arrange
        var earlier = await GivenWorkout("2026-11-03", "COMPLETED", User, null, new Entry(Press, [Reps(1, 5)], [Rec(5, 50m)]));
        await GivenWorkout("2026-11-12", "COMPLETED", User, null, new Entry(Press, [Reps(1, 12)], [Rec(12, 90m)]));
        await GivenWorkout("2026-11-08", "STARTED", User, null, new Entry(Press, [Reps(1, 10)], [Rec(10, 80m)]));
        await GivenWorkout("2026-11-09", "COMPLETED", Trainer, null, new Entry(Press, [Reps(1, 15)], [Rec(15, 100m)]));
        var current = await GivenWorkout("2026-11-10", "COMPLETED", User, null, new Entry(Press, [Reps(1, 10)], [Rec(10, 70m)]));

        // Act
        var summary = await SummaryOf(SeedCredentials.User, current);

        // Assert
        ExerciseOf(summary, Press).GetProperty("previous").GetProperty("workoutId").GetGuid().Should().Be(earlier);
    }

    [Fact]
    public async Task Get_ShouldReturnNullAsThePreviousExecutionOfAnExerciseNeverDoneBefore()
    {
        // Arrange
        await GivenWorkout("2026-11-03", "COMPLETED", User, null, new Entry(Squat, [Reps(1, 5)], [Rec(5, 100m)]));
        var current = await GivenWorkout("2026-11-10", "COMPLETED", User, null, new Entry(Press, [Reps(1, 10)], [Rec(10, 70m)]));

        // Act
        var summary = await SummaryOf(SeedCredentials.User, current);

        // Assert
        ExerciseOf(summary, Press).GetProperty("previous").ValueKind.Should().Be(JsonValueKind.Null);
    }

    /// <summary>
    /// A workout the trainer prescribed to the user, an earlier one the trainer prescribed too, and a later earlier
    /// one the user created on their own. Returns the ids of the current workout, the prescribed one and the own one.
    /// </summary>
    private async Task<(Guid Current, Guid Prescribed, Guid Own)> GivenPrescribedAndOwnSessions()
    {
        var prescribed = await GivenWorkout("2026-11-03", "COMPLETED", User, Trainer, new Entry(Press, [Reps(1, 5)], [Rec(5, 50m)]));
        var own = await GivenWorkout("2026-11-08", "COMPLETED", User, null, new Entry(Press, [Reps(1, 9)], [Rec(9, 70m)]));
        var current = await GivenWorkout("2026-11-10", "COMPLETED", User, Trainer, new Entry(Press, [Reps(1, 10)], [Rec(10, 70m)]));

        return (current, prescribed, own);
    }

    [Fact]
    public async Task Get_ShouldGiveATrainerOnlyThePreviousExecutionsTheyPrescribed()
    {
        // Arrange
        var (current, prescribed, _) = await GivenPrescribedAndOwnSessions();

        // Act
        var summary = await SummaryOf(SeedCredentials.Trainer, current);

        // Assert
        ExerciseOf(summary, Press).GetProperty("previous").GetProperty("workoutId").GetGuid().Should().Be(prescribed);
    }

    [Fact]
    public async Task Get_ShouldGiveTheAthleteTheirLatestPreviousExecutionWhoeverPrescribedIt()
    {
        // Arrange
        var (current, _, own) = await GivenPrescribedAndOwnSessions();

        // Act
        var summary = await SummaryOf(SeedCredentials.User, current);

        // Assert
        ExerciseOf(summary, Press).GetProperty("previous").GetProperty("workoutId").GetGuid().Should().Be(own);
    }

    [Fact]
    public async Task Get_ShouldGiveAnAdminTheLatestPreviousExecutionOfTheOwner()
    {
        // Arrange
        var (current, _, own) = await GivenPrescribedAndOwnSessions();

        // Act
        var summary = await SummaryOf(SeedCredentials.Admin, current);

        // Assert
        ExerciseOf(summary, Press).GetProperty("previous").GetProperty("workoutId").GetGuid().Should().Be(own);
    }

    [Fact]
    public async Task Get_ShouldReturnNullAsThePreviousExecutionForATrainerWhenOnlyTheAthletesOwnSessionsExist()
    {
        // Arrange
        await GivenWorkout("2026-11-08", "COMPLETED", User, null, new Entry(Press, [Reps(1, 9)], [Rec(9, 70m)]));
        var current = await GivenWorkout("2026-11-10", "COMPLETED", User, Trainer, new Entry(Press, [Reps(1, 10)], [Rec(10, 70m)]));

        // Act
        var summary = await SummaryOf(SeedCredentials.Trainer, current);

        // Assert
        ExerciseOf(summary, Press).GetProperty("previous").ValueKind.Should().Be(JsonValueKind.Null);
    }
}
