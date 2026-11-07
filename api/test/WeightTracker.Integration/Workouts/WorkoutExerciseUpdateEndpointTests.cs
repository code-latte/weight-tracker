using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Covers <c>PUT /workouts/{workoutId}/exercises/{exerciseId}</c>, including every branch of the
/// workout edit rule reachable with the seeded users. A second trainer on a prescribed workout is covered by unit tests.
/// </summary>
public class WorkoutExerciseUpdateEndpointTests : WorkoutExerciseEndpointTest
{
    public WorkoutExerciseUpdateEndpointTests(ApiFactory factory) : base(factory)
    { }

    /// <summary>Sentadilla in the trained workout is seeded as 5 x 5 reps, MIDHIGH, in first position.</summary>
    private static object ASentadillaUpdate() => Body(SeedData.SentadillaId, "HIGH", Reps(2, 12), Sets(1, "MaxReps"));

    [Fact]
    public async Task Put_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await Client.PutAsJsonAsync(ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.SentadillaId), ASentadillaUpdate());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.PutAsJsonAsync(ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.SentadillaId), ASentadillaUpdate());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldRejectATokenWhoseUserNoLongerExists()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);
        await Factory.RemoveUser(SeedCredentials.Trainer);

        // Act
        var response = await client.PutAsJsonAsync(ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.SentadillaId), ASentadillaUpdate());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldReplaceEffortAndEverySetKeepingTheEntryIdAndPositionForThePrescribingTrainer()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "DRAFT");
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PutAsJsonAsync(ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.SentadillaId), ASentadillaUpdate());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        updated.GetProperty("id").GetGuid().Should().Be(SeedData.TrainedSentadillaEntryId);
        updated.GetProperty("effort").GetString().Should().Be("HIGH");
        var entries = await EntriesOf(SeedData.TrainedWorkoutId);
        ExerciseIds(entries).Should().Equal(SeedData.SentadillaId, SeedData.PlanchaId);
        entries[0].GetProperty("id").GetGuid().Should().Be(SeedData.TrainedSentadillaEntryId);
        entries[0].GetProperty("effort").GetString().Should().Be("HIGH");
        SetsOf(entries[0]).Should().Equal((2, "Reps", (int?)12), (1, "MaxReps", (int?)null));
        SetsOf(entries[1]).Should().Equal((3, "Duration", (int?)40));
    }

    [Fact]
    public async Task Put_ShouldReplaceTheStoredSetRowsEvenWhenOnlyTheEffortChanges()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "DRAFT");
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var before = await StoredSetRowIds(SeedData.TrainedSentadillaEntryId);

        // Act
        var response = await client.PutAsJsonAsync(
            ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.SentadillaId), Body(SeedData.SentadillaId, "LOW", Reps(5, 5)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var after = await StoredSetRowIds(SeedData.TrainedSentadillaEntryId);
        before.Should().ContainSingle();
        after.Should().ContainSingle();
        after.Should().NotIntersectWith(before);
        var entry = (await EntriesOf(SeedData.TrainedWorkoutId))[0];
        entry.GetProperty("effort").GetString().Should().Be("LOW");
        SetsOf(entry).Should().Equal((5, "Reps", (int?)5));
    }

    [Fact]
    public async Task Put_ShouldLeaveExactlyTheNewSetRowsWhenThereAreMoreOfThemThanBefore()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "DRAFT");
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var before = await StoredSetRowIds(SeedData.TrainedSentadillaEntryId);

        // Act
        var response = await client.PutAsJsonAsync(
            ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.SentadillaId), Body(SeedData.SentadillaId, "MID", Reps(2, 8), Reps(2, 6), Sets(1, "MaxReps")));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var after = await StoredSetRowIds(SeedData.TrainedSentadillaEntryId);
        after.Should().HaveCount(3);
        after.Should().NotIntersectWith(before);
    }

    [Fact]
    public async Task Put_ShouldUpdateTheExerciseForTheOwnerOfASelfCreatedWorkout()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(
            ExerciseRoute(SeedData.OwnedWorkoutId, SeedData.PressBancaId), Body(SeedData.PressBancaId, "LOW", Reps(3, 15)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SetsOf((await EntriesOf(SeedData.OwnedWorkoutId))[0]).Should().Equal((3, "Reps", (int?)15));
    }

    [Fact]
    public async Task Put_ShouldReplaceTheSetsWithARepsRange()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(
            ExerciseRoute(SeedData.OwnedWorkoutId, SeedData.PressBancaId), Body(SeedData.PressBancaId, "MID", RepsRange(4, 8, 12)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entry = (await EntriesOf(SeedData.OwnedWorkoutId))[0];
        SetsOf(entry).Should().Equal((4, "RepsRange", (int?)8));
        MaxValuesOf(entry).Should().Equal(12);
    }

    [Fact]
    public async Task Put_ShouldRejectARepsRangeWhoseMaxIsNotAboveTheMinAndChangeNothing()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(
            ExerciseRoute(SeedData.OwnedWorkoutId, SeedData.PressBancaId), Body(SeedData.PressBancaId, "MID", RepsRange(4, 12, 8)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        SetsOf((await EntriesOf(SeedData.OwnedWorkoutId))[0]).Should().Equal((4, "Reps", (int?)10));
    }

    [Fact]
    public async Task Put_ShouldUpdateTheExerciseForAnAdmin()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PutAsJsonAsync(ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.SentadillaId), ASentadillaUpdate());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Put_ShouldForbidTheOwnerOfAPrescribedWorkoutAndChangeNothing()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.SentadillaId), ASentadillaUpdate());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var entry = (await EntriesOf(SeedData.TrainedWorkoutId))[0];
        entry.GetProperty("effort").GetString().Should().Be("MIDHIGH");
        SetsOf(entry).Should().Equal((5, "Reps", (int?)5));
    }

    [Fact]
    public async Task Put_ShouldForbidATrainerOnASelfCreatedWorkoutTheyDoNotOwnAndChangeNothing()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PutAsJsonAsync(
            ExerciseRoute(SeedData.OwnedWorkoutId, SeedData.PressBancaId), Body(SeedData.PressBancaId, "LOW", Reps(3, 15)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        SetsOf((await EntriesOf(SeedData.OwnedWorkoutId))[0]).Should().Equal((4, "Reps", (int?)10));
    }

    [Fact]
    public async Task Put_ShouldReturnNotFoundForAnUnknownWorkout()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PutAsJsonAsync(ExerciseRoute(Guid.NewGuid(), SeedData.SentadillaId), ASentadillaUpdate());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_ShouldReturnNotFoundForAnExerciseNotInTheWorkout()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "DRAFT");
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PutAsJsonAsync(
            ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.CurlBicepsId), Body(SeedData.CurlBicepsId, "MID", Reps(3, 10)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ExerciseIds(await EntriesOf(SeedData.TrainedWorkoutId)).Should().Equal(SeedData.SentadillaId, SeedData.PlanchaId);
    }

    [Fact]
    public async Task Put_ShouldRejectABodyThatNamesAnotherExerciseThanTheRouteAndChangeNothing()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PutAsJsonAsync(
            ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.SentadillaId), Body(SeedData.PlanchaId, "LOW", Reps(3, 10)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var entries = await EntriesOf(SeedData.TrainedWorkoutId);
        ExerciseIds(entries).Should().Equal(SeedData.SentadillaId, SeedData.PlanchaId);
        SetsOf(entries[0]).Should().Equal((5, "Reps", (int?)5));
        SetsOf(entries[1]).Should().Equal((3, "Duration", (int?)40));
    }

    [Fact]
    public async Task Put_ShouldRejectAMalformedBodyAndChangeNothing()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var before = await StoredSetRowIds(SeedData.TrainedSentadillaEntryId);

        // Act
        var response = await client.PutAsJsonAsync(
            ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.SentadillaId), Body(SeedData.SentadillaId, "HIGH"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await StoredSetRowIds(SeedData.TrainedSentadillaEntryId)).Should().Equal(before);
    }

    [Fact]
    public async Task Put_ShouldRejectAnUndefinedEffortAndChangeNothing()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "DRAFT");
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PutAsJsonAsync(
            ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.SentadillaId), Body(SeedData.SentadillaId, 9, Reps(5, 5)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await EntriesOf(SeedData.TrainedWorkoutId))[0].GetProperty("effort").GetString().Should().Be("MIDHIGH");
    }

    [Fact]
    public async Task Put_ShouldRejectANewTotalOfSetsInsideASuperSeriesAndChangeNothing()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.PrescribedSuperSeriesWorkoutId, "DRAFT");
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PutAsJsonAsync(
            ExerciseRoute(SeedData.PrescribedSuperSeriesWorkoutId, SeedData.PressBancaId), Body(SeedData.PressBancaId, "LOW", Reps(4, 10)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var entry = (await EntriesOf(SeedData.PrescribedSuperSeriesWorkoutId))[0];
        entry.GetProperty("effort").GetString().Should().Be("HIGH");
        entry.GetProperty("superSeriesId").GetInt32().Should().Be(SeedData.SeededSuperSeries);
        SetsOf(entry).Should().Equal((3, "Reps", (int?)10));
    }

    [Fact]
    public async Task Put_ShouldKeepTheSuperSeriesWhenTheTotalOfSetsIsTheSame()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.PrescribedSuperSeriesWorkoutId, "DRAFT");
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PutAsJsonAsync(
            ExerciseRoute(SeedData.PrescribedSuperSeriesWorkoutId, SeedData.PressBancaId),
            Body(SeedData.PressBancaId, "LOW", Reps(2, 8), Sets(1, "MaxReps")));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entry = (await EntriesOf(SeedData.PrescribedSuperSeriesWorkoutId))[0];
        entry.GetProperty("superSeriesId").GetInt32().Should().Be(SeedData.SeededSuperSeries);
        SetsOf(entry).Should().Equal((2, "Reps", (int?)8), (1, "MaxReps", (int?)null));
    }

    private static object APressBancaUpdate() => Body(SeedData.PressBancaId, "LOW", Reps(2, 12));

    [Fact]
    public async Task Put_ShouldRejectThePrescribingTrainerOnAStartedWorkoutWithConflict()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync(ExerciseRoute(SeedData.PrescribedStartedWorkoutId, SeedData.PressBancaId), APressBancaUpdate());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var entry = (await EntriesOf(SeedData.PrescribedStartedWorkoutId)).Single();
        entry.GetProperty("effort").GetString().Should().Be("HIGH");
    }

    [Fact]
    public async Task Put_ShouldRejectTheOwnerOfAFinishedWorkoutWithoutTrainerWithConflict()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.OwnedWorkoutId, "COMPLETED");
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.PutAsJsonAsync(ExerciseRoute(SeedData.OwnedWorkoutId, SeedData.PressBancaId), APressBancaUpdate());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var entry = (await EntriesOf(SeedData.OwnedWorkoutId)).Single();
        entry.GetProperty("effort").GetString().Should().Be("HIGH");
    }

    [Fact]
    public async Task Put_ShouldRejectACallerWhoMayNotEditAStartedWorkoutWithForbidden()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.PutAsJsonAsync(ExerciseRoute(SeedData.PrescribedStartedWorkoutId, SeedData.PressBancaId), APressBancaUpdate());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Put_ShouldAllowAnAdminOnAStartedWorkout()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PutAsJsonAsync(ExerciseRoute(SeedData.PrescribedStartedWorkoutId, SeedData.PressBancaId), APressBancaUpdate());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entry = (await EntriesOf(SeedData.PrescribedStartedWorkoutId)).Single();
        entry.GetProperty("effort").GetString().Should().Be("LOW");
    }

    [Fact]
    public async Task Put_ShouldConflictForThePrescribingTrainerOnAPublishedWorkoutAndChangeNothing()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var before = (await WorkoutAsAdmin(SeedData.TrainedWorkoutId)).GetProperty("exercises").GetRawText();

        // Act
        var response = await trainer.PutAsJsonAsync(ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.SentadillaId), ASentadillaUpdate());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var stored = await WorkoutAsAdmin(SeedData.TrainedWorkoutId);
        stored.GetProperty("exercises").GetRawText().Should().Be(before);
        stored.GetProperty("status").GetString().Should().Be("PUBLISHED");
    }

    [Fact]
    public async Task Put_ShouldLetThePrescribingTrainerEditAfterUnpublishing()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        (await trainer.PutAsJsonAsync($"/workouts/{SeedData.TrainedWorkoutId}/status", new { status = "DRAFT" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await trainer.PutAsJsonAsync(ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.SentadillaId), ASentadillaUpdate());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await WorkoutAsAdmin(SeedData.TrainedWorkoutId)).GetProperty("status").GetString().Should().Be("DRAFT");
    }

    [Fact]
    public async Task Put_ShouldConflictForAnAdminOnAReviewedWorkoutAndChangeNothing()
    {
        // Arrange
        await GivenReviewed(SeedData.TrainedWorkoutId);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var before = (await WorkoutAsAdmin(SeedData.TrainedWorkoutId)).GetProperty("exercises").GetRawText();

        // Act
        var response = await admin.PutAsJsonAsync(ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.SentadillaId), ASentadillaUpdate());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await WorkoutAsAdmin(SeedData.TrainedWorkoutId)).GetProperty("exercises").GetRawText().Should().Be(before);
    }

    [Fact]
    public async Task Put_ShouldReplacePerSideAndRestAndReturnThem()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "DRAFT");
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var body = BodyWith(SeedData.SentadillaId, "HIGH", true, 1.5m, Reps(2, 12));

        // Act
        var response = await trainer.PutAsJsonAsync(ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.SentadillaId), body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var returned = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        returned.GetProperty("perSide").GetBoolean().Should().BeTrue();
        returned.GetProperty("restMinutes").GetDecimal().Should().Be(1.5m);
        var stored = (await EntriesOf(SeedData.TrainedWorkoutId)).Single(entry => entry.GetProperty("exerciseId").GetGuid() == SeedData.SentadillaId);
        stored.GetProperty("perSide").GetBoolean().Should().BeTrue();
        stored.GetProperty("restMinutes").GetDecimal().Should().Be(1.5m);
    }

    [Fact]
    public async Task Put_ShouldClearPerSideAndRestWhenTheyAreOmitted()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "DRAFT");
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var route = ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.SentadillaId);
        (await trainer.PutAsJsonAsync(route, BodyWith(SeedData.SentadillaId, "HIGH", true, 2m, Reps(2, 12)))).EnsureSuccessStatusCode();

        // Act
        var response = await trainer.PutAsJsonAsync(route, ASentadillaUpdate());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = (await EntriesOf(SeedData.TrainedWorkoutId)).Single(entry => entry.GetProperty("exerciseId").GetGuid() == SeedData.SentadillaId);
        stored.GetProperty("perSide").GetBoolean().Should().BeFalse();
        stored.GetProperty("restMinutes").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Put_ShouldRejectAnInvalidRestAndKeepThePreviousValues()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "DRAFT");
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var route = ExerciseRoute(SeedData.TrainedWorkoutId, SeedData.SentadillaId);
        (await trainer.PutAsJsonAsync(route, BodyWith(SeedData.SentadillaId, "HIGH", true, 2m, Reps(2, 12)))).EnsureSuccessStatusCode();

        // Act
        var response = await trainer.PutAsJsonAsync(route, BodyWith(SeedData.SentadillaId, "LOW", false, -1m, Reps(5, 5)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var stored = (await EntriesOf(SeedData.TrainedWorkoutId)).Single(entry => entry.GetProperty("exerciseId").GetGuid() == SeedData.SentadillaId);
        stored.GetProperty("effort").GetString().Should().Be("HIGH");
        stored.GetProperty("perSide").GetBoolean().Should().BeTrue();
        stored.GetProperty("restMinutes").GetDecimal().Should().Be(2m);
        SetsOf(stored).Should().Equal((2, "Reps", (int?)12));
    }

    [Fact]
    public async Task Put_ShouldKeepAnEntryEditableAfterItsExerciseIsArchived()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "DRAFT");
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var created = await trainer.PostAsJsonAsync("/exercises", new { languageCode = "es", name = "Remo con banda", muscle = "MIDDLE_BACK", type = "STRENGTH" });
        var custom = (await created.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("id").GetGuid();
        (await trainer.PostAsJsonAsync(ExercisesRoute(SeedData.TrainedWorkoutId), Body(custom, "MID", Reps(3, 10)))).EnsureSuccessStatusCode();
        (await trainer.DeleteAsync($"/exercises/{custom}")).EnsureSuccessStatusCode();

        // Act
        var response = await trainer.PutAsJsonAsync(ExerciseRoute(SeedData.TrainedWorkoutId, custom), Body(custom, "HIGH", Reps(4, 8)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = (await EntriesOf(SeedData.TrainedWorkoutId)).Single(entry => entry.GetProperty("exerciseId").GetGuid() == custom);
        stored.GetProperty("effort").GetString().Should().Be("HIGH");
        SetsOf(stored).Should().Equal((4, "Reps", (int?)8));
    }
}
