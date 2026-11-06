# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-mesocycle-workouts-endpoint.md`.

## Why

`GET /workouts` items carry no mesocycle placement, although `WorkoutResponse` and `WorkoutDetailResponse` already do,
so the front cannot tell which mesocycle and week a listed workout belongs to. There is also no way to read the workouts
of one mesocycle without listing everything and filtering on a field the list does not return. The API owns placement,
so it should expose both.

## What Changes

- `GET /workouts` items (`WorkoutListItemResponse`) gain `mesocycleId` and `mesocycleWeekNumber`. Both are always
  present and `null` for a standalone workout. The change is additive; every existing field, scope and order stays
  the same.
- New `GET /mesocycles/{id}/workouts`: lists the workouts placed in mesocycle `{id}` across all its weeks, with the
  same item shape as `GET /workouts`, ordered by week number and then workout date.
  - `401` with no token or an invalid token.
  - `404` for an unknown mesocycle.
  - `403` when the caller may not see the mesocycle, under the same rule as `GET /mesocycles/{id}`.
  - Within a visible mesocycle, only the workouts the caller may see under the workout visibility rule are listed, so
    an athlete never gets a prescribed draft. A mesocycle with no visible placed workouts returns an empty list.

- `GET /workouts/{id}` gains `athlete` and `trainer`, the same public profiles the list items carry (`trainer` is
  `null` when the workout has none), under its own schema `WorkoutByIdResponse`. Entries stay `exerciseId`-only. The
  write routes that return `WorkoutDetailResponse` (`PUT /workouts/{id}`, `PUT .../exercises/order`,
  `PUT .../status`) are unchanged. The change only adds fields to the by-id response.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `workout-retrieval`: the workout list item carries the mesocycle placement, and the OpenAPI document declares it
  as nullable. The workout read by id names its athlete and trainer, under its own documented schema.
- `mesocycle-retrieval`: adds the read of a mesocycle's workouts, with its auth, visibility, not-found, scoping and
  order rules.
- `workout-mesocycle-placement`: drops the clause "The workout list item shape SHALL NOT change" from
  "Workout responses expose the placement", which this change deliberately reverses.

## Canonical Template Mapping

- List item placement -> `WorkoutResponse.From` (`Endpoints/Workouts/Dtos/WorkoutResponse.cs`) -> already projects
  the same two fields.
- Mesocycle workouts read -> `GetMesocycleByIdUseCase` (gate on one mesocycle) + `GetAllWorkoutsUseCase` (scoped list
  with exercise and user joins) -> nearest read that checks one aggregate and lists another.
- Route -> `MesocyclesController.GetById` and `WorkoutsController.GetAll` -> `AuthorizedController`,
  `ExceptionMapper.Map`, `WorkoutListItemResponse` mapping.
- Tests -> `GetAllWorkoutsUseCaseTests`, `GetMesocycleByIdUseCaseTests`, `MesocycleReadEndpointsTests`,
  `WorkoutScopingTests`, `OpenApiDocumentTests`.

## Module Classification And Identifiers

- Workouts and Mesocycles are both mutable aggregates (domain areas; the architecture is layer-first). This change only
  adds reads; neither area becomes a catalog.
- `Mesocycle.Id` is the path key. It is generated with `Id.New()` and is immutable. No identifier is added or changed.
- `Workout.MesocycleId` / `Workout.MesocycleWeekNumber` are only exposed. Their mutability (through `POST`/`PUT /workouts`
  while `DRAFT`/`PUBLISHED`) is unchanged.
- No conflict with `.stpr/template/STPR_INVARIANTS.md` or the template was found.

## Impact

- **Api:** `WorkoutListItemResponse`, `MesocyclesController` (new action and constructor dependency), new
  `WorkoutByIdResponse` returned by `WorkoutsController.GetById`.
- **Application (by id):** `GetWorkoutByIdUseCase` returns a `WorkoutView` (workout, athlete, trainer) instead of the
  bare `Workout`.
- **Application:**
  - new `GetMesocycleWorkoutsUseCase`;
  - the exercise and user joins of `GetAllWorkoutsUseCase` move to a shared loader;
  - `WorkoutVisibility` gains a per-workout check that `GetWorkoutByIdUseCase` reuses.
- **Infra:** `IWorkoutRepository` / `WorkoutRepository` gain `GetAllByMesocycleId`. No migration, no seed change
  expected.
- **Registration:** `Program.cs`.
- **Consumers:** the front change `attach-workouts-to-mesocycle-weeks` uses both and refreshes its OpenAPI snapshot.

## Out Of Scope

- Filtering by week, embedding workouts in `MesocycleResponse`, and any change to `WorkoutResponse` /
  `WorkoutDetailResponse`.
- Front changes.
