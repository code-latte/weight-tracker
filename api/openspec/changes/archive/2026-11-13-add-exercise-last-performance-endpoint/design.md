# Design

## Context

See proposal.md (Why) and `specs/exercise-last-performance/spec.md` for the behavior.

Relevant current state:

- Set records live in `SetRecords` (`WorkoutExerciseId`, `Position`, `Reps`, `DurationSeconds`, `Weight`), under `WorkoutExercises` (`WorkoutId`, `ExerciseId`), under `Workouts` (`UserId`, `WorkoutDate` as `date`, `Status` as the integer value of `WorkoutStatus`, `Id` as a time-ordered `Id.New()` value).
- Existing indexes: `IX_Workouts_UserId`, the unique `WorkoutExercises (WorkoutId, ExerciseId)` and `IX_SetRecords_WorkoutExerciseId`.
- `IWorkoutRepository` returns `Workout` aggregates, hydrated with every child table through `WithAggregateTables`.
- `GetExerciseByIdUseCase` resolves an exercise and calls `ExerciseVisibility.EnsureVisible`, throwing `ExerciseNotFoundException` (`404` in `ExceptionMapper`).
- Guid route segments use the `:guid` constraint, so a malformed id never reaches the action and ends in `404`.

## Goals / Non-Goals

**Goals:**

- Answer with at most two short, index-driven SQL statements whose cost depends on how far back the caller last did the exercise, not on the size of their history or of the whole table.
- Keep the layering of the other `me` routes: controller -> use case -> repository interface in Application -> EF implementation in Infra.

**Non-Goals:**

- No domain change. `Workout`, `WorkoutExercise` and `SetRecord` are not touched and no aggregate method is added.
- No caching, no denormalized "last performance" table, no change to how set records are written.

## Decisions

### Layer mapping

- **Api**: `UsersController` gets `[HttpGet("me/exercises/{exerciseId:guid}/last-performance")]`. It calls the use case with `CurrentUser`, returns `NoContent()` for `null` and `Ok(ExerciseLastPerformanceResponse.From(view))` otherwise, and maps every exception through `ExceptionMapper.Map`. DTOs live in `src/WeightTracker.Api/Endpoints/Users/Dtos`: `ExerciseLastPerformanceResponse(DateOnly WorkoutDate, string Effort, IReadOnlyList<LastPerformanceSetResponse> Sets)`, `Effort` as the enum name like `WorkoutByIdResponse`, and `LastPerformanceSetResponse(int? Reps, int? DurationSeconds, decimal? Weight)`.
- **Application**: `GetMyExerciseLastPerformanceUseCase` in `UseCases/Users`. It loads the exercise with `IExerciseRepository.GetById`, throws `ExerciseNotFoundException` when it is missing, calls `ExerciseVisibility.EnsureVisible`, then returns `IExerciseLastPerformanceQuery.Get(callerId, exerciseId)`. Read models `ExerciseLastPerformanceView(DateOnly WorkoutDate, Effort Effort, IReadOnlyList<LastPerformanceSetView> Sets)` and `LastPerformanceSetView(int? Reps, int? DurationSeconds, decimal? Weight)` sit next to the use case, as `UserWithTrainerView` does.
- **Domain**: no change.
- **Infrastructure**: `ExerciseLastPerformanceQuery` in `Infra/Queries` and a new index in `WeightTrackerDbContext`, plus a FluentMigrator migration.
- **Cross-module dependencies**: the use case depends on Exercises (`IExerciseRepository`, `ExerciseVisibility`) for existence and visibility, and on the Workout tables through `IExerciseLastPerformanceQuery` for the read. Workouts never call Exercises.

The exercise check runs before the read so that an unknown or hidden exercise is `404` even when no record exists, and `204` keeps meaning "visible but never done".

### Read projection, not aggregate

`IExerciseLastPerformanceQuery.Get` returns `ExerciseLastPerformanceView?` built from a no-tracking projection, following "Read projections" in `STPR_INVARIANTS.md`. Hydrating `Workout` would load every exercise, prescription and record of every candidate workout to read three columns of a few rows.

Alternatives: a `GetLastPerformance` method on `IWorkoutRepository` was rejected because repositories take and return domain types only and never load a fragment of an aggregate. Reusing `GetAllByUserId` and filtering in memory was rejected: it reads the whole history.

The set values stay primitive (`int?`, `decimal?`) in the view because they are read back, never validated or mutated. The query maps what the database already guarantees through `CK_SetRecords_RepsXorDuration`.

### Query

Step 1 finds the reference `WorkoutExercises` row, its date and its prescribed effort:

```sql
SELECT w."WorkoutDate", we."Effort", we."Id"
FROM "Workouts" w
JOIN "WorkoutExercises" we ON we."WorkoutId" = w."Id" AND we."ExerciseId" = @exerciseId
WHERE w."UserId" = @userId
  AND w."Status" IN (4, 5)                       -- COMPLETED, REVIEWED
  AND EXISTS (SELECT 1 FROM "SetRecords" sr WHERE sr."WorkoutExerciseId" = we."Id")
ORDER BY w."WorkoutDate" DESC, w."Id" DESC
LIMIT 1;
```

