using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.WorkoutTemplates;

/// <summary>
/// Adding, updating, removing and reordering the exercises of a template, and grouping them into superseries.
/// The access matrix lives in <see cref="WorkoutTemplateAuthEndpointsTests"/>.
/// </summary>
public class WorkoutTemplateContentEndpointsTests : WorkoutTemplateEndpointTest
{
    private static readonly string ExercisesRoute = $"{SeededTemplateRoute}/exercises";

    private static readonly string SuperSeriesRoute = $"{SeededTemplateRoute}/superseries";

    public WorkoutTemplateContentEndpointsTests(ApiFactory factory) : base(factory)
    { }

    [Fact]
    public async Task PostExercise_ShouldAppendTheExercise()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(ExercisesRoute, AnExerciseBody(SeedData.CurlBicepsId, "MID", Reps(3, 10)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.AbsolutePath.Should().Be(SeededTemplateRoute);
        ExerciseIds(await TemplateAsAdmin(SeedData.TrainerTemplateId))
            .Should().Equal(SeedData.SentadillaId, SeedData.PressBancaId, SeedData.PlanchaId, SeedData.CurlBicepsId);
    }

    [Fact]
    public async Task PostExercise_ShouldAcceptTheEffortByNumber()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(ExercisesRoute, AnExerciseBody(SeedData.CurlBicepsId, 3, Reps(3, 10)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await Body(response)).GetProperty("effort").GetString().Should().Be("MID");
    }

    [Fact]
    public async Task PostExercise_ShouldRejectEmptySets()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(ExercisesRoute, AnExerciseBody(SeedData.CurlBicepsId, "MID"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await SeededTemplateShouldBeUnchanged();
    }

    [Fact]
    public async Task PostExercise_ShouldRejectAnExerciseAlreadyInTheTemplate()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(ExercisesRoute, AnExerciseBody(SeedData.PlanchaId, "MID", Reps(3, 10)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await SeededTemplateShouldBeUnchanged();
    }

    [Fact]
    public async Task PostExercise_ShouldRejectAnUnknownExercise()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(ExercisesRoute, AnExerciseBody(Guid.NewGuid(), "MID", Reps(3, 10)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await SeededTemplateShouldBeUnchanged();
    }

    [Fact]
    public async Task PostExercise_ShouldAcceptACustomExerciseOfTheTemplateTrainer()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var custom = await GivenCustomExercise(trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(ExercisesRoute, AnExerciseBody(custom, "MID", Reps(3, 10)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        ExerciseIds(await TemplateAsAdmin(SeedData.TrainerTemplateId)).Should().Contain(custom);
    }

    [Fact]
    public async Task PostExercise_ShouldRejectSomeoneElsesCustomExerciseForAnAdmin()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);
        var custom = await GivenCustomExercise(user);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(ExercisesRoute, AnExerciseBody(custom, "MID", Reps(3, 10)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await SeededTemplateShouldBeUnchanged();
    }

    [Fact]
    public async Task PostExercise_ShouldRejectAnArchivedExercise()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var custom = await GivenCustomExercise(trainer);
        (await trainer.DeleteAsync($"/exercises/{custom}")).EnsureSuccessStatusCode();

        // Act
        var response = await trainer.PostAsJsonAsync(ExercisesRoute, AnExerciseBody(custom, "MID", Reps(3, 10)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await SeededTemplateShouldBeUnchanged();
    }

    [Fact]
    public async Task PutExercise_ShouldKeepAnEntryEditableAfterItsExerciseIsArchived()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var custom = await GivenCustomExercise(trainer);
        (await trainer.PostAsJsonAsync(ExercisesRoute, AnExerciseBody(custom, "MID", Reps(3, 10)))).EnsureSuccessStatusCode();
        (await trainer.DeleteAsync($"/exercises/{custom}")).EnsureSuccessStatusCode();

        // Act
        var response = await trainer.PutAsJsonAsync($"{ExercisesRoute}/{custom}", AnExerciseBody(custom, "HIGH", Reps(2, 12)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PutExercise_ShouldReplaceTheSetsAndKeepThePosition()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync($"{ExercisesRoute}/{SeedData.PlanchaId}", AnExerciseBody(SeedData.PlanchaId, "MID", Reps(2, 15)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var plancha = (await TemplateAsAdmin(SeedData.TrainerTemplateId)).GetProperty("exercises")[2];
        plancha.GetProperty("exerciseId").GetGuid().Should().Be(SeedData.PlanchaId);
        plancha.GetProperty("id").GetGuid().Should().Be(SeedData.TrainerTemplatePlanchaEntryId);
        plancha.GetProperty("sets").GetArrayLength().Should().Be(1);
        plancha.GetProperty("sets")[0].GetProperty("count").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task PutExercise_ShouldRejectABodyNamingAnotherExercise()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync($"{ExercisesRoute}/{SeedData.PlanchaId}", AnExerciseBody(SeedData.SentadillaId, "MID", Reps(4, 6)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await SeededTemplateShouldBeUnchanged();
    }

    [Fact]
    public async Task PutExercise_ShouldRejectANewTotalInsideASuperSeries()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync($"{ExercisesRoute}/{SeedData.SentadillaId}", AnExerciseBody(SeedData.SentadillaId, "MID", Reps(3, 6)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await SeededTemplateShouldBeUnchanged();
    }

    [Fact]
    public async Task PutExercise_ShouldRejectAnExerciseNotInTheTemplate()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync($"{ExercisesRoute}/{SeedData.CurlBicepsId}", AnExerciseBody(SeedData.CurlBicepsId, "MID", Reps(3, 10)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteExercise_ShouldUngroupTheRemainingMemberOfThePair()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.DeleteAsync($"{ExercisesRoute}/{SeedData.SentadillaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var stored = await TemplateAsAdmin(SeedData.TrainerTemplateId);
        ExerciseIds(stored).Should().Equal(SeedData.PressBancaId, SeedData.PlanchaId);
        SuperSeries(stored).Should().OnlyContain(superSeries => superSeries == null);
    }

    [Fact]
    public async Task DeleteExercise_ShouldRejectAnExerciseNotInTheTemplate()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.DeleteAsync($"{ExercisesRoute}/{SeedData.CurlBicepsId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await SeededTemplateShouldBeUnchanged();
    }

    [Fact]
    public async Task PutOrder_ShouldReorderTheExercises()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var order = new[] { SeedData.PlanchaId, SeedData.PressBancaId, SeedData.SentadillaId };

        // Act
        var response = await trainer.PutAsJsonAsync($"{ExercisesRoute}/order", new { exerciseIds = order });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ExerciseIds(await Body(response)).Should().Equal(order);
        ExerciseIds(await TemplateAsAdmin(SeedData.TrainerTemplateId)).Should().Equal(order);
    }

    [Fact]
    public async Task PutOrder_ShouldRejectAnOrderMissingAnExercise()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync($"{ExercisesRoute}/order", new { exerciseIds = new[] { SeedData.PlanchaId, SeedData.SentadillaId } });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await SeededTemplateShouldBeUnchanged();
    }

    [Fact]
    public async Task PostSuperSeries_ShouldGroupTheEntries()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var curl = await Body(await trainer.PostAsJsonAsync(ExercisesRoute, AnExerciseBody(SeedData.CurlBicepsId, "MID", Reps(3, 10))));

        // Act
        var response = await trainer.PostAsJsonAsync(SuperSeriesRoute, new
        {
            exerciseIds = new[] { SeedData.TrainerTemplatePlanchaEntryId, curl.GetProperty("id").GetGuid() },
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var superSeriesId = (await Body(response)).GetProperty("id").GetInt32();
        SuperSeries(await TemplateAsAdmin(SeedData.TrainerTemplateId))
            .Should().Equal(SeedData.SeededSuperSeries, SeedData.SeededSuperSeries, superSeriesId, superSeriesId);
    }

    [Fact]
    public async Task PostSuperSeries_ShouldRejectEntriesWithDifferentTotals()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var curl = await Body(await trainer.PostAsJsonAsync(ExercisesRoute, AnExerciseBody(SeedData.CurlBicepsId, "MID", Reps(5, 10))));

        // Act
        var response = await trainer.PostAsJsonAsync(SuperSeriesRoute, new
        {
            exerciseIds = new[] { SeedData.TrainerTemplatePlanchaEntryId, curl.GetProperty("id").GetGuid() },
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostSuperSeries_ShouldRejectAnEntryAlreadyGrouped()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(SuperSeriesRoute, new
        {
            exerciseIds = new[] { SeedData.TrainerTemplateSentadillaEntryId, SeedData.TrainerTemplatePlanchaEntryId },
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await SeededTemplateShouldBeUnchanged();
    }

    [Fact]
    public async Task DeleteSuperSeries_ShouldUngroupTheEntriesAndKeepThem()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.DeleteAsync($"{SuperSeriesRoute}/{SeedData.SeededSuperSeries}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var stored = await TemplateAsAdmin(SeedData.TrainerTemplateId);
        ExerciseIds(stored).Should().Equal(SeedData.SentadillaId, SeedData.PressBancaId, SeedData.PlanchaId);
        SuperSeries(stored).Should().OnlyContain(superSeries => superSeries == null);
    }

    [Fact]
    public async Task DeleteSuperSeries_ShouldRejectAnUnknownSuperSeries()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.DeleteAsync($"{SuperSeriesRoute}/7");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Content_ShouldStayEditableAfterAWorkoutFromItHasStarted()
    {
        // Arrange
        var workoutId = await GivenWorkoutFromSeededTemplate();
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        (await admin.PutAsJsonAsync($"/workouts/{workoutId}/status", new { status = "STARTED" })).EnsureSuccessStatusCode();
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.DeleteAsync($"{ExercisesRoute}/{SeedData.PlanchaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
