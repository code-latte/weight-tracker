using Microsoft.EntityFrameworkCore;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.CustomerLeads;
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

    public async Task<IReadOnlyList<CustomerLeadView>> GetAll(CancellationToken cancellationToken)
    {
        var entities = await _context.CustomerLeads
            .AsNoTracking()
            .OrderByDescending(lead => lead.RegisteredAtUtc)
            .ThenBy(lead => lead.Email)
            .ToListAsync(cancellationToken);

        return entities
            .Select(lead => new CustomerLeadView(Email.From(lead.Email), lead.RegisteredAtUtc))
            .ToList();
    }

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
