# Design

## Context

See proposal.md - Why. The previous change (`archive/2026-11-07-add-workout-exercise-set-records`) left:

- `SetRecord : Entity` with `Id`, `Reps`, `Weight`, `RegisteredAtUtc`, held by `WorkoutExercise._setRecords`.
- `WorkoutExercise.SetRecords` returns a snapshot sorted by `RegisteredAtUtc`; the mapper writes `Position` from that snapshot and reloads by `Position`.
- `Workout.AddSetRecord(exerciseId, reps, weight, registeredAtUtc)` checks `STARTED`, the entry and the rep-based rule.
- `WorkoutRepository.ReconcileSetRecords` matches rows by id: inserts new ones, removes missing ones, and only updates `Position` on survivors.
- `IClock` exists in Application and is already used by auth use cases.

## Goals / Non-Goals

**Goals:**
- Edit, remove and reorder set records in the aggregate, with `Position` as the order.
- One recording authorizer shared by the four use cases.
- Four endpoints that return the updated workout with set records in the detail response.

**Non-Goals:**
- No new migration: `Position` already exists with the right type.
- No change to `WorkoutTemplate` or its mapper.
- No change to `WorkoutEditAuthorizer` or `WorkoutStatusAuthorizer`.

## Decisions

### Domain: explicit order

`WorkoutExercise.SetRecords` returns `_setRecords` in list order (still a snapshot copy). Add appends, remove removes, reorder rebuilds the list. The mapper already writes `Position` from snapshot index, so it needs no change, and `Rehydrate` already receives rows ordered by `Position`.

Alternative rejected: keep sorting by time and add a separate sort key. Two orderings would disagree after a reorder, and the user confirmed `Position` is the source of truth.

### Domain: new actions

On `Workout`, each action checks `Status == STARTED` first (`WorkoutNotStartedException`), then finds the entry by `ExerciseId` (`ExerciseNotInWorkoutException`), then delegates to an `internal` method on `WorkoutExercise`. This matches `AddSetRecord`.

- `Workout.UpdateSetRecord(Id exerciseId, Id setRecordId, RepCount reps, Weight weight)` → `WorkoutExercise.UpdateSetRecord` → `SetRecord.Change(reps, weight)` (`internal`). `Reps` and `Weight` become private-set; `Id` and `RegisteredAtUtc` stay get-only.
- `Workout.RemoveSetRecord(Id exerciseId, Id setRecordId)` → `WorkoutExercise.RemoveSetRecord`.
- `Workout.ReorderSetRecords(Id exerciseId, IReadOnlyList<Id> setRecordIds)` → `WorkoutExercise.ReorderSetRecords`. The id check mirrors `Workout.ReorderExercises`: same count, no duplicates, the same set of ids.
- Unknown set record id → new `SetRecordNotFoundException` (404).
- Order mismatch → new `SetRecordOrderMismatchException` (409), modeled on `WorkoutExerciseOrderMismatchException`.

The rep-based check is not repeated on edit, remove and reorder. Set records can only exist on rep-based entries, because `Replace` already refuses non-rep targets while records exist.

`RepCount` and `Weight` are built in the use case from request primitives through their `From` factories, so invalid values throw before the aggregate is touched.

### Application: recording authorizer

New `WorkoutRecordingAuthorizer` in `Application/Services`, shaped like `WorkoutEditAuthorizer`:

```
EnsureCanRecord(Workout workout, Id callerId, Role callerRole, string operation)
  ADMIN                                  -> allowed
  TRAINER                                -> UnauthorizedOperationException
  workout.UserId == callerId (USER)      -> allowed
  otherwise                              -> UnauthorizedOperationException
```

It does not check status. The aggregate does that after authorization, which keeps 403 ahead of 409.

Alternative rejected: reuse `WorkoutEditAuthorizer` with a flag. The two rules disagree on the prescribing trainer and the lock, so a flag would hide two rules in one service.

### Application: use cases

`AddSetRecordUseCase`, `UpdateSetRecordUseCase`, `RemoveSetRecordUseCase` and `ReorderSetRecordsUseCase` in `UseCases/Workouts`. Each one loads the workout (`WorkoutNotFoundException`), calls `EnsureCanRecord`, runs the aggregate action, then `Save` + `SaveChangesAsync`, and returns the `Workout`, as `ReorderWorkoutExercisesUseCase` does. `AddSetRecordUseCase` takes `IClock` and passes `_clock.UtcNow` as `registeredAtUtc`.

### API

- Routes live in `WorkoutsController` next to the exercise routes. `set-records/order` is declared as a literal segment, and `{setRecordId:guid}` cannot match `order`, so there is no route clash.
- DTOs: `AddOrUpdateSetRecordRequest(int? Reps, decimal? Weight)` with `[Required]`, and `ReorderSetRecordsRequest(List<Guid>? SetRecordIds)` with `[Required]`, mirroring `ReorderWorkoutExercisesRequest`.
- Response: `WorkoutExerciseDetailResponse` gains `SetRecords: IReadOnlyList<SetRecordResponse>` with `SetRecordResponse(Guid Id, int Reps, decimal Weight, DateTime RegisteredAtUtc)`. `WorkoutExerciseResponse` (list shape) is not changed.
- `ExceptionMapper`: `SetRecordNotFoundException` → 404, `SetRecordOrderMismatchException` → 409. `WorkoutNotStartedException`, `InvalidRepCountException` and `InvalidWeightException` are already mapped.

### Infra: reconcile edited values

`ReconcileSetRecords` currently copies only `Position` to surviving rows. It will also copy `Reps` and `Weight`. `RegisterDatetimeUtc` is still never rewritten. The old design said survivors "never rewrite their other values". That is narrowed: stored rows change only when the athlete explicitly edits them.

### Invariants document

In `.stpr/template/STPR_INVARIANTS.md` §Business authorization rules, the add-only note is replaced by a **Recording sets** bullet:

- covers add, edit, remove and reorder;
- allowed to an `ADMIN` always, otherwise to the owner (`UserId`) unless their role is `TRAINER`;
- 403 otherwise, with permission checked before status;
- not an edit, so neither the edit rule nor the lock applies;
- only while `STARTED`, enforced by the aggregate;
- one shared Application service.

## Risks / Trade-offs

- [Existing stored rows were positioned by time] → Data keeps the same order after deploy, since rows already load by `Position`. No backfill is needed.
- [Rule divergence: the prescribing trainer may edit content but not record] → Intended. It is captured in the invariants and covered by a 403 scenario on every route.
- [The `STARTED`-only window blocks corrections after finishing] → Accepted for now; a later change is planned (proposal Out Of Scope).
- [Changes to template files (`Workout.cs`, `WorkoutExercise.cs`, `WorkoutTests.cs`) drift from `.stpr/template`] → Copy them and run the drift check from `AGENTS.md`.

## Migration Plan

No schema change. Deploy as usual; rolling back the code leaves the data valid for the previous version, which reorders rows by time on its next save.
