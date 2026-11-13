namespace WeightTracker.Domain.Common;

/// <summary>
/// This calss represents the base AggregateRoot for DomainDrivenDesing
/// All changes in model will be through this class
/// </summary>
public abstract class AggregateRoot : Entity
{

    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    protected void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Hands over the events raised so far and clears them, so a caller cannot dispatch them and forget to clear,
    /// or clear them before dispatching.
    /// </summary>
    public IReadOnlyList<IDomainEvent> PullDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();

        return events;
    }

}