using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.OpenApi;

/// <summary>
/// Guards the OpenAPI document transformers.
/// </summary>
/// <remarks>
/// The bearer scheme and the per-operation security requirements are contributed by custom
/// transformers. If either stops working the build still succeeds and the API still serves
/// traffic, but the reference UI silently loses its authentication box, so the contract is
/// asserted here instead.
/// </remarks>
public class OpenApiDocumentTests : IntegrationTest
{
    private const string DocumentPath = "/openapi/v1.json";

    public OpenApiDocumentTests(ApiFactory factory) : base(factory)
    { }

    private async Task<JsonElement> Document()
    {
        var response = await Client.GetAsync(DocumentPath);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
    }

    private static JsonElement Operation(JsonElement document, string path, string method)
        => document.GetProperty("paths").GetProperty(path).GetProperty(method);

    [Fact]
    public async Task Document_ShouldBeServed()
    {
        // Arrange & Act
        var response = await Client.GetAsync(DocumentPath);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Document_ShouldDeclareTheBearerSecurityScheme()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var scheme = document
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer");

        scheme.GetProperty("type").GetString().Should().Be("http");
        scheme.GetProperty("scheme").GetString().Should().Be("bearer");
        scheme.GetProperty("bearerFormat").GetString().Should().Be("JWT");
    }

    [Fact]
    public async Task ProtectedOperation_ShouldCarryAResolvedSecurityRequirement()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var security = Operation(document, "/workouts", "get").GetProperty("security");

