# Proposal

## Why

A workout only stores what was prescribed (sets × target). Athletes need to log what they actually did in each set, such as "8 reps at 62.5 kg", against the exercise in the workout. This change adds that log to the `Workout` aggregate and to the database so a later change can expose it through use cases and endpoints.

Source brief: `.stpr/plan/openspec/proposal-change-add-workout-exercise-set-records.md`.

## What Changes

- New `SetRecord` entity inside the `Workout` aggregate. Each `WorkoutExercise` holds a `SetRecords` collection (reps, weight in kilograms, registration time in UTC).
- New aggregate action `Workout.AddSetRecord(exerciseId, reps, weight, registeredAtUtc)`:
  - only while the workout is `STARTED`;
  - only when every prescription of the exercise is rep-based (`Reps`, `RepsRange`, `MaxReps`, in any mix);
  - no cap on how many set records an exercise has.
- New value objects for the recorded reps and the weight.
- `Workout.UpdateExercise` refuses a `Duration` or `Minutes` prescription on an exercise that already has set records, for every caller including an `ADMIN`.
- New `SetRecords` table (FluentMigrator) with a cascading FK to `WorkoutExercises`. `WorkoutMapper`, `WorkoutRepository` and the EF model load and save set records.
- New domain exceptions with explicit `ExceptionMapper` statuses.
- `STPR_INVARIANTS.md` clarifies that adding a set record is not a content edit, so the workout edit rule and its `STARTED`/`FINISHED` lock do not apply to it.
- No use cases, DTOs or endpoints.

## Capabilities

### New Capabilities

- `workout-set-records`: recording the reps and weight actually performed for each set of a workout exercise, the rules for when that is allowed, and how existing set records constrain later changes to the exercise's prescriptions.

### Modified Capabilities

None. `workout-exercises` keeps its requirements. The guard against switching an exercise with set records to a time-based target is owned by `workout-set-records`, because it only exists once set records exist.

## Canonical Template References

- `workout-set-records`, domain -> `WorkoutTests` (`.stpr/template/test/WeightTracker.Unit/Domain/Workouts`) and the `Workout` / `WorkoutExercise` / `TimeInMinutes` pattern in `src/WeightTracker.Domain/Workouts` -> same aggregate, nearest child entity and decimal value object.
- `workout-set-records`, persistence -> `WorkoutMapper`, `WorkoutRepository`, `WeightTrackerDbContext` -> the aggregate's existing mapping and reconciliation.
- `workout-set-records`, migration -> `CreateWorkoutsTable` reference, shaped like `CreateWorkoutTemplateExerciseSetsTable_202611061202` (child table with a cascading FK).

## Classification And Identifiers

- Workouts is a mutable aggregate, not a catalog. Set records are mutable business state appended through the aggregate root.
- `SetRecord.Id` is a technical identifier generated with `Id.New()` and immutable. `WorkoutExerciseId` is immutable once the set record exists. No new unique business identifiers.

## Conflicts With Template Or Invariants

- The invariants lock `STARTED` and `FINISHED` workouts against content edits, and set records can only be added while `STARTED`. This is resolved by declaring set records not to be content edits, in `STPR_INVARIANTS.md`.
- No other deviations from the template.

## Acceptance Criteria

- An athlete's set record can be added to a rep-based exercise of a `STARTED` workout, and it survives a save and reload.
- Adding is refused for any other status, unknown exercise, an exercise with a time-based prescription, or invalid reps or weight, and nothing changes.
- An exercise with set records cannot be updated to a time-based prescription. Rep-based changes still work.
- `dotnet test WeightTracker.slnx` passes with Docker running.

## Impact

- Domain: `src/WeightTracker.Domain/Workouts` (`Workout`, `WorkoutExercise`, new `SetRecord` and value objects), `src/WeightTracker.Domain/Exceptions`.
- Infra: `WorkoutMapper`, `WorkoutRepository`, `WeightTrackerDbContext`, new `SetRecordEntity`.
- Migrations: new `CreateSetRecordsTable` migration.
- Api: `ExceptionMapper` entries only.
- Docs: `.stpr/template/STPR_INVARIANTS.md`.
- Tests: unit tests for the aggregate, the value objects and the mapper. No new integration tests, since there are no new routes.
