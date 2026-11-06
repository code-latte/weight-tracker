## Change

Let trainers and admins remove a mesocycle week that has no workouts, and work out a placed workout's date from its
mesocycle week instead of taking it from the request.

## Objective

`Mesocycle.RemoveWeek` exists in the domain but cannot be reached over HTTP, so a week defined by mistake can only be
redefined, never removed. Separately, a workout placed in a mesocycle week keeps whatever `workoutDate` the client
sends, so its date and its week can disagree. This change exposes week removal, guarded so no workout points to a
removed week, and makes the mesocycle week the only source of a placed workout's date. A standalone workout still sends
its date, and the front sends the Wednesday of the target week.

## Capabilities

- Remove a defined week: `DELETE /mesocycles/{id}/weeks/{weekNumber}`. Refused while any workout is placed in that week.
- `POST /workouts` and `PUT /workouts/{id}` without a placement: `workoutDate` is required.
- `POST /workouts` and `PUT /workouts/{id}` with `mesocycleId` + `mesocycleWeekNumber`: `workoutDate` is ignored, and
  the date is the Wednesday of that mesocycle week.

## Module Classification

Domain areas, not modules (the architecture is layer-first).

- Mesocycles (domain + application + api) -> mutable aggregate. It gains a week-date calculation and a new use case and
  route for removing a week -> `src/WeightTracker.Domain/Mesocycles/Mesocycle.cs`,
  `UseCases/Mesocycles/RedefineMesocycleWeekUseCase.cs`, `Endpoints/Mesocycles/MesocyclesController.cs`
- Workouts (application + api + infra) -> existing mutable aggregate. The create and update use cases and request DTOs
  change, and the repository gains an existence query -> `CreateWorkoutUseCase`, `UpdateWorkoutUseCase`,
  `IWorkoutRepository`

Neither area is a catalog. Removing a week changes the `Mesocycle` aggregate, and the date rule changes `Workout`
state on create and update.

## Canonical Template Mapping

- `RemoveMesocycleWeekUseCase` -> `RedefineMesocycleWeekUseCase` (load, authorize with `MesocycleEditAuthorizer`, act on
  the inner entity through the root, save) plus `RemoveWorkoutExerciseUseCase` for the delete semantics -> nearest
  removal of an inner entity through its root
- `DELETE /mesocycles/{id}/weeks/{weekNumber}` -> `DELETE /workouts/{workoutId}/exercises/{exerciseId}` in
  `WorkoutsController` -> same `204 No Content` delete shape, `ExceptionMapper.Map` in the catch
- Week date -> `Mesocycle.AddWeek` range guard -> the aggregate owns the week range, so it owns the week's date
- Workout date resolution -> `MesocyclePlacementValidator` -> shared Application service used by workout create and
  update
- `IWorkoutRepository.ExistsByMesocycleWeek` -> existing `IWorkoutRepository` queries -> same repository, a query with
  no mapping
- Tests -> existing mesocycle week endpoint tests and `WorkoutUpdateEndpointTests` -> auth matrix per route

## State And Mutability

- `DELETE /mesocycles/{id}/weeks/{weekNumber}` removes a `MesocycleWeek` from the `Mesocycle` aggregate. The other
  weeks keep their numbers, and the duration is not changed.
- On create and update, a placed workout's `WorkoutDate` is set from its mesocycle week.
- No new read-only data.

## Business Rules

- **Remove week:**
  - Only a defined week can be removed. A missing week is `404` (`MesocycleWeekNotFoundException`).
  - It is refused with `409` while any workout, whatever its status, has `MesocycleId = id` and
    `MesocycleWeekNumber = weekNumber`. New `MesocycleWeekHasWorkoutsException`.
  - Workouts placed in an *undefined* week number do not block anything, because only defined weeks can be removed.
- **Week date (domain):** `Mesocycle.DateOfWeek(WeekNumber)`:
  - Week 1 is the Wednesday of the calendar week that contains `StartDate`. Week *n* is that Wednesday plus `7 × (n − 1)`
    days.
  - A week beyond the duration throws `MesocycleWeekOutOfRangeException` (`409`).
  - Weeks start on Monday (ISO). Example: a start of Mon 2026-11-02 gives week 1 = Wed 2026-11-04 and week 2 =
    Wed 2026-11-11. A start of Sun 2026-11-08 also gives week 1 = Wed 2026-11-04, so that date falls before the start.
- **Workout date (POST and PUT `/workouts`):**
  - No placement and no `workoutDate` is a `400`. New `MissingWorkoutDateException`. This is conditional, so it is
    checked in the use case, not the DTO.
  - With a placement, any `workoutDate` that is sent is silently ignored. The stored date is
    `mesocycle.DateOfWeek(weekNumber)`.
  - On `PUT`, the date is recalculated on every request with a placement, even when the placement did not change.
    Existing placement rules stay as they are: both or neither, `404`, `409` mismatches, `403` for a regular user,
    and `WorkoutPlacementLockedException`.
  - A standalone workout's date is stored as sent. The API does not check that it is a Wednesday.

## Roles And Visibility

| Route | USER | TRAINER | ADMIN |
| --- | --- | --- | --- |
| `DELETE /mesocycles/{id}/weeks/{weekNumber}` | `403` | own (`Mesocycle.TrainerId == caller`) | all |

- The workout date rule applies to every caller who is already allowed to create or update the workout. It adds no
  new permission.

## Business Identifiers And Mutability

- `(Mesocycle.Id, WeekNumber)` is still the immutable business identifier of a week, and now also the key of the
  `DELETE` route.
