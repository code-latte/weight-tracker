# Design

## Context

See proposal.md (Why) for motivation and the specs for the requirements.

- `GET /workouts/{id}` already loads one `Workout` aggregate and gates it with `WorkoutVisibility.CanSee`. A workout holds a handful of exercises and set records, so loading it whole is cheap.
- `ExerciseLastPerformanceQuery` (`Infra/Queries`) already answers "the user's latest finished workout with set records for an exercise" through `IX_Workouts_UserId_WorkoutDate_Id`, walking the user's workouts newest first and stopping at the first match.
- Set records carry `SetResult` (reps or seconds) and an optional `Weight`. Prescriptions are `SetPrescription(count, SetTarget)` on `WorkoutExercise`, and set records are kept in position order.
- Read projections are allowed by "Read projections" in `STPR_INVARIANTS.md`: their own `I<Projection>Query` contract, no-tracking implementation in `Infra/Queries`, Application view records, authorization in the use case.

## Goals / Non-Goals

**Goals:**
- One definition of in-target, volume, estimated one-rep max and top set, in the domain, used by both endpoints.
- Bounded reads: the summary reads one workout plus one short lookup per exercise; the history reads at most `limit` sessions.

**Non-Goals:**
- Caching or persisting computed stats.
- Changing `ExerciseLastPerformanceQuery` or the last-performance route.
- Any index other than the one named under Decisions, and dropping the redundant `IX_Workouts_UserId`.

## Decisions

### Domain rules

- `SetTarget.IsMetBy(SetResult result) : bool`, one override per target kind (`Reps`: reps ≥ value; `RepsRange`: reps ≥ min; `MaxReps`: true for a reps result; `Duration`: seconds ≥ value; `Minutes`: seconds ≥ minutes × 60; any kind mismatch: false). The behaviour sits on the value objects it compares, and the existing `SetTargetTests` grow with it.
- `SetMetrics` (static, `Domain/Workouts`): `Volume(SetResult, Weight?) : decimal?` and `EstimatedOneRepMax(SetResult, Weight?) : decimal?`, `null` unless the result is reps and the weight is not null. The estimate is `weight × (1 + reps / 30)` rounded to two decimals with `MidpointRounding.AwayFromZero`. Volume is exact (reps × kg with two-decimal weights), so it needs no rounding.
- `TopSetComparer`: orders `(SetResult, Weight?, position)` by weight desc (null below `0`), reps desc, seconds desc, position asc.
- `WorkoutExercise.SetsInTarget() : (int inTarget, int evaluated)`: expands `Sets` by `count`, zips with `SetRecords` by position, and applies `IsMetBy`. It reads only its own state, so it belongs on the entity.
- Alternative rejected: computing these in the use cases with primitives. That violates "domain-significant primitives in use cases" and would leave the summary and the history free to drift.

### Summary flow (`GetWorkoutSummaryUseCase`)

1. `IWorkoutRepository.GetById` → `WorkoutNotFoundException`; `WorkoutVisibility.CanSee` → `UnauthorizedOperationException` (as `GetWorkoutByIdUseCase`).
2. Per exercise: `SetsInTarget`, volume via `SetMetrics`, top set via `TopSetComparer`.
3. Previous executions through `IExercisePreviousExecutionQuery.Get(ownerId, exerciseIds, beforeDate, beforeWorkoutId, prescribedBy, ct)`, returning a view per exercise found (workout id, date, set values). The use case maps the values into `SetResult` / `Weight` and applies `TopSetComparer`.
4. Totals: sums of `inTarget` / `evaluated`; workout volume is the sum of non-null exercise volumes, `null` when all are `null`.

### Index for rare and never-done exercises

New `IX_WorkoutExercises_ExerciseId_WorkoutId` on `WorkoutExercises (ExerciseId, WorkoutId)`, through a FluentMigrator migration whose `Down()` drops it, declared in `WeightTrackerDbContext` too (no EF migration). Without it, an exercise that is rare or never done makes Postgres start from `WorkoutExercises` and seq-scan it (see Risks). `exercise-last-performance` deferred this index by decision; the history route returns `200` with an empty list for a never-done exercise, which is a common path, so the index is added here and benefits both reads. Alternative rejected: leaving it out, because the cost grows with the table across all users, not with the caller's data.

### Visibility scoping of the projections

For finished workouts, `WorkoutVisibility` reduces to: admin or owner sees all of the owner's workouts; a trainer who is not the owner sees only the ones with `TrainerId` = caller. Unpublished drafts never qualify because only `COMPLETED` / `REVIEWED` count. The use cases translate this into an optional `prescribedBy` filter (`null` for admin and owner, the caller's id otherwise) passed to the projection, so the projections stay free of role logic. The summarised workout itself already passed `CanSee`, so a `USER` caller is always its owner.

### `IExercisePreviousExecutionQuery`

