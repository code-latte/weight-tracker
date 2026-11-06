# Design

## Context

See `proposal.md` for the motivation and `specs/` for the required behavior.

- `Mesocycle.RemoveWeek(WeekNumber)` already exists. It throws `MesocycleWeekNotFoundException` for an undefined week,
  and its removal already round-trips through `MesocycleRepository`. Nothing calls it.
- The `Mesocycle` aggregate does not hold its workouts. A workout points to `(MesocycleId, MesocycleWeekNumber)` by
  value, and the `workouts` table has those two columns plus a foreign key to `mesocycles`, but none to
  `mesocycle_weeks`. A workout may be placed in an undefined week.
- `CreateWorkoutUseCase` and `UpdateWorkoutUseCase` call `MesocyclePlacementValidator.EnsureComplete` first, then
  `EnsureCanPlace`. On `PUT`, `EnsureCanPlace` runs only when the placement, the owner or the trainer changes. Both use
  cases receive a non-null `DateTime workoutDate`, because the DTOs mark `WorkoutDate` as `[Required]`.
- The seeded mesocycle starts on Thursday `2026-10-01` and has weeks 1, 2 and 4 defined. `TrainedWorkout` (published,
  dated `2026-10-03`) is in week 1, and `PrescribedStartedWorkout` (started, dated `2026-10-17`) is in week 2. Week 4
  has no workouts.
- `IWorkoutRepository` is copied in `.stpr/template`. `CreateWorkoutUseCase`, `UpdateWorkoutUseCase` and `Mesocycle`
  are not.

## Goals / Non-Goals

**Goals:**
- Expose week removal through the existing week-route pattern.
- Have one domain rule for a week's date, used by both workout use cases.

**Non-Goals:**
- No schema change and no FluentMigrator migration.
- No change to `Workout`. It still receives a concrete date in `Create` / `UpdateDetails`, and does not know how that
  date was obtained.
- No new authorization rule. `MesocycleEditAuthorizer` and `WorkoutEditAuthorizer` are reused as they are.

## Decisions

### 1. The week date lives in the `Mesocycle` aggregate

`Mesocycle.DateOfWeek(WeekNumber weekNumber) : DateTime`:
- It throws `MesocycleWeekOutOfRangeException` when `weekNumber > DurationInWeeks`.
- Otherwise it returns `monday + 2 days + 7 × (n − 1) days`, where `monday = StartDate − ((int)StartDate.DayOfWeek + 6) % 7`
  days.
- It reads only `StartDate` and `DurationInWeeks`, so it works for undefined weeks.

*Alternative:* an Application helper. Rejected, because the start date and the week range are mesocycle state, and
the aggregate is the gate for rules over its own state.

### 2. The "no workouts in the week" check lives in the use case

`RemoveMesocycleWeekUseCase` runs these steps, failing fast:
1. `GetById`, or `MesocycleNotFoundException` (`404`).
2. `MesocycleEditAuthorizer.EnsureCanEdit` (`403`).
3. If the week is not among `mesocycle.Weeks`, `MesocycleWeekNotFoundException` (`404`).
4. If `IWorkoutRepository.ExistsByMesocycleWeek(mesocycleId, weekNumber)`, `MesocycleWeekHasWorkoutsException` (`409`).
5. `mesocycle.RemoveWeek`, `Save`, `SaveChangesAsync`.

Step 3 comes before the workouts query so that an undefined week is always `404`, even if workouts are placed in that
number. It also means every input is validated before the aggregate is changed.

`MesocycleWeekHasWorkoutsException` goes in `Application/Exceptions`, next to `MesocycleAthleteMismatchException`,
because the rule spans two aggregates.

*Alternative:* `Mesocycle.RemoveWeek(weekNumber, bool hasWorkouts)`. Rejected, because a boolean flag only hands the
decision to the caller while looking like a domain guard. It would also change a domain method that is already tested.

### 3. Workout date resolution goes in `MesocyclePlacementValidator`

Two new members, used by both workout use cases:
- `static EnsureDateOrPlacement(Id? mesocycleId, DateTime? workoutDate)`:
  - It throws `MissingWorkoutDateException` (`400`) when there is no placement and no date.
  - It runs right after `EnsureComplete`, before anything is loaded, so it behaves like the other structural checks.
