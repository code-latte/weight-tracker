# Change Proposal: List Standalone Workouts

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/list-standalone-workouts/`.

## Change

`GET /workouts` accepts an optional `standalone` query parameter. With `standalone=true` it lists only the workouts
the caller may see that are not placed in a mesocycle.

## Objective

The backoffice now shows mesocycles and workouts on separate screens. Its workouts screen must show only the workouts
outside any plan, but `WorkoutListItemResponse` carries no placement and the list cannot be filtered, so the front
cannot tell them apart. The API, which owns placement, answers which workouts are standalone.

## Capabilities

- `GET /workouts?standalone=true` lists the caller's visible workouts with no mesocycle.
- Without the parameter, or with `standalone=false`, the list is unchanged.

## Module Classification

- Workouts -> mutable aggregate -> `WorkoutRepository` (skeleton-backed); only the list read changes.

## Canonical Template Mapping

- `GetAllWorkoutsUseCase` -> the existing use case, extended in place -> it already owns role scoping for the list.
- `WorkoutsController.GetAll` -> the existing action, extended in place with `[FromQuery]`.
- Tests -> `GetAllWorkoutsUseCaseTests` and `WorkoutScopingTests` -> same Arrange / Act / Assert shape and seed data.

## State And Mutability

- No state changes: a read-only filter over the existing list.

## Business Rules

- A workout is standalone when it has no `MesocycleId`.
- The filter applies after role scoping, so it can only narrow what the caller already sees.

## Roles And Visibility

- `USER`, `TRAINER`, `ADMIN`: same visibility as today; the parameter never widens it.

## Business Identifiers And Mutability

- None affected.

## Out Of Scope

- Exposing `mesocycleId` / `mesocycleWeekNumber` on `WorkoutListItemResponse`.
- Listing the workouts of one mesocycle or week.
- Filtering in the repository query (the list is filtered in memory after scoping, as today's join loading is).

## Notes

- The front change `add-backoffice-mesocycle-screens` (`front/openspec/`) consumes it and refreshes the snapshot.
- No deviation from the mapped references.
