using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.WorkoutTemplates;

/// <summary>
/// <c>POST /workout-templates/{id}/workouts</c>: creating a workout for an athlete by copying a template. The seeded
/// trainer actively trains the seeded user and prepares the seeded four-week mesocycle for them. The access matrix
/// lives in <see cref="WorkoutTemplateAuthEndpointsTests"/>.
/// </summary>
public class WorkoutFromTemplateEndpointsTests : WorkoutTemplateEndpointTest
{
    private static readonly string WorkoutsRoute = $"{SeededTemplateRoute}/workouts";

    public WorkoutFromTemplateEndpointsTests(ApiFactory factory) : base(factory)
    { }

    [Fact]
    public async Task Post_ShouldCreateADraftCopyPrescribedByTheTemplateTrainer()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(WorkoutsRoute, new { userId = SeedData.UserId, workoutDate = "2026-12-01" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await Body(response);
        var id = body.GetProperty("id").GetGuid();
        response.Headers.Location!.AbsolutePath.Should().Be($"/workouts/{id}");
        body.GetProperty("name").GetString().Should().Be(SeedData.TrainerTemplateName);
        body.GetProperty("description").GetString().Should().Be(SeedData.TrainerTemplateDescription);
        body.GetProperty("userId").GetGuid().Should().Be(SeedData.UserId);
        body.GetProperty("trainerId").GetGuid().Should().Be(SeedData.TrainerId);
        body.GetProperty("status").GetString().Should().Be("DRAFT");
        body.GetProperty("workoutDate").GetString().Should().Be("2026-12-01");
        ExerciseIds(body).Should().Equal(SeedData.SentadillaId, SeedData.PressBancaId, SeedData.PlanchaId);
        SuperSeries(body).Should().Equal(SeedData.SeededSuperSeries, SeedData.SeededSuperSeries, null);
        body.GetProperty("exercises")[0].GetProperty("translations").GetArrayLength().Should().BeGreaterThan(0);
        Ids(body.GetProperty("exercises")).Should().NotIntersectWith(
            [SeedData.TrainerTemplateSentadillaEntryId, SeedData.TrainerTemplatePressBancaEntryId, SeedData.TrainerTemplatePlanchaEntryId]);
    }

    [Fact]
    public async Task Post_ShouldKeepTheTemplateTrainerWhenAnAdminCreatesIt()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(WorkoutsRoute, new { userId = SeedData.UserId, workoutDate = "2026-12-01" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await Body(response)).GetProperty("trainerId").GetGuid().Should().Be(SeedData.TrainerId);
    }

    [Fact]
    public async Task Post_ShouldPlaceTheWorkoutInAMesocycleWeekDatedByTheWeek()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(WorkoutsRoute, new
        {
            userId = SeedData.UserId,
            mesocycleId = SeedData.MesocycleId,
            mesocycleWeekNumber = 3,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await Body(response);
        body.GetProperty("mesocycleId").GetGuid().Should().Be(SeedData.MesocycleId);
        body.GetProperty("mesocycleWeekNumber").GetInt32().Should().Be(3);
        body.GetProperty("workoutDate").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Post_ShouldRequireADateOrAPlacement()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(WorkoutsRoute, new { userId = SeedData.UserId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldRejectHalfAPlacement()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(WorkoutsRoute, new { userId = SeedData.UserId, mesocycleId = SeedData.MesocycleId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldRequireTheUser()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(WorkoutsRoute, new { workoutDate = "2026-12-01" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldNotFindAnUnknownUser()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(WorkoutsRoute, new { userId = Guid.NewGuid(), workoutDate = "2026-12-01" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_ShouldForbidATrainerNamingSomeoneWhoIsNotARegularUser()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(WorkoutsRoute, new { userId = SeedData.AdminId, workoutDate = "2026-12-01" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_ShouldRejectAnAdminNamingSomeoneWhoIsNotARegularUser()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var (otherTrainerId, _) = await GivenUser(Role.TRAINER);

        // Act
        var response = await admin.PostAsJsonAsync(WorkoutsRoute, new { userId = otherTrainerId, workoutDate = "2026-12-01" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldRejectADeactivatedAthlete()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        await GivenDeactivated(SeedData.UserId);

        // Act
        var response = await trainer.PostAsJsonAsync(WorkoutsRoute, new { userId = SeedData.UserId, workoutDate = "2026-12-01" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldRejectATemplateWhoseTrainerIsDeactivated()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        await GivenDeactivated(SeedData.TrainerId);

        // Act
        var response = await admin.PostAsJsonAsync(WorkoutsRoute, new { userId = SeedData.UserId, workoutDate = "2026-12-01" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldRejectAMesocycleOfAnotherTrainer()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var (otherTrainerId, _) = await GivenUser(Role.TRAINER);
        var mesocycle = await Body(await admin.PostAsJsonAsync("/mesocycles", new
        {
            trainerId = otherTrainerId,
            athleteId = SeedData.UserId,
            name = "Other block",
            objective = "Other objective",
            startDate = "2026-11-09",
            durationInWeeks = 4,
        }));

        // Act
        var response = await admin.PostAsJsonAsync(WorkoutsRoute, new
        {
            userId = SeedData.UserId,
            mesocycleId = mesocycle.GetProperty("id").GetGuid(),
            mesocycleWeekNumber = 1,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Post_ShouldRejectAMesocycleOfAnotherAthlete()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var (otherAthleteId, _) = await GivenUser(Role.USER);

        // Act
        var response = await admin.PostAsJsonAsync(WorkoutsRoute, new
        {
            userId = otherAthleteId,
            mesocycleId = SeedData.MesocycleId,
            mesocycleWeekNumber = 1,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Workout_ShouldNotFollowLaterChangesToTheTemplate()
    {
        // Arrange
        var workoutId = await GivenWorkoutFromSeededTemplate();
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.DeleteAsync($"{SeededTemplateRoute}/exercises/{SeedData.PlanchaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        ExerciseIds(await WorkoutAsAdmin(workoutId)).Should().Equal(SeedData.SentadillaId, SeedData.PressBancaId, SeedData.PlanchaId);
    }

    [Fact]
    public async Task Workout_ShouldSurviveTheDeletionOfItsTemplate()
    {
        // Arrange
        var workoutId = await GivenWorkoutFromSeededTemplate();
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.DeleteAsync(SeededTemplateRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var workout = await WorkoutAsAdmin(workoutId);
        ExerciseIds(workout).Should().Equal(SeedData.SentadillaId, SeedData.PressBancaId, SeedData.PlanchaId);
        SuperSeries(workout).Should().Equal(SeedData.SeededSuperSeries, SeedData.SeededSuperSeries, null);
    }

    [Fact]
    public async Task Post_ShouldCopyAnExerciseArchivedAfterItWasAddedToTheTemplate()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var custom = await GivenCustomExercise(trainer);
        (await trainer.PostAsJsonAsync($"{SeededTemplateRoute}/exercises", AnExerciseBody(custom, "MID", Reps(3, 10)))).EnsureSuccessStatusCode();
        (await trainer.DeleteAsync($"/exercises/{custom}")).EnsureSuccessStatusCode();

        // Act
        var response = await trainer.PostAsJsonAsync(WorkoutsRoute, new { userId = SeedData.UserId, workoutDate = "2026-12-01" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        ExerciseIds(await Body(response)).Should().Contain(custom);
    }
}
