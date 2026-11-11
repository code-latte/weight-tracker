using WeightTracker.Application.UseCases.CustomerLeads;
using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Interfaces;

/// <summary>
/// Stores customer leads. A lead is a non-domain record with no aggregate: it is written once and only read back as
/// an administrator's list, so the contract takes value objects and returns <see cref="CustomerLeadView"/> instead of
/// an aggregate, and has no mapper. None of the writes commits; the use case calls the unit of work.
/// </summary>
public interface ICustomerLeadRepository
{
    /// <summary>
    /// Whether a lead with this email is already stored.
    /// </summary>
    public Task<bool> ExistsByEmail(Email email, CancellationToken cancellationToken);

    /// <summary>
    /// Every stored lead, newest registration first and by email when two share the same time.
    /// </summary>
    public Task<IReadOnlyList<CustomerLeadView>> GetAll(CancellationToken cancellationToken);

    /// <summary>
    /// Stages a new lead. A concurrent insert of the same email surfaces on commit as
    /// <see cref="Exceptions.CustomerLeadAlreadyExistsException"/>.
    /// </summary>
    public Task Add(Id id, Email email, DateTime registeredAtUtc, CancellationToken cancellationToken);
}
