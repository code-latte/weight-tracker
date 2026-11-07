using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.WorkoutTemplates;

/// <summary>
/// <c>POST /workouts/{workoutId}/templates</c>: saving an existing workout as a template owned by its trainer. The
/// seeded trainer prescribes the superseries workout (Press Banca and Sentadilla in superseries 1, then Plancha) and
/// the started workout; the self-created workouts have no trainer.
/// </summary>
public class WorkoutTemplateFromWorkoutEndpointsTests : WorkoutTemplateEndpointTest
{
    public WorkoutTemplateFromWorkoutEndpointsTests(ApiFactory factory) : base(factory)
    { }

    private static string TemplatesOf(Guid workoutId) => $"/workouts/{workoutId}/templates";

    /// <summary>
    /// Creates a workout for the seeded user through <c>POST /workouts</c>, with no exercises.
    /// </summary>
    private async Task<Guid> GivenWorkout(string callerEmail, Guid? trainerId = null)
    {
        var caller = await AuthenticatedClientFor(callerEmail);

        var response = await caller.PostAsJsonAsync("/workouts", new
        {
            userId = SeedData.UserId,
            trainerId,
            name = "Extra workout",
            workoutDate = "2026-12-01",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created, "the extra workout must be arranged first");

        return (await Body(response)).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Post_ShouldRejectAnonymousCallers()
    {
        // Arrange
        var anonymous = Client;

        // Act
        var response = await anonymous.PostAsync(TemplatesOf(SeedData.PrescribedSuperSeriesWorkoutId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await TemplateCountAsAdmin()).Should().Be(1);
    }

    [Fact]
    public async Task Post_ShouldRejectAnInvalidToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.PostAsync(TemplatesOf(SeedData.PrescribedSuperSeriesWorkoutId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await TemplateCountAsAdmin()).Should().Be(1);
    }

    /// <summary>Workouts the seeded user owns: one prescribed by the trainer, one without trainer.</summary>
    public static TheoryData<string> UserOwnedWorkouts() => new()
    {
        SeedData.PrescribedSuperSeriesWorkoutId.ToString(),
        SeedData.SelfCreatedPairWorkoutId.ToString(),
    };

    [Theory]
    [MemberData(nameof(UserOwnedWorkouts))]
    public async Task Post_ShouldForbidTheRegularUserWhoOwnsTheWorkout(string workoutId)
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.PostAsync(TemplatesOf(Guid.Parse(workoutId)), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await TemplateCountAsAdmin()).Should().Be(1);
    }

    [Fact]
    public async Task Post_ShouldForbidAnotherTrainer()
    {
        // Arrange
        var (_, otherTrainerEmail) = await GivenUser(Role.TRAINER);
        var otherTrainer = await AuthenticatedClientFor(otherTrainerEmail);

        // Act
        var response = await otherTrainer.PostAsync(TemplatesOf(SeedData.PrescribedSuperSeriesWorkoutId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await TemplateCountAsAdmin()).Should().Be(1);
    }

    [Fact]
    public async Task Post_ShouldForbidATrainerOnTheirOwnWorkoutWithoutTrainer()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsync(TemplatesOf(SeedData.TrainerOwnWorkoutId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await TemplateCountAsAdmin()).Should().Be(1);
    }

    [Fact]
    public async Task Post_ShouldNotFindAnUnknownWorkout()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsync(TemplatesOf(Guid.NewGuid()), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_ShouldRejectAnAdminOnAWorkoutWithoutTrainer()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsync(TemplatesOf(SeedData.SelfCreatedPairWorkoutId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await TemplateCountAsAdmin()).Should().Be(1);
    }

    [Fact]
    public async Task Post_ShouldRejectAWorkoutWhoseTrainerIsAnAdmin()
    {
        // Arrange
        var workoutId = await GivenWorkout(SeedCredentials.Admin, SeedData.AdminId);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsync(TemplatesOf(workoutId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await TemplateCountAsAdmin()).Should().Be(1);
    }

    [Fact]
    public async Task Post_ShouldRejectAWorkoutWhoseTrainerIsDeactivated()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        await GivenDeactivated(SeedData.TrainerId);

        // Act
        var response = await admin.PostAsync(TemplatesOf(SeedData.PrescribedSuperSeriesWorkoutId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await TemplateCountAsAdmin()).Should().Be(1);
    }

    [Fact]
    public async Task Post_ShouldSaveTheTrainersWorkoutAsTheirTemplate()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsync(TemplatesOf(SeedData.PrescribedSuperSeriesWorkoutId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await Body(response);
        var id = body.GetProperty("id").GetGuid();
        response.Headers.Location!.AbsolutePath.Should().Be($"{TemplatesRoute}/{id}");
        body.GetProperty("trainerId").GetGuid().Should().Be(SeedData.TrainerId);
        ExerciseIds(body).Should().Equal(SeedData.PressBancaId, SeedData.SentadillaId, SeedData.PlanchaId);
        SuperSeries(body).Should().Equal(SeedData.SeededSuperSeries, SeedData.SeededSuperSeries, null);
        Ids(body.GetProperty("exercises")).Should().NotIntersectWith(
        [
            SeedData.PrescribedSuperSeriesPressBancaEntryId,
            SeedData.PrescribedSuperSeriesSentadillaEntryId,
            SeedData.PrescribedSuperSeriesPlanchaEntryId,
        ]);
        var workout = await WorkoutAsAdmin(SeedData.PrescribedSuperSeriesWorkoutId);
        body.GetProperty("name").GetString().Should().Be(workout.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Post_ShouldGiveTheTemplateToTheWorkoutTrainerWhenAnAdminSavesIt()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsync(TemplatesOf(SeedData.PrescribedSuperSeriesWorkoutId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await Body(response)).GetProperty("trainerId").GetGuid().Should().Be(SeedData.TrainerId);
    }

    [Fact]
    public async Task Post_ShouldSaveAStartedWorkoutAndLeaveItUnchanged()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsync(TemplatesOf(SeedData.PrescribedStartedWorkoutId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        ExerciseIds(await Body(response)).Should().Equal(SeedData.PressBancaId);
        var workout = await WorkoutAsAdmin(SeedData.PrescribedStartedWorkoutId);
        workout.GetProperty("status").GetString().Should().Be("STARTED");
        workout.GetProperty("userId").GetGuid().Should().Be(SeedData.UserId);
        ExerciseIds(workout).Should().Equal(SeedData.PressBancaId);
    }

    [Fact]
    public async Task Post_ShouldSaveAWorkoutWithoutExercises()
    {
        // Arrange
        var workoutId = await GivenWorkout(SeedCredentials.Trainer);
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsync(TemplatesOf(workoutId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await Body(response)).GetProperty("exercises").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Post_ShouldCopyAnExerciseArchivedAfterItWasAddedToTheWorkout()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var custom = await GivenCustomExercise(trainer);
        // Only a draft can be edited, so the trainer unpublishes the seeded workout first
        (await trainer.PutAsJsonAsync($"/workouts/{SeedData.TrainedWorkoutId}/status", new { status = "DRAFT" })).EnsureSuccessStatusCode();
        (await trainer.PostAsJsonAsync($"/workouts/{SeedData.TrainedWorkoutId}/exercises", AnExerciseBody(custom, "MID", Reps(3, 10)))).EnsureSuccessStatusCode();
        (await trainer.DeleteAsync($"/exercises/{custom}")).EnsureSuccessStatusCode();

        // Act
        var response = await trainer.PostAsync(TemplatesOf(SeedData.TrainedWorkoutId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        ExerciseIds(await Body(response)).Should().Contain(custom);
    }

    [Fact]
    public async Task Post_ShouldListTheTemplateForItsTrainerOnly()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var (_, otherTrainerEmail) = await GivenUser(Role.TRAINER);
        var otherTrainer = await AuthenticatedClientFor(otherTrainerEmail);

        // Act
        var response = await trainer.PostAsync(TemplatesOf(SeedData.PrescribedSuperSeriesWorkoutId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await Body(response)).GetProperty("id").GetGuid();
        Ids(await Body(await trainer.GetAsync(TemplatesRoute))).Should().Contain(id);
        Ids(await Body(await otherTrainer.GetAsync(TemplatesRoute))).Should().NotContain(id);
    }

    [Fact]
    public async Task Template_ShouldNotFollowLaterChangesToTheWorkout()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var created = await trainer.PostAsync(TemplatesOf(SeedData.PrescribedSuperSeriesWorkoutId), null);
        var templateId = (await Body(created)).GetProperty("id").GetGuid();
        // Only a draft can be edited, so the trainer unpublishes the seeded workout first
        (await trainer.PutAsJsonAsync($"/workouts/{SeedData.PrescribedSuperSeriesWorkoutId}/status", new { status = "DRAFT" })).EnsureSuccessStatusCode();

        // Act
        var response = await trainer.DeleteAsync($"/workouts/{SeedData.PrescribedSuperSeriesWorkoutId}/exercises/{SeedData.PlanchaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        ExerciseIds(await TemplateAsAdmin(templateId)).Should().Equal(SeedData.PressBancaId, SeedData.SentadillaId, SeedData.PlanchaId);
    }

    [Fact]
    public async Task Post_ShouldCreateAnotherTemplateWhenTheSameWorkoutIsSavedAgain()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var first = (await Body(await trainer.PostAsync(TemplatesOf(SeedData.PrescribedSuperSeriesWorkoutId), null))).GetProperty("id").GetGuid();

        // Act
        var response = await trainer.PostAsync(TemplatesOf(SeedData.PrescribedSuperSeriesWorkoutId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await Body(response)).GetProperty("id").GetGuid().Should().NotBe(first);
        (await TemplateCountAsAdmin()).Should().Be(3);
    }
}
