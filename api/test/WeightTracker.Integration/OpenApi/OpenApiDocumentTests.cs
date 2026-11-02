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
    [InlineData("/exercises")]
    [InlineData("/exercises/{id}")]
    public async Task ExercisePath_ShouldDocumentOnlyTheReadOperation(string path)
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        // Exercises are a read-only catalog: no write operation is served or documented
        document.GetProperty("paths").GetProperty(path).EnumerateObject()
            .Select(property => property.Name)
            .Where(name => name is "get" or "post" or "put" or "patch" or "delete")
            .Should().Equal("get");
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
