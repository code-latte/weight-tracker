using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Common.Dispatcher;

/// <summary>
/// Reacts to one type of domain event.
/// </summary>
/// <remarks>
/// Implementing this interface is enough to be run: handlers are discovered and registered
/// automatically, never by hand. A handler only receives events of exactly <typeparamref name="TEvent"/>,
/// not of types derived from it. Handlers of the same event must not depend on each other.
/// </remarks>
public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
{
    public Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
