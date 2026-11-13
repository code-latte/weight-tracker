## Change

Add an event dispatcher and auto-registered event handlers (observer pattern) in the Application layer

## Objective

`AggregateRoot` already collects `IDomainEvent`s, but the invariants mark domain events as dormant because nothing delivers them. This change adds the delivery mechanism only: a dispatcher that, given a set of events, runs every handler declared for each event's type.

Handlers must be discovered automatically. Declaring

```csharp
public sealed class SendWelcomeEmail : IEventHandler<SignupEvent> { ... }
```

is enough for it to run whenever a `SignupEvent` is dispatched, with no per-handler registration in `Program.cs`.

No event is raised or dispatched yet. Wiring events into use cases is a later change.

## Capabilities

- Declare a handler for an event type by implementing `IEventHandler<TEvent>`.
- Dispatch one or more events through `IEventDispatcher`, which runs every handler registered for each event's runtime type.
- Register every concrete handler in the DI container with one call, by scanning assemblies.

## Module Classification

- Application: Common/Dispatcher -> cross-cutting application infrastructure (not a domain area, no aggregate, no persistence) -> `src/WeightTracker.Application/Common` (`IUnitOfWork.cs` as the nearest shared-contract reference)
- Api: Common -> handler scanning and registration extension -> `src/WeightTracker.Api/Common` (`ExceptionMapper.cs` as the nearest Api-side shared helper)
- Api: Program.cs -> composition root, one registration call -> `src/WeightTracker.Api/Program.cs`

No domain area is affected. Nothing is catalog-like or mutable here: the dispatcher holds no business state.

## Canonical Template Mapping

- Dispatcher contracts and implementation -> `src/WeightTracker.Application/Common` -> shared Application contracts already live here (`IUnitOfWork`)
- Event type constraint -> `src/WeightTracker.Domain/Common/IDomainEvent.cs` -> reuse the existing event contract instead of introducing a second one
- Handler scanning -> `src/WeightTracker.Api/Common` -> DI is a composition-root concern, so the scanning lives in Api and Application stays free of DI packages
- DI registration -> `src/WeightTracker.Api/Program.cs` -> all services are registered in the composition root
- Unit tests -> `test/WeightTracker.Unit/Application/Services` -> closest existing tests of application-level collaborators; new tests go in `test/WeightTracker.Unit/Application/Common/Dispatcher` and `test/WeightTracker.Unit/Api/Common`

## Design

The contracts and the dispatcher live in `src/WeightTracker.Application/Common/Dispatcher`, namespace `WeightTracker.Application.Common.Dispatcher`. Application gains no new package reference: `IServiceProvider` is part of the base library.

- `IEventHandler<in TEvent> where TEvent : IDomainEvent`
  - `Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken)`
- `IEventDispatcher`
  - `Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken)`
- `EventDispatcher : IEventDispatcher`
  - Takes `IServiceProvider`. For each event, builds `IEventHandler<>` closed over the event's **runtime** type, resolves all registered implementations and awaits `HandleAsync` on each.
- `EventHandlerRegistration` (static extension class) in `src/WeightTracker.Api/Common`, namespace `WeightTracker.Api.Common`
  - `IServiceCollection AddEventHandlers(this IServiceCollection services, params Assembly[] assemblies)`
  - Registers `IEventDispatcher -> EventDispatcher` as scoped.
  - Scans the given assemblies for non-abstract, non-generic classes implementing one or more closed `IEventHandler<T>`, and registers each as scoped against every `IEventHandler<T>` it implements.
- `Program.cs` calls `builder.Services.AddEventHandlers(<Application assembly>, <Infra assembly>)` once. Infra is included so handlers that need infrastructure (email, etc.) can live there.

## Business Rules (dispatch semantics)

- Events are processed in the order given; handlers of one event run sequentially, in registration order, before the next event starts. No parallelism.
- Matching is by exact runtime type. A handler for a base type or interface does not receive derived events.
- An event with no handlers is a no-op, not an error.
- A handler exception propagates to the caller unchanged and stops the dispatch. The dispatcher does not catch, wrap, log or retry.
- Handlers are scoped, so they share the request scope (and the same `IUnitOfWork`) with the use case that dispatches.
- Dispatching does not clear the aggregate's events; that responsibility belongs to whichever change wires dispatch into use cases.

## Roles And Visibility

- N/A. The dispatcher is internal plumbing; it exposes no endpoint.

## Business Identifiers And Mutability

- N/A. No identifiers are introduced or changed.

## Tests

Unit tests use a real `ServiceCollection` with test-only events and handlers (no database, no HTTP). Each test uses labelled Arrange / Act / Assert phases.

`test/WeightTracker.Unit/Application/Common/Dispatcher` (handlers registered by hand, so these tests do not depend on the Api scanning):

- Dispatching an event runs every handler registered for its type.
- Handlers of other event types are not run (assert the exclusion).
- A handler for a base type does not receive a derived event.
- An event with no handlers completes without error.
- Multiple events are handled in the order given.
- A handler exception propagates and later handlers are not run.

`test/WeightTracker.Unit/Api/Common` (scanning the test assembly):

- `AddEventHandlers` registers a class implementing two `IEventHandler<T>` against both interfaces.
- `AddEventHandlers` ignores abstract classes and open generic handlers.
- `AddEventHandlers` registers `IEventDispatcher` so it resolves from the container.

No integration tests: no endpoint behavior changes. Because `Program.cs` changes, the full `dotnet test WeightTracker.slnx` still runs with Docker to confirm the API starts.

## Out Of Scope

- Raising any domain event (`AddDomainEvent` stays unused).
- Calling the dispatcher from any use case, repository or `UnitOfWork`.
- Clearing aggregate events after dispatch.
- Outbox, background, async or retried delivery, and transactional guarantees between save and dispatch.
- Integration events or any event type not based on `IDomainEvent`.
- Third-party libraries (MediatR, Scrutor). Scanning is hand-written.

## Notes

- Only `AddEventHandlers` lives in the Api project; the contracts and `EventDispatcher` stay in Application. Application takes no DI package dependency.
- `WeightTracker.Unit` gains a project reference to `WeightTracker.Api` so the scanning tests can call `AddEventHandlers`. Its tests go in a new `Api/Common` folder, mirroring `src`.
- Update `.stpr/template/STPR_INVARIANTS.md`, section "Domain events: dormant": a dispatcher now exists, but no aggregate raises events and no use case dispatches them. `AddDomainEvent` still requires an approved change that wires dispatch.
- No new identifiers, so `Id` is not involved.
- No deviations from the mapped references are allowed beyond the unit test project reference above.
