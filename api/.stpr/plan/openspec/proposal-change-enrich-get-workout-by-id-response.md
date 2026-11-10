## Change

Include the catalog exercise data and the mesocycle, with its weeks, in the `GET /workouts/{id}` response.

## Objective

`GET /workouts/{id}` returns each entry with its `ExerciseId` only and the workout's mesocycle as `MesocycleId` and
`MesocycleWeekNumber` only. The front then has to call the exercises endpoint and `GET /mesocycles/{id}` every time it
opens a workout before it can show exercise names, muscles or the week it belongs to. This change makes the workout
detail self-contained so the workout screen loads with one request.

## Capabilities

- `GET /workouts/{id}` returns, for every workout exercise entry, the referenced exercise's data (translations, video
  URL, muscle, body part, type) next to the entry's existing fields, including set targets, superseries and set records.
- `GET /workouts/{id}` returns the mesocycle the workout is placed in, with its name, objective, start date, duration
  and all its defined weeks ordered by week number. It is `null` for a standalone workout.
- Every other endpoint keeps its current response unchanged.

## Module Classification

- Workouts (`WeightTracker.Application/UseCases/Workouts`, `WeightTracker.Api/Endpoints/Workouts`) -> mutable
  aggregate, read path only in this change -> `GetWorkoutByIdUseCase` / `WorkoutsController.GetById`
- Exercises -> read-only catalog, only read here -> `IExerciseRepository.GetAllByIds`
- Mesocycles -> mutable aggregate, only read here -> `IMesocycleRepository`

Workouts and mesocycles are not catalog-like: they change through their own endpoints. This change only reads them.

## Canonical Template Mapping

- Exercise join for a workout -> `WeightTracker.Application/Services/WorkoutListLoader.cs` and
  `WorkoutListItemResponse.From(workout, exercises, users)` -> already joins workouts with only the exercises they
  reference and projects them per entry.
- Exercise projection per entry -> `WorkoutExerciseResponse.From(entry, exercise)` in
  `Endpoints/Workouts/Dtos/WorkoutResponse.cs` -> same exercise fields the list already exposes.
- Mesocycle and weeks projection -> `Endpoints/Mesocycles/Dtos/MesocycleResponse.cs` (`MesocycleWeekResponse`) ->
  canonical shape of a mesocycle with its weeks.
- Use case and view -> `GetWorkoutByIdUseCase` and `WorkoutView` -> extended in place, no new use case.

## State And Mutability

- No business state changes. Read only.
- No migration, no EF configuration change, no mapper change.

## Business Rules

- The workout visibility rule in `WorkoutVisibility` stays the only gate. If the caller can see the workout, they get
  its exercises and its mesocycle; if not, nothing else is read, so a refused read discloses no exercise or mesocycle.
- Only the exercises referenced by the workout's entries are read, and each is read once even if repeated.
- The embedded mesocycle is the one the workout references; it is not checked again with `MesocycleVisibility`.
  `MesocyclePlacementValidator` makes the workout owner the mesocycle athlete and, when set, its trainer the mesocycle
  trainer, so anyone who can see the workout can already see its mesocycle.
- A referenced exercise or mesocycle that does not exist is a data-integrity failure (`InvalidOperationException`),
  as for missing users today.

## Roles And Visibility

- Unchanged: admins see every workout, trainers the ones they own or prescribed, users the ones they own. A
  non-admin athlete is still refused a prescribed draft.

## Business Identifiers And Mutability

- N/A. No identifier is added or changed.

## Out Of Scope

- `GET /workouts`, `GET /mesocycles/{id}/workouts`, create/update responses and workout template endpoints.
- Embedding the mesocycle's trainer and athlete profiles: the workout already returns `Athlete` and `Trainer`.
- Favorites or per-user exercise flags.
- Front-end changes to consume the new fields.

## Notes

- Response shape (breaking only by addition):
  - each item of `Exercises` keeps `ExerciseId` and gains the exercise fields `Translations`, `Muscle`, `BodyPart`,
    `Type`, mirroring `WorkoutExerciseResponse`.
  - new `Mesocycle` field: `{ Id, Name, Objective, StartDate, DurationInWeeks, Weeks: [{ WeekNumber,
    TrainingWeekType, Objective }] }`, reusing `MesocycleWeekResponse`; `null` when `MesocycleId` is `null`.
    `MesocycleId` and `MesocycleWeekNumber` stay for compatibility.
- `WorkoutView` gains the referenced exercises keyed by `Id` and the `Mesocycle?`. `GetWorkoutByIdUseCase` takes
  `IExerciseRepository` and `IMesocycleRepository`; update its registration in `Program.cs` only if needed.
- `WorkoutExerciseDetailResponse` is used by other endpoints (e.g. `PUT /workouts/{id}`), so add the exercise fields
  through a dedicated per-entry DTO for `GET /workouts/{id}` or an overload that takes the exercise, not by changing
  the shared one.
- Tests: unit tests for `GetWorkoutByIdUseCase` (exercises and mesocycle loaded, standalone workout gets `null`, no
  extra reads when refused). Integration tests on `GET /workouts/{id}` assert the exercise fields per entry and the
  mesocycle with its weeks for a placed workout, `null` for a standalone one; the existing 401/403/404 matrix stays.
- No deviations from the mapped references are allowed.
