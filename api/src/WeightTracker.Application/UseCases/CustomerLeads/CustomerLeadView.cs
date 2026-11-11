using WeightTracker.Domain.Common;

namespace WeightTracker.Application.UseCases.CustomerLeads;

/// <summary>
/// A stored customer lead as an administrator sees it. Leads have no domain type, so this view is what the repository
/// returns instead of an aggregate.
/// </summary>
public sealed record CustomerLeadView(Email Email, DateTime RegisteredAtUtc);
