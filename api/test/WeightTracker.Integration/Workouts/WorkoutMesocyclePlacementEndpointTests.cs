using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Placement of workouts in a mesocycle week through <c>POST /workouts</c> and <c>PUT /workouts/{id}</c>. The seeded
/// four-week mesocycle goes from the trainer to the user and starts on Thursday 2026-10-01, so its weeks fall on the
/// Wednesdays 2026-09-30, 2026-10-07, 2026-10-14 and 2026-10-21. The published trained workout is placed in week 1 and
/// the started prescribed workout in week 2.
/// </summary>
public class WorkoutMesocyclePlacementEndpointTests : WorkoutEndpointTest
{
    public WorkoutMesocyclePlacementEndpointTests(ApiFactory factory) : base(factory)
    { }

    private static object ANewWorkout(Guid? userId, Guid? mesocycleId, int? mesocycleWeekNumber, Guid? trainerId = null)
        => new
        {
            userId,
            trainerId,
            name = "Planned day",
            workoutDate = "2026-10-20",
            mesocycleId,
            mesocycleWeekNumber,
        };

    private static object AnUpdate(Guid? mesocycleId, int? mesocycleWeekNumber, Guid? userId = null, string name = "Renamed day")
        => new
        {
            name,
            workoutDate = "2026-10-03",
            userId = userId ?? SeedData.UserId,
            trainerId = SeedData.TrainerId,
            mesocycleId,
            mesocycleWeekNumber,
        };

    private static readonly string TrainedWorkoutRoute = $"/workouts/{SeedData.TrainedWorkoutId}";

    private static readonly string StartedWorkoutRoute = $"/workouts/{SeedData.PrescribedStartedWorkoutId}";

    private static async Task<JsonElement> Body(HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

    private async Task<JsonElement> WorkoutAsAdmin(Guid workoutId)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        return await Body(await admin.GetAsync($"/workouts/{workoutId}"));
    }

