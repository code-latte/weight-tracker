using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

public class WorkoutResponseShapeTests : IntegrationTest
{
    public WorkoutResponseShapeTests(ApiFactory factory) : base(factory)
    { }

    private async Task<JsonElement> WorkoutsSeenByAdmin()
    {
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        var response = await client.GetAsync("/workouts");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
    }

    private static JsonElement WorkoutWithId(JsonElement workouts, Guid id)
        => workouts.EnumerateArray().Single(workout => workout.GetProperty("id").GetGuid() == id);

    [Fact]
    public async Task Workout_ShouldCarryOwnershipAndDate()
    {
        // Arrange & Act
        var workout = WorkoutWithId(await WorkoutsSeenByAdmin(), SeedData.TrainedWorkoutId);

        // Assert
        workout.GetProperty("workoutDate").GetString().Should().Be($"{SeedData.TrainedWorkoutDate:yyyy-MM-dd}");
        workout.GetProperty("userId").GetGuid().Should().Be(SeedData.UserId);
        workout.GetProperty("trainerId").GetGuid().Should().Be(SeedData.TrainerId);
    }

    [Fact]
    public async Task Workout_ShouldCarryItsNameAndDescription()
    {
        // Arrange & Act
        var workout = WorkoutWithId(await WorkoutsSeenByAdmin(), SeedData.OwnedWorkoutId);

        // Assert
        workout.GetProperty("name").GetString().Should().Be(SeedData.OwnedWorkoutName);
        workout.GetProperty("description").GetString().Should().Be(SeedData.OwnedWorkoutDescription);
    }

