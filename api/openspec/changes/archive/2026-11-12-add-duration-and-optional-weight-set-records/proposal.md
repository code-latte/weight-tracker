# Proposal

## Why

A set record always carries reps and a weight, both required, and it is only allowed on exercises whose prescriptions are all rep-based. A plank (40 s) cannot be recorded at all, and a bodyweight exercise such as jumping jacks forces the athlete to type a weight. Athletes need to track every set they do, even when there is nothing to measure beyond "done". No client uses the set-record routes yet, so the request and response can change shape freely.

Source brief: `.stpr/plan/openspec/proposal-change-add-duration-and-optional-weight-set-records.md`.

## What Changes

- `SetRecord` holds a `SetResult` (either reps or a duration in seconds) and an optional `Weight`. A `null` weight means "not tracked". `0` still means bodyweight.
- New value objects:
  - `SetResult`, a closed hierarchy shaped like `SetTarget` with `Reps(RepCount)` and `Duration(DurationInSeconds)`;
  - `DurationInSeconds`, a whole number of seconds ≥ 1.
- An exercise can have set records when its prescriptions are all rep-based or all time-based (`Duration`, `Minutes`). An exercise that mixes the two kinds still cannot.
- A result must match its exercise's kind. A mismatch fails with `SetRecordNotAllowedForTargetException` (409).
- **Done as prescribed.** `Workout.AddSetRecord` accepts an empty result and fills it in from the next prescribed set:
  - `Reps(n)` becomes `n` reps;
  - `Duration(s)` becomes `s` seconds;
  - `Minutes(m)` becomes `m × 60` seconds, rounded to the nearest second;
  - `RepsRange`, `MaxReps`, or a set beyond the prescription fails with a new `SetResultRequiredException` (400).
- `Workout.UpdateSetRecord` takes an explicit result and an optional weight.
- The guard in `WorkoutExercise.Replace` changes: while an exercise has set records, its prescriptions cannot switch to the other kind, in either direction.
- `POST` and `PUT .../set-records` take `{ reps?, durationSeconds?, weight? }`:
  - sending both `reps` and `durationSeconds` is 400;
  - `POST` with neither means "done as prescribed";
  - `PUT` must send exactly one of them.
- `SetRecordResponse` becomes `(Id, Type, Reps?, DurationSeconds?, Weight?, RegisteredAtUtc)`, with `Type` set to `"Reps"` or `"Duration"`.
- New migration on `SetRecords`:
  - `Reps` and `Weight` become nullable;
  - a new nullable `DurationSeconds` column;
  - a check constraint that exactly one of `Reps` and `DurationSeconds` is set.
- `ExceptionMapper` maps `SetResultRequiredException` and `InvalidDurationInSecondsException` to 400.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `workout-set-records`: set records can hold a duration instead of reps, and the weight becomes optional. A set can be checked as done with an empty body. Recordable exercises are those of a single kind, and the guard on later prescription changes keeps that kind.

## Canonical Template References

- `workout-set-records`, value objects -> `SetTarget`, `RepCount` and `TimeInMinutes` in `src/WeightTracker.Domain/Workouts` -> same closed record hierarchy and `From` factory pattern.
- `workout-set-records`, aggregate -> `Workout.AddSetRecord` / `UpdateSetRecord` and `WorkoutExercise.Replace` -> the current implementation; only the parameters and the target guard change. Tests follow `WorkoutTests` (`.stpr/template/test/WeightTracker.Unit/Domain/Workouts`).
- `workout-set-records`, application -> `AddSetRecordUseCase` / `UpdateSetRecordUseCase` -> same orchestration with nullable inputs.
- `workout-set-records`, API -> `WorkoutsController` set-record actions, `AddOrUpdateSetRecordRequest`, `SetRecordResponse` -> same routes, same DTOs reshaped.
- `workout-set-records`, persistence -> `WorkoutMapper`, `WorkoutRepository.ReconcileSetRecords`, `WeightTrackerDbContext` -> existing set-record mapping.
- `workout-set-records`, migration -> `202611101200_AddCompletedAtToWorkouts` -> nearest migration that alters an existing table.

## Classification And Identifiers

- Workouts is a mutable aggregate, not a catalog. Set records are mutable business state changed only through the aggregate root.
- `SetRecord.Id` is unchanged: a technical identifier from `Id.New()`, immutable. A record's kind (reps or duration) is fixed by its exercise. No new unique business identifiers.

## Conflicts With Template Or Invariants

- None. Recording stays outside the workout edit rule, as `STPR_INVARIANTS.md` already states. No deviations from the template.

## Acceptance Criteria

- A set of a duration exercise can be recorded with an explicit number of seconds, or with an empty body that copies the prescription.
- A set of a rep exercise can be recorded without a weight, and an empty body records the prescribed reps when the target is `Reps(n)`.
- An empty body is refused (400) for `RepsRange`, `MaxReps` and sets beyond the prescription. A result of the wrong kind and an exercise that mixes kinds are refused (409). Nothing changes in either case.
- An exercise with set records cannot switch to the other kind of prescription.
- Existing reps-and-weight records load unchanged after the migration.
- `dotnet test WeightTracker.slnx` passes with Docker running.

## Impact

- Domain: `SetRecord`, `WorkoutExercise`, `Workout`, new `SetResult`, `DurationInSeconds`, `SetResultRequiredException` and `InvalidDurationInSecondsException`.
- Application: `AddSetRecordUseCase`, `UpdateSetRecordUseCase`.
- Api: `WorkoutsController`, `AddOrUpdateSetRecordRequest`, `SetRecordResponse`, `ExceptionMapper`.
- Infra: `SetRecordEntity`, EF configuration, `WorkoutMapper`, `WorkoutRepository.ReconcileSetRecords`.
- Migrations: new `AddDurationToSetRecords`. Its `Down()` is destructive: it deletes duration records.
- Tests: unit tests (value objects, aggregate, use cases, mapper) and integration tests on `POST` and `PUT .../set-records`.
