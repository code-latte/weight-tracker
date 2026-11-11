using System.ComponentModel.DataAnnotations;

namespace WeightTracker.Api.Endpoints.CustomerLeads.Dtos;

public sealed record RegisterCustomerLeadRequest
{
    [Required]
    public string Email { get; init; } = string.Empty;
}