- When the week is defined again after removal, it gets a fresh `MesocycleWeek.Id` (internal, never exposed).
- `Workout.WorkoutDate` stays mutable, but for a placed workout it can only be changed through its placement.
- No new identifiers. Nothing new is generated, so `Id.New()` is not involved beyond the existing `AddWeek`.

## Out Of Scope

- Recalculating the dates of placed workouts when `PUT /mesocycles/{id}` changes `StartDate`. Dates are refreshed only
  on the workout's next `PUT`.
- Changing a mesocycle's duration based on its linked workouts (an existing gap).
- Renumbering the remaining weeks after a removal.
- Deleting mesocycles.
- Migrating existing workout rows whose date does not match their week.
- Front changes.

## Notes

- **Domain:**
  - New `Mesocycle.DateOfWeek(WeekNumber) : DateTime`, with the range guard.
  - New `MesocycleWeekHasWorkoutsException` in `Domain/Exceptions`, or in `Application/Exceptions` if the check stays in
    the use case. See open decision 2.
  - `RemoveWeek` is unchanged.
- **Use cases:**
  - New `RemoveMesocycleWeekUseCase`
    (`Execute(callerId, callerRole, mesocycleId, weekNumber, ct) : Task`):
    1. `GetById`, or `MesocycleNotFoundException`.
    2. `MesocycleEditAuthorizer.EnsureCanEdit`.
    3. `IWorkoutRepository.ExistsByMesocycleWeek(mesocycleId, weekNumber)`. If true, throw
       `MesocycleWeekHasWorkoutsException`.
    4. `mesocycle.RemoveWeek`.
    5. Save, then `SaveChangesAsync`.

    A missing week can be checked before the workouts query so it always gets `404`.
  - `CreateWorkoutUseCase` / `UpdateWorkoutUseCase`: `workoutDate` becomes `DateTime?`. After `EnsureComplete`, the
    date is resolved: with a placement it is `mesocycle.DateOfWeek(week)`, and without one it is the sent date or
    `MissingWorkoutDateException`. The resolved date is passed to `Workout.Create` / `UpdateDetails`.
  - So that the mesocycle is not loaded twice, `MesocyclePlacementValidator` gets a `ResolveWorkoutDate(mesocycleId?,
    weekNumber?, workoutDate?, ct)` that loads the mesocycle. Alternatively, `EnsureCanPlace` returns the `Mesocycle` it
    loaded. On `PUT` with an unchanged placement, `EnsureCanPlace` is skipped today, so the mesocycle still has to be
    loaded for the date.
- **Repository:** `IWorkoutRepository.ExistsByMesocycleWeek(Id mesocycleId, WeekNumber weekNumber, CancellationToken)`.
  It is an `AnyAsync` on the existing columns and needs no migration.
- **API:**
  - `WorkoutDate` loses `[Required]` on `CreateWorkoutRequest` and `UpdateWorkoutRequest`. The XML docs explain when
    it is required and when it is ignored.
  - New `RemoveWeek` action on `MesocyclesController` returns `204`.
  - Both new exceptions get explicit `ExceptionMapper` entries: `MesocycleWeekHasWorkoutsException` -> `409`,
    `MissingWorkoutDateException` -> `400`. Add them to `ExceptionMapperTests`.
- **Registration:** `RemoveMesocycleWeekUseCase` is registered in `Program.cs`.
- **Seed:** the seeded mesocycle's linked workouts (`TrainedWorkout`, `PrescribedStartedWorkout`) should have dates
  that match their weeks, so that tests can assert them. A defined week with no workouts is needed for the removal
  success case, and a defined week with a linked workout for the `409`.
- **Tests:**
  - Unit:
    - `DateOfWeek`: start on Monday, Wednesday and Sunday; week 1 and week *n*; out of range.
    - `RemoveMesocycleWeekUseCase`: trainer, admin, user `403`, not found, week missing, blocked by workouts.
    - Create and update: date ignored with a placement, `400` without a placement and without a date, recalculated on
      `PUT` with an unchanged placement.
  - Integration:
    - `DELETE /mesocycles/{id}/weeks/{weekNumber}`: `401` with no token, `401` with an invalid token, `403` for a user
      and for another trainer, `404` for the mesocycle, `404` for the week, `409` with workouts, `204` with the week
      gone in a following `GET`.
    - `POST`/`PUT /workouts`: date derived even when a conflicting date is sent, and `400` without a date.
  - Full `dotnet test WeightTracker.slnx` with Docker.
- **Template parity:** if `CreateWorkoutUseCase`, `Workout` or their tests are copied in `.stpr/template`, update the
  copies in the same change.
- No deviations from the mapped template references are allowed.

## Open Decisions

Defaults used unless changed before implementation:

1. Weeks start on Monday, so a mesocycle starting on Sunday has its week 1 Wednesday *before* the start date. The
   alternative is Sunday-start weeks, or "the first Wednesday on or after the start".
2. The "no workouts in the week" check lives in `RemoveMesocycleWeekUseCase` (the aggregate does not hold workouts)
   and throws an Application exception. The alternative is `RemoveWeek(weekNumber, hasWorkouts)`, so the aggregate
   gates it.
3. Workouts of any status block removal, including `FINISHED` ones.
4. A `workoutDate` sent with a placement is ignored, not rejected with `400`.
5. Rescheduling a mesocycle does not update its workouts' dates (out of scope above).
6. `DELETE` returns `204`, like the other deletes, not the updated `MesocycleResponse` that the week `POST`/`PUT` return.
