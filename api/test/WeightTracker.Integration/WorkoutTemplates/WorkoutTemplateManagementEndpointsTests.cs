using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.WorkoutTemplates;

/// <summary>
/// Creating, reading, listing, renaming and deleting workout templates. The access matrix lives in
/// <see cref="WorkoutTemplateAuthEndpointsTests"/>.
/// </summary>
public class WorkoutTemplateManagementEndpointsTests : WorkoutTemplateEndpointTest
{
    public WorkoutTemplateManagementEndpointsTests(ApiFactory factory) : base(factory)
    { }

    [Fact]
    public async Task Post_ShouldCreateAnEmptyTemplateOwnedByTheTrainer()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(TemplatesRoute, new { name = "  Upper body  ", description = "Push and pull" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await Body(response);
        var id = body.GetProperty("id").GetGuid();
        response.Headers.Location!.AbsolutePath.Should().Be($"{TemplatesRoute}/{id}");
        body.GetProperty("name").GetString().Should().Be("Upper body");
        body.GetProperty("description").GetString().Should().Be("Push and pull");
        body.GetProperty("trainerId").GetGuid().Should().Be(SeedData.TrainerId);
        body.GetProperty("exercises").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Post_ShouldLeaveTheDescriptionEmptyWhenOmitted()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(TemplatesRoute, new { name = "Upper body" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await Body(response)).GetProperty("description").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
    }

    [Fact]
    public async Task Post_ShouldRejectATrainerIdFromATrainer()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(TemplatesRoute, new { name = "Upper body", trainerId = SeedData.TrainerId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await TemplateCountAsAdmin()).Should().Be(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Post_ShouldRejectABlankName(string name)
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(TemplatesRoute, new { name });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await TemplateCountAsAdmin()).Should().Be(1);
    }

    [Fact]
    public async Task Post_ShouldRejectANameLongerThan256Characters()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(TemplatesRoute, new { name = new string('a', 257) });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldLetAnAdminCreateATemplateForATrainerWithoutOwningIt()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(TemplatesRoute, new { name = "Upper body", trainerId = SeedData.TrainerId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await Body(response)).GetProperty("trainerId").GetGuid().Should().Be(SeedData.TrainerId);
    }

    [Fact]
    public async Task Post_ShouldRequireATrainerIdFromAnAdmin()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(TemplatesRoute, new { name = "Upper body" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await TemplateCountAsAdmin()).Should().Be(1);
    }

    public static TheoryData<string> InvalidTrainers() => new()
    {
        SeedData.AdminId.ToString(),
        SeedData.UserId.ToString(),
        Guid.NewGuid().ToString(),
    };

    [Theory]
    [MemberData(nameof(InvalidTrainers))]
    public async Task Post_ShouldRejectAnAdminNamingSomeoneWhoIsNotATrainer(string trainerId)
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(TemplatesRoute, new { name = "Upper body", trainerId = Guid.Parse(trainerId) });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await TemplateCountAsAdmin()).Should().Be(1);
    }

    [Fact]
    public async Task Post_ShouldRejectADeactivatedTrainer()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        await GivenDeactivated(SeedData.TrainerId);

        // Act
        var response = await admin.PostAsJsonAsync(TemplatesRoute, new { name = "Upper body", trainerId = SeedData.TrainerId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await TemplateCountAsAdmin()).Should().Be(1);
    }

