# Proposal

## Why

A mesocycle week defined by mistake can be redefined but never removed over HTTP, because `Mesocycle.RemoveWeek` is not
exposed. A workout placed in a mesocycle week also keeps whatever `workoutDate` the client sends, so its date and its
week can disagree. This change exposes week removal, guarded so no workout is left in a removed week, and makes the
mesocycle week the only source of a placed workout's date.

## What Changes

- New `DELETE /mesocycles/{id}/weeks/{weekNumber}` for the mesocycle's trainer and admins. It answers `204`. It answers
  `404` for an unknown mesocycle or an undefined week, and `409` while any workout, of any status, is placed in that
  week. The other weeks keep their numbers, and the duration is unchanged.
- New domain rule: each week of a mesocycle has a date. Week 1 is the Wednesday of the ISO week (Monday to Sunday)
  that contains the start date, and week *n* is that Wednesday plus `7 × (n − 1)` days. A week beyond the duration has
  no date.
- `POST /workouts` and `PUT /workouts/{id}`:
  - **BREAKING** (relaxes a requirement): `workoutDate` is required only when no placement is sent.
  - Without a placement and without `workoutDate`, the answer stays `400`.
  - With `mesocycleId` + `mesocycleWeekNumber`, any `workoutDate` that is sent is ignored, and the stored date is the
    date of that mesocycle week. On `PUT`, the date is recalculated whenever a placement is sent, even an unchanged
    one.
- The API does not check that a standalone workout's date is a Wednesday. The front sends the Wednesday of the target
  week.
- The seeded linked workouts get the dates of their weeks, so the seed matches the new rule.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `mesocycles`:
  - Adds the week date rule (Wednesday of the start's ISO week, plus whole weeks).
  - "Defined weeks can be removed" states that the aggregate does not check workouts.
- `mesocycle-management`:
  - Adds `DELETE /mesocycles/{id}/weeks/{weekNumber}` to the auth, user-refusal and trainer-or-admin requirements.
  - Adds the week removal requirement with its `404`/`409` rules.
- `workout-creation`: "The workout date is required and stored without time" becomes conditional on there being no
  placement.
- `workout-details`: "The request replaces every editable property" no longer requires `workoutDate` when a placement
  is sent.
- `workout-mesocycle-placement`: adds the rule that a placed workout's date is derived from its week on `POST` and
  `PUT`, and that any sent date is ignored.

### Canonical template references

| Capability | Reference | Why |
| --- | --- | --- |
| Week removal use case | `UseCases/Mesocycles/RedefineMesocycleWeekUseCase.cs` | load, authorize with `MesocycleEditAuthorizer`, act on an inner entity through the root, save |
| Week removal route | `DELETE /workouts/{workoutId}/exercises/{exerciseId}` in `WorkoutsController` | same `204` delete shape and `ExceptionMapper.Map` catch |
| Week date | `Mesocycle.AddWeek` range guard | the aggregate owns the week range, so it owns the week's date |
| Workout date on create/update | `CreateWorkoutUseCase`, `UpdateWorkoutUseCase` with `MesocyclePlacementValidator` | the existing placement flow these use cases already follow |
| Workouts-in-week query | `IWorkoutRepository` / `IUserRepository.ExistsByEmail` | existing existence query pattern |

No deviation from these references is allowed.

### Business identifiers

- `(Mesocycle.Id, WeekNumber)` stays the immutable business identifier of a week and is the key of the `DELETE` route.
- A week defined again after removal gets a new internal `MesocycleWeek.Id`, generated with `Id.New()` and never
  exposed.
- `Workout.WorkoutDate` stays mutable. For a placed workout it can only be changed through its placement.
- No new identifiers.

## Impact

- **Domain:** `Mesocycle` gains `DateOfWeek(WeekNumber)`. Unit tests in `test/WeightTracker.Unit/Domain/Mesocycles`.
- **Application:**
  - New `RemoveMesocycleWeekUseCase`.
  - `CreateWorkoutUseCase` and `UpdateWorkoutUseCase` take an optional date and resolve it.
  - New `MesocycleWeekHasWorkoutsException` (`409`) and `MissingWorkoutDateException` (`400`).
  - `IWorkoutRepository.ExistsByMesocycleWeek`.
- **Infra:** `WorkoutRepository.ExistsByMesocycleWeek`. No migration, since it reads existing columns. The seeded
  linked workouts get new dates.
- **API:**
  - New `MesocyclesController` action.
  - `WorkoutDate` loses `[Required]` on `CreateWorkoutRequest` and `UpdateWorkoutRequest`.
  - New `ExceptionMapper` entries.
  - Registration in `Program.cs`.
- **Clients:** the front can omit `workoutDate` when it sends a placement. A date sent with a placement no longer
  changes anything.
- **Out of scope:**
  - Recalculating the dates of placed workouts when a mesocycle is rescheduled. They are refreshed on the workout's
    next `PUT`.
  - Duration changes against linked workouts.
  - Renumbering weeks.
  - Deleting mesocycles.
  - A data migration for existing rows.
  - Front changes.
