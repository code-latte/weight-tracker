using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Application.Common.Dispatcher;
using WeightTracker.Domain.Common;
using Xunit;

namespace WeightTracker.Unit.Application.Common.Dispatcher;

public class EventDispatcherTests
{
    private record FirstEvent : IDomainEvent
    {
        public DateTime OccuredOnUtc { get; } = DateTime.UtcNow;
    }

    private record SecondEvent : IDomainEvent
    {
        public DateTime OccuredOnUtc { get; } = DateTime.UtcNow;
    }

    private record UnhandledEvent : IDomainEvent
    {
        public DateTime OccuredOnUtc { get; } = DateTime.UtcNow;
    }

    private record BaseEvent : IDomainEvent
    {
        public DateTime OccuredOnUtc { get; } = DateTime.UtcNow;
    }

    private record DerivedEvent : BaseEvent;

    private sealed class RecordingHandler<TEvent>(List<string> log, string name) : IEventHandler<TEvent>
        where TEvent : IDomainEvent
    {
        public Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken)
        {
            log.Add($"{name}:{typeof(TEvent).Name}");
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler<TEvent>(Exception exception) : IEventHandler<TEvent>
        where TEvent : IDomainEvent
    {
        public Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken) => throw exception;
    }

    private static ServiceCollection AServiceCollection() => new();

    private static EventDispatcher ADispatcher(ServiceCollection services)
        => new(services.BuildServiceProvider());

    [Fact]
    public async Task DispatchAsync_ShouldRunEveryHandlerOfTheEventType()
    {
        // Arrange
        var log = new List<string>();
        var services = AServiceCollection();
        services.AddSingleton<IEventHandler<FirstEvent>>(new RecordingHandler<FirstEvent>(log, "one"));
        services.AddSingleton<IEventHandler<FirstEvent>>(new RecordingHandler<FirstEvent>(log, "two"));

        // Act
        await ADispatcher(services).DispatchAsync([new FirstEvent()], CancellationToken.None);

        // Assert
        log.Should().BeEquivalentTo("one:FirstEvent", "two:FirstEvent");
    }

    [Fact]
    public async Task DispatchAsync_ShouldNotRunHandlersOfOtherEventTypes()
    {
        // Arrange
        var log = new List<string>();
        var services = AServiceCollection();
        services.AddSingleton<IEventHandler<FirstEvent>>(new RecordingHandler<FirstEvent>(log, "first"));
        services.AddSingleton<IEventHandler<SecondEvent>>(new RecordingHandler<SecondEvent>(log, "second"));

        // Act
        await ADispatcher(services).DispatchAsync([new FirstEvent()], CancellationToken.None);

        // Assert
        log.Should().Equal("first:FirstEvent");
    }

    [Fact]
    public async Task DispatchAsync_ShouldNotRunABaseTypeHandlerForADerivedEvent()
    {
        // Arrange
        var log = new List<string>();
        var services = AServiceCollection();
        services.AddSingleton<IEventHandler<BaseEvent>>(new RecordingHandler<BaseEvent>(log, "base"));

        // Act
        await ADispatcher(services).DispatchAsync([new DerivedEvent()], CancellationToken.None);

        // Assert
        log.Should().BeEmpty();
    }

    [Fact]
    public async Task DispatchAsync_ShouldCompleteWhenTheEventHasNoHandlers()
    {
        // Arrange
        var services = AServiceCollection();

        // Act
        var act = () => ADispatcher(services).DispatchAsync([new UnhandledEvent()], CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DispatchAsync_ShouldHandleEventsInTheOrderGiven()
    {
        // Arrange
        var log = new List<string>();
        var services = AServiceCollection();
        services.AddSingleton<IEventHandler<FirstEvent>>(new RecordingHandler<FirstEvent>(log, "a1"));
        services.AddSingleton<IEventHandler<FirstEvent>>(new RecordingHandler<FirstEvent>(log, "a2"));
        services.AddSingleton<IEventHandler<SecondEvent>>(new RecordingHandler<SecondEvent>(log, "b1"));

        // Act
        await ADispatcher(services).DispatchAsync([new FirstEvent(), new SecondEvent()], CancellationToken.None);

        // Assert
        log.Should().Equal("a1:FirstEvent", "a2:FirstEvent", "b1:SecondEvent");
    }

    [Fact]
    public async Task DispatchAsync_ShouldPropagateTheHandlerExceptionAndStopTheDispatch()
    {
        // Arrange
        var log = new List<string>();
        var failure = new InvalidOperationException("boom");
        var services = AServiceCollection();
        services.AddSingleton<IEventHandler<FirstEvent>>(new ThrowingHandler<FirstEvent>(failure));
        services.AddSingleton<IEventHandler<FirstEvent>>(new RecordingHandler<FirstEvent>(log, "later"));
        services.AddSingleton<IEventHandler<SecondEvent>>(new RecordingHandler<SecondEvent>(log, "next"));

        // Act
        var act = () => ADispatcher(services).DispatchAsync([new FirstEvent(), new SecondEvent()], CancellationToken.None);

        // Assert
        var thrown = await act.Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.Should().BeSameAs(failure);
        log.Should().BeEmpty();
    }
}