- `async ResolveWorkoutDate(Id? mesocycleId, WeekNumber? weekNumber, DateTime? workoutDate, CancellationToken) : DateTime`:
  - Without a placement, it returns `workoutDate`.
  - With a placement, it loads the mesocycle (`MesocycleNotFoundException` if gone) and returns
    `mesocycle.DateOfWeek(weekNumber)`.
  - It does no authorization. It runs after the edit rule and, when it applies, `EnsureCanPlace`.

On `PUT` with an unchanged placement, `EnsureCanPlace` is still skipped, as today. The date is recalculated anyway,
which is what lets an admin update a locked workout and lets the owner of a self-planned workout edit it. Neither of
them goes through the mesocycle's authorization.

*Alternative:* make `EnsureCanPlace` return the `Mesocycle` it loaded, to avoid a second read. Rejected for now. It
covers only the changed-placement path, so the unchanged path would still need its own load, and two code paths cost
more than one extra primary-key read.

`MissingWorkoutDateException` goes in `Application/Exceptions`.

### 4. Use case signatures

- `CreateWorkoutUseCase.Execute(..., DateTime? workoutDate, ...)`
- `UpdateWorkoutUseCase.Execute(..., DateTime? workoutDate, ...)`

The controller maps `request.WorkoutDate?.ToDateTime(TimeOnly.MinValue)` instead of `!.Value`.

- `RemoveMesocycleWeekUseCase.Execute(Id callerId, Role callerRole, Id mesocycleId, WeekNumber weekNumber, CancellationToken) : Task`

### 5. API

- `MesocyclesController.RemoveWeek` handles `[HttpDelete("{id:guid}/weeks/{weekNumber:int}")]`. It returns
  `NoContent()`, and its `ProducesResponseType` entries are 204/400/401/403/404/409, mirroring
  `WorkoutsController.RemoveExercise`.
- `{weekNumber}` is converted with `WeekNumber.From`, as in the `PUT` week route, so `0` is `400`.
- `ExceptionMapper` gets `MesocycleWeekHasWorkoutsException` -> `409` and `MissingWorkoutDateException` -> `400`.

### 6. Infra

`WorkoutRepository.ExistsByMesocycleWeek` runs
`AnyAsync(w => w.MesocycleId == id && w.MesocycleWeekNumber == n)` over every status. It has no mapping and needs no
index change.

### 7. Seed

`TrainedWorkout` becomes `2026-09-30` (week 1) and `PrescribedStartedWorkout` becomes `2026-10-07` (week 2), so every
seeded placed workout matches the rule. The integration tests that assert those dates are updated to match:
`WorkoutUpdateEndpointTests`, `WorkoutGetByIdEndpointTests`, `WorkoutMesocyclePlacementEndpointTests` and
`WorkoutResponseShapeTests`. The new values are expressed through `SeedData` constants where the tests compare dates.

## Risks / Trade-offs

- **[Week 1 can fall before the start date]** A mesocycle starting Thursday to Sunday has a week 1 Wednesday earlier
  than its start. → This is accepted by the brief and pinned by the "Start on a Sunday" scenario. Switching to "first
  Wednesday on or after the start" later would change only `DateOfWeek` and its tests.
- **[Stale dates after a reschedule]** Workouts keep their old dates until their next `PUT`. → This is documented as a
  requirement, and recalculating on reschedule is out of scope.
- **[Silent ignore]** A client that sends a date with a placement gets a different date back, with no error. → This is
  documented in the DTO XML docs and in the response, which always carries the stored date.
- **[Existing rows]** Placed workouts already in other environments keep dates that may not match their week. → They
  are corrected on their next `PUT`. No data migration.
- **[Order of `PUT` checks]** `MissingWorkoutDateException` now runs before the workout is loaded, so `PUT` on an
  unknown workout with no date and no placement is `400`, not `404`. → This is consistent with the existing
  `IncompleteMesocyclePlacementException` ordering.

## Migration Plan

There is no schema change. Deploy the API, and old clients that always send `workoutDate` keep working. Rollback
re-requires `workoutDate`, and no data has to be reverted.
