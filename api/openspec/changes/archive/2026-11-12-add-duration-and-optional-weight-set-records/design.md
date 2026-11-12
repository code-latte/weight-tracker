# Design

## Context

See proposal.md (Why) and `specs/workout-set-records/spec.md` for the required behavior.

Current state that shapes the approach:

- `SetRecord` (entity of the `Workout` aggregate) holds `RepCount Reps`, `Weight Weight` and `RegisteredAtUtc`. `Change(reps, weight)` is internal.
- `WorkoutExercise.AddSetRecord` and `Replace` both guard with `AreRepBased(prescriptions)`.
- `WorkoutExercise` keeps its prescriptions as an ordered list of `SetPrescription(count, target)`.
- Set records can be reordered (`ReorderSetRecords`) and removed.
- `SetRecords` table: `Reps int NOT NULL`, `Weight numeric(6,2) NOT NULL`. `ReconcileSetRecords` updates `Position`, `Reps` and `Weight` on surviving rows.
- No client uses the set-record routes yet, so the DTOs can be reshaped without versioning.

## Goals / Non-Goals

**Goals:**

- Record timed sets and sets without weight through the existing routes.
- One call checks a set as done when the prescription defines its value.
- Every rule stays in the aggregate. The use cases only convert primitives to value objects and delegate.

**Non-Goals:**

- A skipped or failed status. A set that wasn't done has no record.
- Recording on exercises that mix rep-based and time-based prescriptions.
- Distance, calories or other measurements.

## Decisions

### Layer map

| Layer | Change |
|---|---|
| Domain | `SetResult`, `DurationInSeconds`, `SetRecord` reshaped, `Workout.AddSetRecord` / `UpdateSetRecord` signatures, kind guard in `WorkoutExercise`, two new exceptions |
| Application | `AddSetRecordUseCase` / `UpdateSetRecordUseCase` take `int? reps, int? durationSeconds, decimal? weight` |
| Infrastructure | `SetRecordEntity` (`Reps?`, `Weight?`, `DurationSeconds?`), EF configuration, `WorkoutMapper`, `ReconcileSetRecords` also copies `DurationSeconds`, migration `AddDurationToSetRecords` |
| Api | `AddOrUpdateSetRecordRequest` all optional plus a structural "not both" check, `SetRecordResponse` reshaped, `ExceptionMapper` entries, XML docs on the two actions |

No cross-aggregate dependencies are added.

### `SetResult` is a value object, weight stays separate

`SetResult` is an abstract record with `Reps(RepCount Count)` and `Duration(DurationInSeconds Value)`, shaped like `SetTarget`. Weight is not part of it, because both kinds can carry a load (weighted plank) or none. `SetRecord` exposes `SetResult Result` and `Weight? Weight`.

### Exercise kind

`WorkoutExercise` derives a private kind from its prescriptions:

- **rep-based** when every target is `Reps`, `RepsRange` or `MaxReps`;
- **time-based** when every target is `Duration` or `Minutes`;
- **mixed** otherwise.

This replaces `AreRepBased`:

- `AddSetRecord` and `UpdateSetRecord` refuse a mixed exercise, and a result whose kind differs from the exercise's, with `SetRecordNotAllowedForTargetException`.
- `Replace` refuses, while set records exist, new prescriptions whose kind differs from the current one.

All of these checks run before any mutation. Because every record of an exercise has the same kind, reordering and removal need no check.

### Resolving "done as prescribed"

`Workout.AddSetRecord(exerciseId, SetResult? result, Weight? weight, registeredAtUtc)`. When `result` is `null`, `WorkoutExercise` resolves it:

1. It expands the prescriptions in order into one target per set (`2x10, 1x8` gives 10, 10, 8).
2. It takes the target at index `SetRecords.Count`. If there is none, it throws `SetResultRequiredException`.
3. It converts that target:
   - `Reps(n)` becomes `SetResult.Reps(n)`;
   - `Duration(s)` becomes `SetResult.Duration(s)`;
   - `Minutes(m)` becomes `SetResult.Duration(round(m × 60))`, with midpoints rounded away from zero and a minimum of 1;
   - `RepsRange` or `MaxReps` throws `SetResultRequiredException`.

The position is the count of existing records, not a set index sent by the client. It follows what the athlete has done so far, and it stays correct after removals. `UpdateSetRecord` never resolves anything; its result is mandatory.

### Request mapping

The controller passes the nullable primitives through. The use case builds:

- `SetResult.Reps(RepCount.From(reps))` when `reps` has a value;
- `SetResult.Duration(DurationInSeconds.From(seconds))` when `durationSeconds` has a value;
- `null` when neither does.

The use case also builds `Weight.From(weight)` when `weight` has a value. All of this happens before loading the workout, so invalid values fail first.

Validation is split this way:

- **DTO (`IValidatableObject`, structural):** sending both `reps` and `durationSeconds` is 400, on both routes.
- **`UpdateSetRecordUseCase`:** a missing result throws `SetResultRequiredException`, which maps to 400. The rule that an edit must state its value lives in the application layer, because the aggregate signature already requires a result.

### Persistence

Migration `AddDurationToSetRecords`:

- **`Up()`:**
  - alters `Reps` and `Weight` to nullable;
  - adds `DurationSeconds int NULL`;
  - adds the check constraint `CK_SetRecords_RepsXorDuration`: `("Reps" IS NULL) <> ("DurationSeconds" IS NULL)`.
- **`Down()`**, destructive and run in this order:
  1. deletes rows where `DurationSeconds IS NOT NULL`;
  2. updates rows with a null `Weight` to `0`;
  3. drops the constraint and the column;
  4. makes `Reps` and `Weight` not null again.

There is no type column: the kind is read from which column is set. `WorkoutMapper.MapToDomain` throws `PersistenceMappingException` when both or neither are set, or when a value is invalid. EF configuration marks the three columns optional, with no EF Core migration.

## Risks / Trade-offs

- **Rounding minutes to seconds.** A `Minutes(1.01)` target (60.6 s) records 61 s. This is acceptable because athletes log whole seconds.
- **An empty body for `Reps(n)` may hide a short set.** The athlete corrects it with `PUT`. This is an explicit product choice in the brief.
- **`Down()` loses duration records.** It is documented and only used in development.

## Migration Plan

1. Run the migrator against a throwaway Postgres container: up, `--down`, then up again. Never run `--down` on the development database.
2. Existing rows have `Reps` set and `DurationSeconds` null, so they satisfy the constraint without a backfill.
