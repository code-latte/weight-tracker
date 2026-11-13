# Proposal

## Why

`AggregateRoot` already collects `IDomainEvent`s, but nothing delivers them, so `STPR_INVARIANTS.md` keeps domain events dormant and forbids raising them. Before any feature can react to an event (for example, sending a welcome email after signup), the project needs a delivery mechanism where declaring a handler is enough for it to run, without registering each handler by hand.

This change adds only that mechanism. No event is raised or dispatched yet.

## What Changes

- New contracts in `src/WeightTracker.Application/Common/Dispatcher`:
  - `IEventHandler<TEvent>` (with `TEvent : IDomainEvent`) declares a handler for one event type.
  - `IEventDispatcher` takes a set of events and runs every handler registered for each event's runtime type.
- New `EventDispatcher` in the same folder, which resolves handlers through `IServiceProvider`. Application takes no new package dependency.
- New `AddEventHandlers(params Assembly[])` extension in `src/WeightTracker.Api/Common`. It scans assemblies, registers every concrete handler against each `IEventHandler<T>` it implements, and registers the dispatcher. Only this scanning method lives in Api.
- `Program.cs` calls `AddEventHandlers` once with the Application and Infra assemblies.
- `WeightTracker.Unit` gains a project reference to `WeightTracker.Api` for the scanning tests.
- `STPR_INVARIANTS.md`, "Domain events: dormant", is updated: a dispatcher now exists, but no aggregate raises events and no use case dispatches them.

No HTTP endpoint, persistence or seed changes. Nothing is breaking.

## Capabilities

### New Capabilities

- `domain-event-dispatch`: how domain events reach their handlers. Covers handler declaration, automatic handler registration, and the dispatch semantics: ordering, exact-type matching, no-handler behavior and error propagation.

### Modified Capabilities

None.

## Canonical Template References

- `domain-event-dispatch`, contracts and dispatcher -> `src/WeightTracker.Application/Common/IUnitOfWork.cs`: the existing shared Application contract in `Common`.
- `domain-event-dispatch`, event constraint -> `src/WeightTracker.Domain/Common/IDomainEvent.cs`: the existing event contract is reused, not duplicated.
- `domain-event-dispatch`, handler scanning -> `src/WeightTracker.Api/Common/ExceptionMapper.cs`: the existing Api-side shared helper. DI wiring belongs to the composition root.
- `domain-event-dispatch`, tests -> `test/WeightTracker.Unit/Application/Services/*Tests.cs`: the closest unit tests of application collaborators.

**Module classification**: this is cross-cutting application infrastructure, not a domain area. There is no aggregate, no catalog, no persistence and no business state.

**Business identifiers**: none are introduced or changed.

## Conflicts With Template Or Invariants

- "Domain events: dormant" says no dispatcher exists. This change makes that statement false, so the section is updated in the same change. The prohibition on calling `AddDomainEvent` stays until a later change wires dispatch into use cases.
- `test/WeightTracker.Unit` has no `Api/` folder and does not reference the Api project. The scanning tests add both, mirroring `src`. This is the only allowed deviation.

## Out Of Scope

- Raising any domain event, or calling the dispatcher from a use case, repository or `UnitOfWork`.
- Clearing aggregate events after dispatch.
- Outbox, background, parallel or retried delivery, and transactional guarantees between save and dispatch.
- Event types not based on `IDomainEvent`.
- Third-party libraries (MediatR, Scrutor).

## Acceptance Criteria

- Declaring `class X : IEventHandler<E>` in a scanned assembly is enough for `X` to run when `E` is dispatched.
- The dispatch semantics in the `domain-event-dispatch` spec are covered by unit tests, each split into Arrange / Act / Assert phases.
- `dotnet build WeightTracker.slnx` passes with warnings as errors, and the full `dotnet test WeightTracker.slnx` passes with Docker running.

## Impact

- Code:
  - `src/WeightTracker.Application/Common/Dispatcher/*` (new)
  - `src/WeightTracker.Api/Common/EventHandlerRegistration.cs` (new)
  - `src/WeightTracker.Api/Program.cs` (one line)
- Tests:
  - `test/WeightTracker.Unit/Application/Common/Dispatcher/*` (new)
  - `test/WeightTracker.Unit/Api/Common/*` (new)
  - `test/WeightTracker.Unit/WeightTracker.Unit.csproj` (adds the Api project reference)
- Docs: `.stpr/template/STPR_INVARIANTS.md`.
- Dependencies: no new packages.