        security.GetArrayLength().Should().Be(1);
        security[0].TryGetProperty("Bearer", out var scopes)
            .Should().BeTrue("the requirement must reference the declared scheme, not serialize empty");
        scopes.GetArrayLength().Should().Be(0);
    }

    [Theory]
    [InlineData("/auth/login")]
    [InlineData("/auth/refresh")]
    [InlineData("/customer-leads")]
    public async Task AnonymousOperation_ShouldCarryNoSecurityRequirement(string path)
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        Operation(document, path, "post").TryGetProperty("security", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("/exercises", "get")]
    [InlineData("/exercises/{id}", "get")]
    public async Task AnonymousExerciseOperation_ShouldCarryNoSecurityRequirement(string path, string method)
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        Operation(document, path, method).TryGetProperty("security", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("/exercises", new[] { "get", "post" })]
    [InlineData("/exercises/{id}", new[] { "get", "put", "delete" })]
    public async Task ExercisePath_ShouldDocumentTheReadAndWriteOperationsOnly(string path, string[] operations)
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        document.GetProperty("paths").GetProperty(path).EnumerateObject()
            .Select(property => property.Name)
            .Where(name => name is "get" or "post" or "put" or "patch" or "delete")
            .Should().BeEquivalentTo(operations);
    }

    [Theory]
    [InlineData("/exercises", "get", new[] { "200" })]
    [InlineData("/exercises/{id}", "get", new[] { "200", "404" })]
    public async Task Exercises_ShouldDocumentItsResponses(string path, string method, string[] statuses)
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var responses = Operation(document, path, method).GetProperty("responses");

        responses.EnumerateObject().Select(property => property.Name).Should().Contain(statuses);
    }

    [Fact]
    public async Task Workouts_ShouldDocumentItsResponses()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var responses = Operation(document, "/workouts", "get").GetProperty("responses");

        responses.EnumerateObject().Select(property => property.Name)
            .Should().Contain(["200", "401", "403"]);
    }

    [Fact]
    public async Task DeleteWorkout_ShouldDocumentItsResponses()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var responses = Operation(document, "/workouts/{workoutId}", "delete").GetProperty("responses");

        responses.EnumerateObject().Select(property => property.Name)
            .Should().Contain(["204", "401", "403", "404", "409"]);
    }

    [Fact]
    public async Task RemoveMesocycleWeek_ShouldDocumentItsResponses()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var responses = Operation(document, "/mesocycles/{id}/weeks/{weekNumber}", "delete").GetProperty("responses");

        responses.EnumerateObject().Select(property => property.Name)
            .Should().Contain(["204", "401", "403", "404", "409"]);
    }

    [Fact]
    public async Task ListMesocycleWorkouts_ShouldDocumentTheWorkoutListItemAndItsResponses()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var responses = Operation(document, "/mesocycles/{id}/workouts", "get").GetProperty("responses");
        responses.EnumerateObject().Select(property => property.Name)
            .Should().Contain(["200", "401", "403", "404"]);
        var itemReference = responses.GetProperty("200").GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("items").GetProperty("$ref").GetString();
        itemReference.Should().Be("#/components/schemas/WorkoutListItemResponse");
    }

    [Fact]
    public async Task CreateWorkout_ShouldDocumentItsSecurityAndResponses()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var operation = Operation(document, "/workouts", "post");

        operation.GetProperty("security")[0].TryGetProperty("Bearer", out _).Should().BeTrue();
        operation.GetProperty("responses").EnumerateObject().Select(property => property.Name)
            .Should().Contain(["201", "400", "401", "403", "404"]);
    }

    [Theory]
    [InlineData("/workouts/{workoutId}/exercises", "post")]
    [InlineData("/workouts/{workoutId}/exercises/{exerciseId}", "put")]
    [InlineData("/workouts/{workoutId}/exercises/{exerciseId}", "delete")]
    [InlineData("/workouts/{workoutId}/superseries", "post")]
    [InlineData("/workouts/{workoutId}/superseries/{superSeriesId}", "delete")]
    public async Task WorkoutEditOperation_ShouldDocumentTheConflictOfALockedWorkout(string path, string method)
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        Operation(document, path, method).GetProperty("responses").EnumerateObject().Select(property => property.Name)
            .Should().Contain("409");
    }

    [Fact]
    public async Task ChangeWorkoutStatus_ShouldDocumentItsSecurityAndResponses()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var operation = Operation(document, "/workouts/{workoutId}/status", "put");

        operation.GetProperty("security")[0].TryGetProperty("Bearer", out _).Should().BeTrue();
        operation.GetProperty("responses").EnumerateObject().Select(property => property.Name)
            .Should().Contain(["200", "400", "401", "403", "404", "409"]);
    }

    [Theory]
    [InlineData("get", new[] { "200", "401" })]
    [InlineData("put", new[] { "200", "400", "401" })]
    public async Task MyProfile_ShouldDocumentItsSecurityAndResponses(string method, string[] statuses)
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var operation = Operation(document, "/users/me", method);

        operation.GetProperty("security")[0].TryGetProperty("Bearer", out _).Should().BeTrue();
        operation.GetProperty("responses").EnumerateObject().Select(property => property.Name)
            .Should().Contain(statuses);
    }

    [Theory]
    [InlineData("/exercises", "post", new[] { "201", "400", "401", "403" })]
    [InlineData("/exercises/{id}", "put", new[] { "200", "400", "401", "403", "404", "409" })]
    [InlineData("/exercises/{id}", "delete", new[] { "204", "401", "403", "404", "409" })]
    public async Task ExerciseWrites_ShouldDocumentTheirSecurityAndResponses(string path, string method, string[] statuses)
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var operation = Operation(document, path, method);

        operation.GetProperty("security")[0].TryGetProperty("Bearer", out _).Should().BeTrue();
        operation.GetProperty("responses").EnumerateObject().Select(property => property.Name)
            .Should().Contain(statuses);
    }

    [Theory]
    [InlineData("/exercises", new[] { "200", "401" })]
    [InlineData("/exercises/{id}", new[] { "200", "401", "404" })]
    public async Task ExerciseReads_ShouldStayPublicAndDocumentTheInvalidTokenResponse(string path, string[] statuses)
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var operation = Operation(document, path, "get");

        operation.TryGetProperty("security", out var security).Should().BeFalse(security.ToString());
        operation.GetProperty("responses").EnumerateObject().Select(property => property.Name)
            .Should().Contain(statuses);
    }

    [Fact]
    public async Task Exercise_ShouldDocumentANullableOwnerAndArchiveDate()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var properties = document.GetProperty("components").GetProperty("schemas").GetProperty("ExerciseResponse").GetProperty("properties");

        foreach (var name in new[] { "ownerId", "archivedAtUtc" })
        {
            var property = properties.GetProperty(name).GetRawText();
            property.Should().Match(text => text.Contains("\"null\"") || text.Contains("\"nullable\": true") || text.Contains("\"nullable\":true"));
        }
    }

    [Theory]
    [InlineData("/exercise-favorites", "get", new[] { "200", "401" })]
    [InlineData("/exercise-favorites/{exerciseId}", "put", new[] { "200", "401", "404", "409" })]
    [InlineData("/exercise-favorites/{exerciseId}", "delete", new[] { "204", "401", "404" })]
    public async Task ExerciseFavorites_ShouldDocumentTheirSecurityAndResponses(string path, string method, string[] statuses)
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var operation = Operation(document, path, method);

        operation.GetProperty("security")[0].TryGetProperty("Bearer", out _).Should().BeTrue();
        operation.GetProperty("responses").EnumerateObject().Select(property => property.Name)
            .Should().Contain(statuses);
    }

    [Fact]
    public async Task ListUsers_ShouldDocumentItsSecurityAndResponses()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var operation = Operation(document, "/users", "get");
        var responses = operation.GetProperty("responses").EnumerateObject().Select(property => property.Name).ToList();

        operation.GetProperty("security")[0].TryGetProperty("Bearer", out _).Should().BeTrue();
        responses.Should().Contain(["200", "401", "403"]);
        responses.Should().NotContain("501");
    }

    [Theory]
    [InlineData("/trainer-athletes", "get", new[] { "200", "401", "403" })]
    [InlineData("/trainer-athletes/{trainerId}", "get", new[] { "200", "401", "403", "404" })]
    [InlineData("/trainer-athletes/{trainerId}", "post", new[] { "201", "400", "401", "403", "404", "409" })]
    [InlineData("/trainer-athletes/{trainerId}/end", "put", new[] { "200", "400", "401", "403", "404" })]
    [InlineData("/athlete-invitations", "get", new[] { "200", "401", "403" })]
    [InlineData("/athlete-invitations", "post", new[] { "201", "400", "401", "403", "409" })]
    [InlineData("/athlete-invitations/{invitationId}/revoke", "put", new[] { "200", "401", "403", "404", "409" })]
    [InlineData("/athlete-invitations/{invitationId}/accept", "put", new[] { "200", "400", "401", "403", "404", "409" })]
    [InlineData("/athlete-invitations/{invitationId}/reject", "put", new[] { "200", "400", "401", "403", "404", "409" })]
    public async Task TrainerAthleteOperations_ShouldDocumentTheirSecurityAndResponses(
        string path, string method, string[] statuses)
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var operation = Operation(document, path, method);

        operation.GetProperty("security")[0].TryGetProperty("Bearer", out _).Should().BeTrue();
        operation.GetProperty("responses").EnumerateObject().Select(property => property.Name)
            .Should().Contain(statuses);
    }

    [Fact]
    public async Task CreateUser_ShouldDocumentItsSecurityAndResponses()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var operation = Operation(document, "/users", "post");

        operation.GetProperty("security")[0].TryGetProperty("Bearer", out _).Should().BeTrue();
        operation.GetProperty("responses").EnumerateObject().Select(property => property.Name)
            .Should().Contain(["201", "400", "401", "403", "409"]);
    }

    [Theory]
    [InlineData("/users/{id}/activate")]
    [InlineData("/users/{id}/deactivate")]
    public async Task UserActivation_ShouldDocumentItsSecurityAndResponses(string path)
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var operation = Operation(document, path, "put");

        operation.GetProperty("security")[0].TryGetProperty("Bearer", out _).Should().BeTrue();
        operation.GetProperty("responses").EnumerateObject().Select(property => property.Name)
            .Should().Contain(["200", "401", "403", "404", "409"]);
    }

    [Fact]
    public async Task ListCustomerLeads_ShouldDocumentItsSecurityResponsesAndItemSchema()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var operation = Operation(document, "/customer-leads", "get");

        operation.GetProperty("security")[0].TryGetProperty("Bearer", out _).Should().BeTrue();
        operation.GetProperty("responses").EnumerateObject().Select(property => property.Name)
            .Should().Contain(["200", "401", "403"]);

        var itemReference = operation
            .GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("items").GetProperty("$ref").GetString();
        itemReference.Should().Be("#/components/schemas/CustomerLeadResponse");

        document.GetProperty("components").GetProperty("schemas").GetProperty("CustomerLeadResponse")
            .GetProperty("properties").EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo("email", "registeredAtUtc");
    }

    [Fact]
    public async Task ListUsers_ShouldDocumentANullableTrainerOnEachItem()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var itemReference = Operation(document, "/users", "get")
            .GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("items").GetProperty("$ref").GetString();
        itemReference.Should().Be("#/components/schemas/UserListItemResponse");

        var trainer = document
            .GetProperty("components").GetProperty("schemas").GetProperty("UserListItemResponse")
            .GetProperty("properties").GetProperty("trainer").GetRawText();
        trainer.Should().Contain("#/components/schemas/UserProfileResponse");
        trainer.Should().Match(text => text.Contains("\"null\"") || text.Contains("\"nullable\": true") || text.Contains("\"nullable\":true"));
    }

    [Fact]
    public async Task ListWorkouts_ShouldDocumentTheAthleteAndANullableTrainerOnEachItem()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var itemReference = Operation(document, "/workouts", "get")
            .GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("items").GetProperty("$ref").GetString();
        itemReference.Should().Be("#/components/schemas/WorkoutListItemResponse");

        var item = document.GetProperty("components").GetProperty("schemas").GetProperty("WorkoutListItemResponse");
        item.GetProperty("required").EnumerateArray().Select(name => name.GetString()).Should().Contain("athlete");
        var athlete = item.GetProperty("properties").GetProperty("athlete").GetRawText();
        athlete.Should().Contain("#/components/schemas/UserProfileResponse");
        var trainer = item.GetProperty("properties").GetProperty("trainer").GetRawText();
        trainer.Should().Contain("#/components/schemas/UserProfileResponse");
        trainer.Should().Match(text => text.Contains("\"null\"") || text.Contains("\"nullable\": true") || text.Contains("\"nullable\":true"));

        var createdReference = Operation(document, "/workouts", "post")
            .GetProperty("responses").GetProperty("201").GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString();
        createdReference.Should().Be("#/components/schemas/WorkoutResponse");
    }

    [Fact]
    public async Task GetWorkoutById_ShouldDocumentTheAthleteAndANullableTrainer()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var reference = Operation(document, "/workouts/{id}", "get")
            .GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString();
        reference.Should().Be("#/components/schemas/WorkoutByIdResponse");

        var schema = document.GetProperty("components").GetProperty("schemas").GetProperty("WorkoutByIdResponse");
        schema.GetProperty("required").EnumerateArray().Select(name => name.GetString()).Should().Contain("athlete");
        schema.GetProperty("properties").GetProperty("athlete").GetRawText()
            .Should().Contain("#/components/schemas/UserProfileResponse");
        var trainer = schema.GetProperty("properties").GetProperty("trainer").GetRawText();
        trainer.Should().Contain("#/components/schemas/UserProfileResponse");
        trainer.Should().Match(text => text.Contains("\"null\"") || text.Contains("\"nullable\": true") || text.Contains("\"nullable\":true"));

        var updatedReference = Operation(document, "/workouts/{id}", "put")
            .GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString();
        updatedReference.Should().Be("#/components/schemas/WorkoutDetailResponse");
    }

    [Fact]
    public async Task GetWorkoutById_ShouldDocumentTheCatalogDataOfEachEntryAndANullableMesocycle()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var schemas = document.GetProperty("components").GetProperty("schemas");
        var properties = schemas.GetProperty("WorkoutByIdResponse").GetProperty("properties");
        properties.GetProperty("exercises").GetRawText()
            .Should().Contain("#/components/schemas/WorkoutByIdExerciseResponse");
        schemas.GetProperty("WorkoutByIdExerciseResponse").GetProperty("properties").EnumerateObject()
            .Select(property => property.Name)
            .Should().Contain(["translations", "videoUrl", "muscle", "bodyPart", "type"]);

        var mesocycle = properties.GetProperty("mesocycle").GetRawText();
        mesocycle.Should().Contain("#/components/schemas/WorkoutMesocycleResponse");
        mesocycle.Should().Match(text => text.Contains("\"null\"") || text.Contains("\"nullable\": true") || text.Contains("\"nullable\":true"));
        schemas.GetProperty("WorkoutMesocycleResponse").GetProperty("properties").GetProperty("weeks").GetRawText()
            .Should().Contain("#/components/schemas/MesocycleWeekResponse");
    }

    [Fact]
    public async Task ListWorkouts_ShouldDocumentANullablePlacementOnEachItem()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var properties = document.GetProperty("components").GetProperty("schemas")
            .GetProperty("WorkoutListItemResponse").GetProperty("properties");
        var mesocycleId = properties.GetProperty("mesocycleId").GetRawText();
        mesocycleId.Should().Contain("\"uuid\"");
        mesocycleId.Should().Match(text => text.Contains("\"null\"") || text.Contains("\"nullable\": true") || text.Contains("\"nullable\":true"));
        var weekNumber = properties.GetProperty("mesocycleWeekNumber").GetRawText();
        weekNumber.Should().Contain("\"integer\"");
        weekNumber.Should().Match(text => text.Contains("\"null\"") || text.Contains("\"nullable\": true") || text.Contains("\"nullable\":true"));
    }

    [Fact]
    public async Task UserSummary_ShouldDocumentTheActivationFields()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var properties = document
            .GetProperty("components").GetProperty("schemas").GetProperty("UserSummaryResponse")
            .GetProperty("properties");

        properties.EnumerateObject().Select(property => property.Name)
            .Should().Contain(["isActive", "deactivatedAtUtc"]);
    }

    [Fact]
    public async Task WorkoutDetail_ShouldDocumentTheStatusField()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var properties = document
            .GetProperty("components").GetProperty("schemas").GetProperty("WorkoutDetailResponse")
            .GetProperty("properties");

        properties.EnumerateObject().Select(property => property.Name).Should().Contain("status");
    }

    [Fact]
    public async Task Login_ShouldDocumentItsResponses()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var responses = Operation(document, "/auth/login", "post").GetProperty("responses");

        responses.EnumerateObject().Select(property => property.Name)
            .Should().Contain(["200", "400", "401", "403"]);
    }

    [Fact]
    public async Task Refresh_ShouldDocumentItsFailureResponses()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var responses = Operation(document, "/auth/refresh", "post").GetProperty("responses");

        responses.EnumerateObject().Select(property => property.Name)
            .Should().Contain(["200", "401", "403", "404"]);
    }

    [Fact]
    public async Task SuccessfulLogin_ShouldDocumentTheTokenFieldNames()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var schemaName = Operation(document, "/auth/login", "post")
            .GetProperty("responses").GetProperty("200")
            .GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString()!
            .Split('/')[^1];

        var properties = document
            .GetProperty("components").GetProperty("schemas").GetProperty(schemaName)
            .GetProperty("properties");

        properties.EnumerateObject().Select(property => property.Name)
            .Should().Contain(["accessToken", "refreshToken", "expiresIn"]);
    }

    [Fact]
    public async Task ReferenceUi_ShouldNotBeServedOutsideDevelopment()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/scalar");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
