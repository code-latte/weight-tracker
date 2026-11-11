using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Interfaces;

/// <summary>
/// Stores customer leads. A lead is a non-domain record with no aggregate: it is written once and never read
/// back by the API, so the contract takes value objects instead of an aggregate and has no mapper. None of the
/// writes commits; the use case calls the unit of work.
/// </summary>
public interface ICustomerLeadRepository
{
    /// <summary>
    /// Whether a lead with this email is already stored.
    /// </summary>
    public Task<bool> ExistsByEmail(Email email, CancellationToken cancellationToken);

    /// <summary>
    /// Stages a new lead. A concurrent insert of the same email surfaces on commit as
    /// <see cref="Exceptions.CustomerLeadAlreadyExistsException"/>.
    /// </summary>
    public Task Add(Id id, Email email, DateTime registeredAtUtc, CancellationToken cancellationToken);
}