    [Fact]
    public async Task WorkoutWithoutDescription_ShouldKeepAnExplicitNullDescription()
    {
        // Arrange & Act
        var workout = WorkoutWithId(await WorkoutsSeenByAdmin(), SeedData.TrainedWorkoutId);

        // Assert
        workout.GetProperty("name").GetString().Should().Be(SeedData.TrainedWorkoutName);
        workout.TryGetProperty("description", out var description).Should().BeTrue();
        description.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task WorkoutWithoutTrainer_ShouldKeepAnExplicitNullTrainerId()
    {
        // Arrange & Act
        var workout = WorkoutWithId(await WorkoutsSeenByAdmin(), SeedData.OwnedWorkoutId);

        // Assert
        workout.TryGetProperty("trainerId", out var trainerId).Should().BeTrue();
        trainerId.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Workout_ShouldNestItsExercisesAndSets()
    {
        // Arrange
        var workout = WorkoutWithId(await WorkoutsSeenByAdmin(), SeedData.OwnedWorkoutId);

        var exercise = workout.GetProperty("exercises").EnumerateArray().Single();

        exercise.GetProperty("exerciseId").GetGuid().Should().Be(SeedData.PressBancaId);

        // Act
        var set = exercise.GetProperty("sets").EnumerateArray().Single();

        // Assert
        set.GetProperty("count").GetInt32().Should().Be(4);
        set.GetProperty("target").GetProperty("type").GetString().Should().Be("Reps");
        set.GetProperty("target").GetProperty("value").GetInt32().Should().Be(10);
        exercise.GetProperty("effort").GetString().Should().Be("HIGH");
    }

    [Fact]
    public async Task Exercise_ShouldCarryItsTranslationsMuscleBodyPartAndTypeButNoName()
    {
        // Arrange
        var workouts = await WorkoutsSeenByAdmin();

        // Act
        var exercise = WorkoutWithId(workouts, SeedData.OwnedWorkoutId).GetProperty("exercises").EnumerateArray().Single();

        // Assert
        exercise.GetProperty("translations").EnumerateArray()
            .Select(translation => (translation.GetProperty("languageCode").GetString(), translation.GetProperty("name").GetString()))
            .Should().BeEquivalentTo([("es", "Press de banca"), ("en", "Barbell Bench Press - Medium Grip")]);
        exercise.GetProperty("muscle").GetString().Should().Be("CHEST");
        exercise.GetProperty("bodyPart").GetString().Should().Be("CHEST");
        exercise.GetProperty("type").GetString().Should().Be("STRENGTH");
        exercise.TryGetProperty("name", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000021", "01a17aa9-4603-7c62-9606-711a1a1673d9", "HIGH")]
    [InlineData("00000000-0000-0000-0000-000000000022", "01a17aa9-463f-77bd-8839-3304b5121cbd", "MIDHIGH")]
    [InlineData("00000000-0000-0000-0000-000000000022", "01a17aa9-4bd9-7eb1-8ca9-67f1babd1a1e", "LOW")]
    [InlineData("00000000-0000-0000-0000-000000000023", "01a17aa9-4603-7c62-9606-711a1a1673d9", "MID")]
    public async Task Exercise_ShouldCarryItsEffort(string workoutId, string exerciseId, string effort)
    {
        // Arrange
        var workouts = await WorkoutsSeenByAdmin();

        // Act
        var exercise = WorkoutWithId(workouts, Guid.Parse(workoutId))
            .GetProperty("exercises").EnumerateArray()
            .Single(item => item.GetProperty("exerciseId").GetGuid() == Guid.Parse(exerciseId));

        // Assert
        exercise.GetProperty("effort").GetString().Should().Be(effort);
    }

    [Fact]
    public async Task WorkoutWithSeveralExercises_ShouldKeepTheirOrder()
    {
        // Arrange & Act
        var workout = WorkoutWithId(await WorkoutsSeenByAdmin(), SeedData.TrainedWorkoutId);

        // Assert
        workout.GetProperty("exercises").EnumerateArray()
            .Select(exercise => exercise.GetProperty("exerciseId").GetGuid())
            .Should().Equal(SeedData.SentadillaId, SeedData.PlanchaId);
    }

    [Fact]
    public async Task DurationTarget_ShouldCarryItsValueInSeconds()
    {
        // Arrange
        var workout = WorkoutWithId(await WorkoutsSeenByAdmin(), SeedData.TrainedWorkoutId);

        var plancha = workout.GetProperty("exercises").EnumerateArray()
            .Single(exercise => exercise.GetProperty("exerciseId").GetGuid() == SeedData.PlanchaId);

        // Act
        var target = plancha.GetProperty("sets").EnumerateArray().Single().GetProperty("target");

        // Assert
        target.GetProperty("type").GetString().Should().Be("Duration");
        target.GetProperty("value").GetInt32().Should().Be(40);
    }

    [Fact]
    public async Task MaxRepsTarget_ShouldBeSerializedWithoutAValue()
    {
        // Arrange & Act
        var workout = WorkoutWithId(await WorkoutsSeenByAdmin(), SeedData.TrainerOwnWorkoutId);

        // Assert
        var target = workout.GetProperty("exercises").EnumerateArray().Single()
            .GetProperty("sets").EnumerateArray().Single()
            .GetProperty("target");

        target.GetProperty("type").GetString().Should().Be("MaxReps");
        target.TryGetProperty("value", out _).Should().BeFalse();
    }

    [Fact]
    public async Task NonRangeTargets_ShouldBeSerializedWithoutAMaxValue()
    {
        // Arrange
        var workouts = await WorkoutsSeenByAdmin();

        // Act
        var targets = workouts.EnumerateArray()
            .SelectMany(workout => workout.GetProperty("exercises").EnumerateArray())
            .SelectMany(exercise => exercise.GetProperty("sets").EnumerateArray())
            .Select(set => set.GetProperty("target"))
            .ToList();

        // Assert
        targets.Select(target => target.GetProperty("type").GetString())
            .Should().Contain(["Reps", "Duration", "MaxReps"]).And.NotContain("RepsRange");
        targets.Where(target => target.TryGetProperty("maxValue", out _)).Should().BeEmpty();
    }

    [Fact]
    public async Task WorkoutWithSeveralExercises_ShouldReturnThemAll()
    {
        // Arrange & Act
        var workout = WorkoutWithId(await WorkoutsSeenByAdmin(), SeedData.TrainedWorkoutId);

        // Assert
        workout.GetProperty("exercises").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task UngroupedExercise_ShouldCarryItsEntryIdAndAnExplicitNullSuperSeriesId()
    {
        // Arrange & Act
        var workout = WorkoutWithId(await WorkoutsSeenByAdmin(), SeedData.OwnedWorkoutId);

        // Assert
        var exercise = workout.GetProperty("exercises").EnumerateArray().Single();
        exercise.GetProperty("id").GetGuid().Should().Be(SeedData.OwnedPressBancaEntryId);
        exercise.TryGetProperty("superSeriesId", out var superSeriesId).Should().BeTrue();
        superSeriesId.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task GroupedExercises_ShouldCarryTheirEntryIdAndSharedSuperSeriesId()
    {
        // Arrange & Act
        var workout = WorkoutWithId(await WorkoutsSeenByAdmin(), SeedData.PrescribedSuperSeriesWorkoutId);

        // Assert
        var superSeriesByEntry = workout.GetProperty("exercises").EnumerateArray().ToDictionary(
            exercise => exercise.GetProperty("id").GetGuid(),
            exercise => exercise.GetProperty("superSeriesId"));

        superSeriesByEntry[SeedData.PrescribedSuperSeriesPressBancaEntryId].GetInt32().Should().Be(SeedData.SeededSuperSeries);
        superSeriesByEntry[SeedData.PrescribedSuperSeriesSentadillaEntryId].GetInt32().Should().Be(SeedData.SeededSuperSeries);
        superSeriesByEntry[SeedData.PrescribedSuperSeriesPlanchaEntryId].ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Response_ShouldNeverLeakCredentialFields()
    {
        // Arrange & Act
        var raw = (await WorkoutsSeenByAdmin()).GetRawText();

        // Assert
        raw.Should().NotContain("passwordHash");
        raw.Should().NotContain("tokenHash");
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000021", "DRAFT")]
    [InlineData("00000000-0000-0000-0000-000000000022", "PUBLISHED")]
    [InlineData("00000000-0000-0000-0000-000000000028", "DRAFT")]
    [InlineData("00000000-0000-0000-0000-000000000029", "STARTED")]
    public async Task Workout_ShouldCarryItsStatusAsTheEnumName(string workoutId, string status)
    {
        // Arrange
        var workouts = await WorkoutsSeenByAdmin();

        // Act
        var workout = WorkoutWithId(workouts, Guid.Parse(workoutId));

        // Assert
        workout.GetProperty("status").GetString().Should().Be(status);
    }

    [Fact]
    public async Task Entry_ShouldCarryPerSideAndRestInTheListAndTheDetailWithAMinutesTarget()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var body = new
        {
            exerciseId = SeedData.CurlBicepsId,
            effort = "MID",
            perSide = true,
            restMinutes = 2m,
            sets = new object[] { new { count = 1, targetType = "Minutes", targetMinutes = 30m } },
        };
        (await user.PostAsJsonAsync($"/workouts/{SeedData.OwnedWorkoutId}/exercises", body)).EnsureSuccessStatusCode();

        // Act
        var inList = WorkoutWithId(await WorkoutsSeenByAdmin(), SeedData.OwnedWorkoutId).GetProperty("exercises").EnumerateArray().Last();
        var inDetail = (await user.GetFromJsonAsync<JsonElement>($"/workouts/{SeedData.OwnedWorkoutId}", JsonOptions)).GetProperty("exercises").EnumerateArray().Last();

        // Assert
        foreach (var entry in new[] { inList, inDetail })
        {
            entry.GetProperty("perSide").GetBoolean().Should().BeTrue();
            entry.GetProperty("restMinutes").GetDecimal().Should().Be(2m);
            var target = entry.GetProperty("sets")[0].GetProperty("target");
            target.GetProperty("type").GetString().Should().Be("Minutes");
            target.GetProperty("minutes").GetDecimal().Should().Be(30m);
            target.TryGetProperty("value", out _).Should().BeFalse();
            target.TryGetProperty("maxValue", out _).Should().BeFalse();
        }
    }

    [Fact]
    public async Task Entry_ShouldCarryPerSideFalseAndANullRestWhenNoneIsPrescribed()
    {
        // Arrange & Act
        var entry = WorkoutWithId(await WorkoutsSeenByAdmin(), SeedData.OwnedWorkoutId).GetProperty("exercises").EnumerateArray().First();

        // Assert
        entry.GetProperty("perSide").GetBoolean().Should().BeFalse();
        entry.TryGetProperty("restMinutes", out var rest).Should().BeTrue();
        rest.ValueKind.Should().Be(JsonValueKind.Null);
    }

    private static readonly string[] PublicProfileProperties = ["id", "email", "name", "surname", "role"];

    [Fact]
    public async Task PrescribedWorkout_ShouldCarryThePublicProfilesOfItsAthleteAndTrainer()
    {
        // Arrange & Act
        var workout = WorkoutWithId(await WorkoutsSeenByAdmin(), SeedData.TrainedWorkoutId);

        // Assert
        var athlete = workout.GetProperty("athlete");
        var trainer = workout.GetProperty("trainer");
        athlete.GetProperty("id").GetGuid().Should().Be(workout.GetProperty("userId").GetGuid());
        athlete.GetProperty("email").GetString().Should().Be(SeedData.UserEmail);
        athlete.GetProperty("role").GetString().Should().Be("USER");
        trainer.GetProperty("id").GetGuid().Should().Be(workout.GetProperty("trainerId").GetGuid());
        trainer.GetProperty("email").GetString().Should().Be(SeedData.TrainerEmail);
        trainer.GetProperty("role").GetString().Should().Be("TRAINER");
        athlete.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(PublicProfileProperties);
        trainer.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(PublicProfileProperties);
    }

    [Fact]
    public async Task SelfPlannedWorkout_ShouldCarryItsAthleteAndANullTrainer()
    {
        // Arrange & Act
        var workout = WorkoutWithId(await WorkoutsSeenByAdmin(), SeedData.OwnedWorkoutId);

        // Assert
        workout.GetProperty("athlete").GetProperty("id").GetGuid().Should().Be(SeedData.UserId);
        workout.TryGetProperty("trainer", out var trainer).Should().BeTrue();
        trainer.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task CreatedWorkout_ShouldNotCarryAthleteOrTrainer()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.PostAsJsonAsync("/workouts", new { name = "Leg day", workoutDate = "2026-11-10" });

        // Assert
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        created.TryGetProperty("athlete", out _).Should().BeFalse();
        created.TryGetProperty("trainer", out _).Should().BeFalse();
    }

    private async Task<JsonElement> WorkoutReadByAdmin(Guid workoutId)
    {
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        var response = await client.GetAsync($"/workouts/{workoutId}");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
    }

    [Fact]
    public async Task PlacedWorkout_ShouldCarryItsMesocycleAndWeekWhenReadById()
    {
        // Arrange & Act
        var workout = await WorkoutReadByAdmin(SeedData.TrainedWorkoutId);

        // Assert
        workout.GetProperty("mesocycleId").GetGuid().Should().Be(SeedData.MesocycleId);
        workout.GetProperty("mesocycleWeekNumber").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task StandaloneWorkout_ShouldKeepAnExplicitNullPlacementWhenReadById()
    {
        // Arrange & Act
        var workout = await WorkoutReadByAdmin(SeedData.OwnedWorkoutId);

        // Assert
        workout.TryGetProperty("mesocycleId", out var mesocycleId).Should().BeTrue();
        mesocycleId.ValueKind.Should().Be(JsonValueKind.Null);
        workout.TryGetProperty("mesocycleWeekNumber", out var weekNumber).Should().BeTrue();
        weekNumber.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task ListedWorkout_ShouldNotCarryThePlacement()
    {
        // Arrange & Act
        var workout = WorkoutWithId(await WorkoutsSeenByAdmin(), SeedData.TrainedWorkoutId);

        // Assert
        workout.TryGetProperty("mesocycleId", out _).Should().BeFalse();
        workout.TryGetProperty("mesocycleWeekNumber", out _).Should().BeFalse();
    }
}
