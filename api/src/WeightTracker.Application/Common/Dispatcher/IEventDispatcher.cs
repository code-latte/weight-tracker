using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Common.Dispatcher;

/// <summary>
/// Delivers domain events to the handlers declared for them.
/// </summary>
/// <remarks>
/// Events are handled one at a time, in the order given. A handler failure propagates unchanged
/// and stops the dispatch. Dispatching does not clear the events of the aggregate they came from.
/// </remarks>
public interface IEventDispatcher
{
    public Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken);
}
