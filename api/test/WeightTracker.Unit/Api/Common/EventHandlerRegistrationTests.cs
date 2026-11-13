using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Api.Common;
using WeightTracker.Application.Common.Dispatcher;
using WeightTracker.Domain.Common;
using Xunit;

namespace WeightTracker.Unit.Api.Common;

public class EventHandlerRegistrationTests
{
    public record ScannedEvent : IDomainEvent
    {
        public DateTime OccuredOnUtc { get; } = DateTime.UtcNow;
    }

    public record SecondScannedEvent : IDomainEvent
    {
        public DateTime OccuredOnUtc { get; } = DateTime.UtcNow;
    }

    public record SkippedEvent : IDomainEvent
    {
        public DateTime OccuredOnUtc { get; } = DateTime.UtcNow;
    }

    public sealed class ScannedEventHandler(List<string> log) : IEventHandler<ScannedEvent>
    {
        public Task HandleAsync(ScannedEvent domainEvent, CancellationToken cancellationToken)
        {
            log.Add(nameof(ScannedEventHandler));
            return Task.CompletedTask;
        }
    }

    public sealed class TwoEventsHandler(List<string> log) : IEventHandler<SecondScannedEvent>, IEventHandler<ScannedEvent>
    {
        public Task HandleAsync(SecondScannedEvent domainEvent, CancellationToken cancellationToken)
        {
            log.Add($"{nameof(TwoEventsHandler)}:{nameof(SecondScannedEvent)}");
            return Task.CompletedTask;
        }

        public Task HandleAsync(ScannedEvent domainEvent, CancellationToken cancellationToken)
        {
            log.Add($"{nameof(TwoEventsHandler)}:{nameof(ScannedEvent)}");
            return Task.CompletedTask;
        }
    }

    public abstract class AbstractHandler : IEventHandler<SkippedEvent>
    {
        public Task HandleAsync(SkippedEvent domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public class OpenGenericHandler<T> : IEventHandler<SkippedEvent>
    {
        public Task HandleAsync(SkippedEvent domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static ServiceCollection AServiceCollectionWithHandlersScanned()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new List<string>());
        services.AddEventHandlers(typeof(EventHandlerRegistrationTests).Assembly);
        return services;
    }

    [Fact]
    public async Task AddEventHandlers_ShouldRunAScannedHandlerWithoutExplicitRegistration()
    {
        // Arrange
        var services = AServiceCollectionWithHandlersScanned();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var log = scope.ServiceProvider.GetRequiredService<List<string>>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();

        // Act
        await dispatcher.DispatchAsync([new ScannedEvent()], CancellationToken.None);

        // Assert
        log.Should().Contain(nameof(ScannedEventHandler));
    }

    [Fact]
    public void AddEventHandlers_ShouldRegisterAHandlerAgainstEveryEventTypeItDeclares()
    {
        // Arrange
        var services = AServiceCollectionWithHandlersScanned();

        // Act
        var registered = services
            .Where(descriptor => descriptor.ImplementationType == typeof(TwoEventsHandler))
            .Select(descriptor => descriptor.ServiceType)
            .ToList();

        // Assert
        registered.Should().BeEquivalentTo(
            [typeof(IEventHandler<ScannedEvent>), typeof(IEventHandler<SecondScannedEvent>)]);
    }

    [Fact]
    public void AddEventHandlers_ShouldSkipAbstractAndOpenGenericHandlers()
    {
        // Arrange
        var services = AServiceCollectionWithHandlersScanned();

        // Act
        var registered = services.Where(descriptor => descriptor.ServiceType == typeof(IEventHandler<SkippedEvent>));

        // Assert
        registered.Should().BeEmpty();
    }

    [Fact]
    public void AddEventHandlers_ShouldRegisterTheDispatcher()
    {
        // Arrange
        var services = AServiceCollectionWithHandlersScanned();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Act
        var dispatcher = scope.ServiceProvider.GetService<IEventDispatcher>();

        // Assert
        dispatcher.Should().BeOfType<EventDispatcher>();
    }
}