Step 2 reads its sets:

```sql
SELECT sr."Reps", sr."DurationSeconds", sr."Weight"
FROM "SetRecords" sr
WHERE sr."WorkoutExerciseId" = @workoutExerciseId
ORDER BY sr."Position";
```

Both are written as LINQ (`AsNoTracking`, `Where`/join, `OrderByDescending(WorkoutDate).ThenByDescending(Id)`, `Select` into an anonymous projection, `FirstOrDefaultAsync`; then `Where`/`OrderBy(Position)`/`Select`/`ToListAsync`). Status values come from `(int)WorkoutStatus.COMPLETED` and `(int)WorkoutStatus.REVIEWED`, never literals. Step 2 does not run when step 1 finds nothing.

Two statements instead of one: a single LINQ statement with a nested collection under `FirstOrDefault` makes EF emit a lateral join or a split query whose plan is harder to keep stable, and step 2 costs one index lookup. One round trip more is cheaper than an unpredictable plan.

### Index

New index `IX_Workouts_UserId_WorkoutDate_Id` on `Workouts ("UserId" ASC, "WorkoutDate" DESC, "Id" DESC)`.

- It serves the filter and the full `ORDER BY` of step 1, so Postgres walks the caller's workouts newest first with no sort and stops at the first row that passes the probes. `Id` is in the index so ties on the date need no extra sort step. (The brief named it `IX_Workouts_UserId_WorkoutDate`; adding `Id` is the only refinement.)
- Per candidate workout, the probe into `WorkoutExercises` is a point lookup on the unique `(WorkoutId, ExerciseId)` index, and the `EXISTS` is a lookup on `IX_SetRecords_WorkoutExerciseId`. Step 2 uses the same `SetRecords` index; sorting a handful of rows by `Position` does not justify a composite index.
- `Status` is checked on the heap row, not indexed: almost every workout with set records is finished, so a partial index would barely filter and would tie the index to status values.
- Deferred: an index on `WorkoutExercises (ExerciseId, WorkoutId)`. Measurements show it fixes the rare and never-done cases (see Risks), but it is left out for now by decision.
- `IX_Workouts_UserId` becomes redundant but is kept; dropping it is outside this change.

EF declares the same index: `HasIndex(w => new { w.UserId, w.WorkoutDate, w.Id }).IsDescending(false, true, true).HasDatabaseName("IX_Workouts_UserId_WorkoutDate_Id")`.

### Tests

- Unit (`test/WeightTracker.Unit/Application/Users`): the use case returns the view, returns `null`, throws `ExerciseNotFoundException` for a missing exercise and for a hidden one, and does not read workouts in either failing case. Repositories and visibility dependencies are mocked as in the existing exercise use case tests.
- Integration (`test/WeightTracker.Integration/Users`): every scenario of the spec. Workouts, statuses and set records are arranged through the API (`GivenWorkoutStatus`, set record `POST`s), as `SetRecordEndpointTest` does, never by writing rows. Shared helpers go in a small base class only if two or more test classes need them.
- Migration: up, down and up on a throwaway Postgres, like the previous migration.

## Risks / Trade-offs

- [For an exercise that is rare in the data or never done (the `204` path), Postgres starts from `WorkoutExercises` and, with no index on `ExerciseId`, seq-scans the whole table] → Accepted for now. Measured on 51.5k workouts / 154k workout exercises / 463k set records (one user with 1,500 workouts): common exercise 0.25 ms walking `IX_Workouts_UserId_WorkoutDate_Id` with no sort; never-done exercise ~15 ms and exercise only in the oldest workout ~5 ms, both by seq scan, growing with the table across all users. A `WorkoutExercises (ExerciseId, WorkoutId)` index brought them to 0.07 ms and 0.9 ms (common case ~1 ms). Add it in a later migration when the table grows.
- [The planner ignores the index on small tables and seq-scans] → Expected and harmless at small sizes.
- [A user who has not done the exercise in a long time makes step 1 walk many of their workouts] → Each step is two point lookups, so even hundreds of workouts stay in the low milliseconds. Not worth a denormalized table now.
- [`WorkoutDate` is the planned day, which may differ from the day the sets were actually recorded] → Accepted: it is the date the workout is known by everywhere else. Ordering by the time of the sets would require scanning the history.
- [A read projection can drift from the aggregate's own rules, for example about which statuses count] → It only reads, never feeds a write, and its rules are pinned by the query integration tests.

## Migration Plan

- New migration with the next free timestamp after `202611121200_AddDurationToSetRecords`: `Up()` creates `IX_Workouts_UserId_WorkoutDate_Id`, `Down()` deletes it. No data is touched, so rollback is lossless.
- Deploy is additive: the index can exist before the code that uses it, and the code works (slower) without it.
