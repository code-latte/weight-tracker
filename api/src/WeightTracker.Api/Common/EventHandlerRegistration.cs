using System.Reflection;
using WeightTracker.Application.Common.Dispatcher;

namespace WeightTracker.Api.Common;

/// <summary>
/// Registers the event dispatcher and every event handler found in the given assemblies.
/// </summary>
/// <remarks>
/// Handlers are never registered by hand: declaring a concrete class that implements
/// <see cref="IEventHandler{TEvent}"/> is enough. Abstract classes and open generic types are skipped.
/// </remarks>
public static class EventHandlerRegistration
{
    public static IServiceCollection AddEventHandlers(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddScoped<IEventDispatcher, EventDispatcher>();

        var handlerTypes = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false });

        foreach (var handlerType in handlerTypes)
        {
            var handlerInterfaces = handlerType.GetInterfaces()
                .Where(@interface => @interface.IsGenericType
                    && @interface.GetGenericTypeDefinition() == typeof(IEventHandler<>));

            foreach (var handlerInterface in handlerInterfaces)
            {
                services.AddScoped(handlerInterface, handlerType);
            }
        }

        return services;
    }
}