    private async Task<int> WorkoutCount()
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        return (await Body(await admin.GetAsync("/workouts"))).GetArrayLength();
    }

    private static void ShouldBeDated(JsonElement workout, string date)
        => workout.GetProperty("workoutDate").GetString().Should().Be(date);

    private async Task GivenSeededMesocycleRescheduledTo(string startDate)
    {
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        var response = await trainer.PutAsJsonAsync($"/mesocycles/{SeedData.MesocycleId}", new
        {
            name = SeedData.MesocycleName,
            objective = "Build muscle mass before the strength block",
            startDate,
            durationInWeeks = SeedData.MesocycleDurationInWeeks,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, "the mesocycle must be rescheduled first");
    }

    private static void ShouldBePlacedIn(JsonElement workout, Guid? mesocycleId, int? weekNumber)
    {
        var placedIn = workout.GetProperty("mesocycleId");
        var week = workout.GetProperty("mesocycleWeekNumber");

        if (mesocycleId is null)
        {
            placedIn.ValueKind.Should().Be(JsonValueKind.Null);
            week.ValueKind.Should().Be(JsonValueKind.Null);
            return;
        }

        placedIn.GetGuid().Should().Be(mesocycleId.Value);
        week.GetInt32().Should().Be(weekNumber!.Value);
    }

    private async Task<Guid> GivenUser(Role role)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        var response = await admin.PostAsJsonAsync("/users", new
        {
            email = $"{Guid.NewGuid():N}@weighttracker.test",
            password = SeedCredentials.Password,
            role = (int)role,
            name = "Extra",
            surname = "User",
            dateBirth = "1990-01-01",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created, "the extra user must be arranged first");

        return (await Body(response)).GetProperty("id").GetGuid();
    }

    private async Task<Guid> GivenMesocycle(Guid trainerId, Guid athleteId)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        var response = await admin.PostAsJsonAsync("/mesocycles", new
        {
            trainerId,
            athleteId,
            name = "Other block",
            objective = "Other objective",
            startDate = "2026-11-09",
            durationInWeeks = 4,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created, "the extra mesocycle must be arranged first");

        return (await Body(response)).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Create_ShouldPlaceATrainersWorkoutInAnUndefinedWeekOfTheirMesocycle()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync("/workouts", ANewWorkout(SeedData.UserId, SeedData.MesocycleId, 3));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await Body(response);
        ShouldBePlacedIn(body, SeedData.MesocycleId, 3);
        ShouldBePlacedIn(await WorkoutAsAdmin(body.GetProperty("id").GetGuid()), SeedData.MesocycleId, 3);
    }

    [Fact]
    public async Task Create_ShouldLetAnAdminPlaceAWorkout()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync("/workouts", ANewWorkout(SeedData.UserId, SeedData.MesocycleId, 2));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        ShouldBePlacedIn(await Body(response), SeedData.MesocycleId, 2);
    }

    [Fact]
    public async Task Create_ShouldStayStandaloneWithoutPlacement()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.PostAsJsonAsync("/workouts", ANewWorkout(null, null, null));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        ShouldBePlacedIn(await Body(response), null, null);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Create_ShouldRejectHalfAPlacement(bool withMesocycle, bool withWeek)
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var count = await WorkoutCount();

        // Act
        var response = await trainer.PostAsJsonAsync("/workouts", ANewWorkout(
            SeedData.UserId, withMesocycle ? SeedData.MesocycleId : null, withWeek ? 1 : null));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await WorkoutCount()).Should().Be(count);
    }

    [Fact]
    public async Task Create_ShouldRejectAWeekNumberBelowOne()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync("/workouts", ANewWorkout(SeedData.UserId, SeedData.MesocycleId, 0));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldNotFindAnUnknownMesocycle()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var count = await WorkoutCount();

        // Act
        var response = await trainer.PostAsJsonAsync("/workouts", ANewWorkout(SeedData.UserId, Guid.NewGuid(), 1));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await WorkoutCount()).Should().Be(count);
    }

    [Fact]
    public async Task Create_ShouldRefuseAUserPlacingTheirOwnWorkout()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var count = await WorkoutCount();

        // Act
        var response = await user.PostAsJsonAsync("/workouts", ANewWorkout(null, SeedData.MesocycleId, 1));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await WorkoutCount()).Should().Be(count);
    }

    [Fact]
    public async Task Create_ShouldRefuseATrainerPlacingInAnotherTrainersMesocycle()
    {
        // Arrange
        var otherTrainerId = await GivenUser(Role.TRAINER);
        var otherMesocycleId = await GivenMesocycle(otherTrainerId, SeedData.UserId);
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync("/workouts", ANewWorkout(SeedData.UserId, otherMesocycleId, 1));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_ShouldRejectAWorkoutForAnotherAthlete()
    {
        // Arrange
        var otherAthleteId = await GivenUser(Role.USER);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync("/workouts", ANewWorkout(otherAthleteId, SeedData.MesocycleId, 1));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_ShouldRejectAWorkoutWithAnotherTrainer()
    {
        // Arrange
        var otherTrainerId = await GivenUser(Role.TRAINER);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(
            "/workouts", ANewWorkout(SeedData.UserId, SeedData.MesocycleId, 1, otherTrainerId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_ShouldRejectAWeekBeyondTheDuration()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync("/workouts", ANewWorkout(SeedData.UserId, SeedData.MesocycleId, 5));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_ShouldMoveAWorkoutToAnotherWeek()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "DRAFT");
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync(TrainedWorkoutRoute, AnUpdate(SeedData.MesocycleId, 3));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ShouldBePlacedIn(await Body(response), SeedData.MesocycleId, 3);
        ShouldBePlacedIn(await WorkoutAsAdmin(SeedData.TrainedWorkoutId), SeedData.MesocycleId, 3);
    }

    [Fact]
    public async Task Update_ShouldUnlinkWhenThePairIsOmitted()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "DRAFT");
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync(TrainedWorkoutRoute, AnUpdate(null, null));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ShouldBePlacedIn(await WorkoutAsAdmin(SeedData.TrainedWorkoutId), null, null);
    }

    [Fact]
    public async Task Update_ShouldRejectHalfAPlacementAndChangeNothing()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync(TrainedWorkoutRoute, AnUpdate(null, 2));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var workout = await WorkoutAsAdmin(SeedData.TrainedWorkoutId);
        workout.GetProperty("name").GetString().Should().Be(SeedData.TrainedWorkoutName);
        ShouldBePlacedIn(workout, SeedData.MesocycleId, 1);
    }

    [Fact]
    public async Task Update_ShouldRejectAReassignmentThatBreaksThePlacement()
    {
        // Arrange
        var otherAthleteId = await GivenUser(Role.USER);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PutAsJsonAsync(TrainedWorkoutRoute, AnUpdate(SeedData.MesocycleId, 1, otherAthleteId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var workout = await WorkoutAsAdmin(SeedData.TrainedWorkoutId);
        workout.GetProperty("userId").GetGuid().Should().Be(SeedData.UserId);
        ShouldBePlacedIn(workout, SeedData.MesocycleId, 1);
    }

    [Fact]
    public async Task Update_ShouldRefuseAnAdminMovingAStartedWorkout()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PutAsJsonAsync(StartedWorkoutRoute, AnUpdate(SeedData.MesocycleId, 3));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ShouldBePlacedIn(await WorkoutAsAdmin(SeedData.PrescribedStartedWorkoutId), SeedData.MesocycleId, 2);
    }

    [Fact]
    public async Task Update_ShouldRefuseAnAdminMovingACompletedWorkout()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.PrescribedStartedWorkoutId, "COMPLETED");
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PutAsJsonAsync(StartedWorkoutRoute, AnUpdate(SeedData.MesocycleId, 3));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ShouldBePlacedIn(await WorkoutAsAdmin(SeedData.PrescribedStartedWorkoutId), SeedData.MesocycleId, 2);
    }

    [Fact]
    public async Task Update_ShouldRefuseAnAdminMovingAReviewedWorkout()
    {
        // Arrange
        await GivenReviewed(SeedData.PrescribedStartedWorkoutId);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PutAsJsonAsync(StartedWorkoutRoute, AnUpdate(SeedData.MesocycleId, 3));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ShouldBePlacedIn(await WorkoutAsAdmin(SeedData.PrescribedStartedWorkoutId), SeedData.MesocycleId, 2);
    }

    [Fact]
    public async Task Update_ShouldLetAnAdminMoveAPublishedWorkout()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PutAsJsonAsync(TrainedWorkoutRoute, AnUpdate(SeedData.MesocycleId, 3));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ShouldBePlacedIn(await WorkoutAsAdmin(SeedData.TrainedWorkoutId), SeedData.MesocycleId, 3);
    }

    [Fact]
    public async Task Update_ShouldLetAnAdminEditAStartedWorkoutKeepingItsPlacement()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PutAsJsonAsync(StartedWorkoutRoute, AnUpdate(SeedData.MesocycleId, 2, name: "Renamed started"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var workout = await WorkoutAsAdmin(SeedData.PrescribedStartedWorkoutId);
        workout.GetProperty("name").GetString().Should().Be("Renamed started");
        ShouldBePlacedIn(workout, SeedData.MesocycleId, 2);
    }

    // ---- Date of a placed workout

    [Fact]
    public async Task Create_ShouldDateAPlacedWorkoutByItsWeekIgnoringTheSentDate()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync("/workouts", new
        {
            userId = SeedData.UserId,
            name = "Planned day",
            workoutDate = "2026-12-25",
            mesocycleId = SeedData.MesocycleId,
            mesocycleWeekNumber = 3,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await Body(response);
        ShouldBeDated(body, "2026-10-14");
        ShouldBeDated(await WorkoutAsAdmin(body.GetProperty("id").GetGuid()), "2026-10-14");
    }

    [Fact]
    public async Task Create_ShouldAcceptAPlacedWorkoutWithoutADate()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync("/workouts", new
        {
            userId = SeedData.UserId,
            name = "Planned day",
            mesocycleId = SeedData.MesocycleId,
            mesocycleWeekNumber = 1,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        ShouldBeDated(await Body(response), "2026-09-30");
    }

    [Fact]
    public async Task Update_ShouldDateAMovedWorkoutByItsNewWeek()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "DRAFT");
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync(TrainedWorkoutRoute, AnUpdate(SeedData.MesocycleId, 3));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ShouldBeDated(await Body(response), "2026-10-14");
        ShouldBeDated(await WorkoutAsAdmin(SeedData.TrainedWorkoutId), "2026-10-14");
    }

    [Fact]
    public async Task Update_ShouldAcceptAPlacedWorkoutWithoutADate()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "DRAFT");
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync(TrainedWorkoutRoute, new
        {
            name = "Renamed day",
            userId = SeedData.UserId,
            trainerId = SeedData.TrainerId,
            mesocycleId = SeedData.MesocycleId,
            mesocycleWeekNumber = 1,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ShouldBeDated(await Body(response), "2026-09-30");
    }

    [Fact]
    public async Task Update_ShouldRedateAnUnchangedPlacementAfterAReschedule()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "DRAFT");
        await GivenSeededMesocycleRescheduledTo("2026-10-08");
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync(TrainedWorkoutRoute, AnUpdate(SeedData.MesocycleId, 1));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ShouldBeDated(await WorkoutAsAdmin(SeedData.TrainedWorkoutId), "2026-10-07");
    }

    [Fact]
    public async Task RescheduleMesocycle_ShouldLeaveTheDatesOfItsWorkouts()
    {
        // Arrange
        var newStartDate = "2026-10-08";

        // Act
        await GivenSeededMesocycleRescheduledTo(newStartDate);

        // Assert
        ShouldBeDated(await WorkoutAsAdmin(SeedData.TrainedWorkoutId), "2026-09-30");
    }

    [Fact]
    public async Task Update_ShouldKeepTheSentDateWhenUnlinking()
    {
        // Arrange
        // The trainer may only edit a draft, so the published seed goes back to draft first
        await GivenWorkoutStatus(SeedData.TrainedWorkoutId, "DRAFT");
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync(TrainedWorkoutRoute, new
        {
            name = "Renamed day",
            workoutDate = "2026-11-04",
            userId = SeedData.UserId,
            trainerId = SeedData.TrainerId,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var workout = await WorkoutAsAdmin(SeedData.TrainedWorkoutId);
        ShouldBePlacedIn(workout, null, null);
        ShouldBeDated(workout, "2026-11-04");
    }

    [Fact]
    public async Task Update_ShouldRejectUnlinkingWithoutADateAndChangeNothing()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync(TrainedWorkoutRoute, new
        {
            name = "Renamed day",
            userId = SeedData.UserId,
            trainerId = SeedData.TrainerId,
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var workout = await WorkoutAsAdmin(SeedData.TrainedWorkoutId);
        ShouldBePlacedIn(workout, SeedData.MesocycleId, 1);
        ShouldBeDated(workout, "2026-09-30");
    }

    [Fact]
    public async Task Update_ShouldKeepTheDateWhenThePlacementIsRefused()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PutAsJsonAsync(StartedWorkoutRoute, AnUpdate(SeedData.MesocycleId, 3));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ShouldBeDated(await WorkoutAsAdmin(SeedData.PrescribedStartedWorkoutId), "2026-10-07");
    }
}
