namespace WeightTracker.Infra.Entities;

/// <summary>EF representation of the <c>CustomerLeads</c> table.</summary>
public class CustomerLeadEntity
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public DateTime RegisteredAtUtc { get; set; }
}
