# domain-event-dispatch Specification

## Purpose

Defines how domain events reach the code that reacts to them: declaring a handler for an event type is enough for it to run when that event is dispatched, with defined ordering, matching and failure behavior.

## Requirements

### Requirement: Declared handlers are registered automatically
A handler declared for an event type in a scanned assembly SHALL run when that event type is dispatched, without any registration specific to that handler. A handler declared for several event types SHALL run for each of them. Abstract and open generic handler types SHALL NOT be registered.

#### Scenario: Handler declared without explicit registration
- **WHEN** a concrete handler for event type `E` exists in a scanned assembly and no registration names it
- **AND** an event of type `E` is dispatched
- **THEN** that handler runs once for the event

#### Scenario: Handler declared for two event types
- **WHEN** one concrete handler is declared for event types `E1` and `E2`
- **THEN** it runs when an `E1` is dispatched and when an `E2` is dispatched

#### Scenario: Abstract and open generic handlers are skipped
- **WHEN** a scanned assembly contains an abstract handler and an open generic handler
- **THEN** neither is registered

### Requirement: Dispatch runs every handler for the event's exact type
Dispatching an event SHALL run every handler declared for the event's exact runtime type and SHALL NOT run handlers declared for other event types, including base types or interfaces the event derives from.

#### Scenario: Several handlers for one event
- **WHEN** two handlers are declared for `E` and an `E` is dispatched
- **THEN** both handlers run

#### Scenario: Handlers of other event types are excluded
- **WHEN** a handler is declared for `F` and an `E` is dispatched
- **THEN** the `F` handler does not run

#### Scenario: Base type handlers are excluded
- **WHEN** a handler is declared for base event type `B`, and `D` derives from `B`
- **AND** a `D` is dispatched
- **THEN** the `B` handler does not run

### Requirement: Events without handlers are ignored
Dispatching an event that has no handlers SHALL complete successfully and have no effect.

#### Scenario: No handler declared
- **WHEN** an event with no declared handlers is dispatched
- **THEN** the dispatch completes without error

### Requirement: Dispatch is sequential and ordered
Events SHALL be handled in the order they are given. All handlers of one event SHALL finish before handling of the next event starts, and handlers SHALL NOT run in parallel.

#### Scenario: Multiple events
- **WHEN** events `A` and then `B` are dispatched together
- **THEN** every handler of `A` finishes before any handler of `B` starts

### Requirement: Handler failures propagate and stop the dispatch
When a handler fails, the dispatch SHALL fail with the same error. It SHALL NOT catch, wrap or retry the error, and no later handler or event SHALL run.

#### Scenario: Handler throws
- **WHEN** the first handler of an event throws an error
- **THEN** the dispatch fails with that same error
- **AND** the remaining handlers and later events do not run
