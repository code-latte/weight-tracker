using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Persistence;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.CustomerLeads;

public class CustomerLeadEndpointTests : IntegrationTest
{
    public CustomerLeadEndpointTests(ApiFactory factory) : base(factory)
    { }

    private const string Route = "/customer-leads";

    private const string LeadEmail = "lead@example.com";

    private Task<List<CustomerLeadEntity>> StoredLeads()
        => Factory.WithScope(services => services.GetRequiredService<WeightTrackerDbContext>()
            .CustomerLeads.AsNoTracking().ToListAsync());

    /// <summary>
    /// A host of its own with a low limit, so the limiter can be exhausted without affecting the shared
    /// factory every other test uses. It shares the shared factory's database.
    /// </summary>
    private WebApplicationFactory<Program> AFactoryLimitedTo(int permitLimit)
        => Factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:CustomerLeads:PermitLimit"] = permitLimit.ToString(),
                    ["RateLimiting:CustomerLeads:WindowSeconds"] = "3600",
                })));

    [Fact]
    public async Task Register_ShouldStoreANewEmailWithTheServerUtcTime()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var response = await Client.PostAsJsonAsync(Route, new { email = LeadEmail });

        // Assert
        var after = DateTime.UtcNow;
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();

        var stored = (await StoredLeads()).Should().ContainSingle().Subject;
        stored.Email.Should().Be(LeadEmail);
        stored.RegisteredAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        stored.RegisteredAtUtc.Should().BeOnOrAfter(before.AddSeconds(-1)).And.BeOnOrBefore(after.AddSeconds(1));
    }

    [Fact]
    public async Task Register_ShouldStoreTheEmailNormalized()
    {
        // Act
        var response = await Client.PostAsJsonAsync(Route, new { email = "  Lead@Example.COM " });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await StoredLeads()).Should().ContainSingle().Which.Email.Should().Be(LeadEmail);
    }

    public static TheoryData<string, object> InvalidBodies => new()
    {
        { "missing email", new { } },
        { "null email", new { email = (string?)null } },
        { "empty email", new { email = "" } },
        { "no domain", new { email = "lead@" } },
        { "no at sign", new { email = "not-an-email" } },
        { "longer than 254", new { email = new string('a', 250) + "@example.com" } },
    };

    [Theory]
    [MemberData(nameof(InvalidBodies))]
    public async Task Register_ShouldRejectAMissingOrInvalidEmailWithoutStoringIt(string _, object body)
    {
        // Act
        var response = await Client.PostAsJsonAsync(Route, body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await StoredLeads()).Should().BeEmpty();
    }

    [Fact]
    public async Task Register_ShouldAcceptADuplicateInAnotherCasingWithoutStoringItAgain()
    {
        // Arrange
        (await Client.PostAsJsonAsync(Route, new { email = LeadEmail })).EnsureSuccessStatusCode();
        var original = (await StoredLeads()).Single();

        // Act
        var response = await Client.PostAsJsonAsync(Route, new { email = "LEAD@example.com" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var stored = (await StoredLeads()).Should().ContainSingle().Subject;
        stored.Id.Should().Be(original.Id);
        stored.RegisteredAtUtc.Should().Be(original.RegisteredAtUtc);
    }

    [Fact]
    public async Task Register_ShouldIgnoreAClientSuppliedRegistrationTime()
    {
        // Arrange
        var supplied = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var response = await Client.PostAsJsonAsync(Route, new { email = LeadEmail, registeredAtUtc = supplied });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await StoredLeads()).Should().ContainSingle().Which.RegisteredAtUtc.Should().NotBe(supplied);
    }

    [Fact]
    public async Task Register_ShouldIgnoreAnInvalidBearerToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.PostAsJsonAsync(Route, new { email = LeadEmail });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await StoredLeads()).Should().ContainSingle();
    }

    [Fact]
    public async Task Register_ShouldRejectRequestsOverTheLimitWithoutStoringThem()
    {
        // Arrange
        await using var limited = AFactoryLimitedTo(2);
        var client = limited.CreateClient();
        (await client.PostAsJsonAsync(Route, new { email = "first@example.com" })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync(Route, new { email = "second@example.com" })).EnsureSuccessStatusCode();

        // Act
        var response = await client.PostAsJsonAsync(Route, new { email = "third@example.com" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await StoredLeads()).Select(lead => lead.Email).Should()
            .BeEquivalentTo("first@example.com", "second@example.com");
    }

    [Fact]
    public async Task Register_ExhaustedLimitShouldNotLimitOtherEndpoints()
    {
        // Arrange
        await using var limited = AFactoryLimitedTo(1);
        var client = limited.CreateClient();
        (await client.PostAsJsonAsync(Route, new { email = LeadEmail })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync(Route, new { email = "other@example.com" }))
            .StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        // Act
        var response = await client.PostAsJsonAsync(
            "/auth/login", new { email = SeedCredentials.User, password = SeedCredentials.Password });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
