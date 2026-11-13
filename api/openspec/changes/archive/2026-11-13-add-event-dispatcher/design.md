# Design

## Context

- `WeightTracker.Domain/Common` defines `IDomainEvent` (`OccuredOnUtc`), and `AggregateRoot` collects events through `AddDomainEvent`, `DomainEvents` and `ClearDomainEvents`. No code calls `AddDomainEvent`.
- `WeightTracker.Application` references only Domain and has no package references. Its shared contracts live in `Application/Common` (`IUnitOfWork`).
- All DI registration happens in `src/WeightTracker.Api/Program.cs`, one explicit `AddScoped` / `AddSingleton` line per service, with no scanning today.
- `test/WeightTracker.Unit` references Domain, Application and Infra, but not Api.

See `proposal.md` for the motivation and `specs/domain-event-dispatch/spec.md` for the required behavior.

## Goals / Non-Goals

**Goals:**
- Keep Application free of DI packages; only the composition root knows about `IServiceCollection`.
- Call handlers through a typed generic call, not reflection `Invoke`, so a handler's exception reaches the caller unchanged.

**Non-Goals:**
- Guaranteeing the order of handlers *within* one event. The spec fixes event order only; handlers of the same event must not depend on each other.
- Changing how any existing service is registered. Only handlers use scanning.

## Layer Mapping

| Layer | Change |
| --- | --- |
| Domain | None. `IDomainEvent` is reused as is. |
| Application | `Common/Dispatcher/IEventHandler.cs`, `IEventDispatcher.cs`, `EventDispatcher.cs` (namespace `WeightTracker.Application.Common.Dispatcher`). |
| Infrastructure | None. Its assembly is scanned so future infrastructure handlers are picked up. |
| Api | `Common/EventHandlerRegistration.cs` (namespace `WeightTracker.Api.Common`), and one call in `Program.cs`. |

Dependencies between layers: Application → Domain (`IDomainEvent`), and Api → Application (contracts, `EventDispatcher`). No new dependency between layers.

## Decisions

### 1. Contracts

```csharp
public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}

public interface IEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken);
}
```

The constraint is `IDomainEvent`, so there is one event contract in the system. The dispatcher takes a collection because aggregates expose `DomainEvents` as a collection.

*Alternative:* a separate `IEvent` in Application. Rejected because it would duplicate `IDomainEvent` with no consumer that needs the difference.

### 2. Exact-type resolution through `IServiceProvider`

For each event, `EventDispatcher` builds `IEnumerable<IEventHandler<T>>`, where `T` is `domainEvent.GetType()`, and resolves it with `IServiceProvider.GetService`. Microsoft DI returns every registration of that closed interface, or an empty sequence. `IServiceProvider` is in `System`, so Application needs no package.

Matching is by exact type because the container resolves `IEventHandler<D>` registrations only. Contravariance (`in TEvent`) makes the C# types assignable, but the container does not use it to find `IEventHandler<B>` registrations, which matches the spec's exclusion of base-type handlers.

### 3. Typed invocation through a cached generic invoker

A private nested `HandlerInvoker<TEvent> : HandlerInvoker` resolves handlers and awaits `handler.HandleAsync((TEvent)domainEvent, ct)` with a normal typed call. One invoker per event type is created with `Activator.CreateInstance` and cached in a static `ConcurrentDictionary<Type, HandlerInvoker>`.

*Alternatives:*
- `MethodInfo.Invoke` wraps handler exceptions in `TargetInvocationException`, which breaks the "same error" requirement and needs unwrapping.
- `dynamic` pulls in the runtime binder and hides type errors until runtime.

### 4. Sequential loop with no error handling

The dispatcher uses plain `foreach` over events and, inside it, `foreach` over handlers, awaiting each call. There is no `try`/`catch`, so the first exception ends the dispatch. This implements the ordering and propagation requirements with no extra code.

### 5. Scanning in Api (`AddEventHandlers`)

```csharp
public static IServiceCollection AddEventHandlers(this IServiceCollection services, params Assembly[] assemblies)
```

- Registers `IEventDispatcher -> EventDispatcher` as scoped.
- For each type in each assembly that is a class, not abstract and not a generic type definition, and for each implemented interface that is a closed `IEventHandler<>`, it calls `services.AddScoped(interfaceType, implementationType)`.
- `Program.cs` calls it next to the other service registrations, passing `typeof(IUnitOfWork).Assembly` (Application) and `typeof(UnitOfWork).Assembly` (Infra). These are existing types used as assembly markers.

Handlers are scoped so they can depend on repositories and `IUnitOfWork`, which are scoped. The dispatcher is scoped so the `IServiceProvider` injected into it is the request scope.

*Alternatives:*
- Scrutor: rejected; the scan is about 15 lines and the project avoids extra packages.
- Placing `AddEventHandlers` in Application: rejected by the user, because Application would need `Microsoft.Extensions.DependencyInjection.Abstractions`.

### 6. Tests

- `test/WeightTracker.Unit/Application/Common/Dispatcher/EventDispatcherTests.cs` builds a `ServiceCollection` by hand with test-only events and recording handlers, then resolves `EventDispatcher` from it. `ServiceCollection` is already available to the unit project through its Infra reference. These tests cover every dispatch requirement in the spec.
- `test/WeightTracker.Unit/Api/Common/EventHandlerRegistrationTests.cs` calls `AddEventHandlers(typeof(EventHandlerRegistrationTests).Assembly)` and checks the registrations: multi-interface, abstract and open generic skipped, dispatcher resolvable. It also runs one end-to-end check that a scanned handler runs on dispatch. This needs `WeightTracker.Unit.csproj` to reference `WeightTracker.Api`.
- Style follows `test/WeightTracker.Unit/Application/Services/*Tests.cs`: FluentAssertions, `[Fact]`, `Method_Should...` names, and `// Arrange` / `// Act` / `// Assert` with one Act.
- No integration tests, since no route changes. The full suite still runs because `Program.cs` changes.

### 7. Invariants text

Replace the "Domain events: dormant" section of `.stpr/template/STPR_INVARIANTS.md` so it states:
- A dispatcher exists: `IEventDispatcher`, with handlers declared through `IEventHandler<T>` and registered automatically by `AddEventHandlers`. Handlers must never be registered by hand.
- No aggregate raises events and no use case dispatches them yet.
- `AddDomainEvent` still must not be called until a change wires dispatch into use cases, and that change must also decide when events are cleared.

## Risks / Trade-offs

- **[Risk]** A handler placed in an assembly that is not scanned silently never runs. → Mitigation: only Application and Infra may hold handlers, and the invariants text says so.
- **[Risk]** A handler class implementing two event interfaces gets a separate instance per interface in the same scope. → Acceptable: handlers must be stateless. The design notes this; no shared-instance registration is added.
- **[Risk]** Unit tests that scan the whole test assembly also pick up test handlers declared for other tests. → Mitigation: each test uses its own event types, so unrelated handlers are never matched.
- **[Trade-off]** Referencing Api from the unit project couples the two. → Accepted as the cost of keeping scanning out of Application. Only `EventHandlerRegistration` is exercised from it.

## Migration Plan

No data or deployment steps. Rollback is a code revert: no events are raised, so removing the dispatcher affects no behavior.
