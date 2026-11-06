# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-list-standalone-workouts.md`.

## Why

The backoffice now shows mesocycles and workouts on separate screens, and its workouts screen must list only the
workouts outside any plan. `WorkoutListItemResponse` carries no placement and `GET /workouts` takes no filter, so only
the API, which owns placement, can answer which workouts are standalone.

## What Changes

- `GET /workouts` accepts an optional `standalone` boolean query parameter.
- With `standalone=true` the list keeps only the visible workouts with no mesocycle. Without it, or with `false`, the
  list is unchanged. Not breaking.
- The OpenAPI document declares the parameter.

Canonical references: `GetAllWorkoutsUseCase` and `WorkoutsController.GetAll`, extended in place (Workouts is a
mutable aggregate whose reference is the skeleton-backed `WorkoutRepository`); tests follow
`GetAllWorkoutsUseCaseTests` and `WorkoutScopingTests`. No business identifier is affected. No conflict with the
template or invariants.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `workout-retrieval`: the workout list can be limited to the workouts not placed in a mesocycle.

## Impact

- **Api**: `WorkoutsController.GetAll` binds `[FromQuery] bool standalone`.
- **Application**: `GetAllWorkoutsUseCase.Execute` takes `standaloneOnly` and filters after role scoping.
- **Domain**, **Infrastructure**: unchanged (`Workout.MesocycleId` already exists).
- **Consumers**: the front change `add-backoffice-mesocycle-screens` sends `standalone=true` and refreshes its snapshot.
