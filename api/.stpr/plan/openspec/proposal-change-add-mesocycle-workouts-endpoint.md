# Change Proposal: Add Mesocycle Workouts Endpoint

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-mesocycle-workouts-endpoint/`.

## Change

`WorkoutListItemResponse` gains `mesocycleId` and `mesocycleWeekNumber`, always present and `null` for a standalone
workout. A new `GET /mesocycles/{id}/workouts` lists every workout placed in that mesocycle, across all its weeks.

## Objective

`WorkoutResponse` and `WorkoutDetailResponse` already carry the placement, but `GET /workouts` does not, so the front
cannot tell which mesocycle and week a listed workout belongs to. There is also no way to read the workouts of one
mesocycle: the front would have to list everything and filter by a field it does not receive. The API, which owns
placement, exposes both.

## Capabilities

- `GET /workouts` items include `mesocycleId` and `mesocycleWeekNumber`, serialized as `null` when the workout is
  standalone (never omitted).
- `GET /mesocycles/{id}/workouts` lists the workouts placed in mesocycle `{id}`, whatever their week, with the same item
  shape as `GET /workouts`.

## Module Classification

Domain areas, not modules (the architecture is layer-first).

- Workouts (api) -> mutable aggregate; only the list projection changes -> `Endpoints/Workouts/Dtos/WorkoutListItemResponse.cs`
- Mesocycles (application + api) -> mutable aggregate; gains one read route -> `UseCases/Mesocycles/GetMesocycleByIdUseCase.cs`,
  `Endpoints/Mesocycles/MesocyclesController.cs`
- Workouts (application) -> existing read services reused: `WorkoutVisibility` and the exercise and user joins of
  `GetAllWorkoutsUseCase`

No area here is a catalog, and nothing is only publicly read-only: this change adds reads over aggregates that keep
changing through their existing write routes.

## Canonical Template Mapping

- `WorkoutListItemResponse` -> `WorkoutResponse.From` -> already projects `MesocycleId?.Value` and
  `MesocycleWeekNumber?.Value`; copy the same two fields and XML docs.
- `GetMesocycleWorkoutsUseCase` -> `GetMesocycleByIdUseCase` (load, `MesocycleNotFoundException`,
  `MesocycleVisibility.CanSee`) followed by `GetAllWorkoutsUseCase` (scope with `WorkoutVisibility`, then join exercises
  and users, return `WorkoutListDTO`) -> nearest read that checks one aggregate and then lists another.
- `MesocyclesController.GetWorkouts` -> `MesocyclesController.GetById` + `WorkoutsController.GetAll` -> same
  `AuthorizedController` base, `ExceptionMapper.Map`, and `WorkoutListItemResponse` mapping.
- Tests -> `GetAllWorkoutsUseCaseTests`, `GetMesocycleByIdUseCaseTests`, `WorkoutScopingTests` and the mesocycle
  integration tests -> same Arrange / Act / Assert shape and seed data.

## State And Mutability

- No state changes. Both capabilities are reads.
- Placement keeps changing through `POST`/`PUT /workouts`, as today.

## Business Rules

- A workout belongs to a mesocycle when its `MesocycleId` equals `{id}`. The week number does not filter: all weeks are
  returned.
- Unknown mesocycle -> `404` (`MesocycleNotFoundException`).
- A mesocycle the caller may not see -> the same response `GET /mesocycles/{id}` gives (`403`).
- The workout list is narrowed by `WorkoutVisibility` after the mesocycle check, so this route never shows a workout
  that `GET /workouts` would hide from the same caller.
- A visible mesocycle with no placed workouts returns `200` with an empty list.
- Order: `mesocycleWeekNumber` ascending, then `workoutDate` ascending.

## Roles And Visibility

| Route | USER | TRAINER | ADMIN |
| --- | --- | --- | --- |
| `GET /workouts` (new fields) | unchanged | unchanged | unchanged |
| `GET /mesocycles/{id}/workouts` | if athlete of it | if trainer of it | all |

- The mesocycle gate is `MesocycleVisibility.CanSee`; the workout gate is `WorkoutVisibility`. Both must pass.

## Business Identifiers And Mutability

- `Mesocycle.Id` is the path key, generated with `Id.New()` and immutable. No identifier is added or changed.
- `Workout.MesocycleId` and `Workout.MesocycleWeekNumber` are only exposed here; their mutability rules are unchanged.

## Out Of Scope

- Filtering by week (`GET /mesocycles/{id}/weeks/{weekNumber}/workouts` or a `week` query parameter).
- Embedding workouts in `MesocycleResponse`.
- Changes to `WorkoutResponse` / `WorkoutDetailResponse`, which already expose the pair.
- Front changes (the front brief `attach-workouts-to-mesocycle-weeks` consumes this and refreshes its snapshot).

## Notes

- **Nulls:** the API has no global `WhenWritingNull`, so nullable record fields already serialize as `null`. Do not add
  `[JsonIgnore]` to the new fields.
- **Field names:** `mesocycleWeekNumber`, not `weekNumber`, to match `WorkoutResponse` and the request DTOs.
- **Shared join:** the exercise and user loading of `GetAllWorkoutsUseCase` is extracted into one Application service
  (for example `WorkoutListLoader`, following `MesocycleViewLoader`) used by both use cases, instead of being duplicated.
- **Repository:** `IWorkoutRepository` gains `GetAllByMesocycleId(Id mesocycleId, ...)`; the result is intersected with
  what `WorkoutVisibility` lets the caller see.
- **Registration:** `GetMesocycleWorkoutsUseCase` and the shared loader are registered in `Program.cs`.
- **Seed:** the existing seeded mesocycle with `TrainedWorkout` and `PrescribedStartedWorkout` linked is the baseline;
  at least one standalone workout and, if needed, a workout in another mesocycle prove what is excluded.
- **Tests:**
  - Unit: `GetMesocycleWorkoutsUseCase` (not found, not visible, each role, empty, ordering, excludes workouts the caller
    cannot see); `GetAllWorkoutsUseCase` still passes after the loader extraction.
  - Integration `GET /workouts`: a placed workout returns both values; a standalone one returns both keys as `null`.
  - Integration `GET /mesocycles/{id}/workouts`: `401` no token, `401` invalid token, `403` for a user who is not its
    athlete and for another trainer, `404` unknown id, `200` for athlete, trainer and admin. Assert workouts from other
    weeks are included and standalone and other-mesocycle workouts are excluded.
  - Full `dotnet test WeightTracker.slnx` with Docker.
- **Template parity:** if `WorkoutListItemResponse`, `GetAllWorkoutsUseCase` or their tests are template copies, update
  `.stpr/template` at the same time.
- No deviations from the mapped template references are allowed.

## Open Decisions

Defaults used unless changed before implementation:

1. Mesocycle visibility and workout visibility both apply (intersection), rather than mesocycle visibility alone.
2. Ordered by week number, then workout date.
3. A dedicated repository query instead of filtering `WorkoutVisibility.VisibleTo` in memory.
