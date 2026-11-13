# Design

## Context

See proposal.md for motivation and specs for the rules. Current state that shapes the approach:

- `AggregateRoot` collects events (`AddDomainEvent`, protected `ClearDomainEvents`), and `IEventDispatcher` delivers them to auto-registered `IEventHandler<T>` (scoped, sequential, exceptions propagate). Nothing raises or dispatches yet.
- `ChangeWorkoutStatusUseCase` loads the workout, authorizes via `WorkoutStatusAuthorizer`, calls `ChangeStatus` or `OverrideStatus` (both end in `Workout.SetStatus`), saves and commits once.
- `WorkoutExercise` holds `ExerciseId`, `PerSide`, prescriptions as `SetPrescription(count, target)` and `SetRecords` (result + optional weight). Exercise position is its index in `Workout.Exercises`.
- `IExercisePreviousExecutionQuery` already reads "the owner's latest earlier finished workout per exercise", with a `prescribedBy` filter. It returns one execution, without targets, per-side or week type.
- A workout placed in a mesocycle has `MesocycleId` + `MesocycleWeekNumber`; the week type is the `TrainingWeekType` column of `MesocycleWeeks`.
- Options are read in `Program.cs` from a section with a `SectionName` constant and fail fast (`TrainerInvitationOptions`).

## Goals / Non-Goals

**Goals:**
- Rules and highlight selection are pure domain code, unit-testable without mocks of history.
- One history read per completion, independent of how many exercises the workout has.
- Event wiring that later producers can copy: raise in the aggregate, dispatch after commit, clear by pulling.

**Non-Goals:**
- A generic "dispatch after commit" inside `UnitOfWork` or the repositories. Only `ChangeWorkoutStatusUseCase` dispatches.
- Reusing `GetWorkoutSummaryUseCase` code paths; the summary is unchanged.

## Decisions

### Layer map

| Layer | New / changed |
|---|---|
| Domain/Common | `AggregateRoot.PullDomainEvents()`: returns a copy of the events and clears them. |
| Domain/Workouts | `WorkoutCompletedEvent(Id WorkoutId)`; `Workout.SetStatus` raises it when the target is `COMPLETED`. |
| Domain/Workouts/Observations | `WorkoutObservations` (aggregate root), `WorkoutObservation` (entity), `ObservationCode` (enum, names are the public codes), `ObservationSeverity`, `HighlightPriority`, `ObservationEvidence` (value object), `ObservationThresholds` (value object), `ExerciseExecution` (history input record), `WorkoutObservationGenerator` (domain service). |
| Application | `IExerciseExecutionHistoryQuery` + view; `IWorkoutObservationsRepository`; `WorkoutObservationService`; `GenerateWorkoutObservationsOnCompleted : IEventHandler<WorkoutCompletedEvent>`; `GetWorkoutObservationsUseCase` + view; `ChangeWorkoutStatusUseCase` takes `IEventDispatcher`. |
| Infra | `ExerciseExecutionHistoryQuery` (`Infra/Queries`); `WorkoutObservationsRepository`, `WorkoutObservationsMapper`, EF entity `WorkoutObservationEntity`, `DbContext` configuration. |
| Migrations | `202611131637_CreateWorkoutObservationsTable.cs`. |
| Api | `WorkoutsController.GetObservations`, `WorkoutObservationsResponse` DTOs; `appsettings.json` section; `Program.cs` registrations. |

Observations live in a `Observations` subfolder of `Domain/Workouts` because they are about workouts but are a separate aggregate; the namespace keeps `Workout` uncluttered. Cross-aggregate reference is by `WorkoutId` only.

### Event wiring: dispatch after commit, clear by pulling

`ChangeWorkoutStatusUseCase.Execute`: change status → `Save` → `SaveChangesAsync` → `DispatchAsync(workout.PullDomainEvents())` → return the workout.

- Why after commit: handlers react to persisted facts, and a failed save dispatches nothing. Agreed with the user.
- Why `PullDomainEvents` over a public `ClearDomainEvents`: one call both reads and clears, so a caller cannot dispatch and forget to clear, or clear before dispatching.
- Alternative rejected: dispatching inside `UnitOfWork.SaveChangesAsync` by scanning tracked aggregates. Repositories map aggregates to EF entities, so the `DbContext` never tracks aggregates; it would need a registry of saved aggregates. Too much machinery for one producer.
- The event carries only `WorkoutId`. The service reloads the workout through `IWorkoutRepository.GetById`, so the handler never trusts a stale in-memory copy and the event stays small. The extra read is one query per completion.

