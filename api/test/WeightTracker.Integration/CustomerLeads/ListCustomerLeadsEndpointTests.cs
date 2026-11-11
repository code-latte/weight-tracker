using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.CustomerLeads;

public class ListCustomerLeadsEndpointTests : IntegrationTest
{
    private const string Route = "/customer-leads";

    private const string LeadEmail = "lead@example.com";

    public ListCustomerLeadsEndpointTests(ApiFactory factory) : base(factory)
    { }

    private async Task Register(string email)
    {
        var registered = await Client.PostAsJsonAsync(Route, new { email });
        registered.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static async Task<List<JsonElement>> Items(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        return body.EnumerateArray().ToList();
    }

    [Fact]
    public async Task Get_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await Client.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_ShouldForbidAnAthleteAndReturnNoLeads()
    {
        // Arrange
        await Register(LeadEmail);
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.Content.ReadAsStringAsync()).Should().NotContain(LeadEmail);
    }

    [Fact]
    public async Task Get_ShouldForbidATrainerAndReturnNoLeads()
    {
        // Arrange
        await Register(LeadEmail);
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.Content.ReadAsStringAsync()).Should().NotContain(LeadEmail);
    }

    [Fact]
    public async Task Get_ShouldReturnAnEmptyListForAnAdminWhenNoLeadIsRegistered()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Items(response)).Should().BeEmpty();
    }

    [Fact]
    public async Task Get_ShouldListEveryLeadNewestFirstForAnAdmin()
    {
        // Arrange
        var before = DateTime.UtcNow;
        await Register("  First@Example.COM ");
        await Register("second@example.com");
        var after = DateTime.UtcNow;
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await Items(response);
        items.Select(item => item.GetProperty("email").GetString())
            .Should().Equal("second@example.com", "first@example.com");
        items.Should().OnlyContain(item => item.EnumerateObject().Select(property => property.Name)
            .OrderBy(name => name).SequenceEqual(new[] { "email", "registeredAtUtc" }));

        var registeredAt = items.Select(item => item.GetProperty("registeredAtUtc").GetDateTime()).ToList();
        registeredAt.Should().OnlyContain(time => time.Kind == DateTimeKind.Utc
            && time >= before.AddSeconds(-1) && time <= after.AddSeconds(1));
        registeredAt[0].Should().BeAfter(registeredAt[1]);
    }
}