    [Fact]
    public async Task GetById_ShouldReturnTheTemplateWithItsExercisesInOrder()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.GetAsync(SeededTemplateRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await Body(response);
        body.GetProperty("id").GetGuid().Should().Be(SeedData.TrainerTemplateId);
        body.GetProperty("name").GetString().Should().Be(SeedData.TrainerTemplateName);
        body.GetProperty("description").GetString().Should().Be(SeedData.TrainerTemplateDescription);
        body.GetProperty("trainerId").GetGuid().Should().Be(SeedData.TrainerId);
        ExerciseIds(body).Should().Equal(SeedData.SentadillaId, SeedData.PressBancaId, SeedData.PlanchaId);
        SuperSeries(body).Should().Equal(SeedData.SeededSuperSeries, SeedData.SeededSuperSeries, null);
        var sentadilla = body.GetProperty("exercises")[0];
        sentadilla.GetProperty("effort").GetString().Should().Be("HIGH");
        sentadilla.GetProperty("perSide").GetBoolean().Should().BeTrue();
        sentadilla.GetProperty("restMinutes").GetDecimal().Should().Be(2m);
        body.TryGetProperty("userId", out _).Should().BeFalse();
        body.TryGetProperty("status", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Put_ShouldReplaceNameAndDescriptionAndKeepTheExercises()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync(SeededTemplateRoute, new { name = "Full body B" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await TemplateAsAdmin(SeedData.TrainerTemplateId);
        stored.GetProperty("name").GetString().Should().Be("Full body B");
        stored.GetProperty("description").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
        ExerciseIds(stored).Should().Equal(SeedData.SentadillaId, SeedData.PressBancaId, SeedData.PlanchaId);
    }

    [Fact]
    public async Task Put_ShouldLetAnAdminRenameATrainersTemplateAndKeepItsTrainer()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PutAsJsonAsync(SeededTemplateRoute, new { name = "Renamed by admin" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await Body(response);
        body.GetProperty("name").GetString().Should().Be("Renamed by admin");
        body.GetProperty("trainerId").GetGuid().Should().Be(SeedData.TrainerId);
    }

    [Fact]
    public async Task Put_ShouldRejectABlankNameAndChangeNothing()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PutAsJsonAsync(SeededTemplateRoute, new { name = " " });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await SeededTemplateShouldBeUnchanged();
    }

    [Fact]
    public async Task Delete_ShouldRemoveTheTemplate()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.DeleteAsync(SeededTemplateRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await trainer.GetAsync(SeededTemplateRoute)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_ShouldListOnlyTheTrainersOwnTemplates()
    {
        // Arrange
        var (otherTrainerId, _) = await GivenUser(Role.TRAINER);
        var otherTemplate = await GivenTemplateFor(otherTrainerId);
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.GetAsync(TemplatesRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ids = Ids(await Body(response));
        ids.Should().Equal(SeedData.TrainerTemplateId);
        ids.Should().NotContain(otherTemplate);
    }

    [Fact]
    public async Task GetAll_ShouldIgnoreAnotherTrainersFilterFromATrainer()
    {
        // Arrange
        var (otherTrainerId, _) = await GivenUser(Role.TRAINER);
        var otherTemplate = await GivenTemplateFor(otherTrainerId);
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.GetAsync($"{TemplatesRoute}?trainerId={otherTrainerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ids = Ids(await Body(response));
        ids.Should().Equal(SeedData.TrainerTemplateId);
        ids.Should().NotContain(otherTemplate);
    }

    [Fact]
    public async Task GetAll_ShouldListEveryTemplateForAnAdmin()
    {
        // Arrange
        var (otherTrainerId, _) = await GivenUser(Role.TRAINER);
        var otherTemplate = await GivenTemplateFor(otherTrainerId);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.GetAsync(TemplatesRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Ids(await Body(response)).Should().BeEquivalentTo([SeedData.TrainerTemplateId, otherTemplate]);
    }

    [Fact]
    public async Task GetAll_ShouldFilterByTrainerForAnAdmin()
    {
        // Arrange
        var (otherTrainerId, _) = await GivenUser(Role.TRAINER);
        var otherTemplate = await GivenTemplateFor(otherTrainerId);
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.GetAsync($"{TemplatesRoute}?trainerId={otherTrainerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ids = Ids(await Body(response));
        ids.Should().Equal(otherTemplate);
        ids.Should().NotContain(SeedData.TrainerTemplateId);
    }
}