### The service owns its own commit

`WorkoutObservationService.Generate(workoutId, ct)`:
1. Load the workout. If it has no `TrainerId`, return (no observations, nothing saved).
2. Resolve the week type when the workout has a mesocycle placement (through the history query, see below).
3. Read history for the workout's exercise ids with `IExerciseExecutionHistoryQuery`.
4. `WorkoutObservationGenerator.Generate(workout, weekType, history, thresholds, clock.UtcNow)` returns the new observation list and highlights.
5. Load the existing `WorkoutObservations` for the workout; if present, `Replace(...)` its content, else `WorkoutObservations.Create(...)`.
6. `Save` and `SaveChangesAsync` once.

This is the second `SaveChangesAsync` of the request. The first one already flushed the workout, so the shared scoped `DbContext` only carries observation changes. `STPR_INVARIANTS.md` gets the rule: "a handler that writes commits its own unit of work, once, after the triggering change is committed".

The handler is a one-liner calling the service, so the service can be unit-tested and reused (for example by a future backfill) without the dispatcher.

### Aggregate shape: one per workout, replaced in place

`WorkoutObservations` has `Id`, `WorkoutId`, `GeneratedAt` and a list of `WorkoutObservation`. Invariants checked in `Create` and `Replace`:
- at most 3 highlighted; ranks unique and contiguous from 1;
- only observations with a priority can be highlighted;
- at most one observation per `(code, exerciseId)`.

`Replace` swaps the whole list (new ids from `Id.New()`); the repository deletes the old rows and inserts the new ones for that `WorkoutObservations.Id`. Alternative rejected: one aggregate per observation. The "max 3 highlights" rule spans observations, so it needs a root that owns them all.

Persistence: one table, `WorkoutObservations`, one row per observation (`Id`, `WorkoutId`, `ExerciseId`, `Code`, `Severity`, `Priority`, `HighlightRank`, `Data` jsonb, `CreatedAt`), as specified in the brief. The aggregate root has no row of its own: its identity is the `WorkoutId` and its `Id` equals the `WorkoutId`. This keeps the table as the user described and avoids a header table with nothing but a key. Unique index on `(WorkoutId, Code, ExerciseId)` with nulls not distinct; partial unique index on `(WorkoutId, HighlightRank)` where `HighlightRank` is not null; check `HighlightRank BETWEEN 1 AND 3`. FK to `Workouts` with cascade delete so deleting a workout removes its observations; FK to `Exercises` restrict.

`Code` is stored as the enum name (varchar 64), `Severity` and `Priority` as smallint. `Data` is serialized by the mapper with `System.Text.Json`, camelCase, numbers only; `ObservationEvidence` holds an ordered `IReadOnlyDictionary<string, EvidenceValue>` where a value is a decimal or a list of decimals.

### Rules in a pure domain service

`WorkoutObservationGenerator` is stateless and takes everything as arguments: the completed `Workout`, the current week type (nullable), the history per exercise (`ExerciseExecution` lists, newest first), and `ObservationThresholds`. It returns observations with priorities already assigned, then applies the selection. This keeps every spec scenario testable with in-memory inputs.

Internally, one small private method per rule family (compliance, weight, volume, reps, streaks), each returning zero or one observation per exercise. Comparability filtering happens once per exercise before the comparison rules run. Expanding prescriptions: `SetPrescription(count, target)` is expanded to `count` positional targets, which is how recorded sets are matched (same as `WorkoutExercise.SetsInTarget`).

Magnitudes for ordering are computed per code: absolute `changePercent` for comparisons and records, `100 - completionPercent` for incomplete, share of sets below target for repeated misses, first-to-last percent drop for the downtrend. They are not stored; only `HighlightRank` is.

### History read projection

`IExerciseExecutionHistoryQuery.Get(ownerId, trainerId, exerciseIds, beforeDate, beforeWorkoutId, depth, ct)` returns, per exercise, up to `depth` executions newest first. Each has workout id, workout date, week type (nullable), `PerSide`, the expanded prescribed targets and the recorded sets (position, reps, duration seconds, weight). It also returns, per exercise, the comparable all-time max weight and best reps per weight, computed in SQL over the same filtered set, because records must look past `depth`.

