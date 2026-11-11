namespace WeightTracker.Api.Endpoints.CustomerLeads;

/// <summary>
/// Settings of the <c>RateLimiting:CustomerLeads</c> configuration section: the fixed window that limits
/// how often one client can register leads.
/// </summary>
public class CustomerLeadsRateLimitOptions
{
    public const string SectionName = "RateLimiting:CustomerLeads";

    /// <summary>Name of the rate limiter policy applied to <c>POST /customer-leads</c>.</summary>
    public const string PolicyName = "customer-leads";

    /// <summary>Requests one client may send within a window.</summary>
    public int PermitLimit { get; set; } = 5;

    /// <summary>Length of the window, in seconds.</summary>
    public int WindowSeconds { get; set; } = 60;
}
