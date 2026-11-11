using WeightTracker.Application.UseCases.CustomerLeads;

namespace WeightTracker.Api.Endpoints.CustomerLeads.Dtos;

/// <summary>
/// A customer lead as an administrator lists it: the stored email and the UTC time it was registered.
/// </summary>
public sealed record CustomerLeadResponse(string Email, DateTime RegisteredAtUtc)
{
    public static CustomerLeadResponse From(CustomerLeadView view)
        => new(view.Email.Value, view.RegisteredAtUtc);
}
