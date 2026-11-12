# Tasks

## 1. Value objects and exceptions

- [x] 1.1 Add `DurationInSeconds` (int ≥ 1, `From` factory) and `InvalidDurationInSecondsException`, following `RepCount`. Verify with `DurationInSecondsTests`: 1 and 45 are accepted, 0 and negative values are rejected.
- [x] 1.2 Add `SetResult` with `Reps(RepCount)` and `Duration(DurationInSeconds)`, following `SetTarget`. Verify with `SetResultTests` that equal values are equal and the two kinds are never equal.
- [x] 1.3 Add `SetResultRequiredException` deriving from `DomainException`. Verify the build compiles with warnings as errors.

## 2. Aggregate

- [x] 2.1 Reshape `SetRecord` to `SetResult Result` plus `Weight? Weight` in `Create`, `Rehydrate` and `Change`. Verify that the existing `SetRecord` tests are updated and pass.
- [x] 2.2 Replace `AreRepBased` in `WorkoutExercise` with the kind (rep-based, time-based, mixed) and apply it in `AddSetRecord`, `UpdateSetRecord` and `Replace` before any mutation. Verify with `WorkoutTests`:
  - a mixed exercise is refused;
  - a reps result on a time-based exercise is refused, and so is a duration result on a rep-based one;
  - `UpdateExercise` with set records refuses a switch to the other kind, in both directions, and the exercise is unchanged;
  - `UpdateExercise` accepts a change within the same kind;
  - an exercise without records can switch freely.
- [x] 2.3 Resolve a `null` result in `WorkoutExercise` from the expanded prescription at index `SetRecords.Count`. Verify with `WorkoutTests`:
  - `Reps(10)` resolves to 10 reps;
  - `Duration(45)` resolves to 45 s;
  - `Minutes(1.5)` resolves to 90 s, and `Minutes(1.01)` to 61 s;
  - with `2x10, 1x8` the third empty record resolves to 8;
  - after removing a record, the next empty record follows the new count;
  - `RepsRange`, `MaxReps` and a set beyond `TotalSets` throw `SetResultRequiredException` and nothing changes.
- [x] 2.4 Update the `Workout.AddSetRecord(exerciseId, SetResult?, Weight?, registeredAtUtc)` and `UpdateSetRecord(exerciseId, setRecordId, SetResult, Weight?, updatedAtUtc)` signatures and XML docs (including the `<exception>` tags). Verify with `WorkoutTests`:
  - explicit reps and explicit duration are accepted, each with and without weight;
  - an update clears the weight to `null`;
  - the existing status, unknown exercise and ordering tests still pass;
  - records survive `Rehydrate`.

  Every test uses labelled Arrange / Act / Assert phases.
- [x] 2.5 Review: compare the domain code and tests against `SetTarget`, `RepCount`, `WorkoutTests` and `STPR_INVARIANTS.md` (input validated before mutation, no mutable state outside the root). Verify that `dotnet test test/WeightTracker.Unit` passes.

## 3. Application

- [x] 3.1 `AddSetRecordUseCase.Execute` takes `int? reps, int? durationSeconds, decimal? weight`, builds the value objects before loading the workout, and passes a `null` result when both are missing. Verify with use case tests: each input shape reaches the aggregate correctly, and invalid values fail before the repository is called.
- [x] 3.2 `UpdateSetRecordUseCase.Execute` takes the same inputs and throws `SetResultRequiredException` when neither `reps` nor `durationSeconds` is given. Verify with use case tests.
- [x] 3.3 Review against the current use cases (`WorkoutRecordingAuthorizer` call, save and unit of work unchanged).

## 4. API

- [x] 4.1 Make `AddOrUpdateSetRecordRequest` fields optional, add `DurationSeconds`, and validate structurally that `reps` and `durationSeconds` are not both sent. Update the XML docs, including the meaning of an empty body on `POST`. Verify the generated OpenAPI document shows all three fields as optional.
- [x] 4.2 Pass the nullable values from `AddSetRecord` and `UpdateSetRecord` in `WorkoutsController`, and update their summaries. Every catch still delegates to `ExceptionMapper.Map`.
- [x] 4.3 Reshape `SetRecordResponse` to `(Id, Type, Reps?, DurationSeconds?, Weight?, RegisteredAtUtc)`, with `Type` `"Reps"` or `"Duration"`.
- [x] 4.4 Map `SetResultRequiredException` and `InvalidDurationInSecondsException` to 400 in `ExceptionMapper`. Verify by review against the neighbouring entries.

## 5. Persistence

- [x] 5.1 Add migration `AddDurationToSetRecords` as described in design.md, with a destructive `Down()`. Verify by running the migrator against a throwaway Postgres container: up, `--down`, then up. Never run `--down` on the development database.
- [x] 5.2 Update `SetRecordEntity` (`int? Reps`, `decimal? Weight`, `int? DurationSeconds`) and the EF configuration. Verify the configuration matches the migration column by column.
- [x] 5.3 Update `WorkoutMapper` and `ReconcileSetRecords` (also copy `DurationSeconds`). Verify with `WorkoutMapperTests`:
  - round trip of a reps record with weight, a reps record without weight and a duration record with and without weight;
  - a stored row with both or neither of `Reps` and `DurationSeconds` raises `PersistenceMappingException`.
- [x] 5.4 Review against `202611101200_AddCompletedAtToWorkouts` and the existing set-record mapping. Confirm no EF Core migration was generated.

## 6. Integration tests

- [x] 6.1 `POST .../set-records`:
  - empty body on a `Reps(10)` exercise records 10 reps without weight;
  - empty body on a `Duration(45)` exercise records 45 s;
  - explicit `durationSeconds` with and without weight;
  - reps without weight;
  - `reps` and `durationSeconds` both sent is 400;
  - empty body on `RepsRange` is 400;
  - a duration on a rep exercise is 409;
  - each failure leaves the workout unchanged.

  Follow the existing set-record route tests.
- [x] 6.2 `PUT .../set-records/{id}`:
  - an empty body is 400;
  - correcting a duration and clearing the weight works;
  - the wrong kind is 409.
- [x] 6.3 Keep the existing auth matrix and status cases on both routes, and update their bodies to the new shape where needed. Update the `GET /workouts/{id}` assertions on set records to the new response shape.

## 7. Verification

- [x] 7.1 Run `dotnet build WeightTracker.slnx` with no warnings, then `dotnet test WeightTracker.slnx` with Docker running. Verify that the unit and integration suites pass.
- [x] 7.2 Run the template drift check from `AGENTS.md`. Verify it prints no `DRIFT:` lines.
- [x] 7.3 Walk the `AGENTS.md` quality checklist against the change and note any item that does not apply.

## Workflow follow-up

- Archive this change once it is reviewed and merged.