- Filters: `UserId = ownerId`, `TrainerId = trainerId`, status `COMPLETED`/`REVIEWED`, strictly before `(WorkoutDate, Id)`, excluding `beforeWorkoutId`, at least one set record. Comparability by week type and per-side is applied in SQL for the records and in the domain service for the depth list (the service needs to know which ones it skipped).
- `depth = HistoryDepth`, derived as `max(RepeatedMissCount, PlateauCount, TrendCount, 2) + a margin of 4` so skipped deload sessions rarely starve a rule. Not configurable on its own; it follows the thresholds.
- Uses `IX_Workouts_UserId_WorkoutDate` and the `WorkoutExercises` exercise index. Verified with `EXPLAIN (ANALYZE, BUFFERS)` on a seeded database before closing the task; the result goes in tasks.md.
- Why not extend `IExercisePreviousExecutionQuery`: it serves the summary, returns one execution and no targets; widening it would change the summary's query plan for no benefit.
- The current workout's own week type comes from the same query (a small extra lookup by `MesocycleId`/week number), so the service needs no `IMesocycleRepository`.

### Thresholds

`WorkoutObservationsOptions` (Infra/Services, like `TrainerInvitationOptions`, with `SectionName = "WorkoutObservations"`) is read in `Program.cs`, converted to the domain `ObservationThresholds` (whose constructor validates: counts ≥ 2, percents in (0, 100)), and registered as a singleton. An invalid value throws at startup. Defaults in `appsettings.json`: 3, 4, 3, 70, 15, 5, 10.

### Read endpoint

`GetWorkoutObservationsUseCase.Execute(callerId, callerRole, workoutId, ct)`: load workout (404), `WorkoutVisibility.CanSee` (403, as the summary does), load `WorkoutObservations` by workout id, return a view (empty lists when none). The controller maps to `{ observations: [...], highlights: [...] }`; `data` is emitted as a raw JSON object. Ordering: workout-level first, then exercise position in the workout, then `ObservationCode` declaration order.

### Adjustments found while implementing

- `WorkoutCompletedEvent` carries `(Id WorkoutId, DateTime OccuredOnUtc)`, because `IDomainEvent` already requires `OccuredOnUtc`. Handlers still only use the workout id.
- `WorkoutObservations` has no `GeneratedAt`: the table has no column for it and each observation already carries `CreatedAt`.
- `ExceptionMapper` turns any unmapped `DomainException` into `400`. The two new exceptions (`InvalidWorkoutObservationException`, `InvalidObservationThresholdsException`) are server faults, so they are mapped explicitly to `500`; otherwise a generation failure would answer `400` and break the spec.
- The history projection takes the anchor workout and returns an `ExecutionHistoryView`: the anchor's week type plus an `ExerciseHistory` per exercise (executions and records). Records are grouped in SQL by per-side setting, result kind and weight, so they stay small however long the history is.
- `.stpr/template/src` holds copies of `Workout.cs`, `AggregateRoot.cs` and `WorkoutTests.cs`; they were refreshed from `src/` and `test/` to keep the parity rule.
- Measured plan: the sessions query does not stop early. It reads the athlete's sessions with the trainer and sorts top-N (see tasks 5.2). A `Workouts (UserId, TrainerId, WorkoutDate DESC, Id DESC)` index would restore the early stop if it ever matters.

## Risks / Trade-offs

- [Completion and observations are not atomic] → A failure leaves a completed workout without observations and a `500`. Accepted; an admin re-completion regenerates. Unit test covers it.
- [Client retries the `500`] → The retry hits `COMPLETED → COMPLETED` and gets `409`. Documented in the action's XML docs.
- [Stale snapshot after set record corrections] → Accepted by the user; out of scope.
- [History query cost for long histories] → Index-driven, bounded by `depth`; the record aggregates scan only the athlete's sessions with that trainer for the exercises in the workout. Verified with `EXPLAIN`.
- [Floating point in percents] → Weights are `decimal`; percents computed in `decimal` and rounded half away from zero to 2 decimals.
- [Code drift] → Codes are enum names; a unit test pins the full list so a rename fails the build.

## Migration Plan

- Deploy the migration, then the API. `Down()` drops the table; no existing data is touched.
- Workouts completed before deployment have no observations and return empty lists.
- Rollback: redeploy the previous API and run the migrator with `--down` for this migration only if the table must go.
