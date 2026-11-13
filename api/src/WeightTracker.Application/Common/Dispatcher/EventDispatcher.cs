using System.Collections.Concurrent;
using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Common.Dispatcher;

/// <summary>
/// Runs every <see cref="IEventHandler{TEvent}"/> registered for the exact runtime type of each event.
/// </summary>
/// <remarks>
/// Handlers are resolved from the container, so they share the scope of the caller. There is no
/// error handling on purpose: the first handler exception reaches the caller and ends the dispatch.
/// </remarks>
public class EventDispatcher : IEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, HandlerInvoker> Invokers = new();

    private readonly IServiceProvider _serviceProvider;

    public EventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in events)
        {
            var invoker = Invokers.GetOrAdd(domainEvent.GetType(), CreateInvoker);
            await invoker.InvokeAsync(_serviceProvider, domainEvent, cancellationToken);
        }
    }

    private static HandlerInvoker CreateInvoker(Type eventType)
    {
        var invokerType = typeof(HandlerInvoker<>).MakeGenericType(eventType);
        return (HandlerInvoker)Activator.CreateInstance(invokerType)!;
    }

    private abstract class HandlerInvoker
    {
        public abstract Task InvokeAsync(
            IServiceProvider serviceProvider, IDomainEvent domainEvent, CancellationToken cancellationToken);
    }

    private sealed class HandlerInvoker<TEvent> : HandlerInvoker where TEvent : IDomainEvent
    {
        public override async Task InvokeAsync(
            IServiceProvider serviceProvider, IDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            var handlers = serviceProvider.GetService(typeof(IEnumerable<IEventHandler<TEvent>>))
                as IEnumerable<IEventHandler<TEvent>> ?? [];

            foreach (var handler in handlers)
            {
                await handler.HandleAsync((TEvent)domainEvent, cancellationToken);
            }
        }
    }
}
