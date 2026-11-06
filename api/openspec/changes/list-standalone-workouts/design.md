# Design

## Context

See `proposal.md`. `GetAllWorkoutsUseCase` loads the caller's workouts through `WorkoutVisibility.VisibleTo`, then
joins exercises and users for the response. `Workout.MesocycleId` already records placement.

## Goals / Non-Goals

**Goals:** let a caller ask only for standalone workouts, without changing role scoping or the response shape.

**Non-Goals:** exposing placement on `WorkoutListItemResponse`; filtering in SQL.

## Decisions

- **Api**: `GetAll([FromQuery] bool standalone, CancellationToken)`; a missing parameter binds to `false`. The action
  forwards it; no rule in the controller.
- **Application**: `Execute(callerId, callerRole, standaloneOnly, cancellationToken)` filters
  `workout.MesocycleId is null` right after `VisibleTo`, before the exercise and user joins, so the joins only load
  what is returned and the filter can never widen visibility.
- **Domain / Infrastructure**: unchanged; no new repository method. _Alternative_: a repository query per role with a
  placement predicate. Rejected: three new query variants for a list already loaded and scoped in memory.
- No cross-module dependency is added; authorization stays as it is.

## Risks / Trade-offs

- [In-memory filter loads placed workouts too] → Same cost as today's list; revisit with a repository predicate if
  lists grow.
