using Microsoft.EntityFrameworkCore;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Persistence;

namespace WeightTracker.Infra.Repositories;

public class CustomerLeadRepository : ICustomerLeadRepository
{
    private readonly WeightTrackerDbContext _context;

    public CustomerLeadRepository(WeightTrackerDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExistsByEmail(Email email, CancellationToken cancellationToken)
        => _context.CustomerLeads.AnyAsync(lead => lead.Email == email.Value, cancellationToken);

    public async Task Add(Id id, Email email, DateTime registeredAtUtc, CancellationToken cancellationToken)
    {
        var entity = new CustomerLeadEntity
        {
            Id = id.Value,
            Email = email.Value,
            RegisteredAtUtc = registeredAtUtc,
        };

        await _context.CustomerLeads.AddAsync(entity, cancellationToken);
    }
}
