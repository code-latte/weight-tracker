using FluentAssertions;
using WeightTracker.Domain.Common;
using Xunit;

namespace WeightTracker.Unit.Domain.Common;

public class AggregateRootTests
{
    private sealed record SomethingHappened(DateTime OccuredOnUtc) : IDomainEvent;

    private sealed class Probe : AggregateRoot
    {
        public void Raise(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);
    }

    private static readonly DateTime OccurredOn = new(2026, 11, 13, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void PullDomainEvents_ShouldReturnTheRaisedEventsInOrderAndClearThem()
    {
        // Arrange
        var probe = new Probe();
        var first = new SomethingHappened(OccurredOn);
        var second = new SomethingHappened(OccurredOn.AddMinutes(1));
        probe.Raise(first);
        probe.Raise(second);

        // Act
        var pulled = probe.PullDomainEvents();

        // Assert
        pulled.Should().Equal(first, second);
        probe.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void PullDomainEvents_ShouldReturnNothingOnASecondPull()
    {
        // Arrange
        var probe = new Probe();
        probe.Raise(new SomethingHappened(OccurredOn));
        probe.PullDomainEvents();

        // Act
        var pulled = probe.PullDomainEvents();

        // Assert
        pulled.Should().BeEmpty();
    }
}