- Per requested exercise, the same shape as `ExerciseLastPerformanceQuery` step 1 with the extra anchor `(WorkoutDate, Id) < (beforeDate, beforeWorkoutId)` and the optional `TrainerId` filter, `LIMIT 1`; then one statement reading the set records of all found `WorkoutExercises` rows, ordered by `Position`.
- The anchor is `WorkoutDate <= beforeDate` plus `WorkoutDate < beforeDate OR Id < anchorId`. The first condition is redundant, but without it Postgres can only filter on the `OR` and discards every newer workout of the user before reaching the anchor (1,000 rows removed for an anchor in the middle of 3,000 workouts, 0.41 ms); with it the index range stops at the anchor date (1 row removed, 0.02 ms).
- Runs sequentially (one `DbContext`), one short index walk per exercise. A workout has a handful of exercises, so N small seeks beat one window-function query that EF translates poorly.
- Alternative rejected: generalising `ExerciseLastPerformanceQuery`. Its contract (caller-only, "now" anchor, single exercise) is already specified and tested; a second projection keeps both simple.

### `IExerciseHistoryQuery` and `GetUserExerciseHistoryUseCase`

- Use case order: validate `limit` (`InvalidHistoryLimitException` → `400`); if the caller is a `USER` and `userId` is not the caller → `UnauthorizedOperationException` (`403`) before any lookup, so it discloses nothing; load the user → `UserNotFoundException` (`404`); load the exercise → `ExerciseNotFoundException`, `ExerciseVisibility.EnsureVisible` (`404`); resolve `prescribedBy`; call the projection; map each session with `SetMetrics` (volume, best estimate) and the maximum non-null weight.
- Projection `Get(userId, exerciseId, prescribedBy, limit, ct)`: statement 1 selects the first `limit` qualifying `(WorkoutId, WorkoutDate, WorkoutExerciseId)` ordered by `WorkoutDate desc, Id desc`; statement 2 reads the set records of those `WorkoutExerciseId`s. Statement 2 is skipped when statement 1 is empty.
- `limit` is bound as `int?` from the query; a non-integer value fails model binding and `[ApiController]` answers `400`. The range check is in the use case so the rule is unit-tested; the limit is not a domain concept, so an Application exception is enough.
- Alternative rejected: aggregating max weight and volume in SQL. It would duplicate the volume rule and cannot express the Epley estimate with the same rounding without duplicating it too.

### API

- `WorkoutsController`: `[HttpGet("{workoutId:guid}/summary")]`, `ProducesResponseType` `200`, `401`, `403`, `404`.
- `UsersController`: `[HttpGet("{userId:guid}/exercises/{exerciseId:guid}/history")]` with `[FromQuery] int? limit`, `ProducesResponseType` `200`, `400`, `401`, `403`, `404`. No `RequireRole`: every role may call it, scoping is in the use case.
- DTOs in each controller's `Dtos` folder with `From` mappers: `WorkoutSummaryResponse`, `ExerciseSummaryResponse`, `SetsInTargetResponse`, `PreviousExecutionResponse`, `ExerciseHistorySessionResponse`.

## Risks / Trade-offs

- [For an exercise that is rare in the data or never done, Postgres starts from `WorkoutExercises` and, with no index on `ExerciseId`, seq-scans the whole table and sorts] → Fixed by the `WorkoutExercises (ExerciseId, WorkoutId)` index added in this change (see Decisions); `exercise-last-performance` had deferred it. Measured WITHOUT the index with `EXPLAIN (ANALYZE, BUFFERS)` on a throwaway Postgres 17 with 6,000 workouts (two users, 3,000 each), 24.6k workout exercises and 73.8k set records, first statement of the history projection, `limit` 10: exercise in every workout 5.4 ms (walks `IX_Workouts_UserId_WorkoutDate_Id`, no sort node, stops after 10 rows; 1.2 ms under a forced generic plan); exercise in every 10th workout 9.4 ms (same plan, 100 workouts walked); exercise only in the oldest workout 5.5 ms and never-done exercise 0.8 ms, both by seq scan of `WorkoutExercises` plus a sort, growing with the table across all users. The previous-execution statement uses the same plan: 0.02 ms for an anchor in the middle of the history, 0.14 ms for a recent one. With the index, on the same data: exercise only in the oldest workout 0.16 ms (bitmap scan of the new index, no seq scan) and never-done exercise 0.02 ms (index scan); the sort that remains is over 0 to 2 rows. The common cases are unchanged (0.3 ms and 0.7 ms on a warm cache). The migration ran up, down and up on a throwaway Postgres with the index present after each up.
- [The previous-execution lookup is one index walk per exercise] → Workouts hold few exercises; each walk is the last-performance pattern, already measured. Re-check with `EXPLAIN` on a seeded database (task 2.5).
- [A rarely done exercise walks the whole history of the user] → Same bound as last-performance (documented in its design): proportional to that user's workouts, never other users'.
- [Positional matching misreads a set recorded out of order] → Set records keep the order the athlete reorders them to; that order is the contract.
- [Decimal rounding of the estimate differs from client expectations] → Fixed in the spec (two decimals, away from zero) and covered by unit tests.

## Migration Plan

New migration with the next free timestamp after `202611131300_AddStartedAtToWorkouts`: `Up()` creates `IX_WorkoutExercises_ExerciseId_WorkoutId` on `WorkoutExercises` (`ExerciseId`, `WorkoutId`), `Down()` deletes it. No data is touched, so rollback is lossless. The index can exist before the code that uses it, and the routes work (slower for rare exercises) without it.
