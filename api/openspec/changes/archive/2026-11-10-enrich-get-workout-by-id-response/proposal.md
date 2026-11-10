# Proposal

## Why

`GET /workouts/{id}` returns each entry with its `exerciseId` only and the placement as `mesocycleId` and
`mesocycleWeekNumber` only, so the front has to call the exercises endpoint and `GET /mesocycles/{id}` every time it
opens a workout before it can show exercise names, muscles or the week the workout belongs to. Making the workout
detail self-contained lets the workout screen load with one request.

Source brief: `.stpr/plan/openspec/proposal-change-enrich-get-workout-by-id-response.md`.

## What Changes

- Every entry in the `exercises` list of `GET /workouts/{id}` also carries the referenced exercise's `translations`,
  `muscle`, `bodyPart` and `type`, the same fields `GET /workouts` already carries per entry, plus its `videoUrl`
  (or `null`) so the app can link the exercise's video without reading the catalog. Its existing fields
  (`id`, `exerciseId`, `effort`, `perSide`, `restMinutes`, `sets`, `superSeriesId`, `setRecords`) stay. Entries still
  carry no `name`.
- `GET /workouts/{id}` gains a `mesocycle` property: the mesocycle the workout is placed in, with `id`, `name`,
  `objective`, `startDate`, `durationInWeeks` and all its defined `weeks` (`weekNumber`, `trainingWeekType`,
  `objective`) ordered by week number. It is `null` for a standalone workout. `mesocycleId` and `mesocycleWeekNumber`
  stay.
- The change is additive. It reverses the current rule that `GET /workouts/{id}` carries no catalog exercise data,
  which no client relies on as an absence. No other endpoint changes: `POST /workouts`, `PUT /workouts/{id}`, the
  other workout write responses, `GET /workouts` and `GET /mesocycles/{id}/workouts` keep their shape.
- Visibility is unchanged. The exercises and the mesocycle are read only once the caller may see the workout.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `workout-retrieval`: the by-id response carries catalog exercise data per entry and the embedded mesocycle with its
  weeks; its OpenAPI schema documents both.
- `exercises`: the requirement on workout listings no longer states that `GET /workouts/{id}` carries no exercise data.

## Module Classification And Template Mapping

- Workouts -> mutable aggregate, read path only in this change -> `GetWorkoutByIdUseCase`, `WorkoutView`,
  `WorkoutsController.GetById`, `WorkoutByIdResponse`. Not catalog-like: workouts change through their own endpoints.
- Exercises -> read-only catalog, read only -> `IExerciseRepository.GetAllByIds`, joined the way
  `WeightTracker.Application/Services/WorkoutListLoader.cs` does, projected the way `WorkoutExerciseResponse.From`
  does.
- Mesocycles -> mutable aggregate, read only -> `IMesocycleRepository.GetById`, projected after
  `Endpoints/Mesocycles/Dtos/MesocycleResponse.cs` and its `MesocycleWeekResponse`.

No conflict with `.stpr/template/STPR_INVARIANTS.md`: no aggregate, mapper, persistence or authorization rule changes,
and no deviation from the mapped references is planned.

## Business Identifiers And Mutability

No business identifier is added or changed. `Workout.Id`, `Exercise.Id` and `Mesocycle.Id` stay immutable.

## Impact

- **API**: `GET /workouts/{id}` response body and its `WorkoutByIdResponse` OpenAPI schema.
- **Application**: `GetWorkoutByIdUseCase` and `WorkoutView` read the referenced exercises and the mesocycle.
- **Infrastructure / persistence**: none. No migration, no EF configuration or mapper change.
- **Tests**: `GetWorkoutByIdUseCaseTests` and `WorkoutGetByIdEndpointTests`; the existing assertion that entries
  carry no catalog fields is replaced.
- **Front**: can drop the extra exercises and mesocycle requests when opening a workout (not part of this change).

## Out Of Scope

- Embedding the mesocycle's trainer and athlete profiles; the workout already returns `athlete` and `trainer`.
- Favorites or other per-caller exercise flags.
- Any other endpoint's response.
