## Change

Generate objective observations, and up to 3 highlights, when a workout prescribed by a trainer is completed. Generation is triggered by a `WorkoutCompleted` domain event delivered through the existing dispatcher, and the result is stored and exposed through `GET /workouts/{workoutId}/observations`.

## Objective

After a workout the trainer has to read every set to see what went well and what did not. This change compares what the athlete did against the prescription and against their earlier sessions of each exercise with the same trainer, and stores the result as measurable facts (observations). A small subset of them (highlights, at most 3) are the alerts or milestones worth the trainer's attention. Routine variation is recorded but never highlighted.

It is also the first producer for the dispatcher added by `add-event-dispatcher`: `Workout` raises an event, a use case dispatches it, a handler reacts.

Core principle: observations inform, highlights demand attention. Observations describe facts, never diagnose problems or prescribe changes.

## Capabilities

- `Workout` raises `WorkoutCompletedEvent` whenever it moves to `COMPLETED`, through the lifecycle or an admin override.
- `ChangeWorkoutStatusUseCase` dispatches the workout's events after the completion is committed, then they are cleared.
- An event handler calls an application service that, for workouts with a prescribing trainer only, loads the history it needs, runs the observation rules, selects the highlights and saves the result. Workouts without a trainer get no observations.
- Observations are stored per workout, with a stable code, severity, optional exercise, highlight priority, highlight flag and the numeric evidence as JSON.
- Completing a workout again (admin override back to `STARTED` and then `COMPLETED`) replaces its previous observations; it never accumulates them.
- `GET /workouts/{workoutId}/observations` returns `observations` (all of them) and `highlights` (at most 3, referencing observation ids, in rank order).
- Rule thresholds and highlight thresholds are read from configuration.

## Module Classification

- Workouts -> mutable aggregate -> `src/WeightTracker.Domain/Workouts/Workout.cs`. Gains the event raising only; no new state, no change to its tables.
- Workout observations -> new aggregate, immutable once generated and replaced as a whole -> `src/WeightTracker.Domain/Workouts` (`Workout` as reference for Create / Rehydrate / IsNew). One aggregate per workout (`WorkoutObservations`, identified by its `WorkoutId`, holding the list of `WorkoutObservation` entities). The root has no row of its own. It is not a catalog and not a non-domain record: it has a rule (at most 3 highlighted, unique ranks) and it is read by trainers and athletes, so it does not qualify for the `CustomerLeads` exception.
- Observation rules -> domain service (pure, no I/O) -> `src/WeightTracker.Domain/Workouts/TopSetComparer.cs` as the nearest stateless domain helper.
- Application orchestration -> application service + event handler -> `src/WeightTracker.Application/Services` (`WorkoutStatusAuthorizer` as shape reference) and `src/WeightTracker.Application/Common/Dispatcher/IEventHandler.cs`.
- History read -> read projection -> `src/WeightTracker.Application/Interfaces/IExercisePreviousExecutionQuery.cs` and its `Infra/Queries` implementation.
- Read endpoint -> REST controller action + read use case -> `WorkoutsController.GetSummary` and `GetWorkoutSummaryUseCase`.
- Persistence -> new table via FluentMigrator -> `src/WeightTracker.Migrations/Migrations/202610080931_CreateWorkoutsTable.cs`.

## Canonical Template Mapping

- `WorkoutCompletedEvent` -> `src/WeightTracker.Domain/Common/IDomainEvent.cs` -> sealed record implementing the existing contract, in `Domain/Workouts`.
- Raising the event in `Workout.SetStatus` -> `Workout` / `WorkoutTests` -> the single place both `ChangeStatus` and `OverrideStatus` go through.
- `WorkoutObservations` aggregate, `WorkoutObservation` entity, `ObservationCode`, `ObservationSeverity`, `HighlightPriority`, `ObservationEvidence` -> `Workout`, `WorkoutExercise`, `WorkoutStatus`, value object records -> same Create / Rehydrate / IsNew shape.
- `WorkoutObservationGenerator` (domain service) -> `TopSetComparer` -> pure function of the workout, its history and the thresholds.
- `IWorkoutObservationsRepository` / `WorkoutObservationsRepository` / `WorkoutObservationsMapper` -> `IWorkoutRepository` / `WorkoutRepository` / `WorkoutMapper` -> mapping stays inside the repository; `Save` replaces the workout's rows.
- `IExerciseExecutionHistoryQuery` -> `IExercisePreviousExecutionQuery` -> same "finished workouts of the owner before this one" definition, returning the last N executions instead of one.
- `WorkoutObservationService` (application service) and `GenerateWorkoutObservationsOnCompleted : IEventHandler<WorkoutCompletedEvent>` -> `WorkoutStatusAuthorizer`, `IEventHandler<T>` -> handler stays thin and delegates to the service, as requested.
- `GetWorkoutObservationsUseCase` + `WorkoutsController.GetObservations` -> `GetWorkoutSummaryUseCase` + `GetSummary` -> same visibility check and caller handling, catch through `ExceptionMapper.Map`.
- Migration `202611131637_CreateWorkoutObservationsTable.cs` -> `CreateWorkoutsTable` -> naming, FK, index and `Down()`.
- Tests -> `WorkoutTests`, the unit test of `GetWorkoutSummaryUseCase`, `WorkoutScopingTests`, `AuthEndpointsTests`, the summary endpoint integration test.

## State And Mutability

- Completing a workout is committed first; its observations are written afterwards, in a second commit made by `WorkoutObservationService`. The two writes are not atomic (see Event Wiring).
- A workout's observations are immutable after generation. The only write is replacing the whole set when the workout is completed again.
- `Workout` gains no state: the event is transient and cleared after dispatch.
- Set records can still be corrected while a workout is `COMPLETED`. Observations are a snapshot at completion and are not recomputed by those corrections (see Notes).
- History is read only, from `COMPLETED` and `REVIEWED` workouts of the same owner prescribed by the same trainer.

## Event Wiring

- `WorkoutCompletedEvent(Id WorkoutId, Id UserId, DateTime CompletedAtUtc)`, raised in `Workout.SetStatus` when the target is `COMPLETED`.
- `AggregateRoot` gains a public `PullDomainEvents()` that returns the collected events and clears them (today `ClearDomainEvents` is protected, so nothing outside can clear).
- `ChangeWorkoutStatusUseCase`: `Save(workout)` -> `SaveChangesAsync` -> `dispatcher.DispatchAsync(workout.PullDomainEvents())`. Events are dispatched only after the aggregate that raised them is committed, so handlers react to persisted facts and never run for a change that was rolled back.
- `WorkoutObservationService` is the entry point of its own operation: it stages the observations and calls `SaveChangesAsync` once. The scoped `DbContext` is shared, but the workout was already committed, so this commit only carries the observations. This is the one case where a second `SaveChangesAsync` happens in a request, and the invariants must say so.
- The history query reads committed data, so it must exclude the current workout by id (it is now `COMPLETED` in the database).
- Failure: the completion stays committed and the workout has no observations. The dispatcher propagates the exception, so the request ends in `500` even though the status changed. Accepted for now; an admin re-completion regenerates them. A retry, outbox or background delivery is out of scope.
- Only `ChangeWorkoutStatusUseCase` dispatches in this change. No other use case completes a workout.
- Update `STPR_INVARIANTS.md`, section "Domain events: dispatcher without producers": `Workout` raises `WorkoutCompletedEvent`; events are dispatched after the commit of the aggregate that raised them and cleared with `PullDomainEvents()`; a handler that writes commits its own unit of work.

## Business Rules

### Definitions

- **Scope**: only workouts with a prescribing trainer get observations. Self-created workouts get none.
- **Execution** of an exercise: its sets recorded in one `COMPLETED` or `REVIEWED` workout of the same owner, prescribed by the same trainer as the completed workout. Self-created workouts and workouts of another trainer never count. **Previous executions**: those in workouts strictly before the current one by `WorkoutDate`, ties by the earlier created workout, the current workout excluded. Same definition as `IExercisePreviousExecutionQuery` with `prescribedBy` set.
- **Comparable executions**: same `ExerciseId`, same result kind (reps vs duration), same `perSide`, and neither in a mesocycle week of type `DELOAD` or `RECOVERY`. Non-comparable executions are skipped when walking history; they neither count nor break a streak.
- **Prescription-dependent comparisons** (volume and total reps): also require the same number of prescribed sets. A planned change of set count is not reported as an increase or decrease.
- Sets are matched to the prescription by position (set record 1 to prescribed set 1, and so on). Extra recorded sets beyond the prescription do not count for compliance but do count for weight, reps and volume.
- **Max weight**: the highest weight among sets with weight. **Volume**: Σ(weight × reps) over sets that have both. Sets without weight, and duration sets, are excluded. A rule whose metric has no value on either side is skipped.
- **Percent change**: `(current - baseline) / baseline × 100`, rounded to 2 decimals. Rules with a baseline of 0 are skipped.
- **Deload**: when the current workout is in a `DELOAD` or `RECOVERY` week, every rule that compares with history is skipped. Compliance rules still run.
- Missing history: any rule that needs N previous comparable executions and has fewer is skipped, without an observation.

### Observation rules

One observation per rule per exercise at most; repeated issues in one exercise are aggregated into a single observation whose evidence lists the affected set positions.

| Code | Scope | Condition | Severity |
|---|---|---|---|
| `WORKOUT_INCOMPLETE` | workout | Recorded prescribed sets < prescribed sets | WARNING |
| `EXERCISE_SKIPPED` | exercise | Prescribed exercise with no recorded sets | WARNING |
| `REPS_BELOW_TARGET` | exercise | Reps < `Reps.value` or `RepsRange.min` in any set | WARNING |
| `REPS_ABOVE_TARGET` | exercise | Reps > `RepsRange.max` in any set | INFO |
| `ALL_TARGETS_MET` | workout | Every prescribed set recorded, met (`IsMetBy`) and within range max | SUCCESS |
| `WEIGHT_INCREASE` / `WEIGHT_DECREASE` | exercise | Max weight vs previous comparable execution | INFO |
| `WEIGHT_PERSONAL_RECORD` | exercise | Max weight > max of all previous comparable executions | SUCCESS |
| `REPS_PERSONAL_RECORD` | exercise | Reps at a given weight > the most reps ever done at exactly that weight | SUCCESS |
| `VOLUME_INCREASE` / `VOLUME_DECREASE` | exercise | Volume vs previous comparable execution | INFO |
| `REPS_INCREASE` / `REPS_DECREASE` | exercise | Total reps vs previous comparable execution with the same set count | INFO |
| `REPEATED_TARGET_MISS` | exercise | Rep target missed in this and the previous `RepeatedMissCount - 1` comparable executions | WARNING |
| `WEIGHT_PLATEAU` | exercise | `PlateauCount` executions in a row without exceeding the max weight before them | INFO |
| `WEIGHT_UPTREND` | exercise | Max weight increased in `TrendCount` consecutive executions | SUCCESS |
| `WEIGHT_DOWNTREND` | exercise | Max weight decreased in `TrendCount` consecutive executions | INFO |

`WEIGHT_PERSONAL_RECORD` does not suppress `WEIGHT_INCREASE` in the full list; both are facts. Redundancy is resolved only for highlights.

### Highlight eligibility

Severity (what kind of fact) and priority (how much attention it deserves) are independent. An observation gets a `Priority` only when it meets one of these conditions:

| Priority | Source | Condition |
|---|---|---|
| CRITICAL | `WORKOUT_INCOMPLETE` | Recorded / prescribed sets < `SevereNonCompliancePercent` (70) |
| HIGH | `REPEATED_TARGET_MISS` | Always |
| HIGH | `WEIGHT_DECREASE`, `VOLUME_DECREASE`, `REPS_DECREASE` | Current and previous comparable execution both ≥ `SignificantDeclinePercent` (15) below the execution before them |
| HIGH | `WEIGHT_DOWNTREND` | Always |
| MEDIUM | `WEIGHT_PERSONAL_RECORD` | ≥ `SignificantRecordPercent` (5) above the previous record |
| MEDIUM | `WEIGHT_INCREASE`, `VOLUME_INCREASE`, `REPS_INCREASE` | ≥ `ExceptionalProgressionPercent` (10) vs previous comparable execution |
| LOW | `ALL_TARGETS_MET` | Only when it ends a `REPEATED_TARGET_MISS` streak on at least one exercise (it is recorded but never highlighted otherwise) |

### Selection

1. Take the observations with a `Priority`.
2. Sort by priority (CRITICAL → HIGH → MEDIUM → LOW), then by magnitude descending (absolute percent change, or missing-set ratio for compliance), then by exercise position in the workout.
3. Deduplicate: at most one highlight per exercise (the first after sorting).
4. Keep the first 3 at most, ranked 1 to 3. Zero or one is a valid result; never fill with non-eligible observations.

The aggregate enforces: at most 3 highlighted, ranks unique and contiguous from 1, only observations with a priority can be highlighted.

### Evidence (`Data`)

A flat JSON object of numbers and number arrays, camelCase keys, kg for weights. Each code has a fixed set of keys documented on the code. Examples:

- `WEIGHT_PERSONAL_RECORD`: `{ "previousMaxKg": 120, "currentMaxKg": 130, "changePercent": 8.33 }`
- `WEIGHT_DOWNTREND`: `{ "previousWeightsKg": [110, 105, 100], "currentWeightKg": 95, "consecutiveDecreases": 3 }`
- `REPS_BELOW_TARGET`: `{ "targetMin": 8, "setPositions": [3, 4], "reps": [6, 5] }`
- `WORKOUT_INCOMPLETE`: `{ "recordedSets": 9, "prescribedSets": 15, "completionPercent": 60 }`

### Configuration

`appsettings.json`, section `WorkoutObservations`, bound in `Program.cs` to a plain record passed to the generator (no hardcoded numbers in code):

- `RepeatedMissCount` (3), `PlateauCount` (4), `TrendCount` (3)
- `SevereNonCompliancePercent` (70), `SignificantDeclinePercent` (15), `SignificantRecordPercent` (5), `ExceptionalProgressionPercent` (10)

Missing or invalid values fail at startup. The number of executions loaded per exercise is not configured; it is derived from the counts above plus a margin, so skipped deload sessions rarely starve a rule.

## Persistence

Table `WorkoutObservations`:

- `Id` uuid PK (`Id.New()`, `.ValueGeneratedNever()`), `WorkoutId` uuid FK to `Workouts` with cascade delete, `ExerciseId` uuid null FK to `Exercises`
- `Code` varchar(64), `Severity` smallint, `Priority` smallint null, `HighlightRank` smallint null
- `Data` jsonb not null default `'{}'`, `CreatedAt` timestamp (UTC from `IClock`)
- Unique `(WorkoutId, Code, ExerciseId)` with nulls not distinct. Check constraint `HighlightRank BETWEEN 1 AND 3`; unique `(WorkoutId, HighlightRank)` where not null. `ExerciseId` FK is restrict.
- The aggregate root has no row: its identity is the `WorkoutId`, and replacing deletes and reinserts the workout's rows.
- `Down()` drops the table.

Differences from the class in the request: `HighlightRank` replaces `IsHighlighted` (highlighted = rank not null) so the order of the top 3 is stored, not recomputed; `CreatedAt` is `DateTime` UTC like the other dates; ids use `Id`; the name is `WorkoutObservation` because the codebase calls the session a `Workout`.

## Roles And Visibility

- Generation has no caller check of its own: it runs after the completion already authorized by `WorkoutStatusAuthorizer`.
- `GET /workouts/{workoutId}/observations`: same visibility as `GET /workouts/{workoutId}/summary` (admin every workout, trainer the ones they own or prescribed, user the ones they own except unpublished prescribed drafts). A workout the caller may not see is `403`; an unknown or malformed id is `404`.
- `200` with empty `observations` and `highlights` for a visible workout that has none (not completed yet, without trainer, or completed before this change).

## Business Identifiers And Mutability

- `WorkoutObservation.Id` is generated with `Id.New()` and immutable.
- `WorkoutObservations` is identified by the existing `WorkoutId`, one per workout, immutable; nothing new is generated for it.
- `Code` is a stable public business code: once released, a code is never renamed or reused with another meaning.
- No existing identifier changes.

## API Response

```json
{
  "observations": [
    { "id": "…", "code": "WEIGHT_PERSONAL_RECORD", "severity": "SUCCESS", "exerciseId": "…",
      "priority": "MEDIUM", "data": { "previousMaxKg": 120, "currentMaxKg": 130, "changePercent": 8.33 } }
  ],
  "highlights": [ { "observationId": "…", "rank": 1, "priority": "MEDIUM" } ]
}
```

Observations ordered by exercise position in the workout, workout-level first. XML docs on the action and DTOs; `ProducesResponseType` for `200`, `401`, `403`, `404`.

## Tests

Unit (`test/WeightTracker.Unit`, Arrange / Act / Assert, mirroring `src`):

- `Domain/Workouts`: `Workout` raises `WorkoutCompletedEvent` on `ChangeStatus` and `OverrideStatus` to `COMPLETED`, never on other targets; `PullDomainEvents` returns and clears.
- `Domain/Workouts`: one test per rule (fires, does not fire, skipped without history, skipped in deload, non-comparable executions skipped, prescription change skips volume/reps), and selection (ordering, magnitude tiebreak, one per exercise, `EXERCISE_SKIPPED` never highlighted, cap at 3, zero and one result).
- `Domain/Workouts`: `WorkoutObservations` aggregate rejects more than 3 highlights, duplicate ranks and highlighting without priority.
- `Application`: `WorkoutObservationService` (history query and repository mocked), the handler delegates, `ChangeWorkoutStatusUseCase` dispatches after commit and only on completion, and does not dispatch when saving fails, `GetWorkoutObservationsUseCase` visibility.
- `Infra/Mappers`: `WorkoutObservationsMapper` round-trips, including `Data`.

Integration (`test/WeightTracker.Integration`, auth only through `AuthenticatedClientFor`):

- Completing a prescribed workout through `PUT /workouts/{id}/status` stores observations readable from the new endpoint; a workout without trainer gets none; sessions with another trainer or self-created are excluded from history; history built through the API across several workouts produces a trend and a personal record.
- Admin re-completion replaces observations instead of adding.
- The history of a completion excludes the workout being completed.
- A workout in a deload week produces compliance observations only.
- `GET /workouts/{id}/observations`: `401` without token, `401` invalid token, `404` unknown, `404` malformed id, `403` for a caller who may not see it (assert exclusion), `200` empty for a not-completed workout.
- Full `dotnet test WeightTracker.slnx` with Docker running.

## Out Of Scope

- Observations for workouts without a prescribing trainer.
- Recomputing observations when set records are corrected after completion, or when a later workout changes history.
- Backfilling observations for workouts completed before this change.
- Duration-based rules (time records, time trends); only reps and weight rules in this version.
- Estimated one-rep max, RPE/effort-based comparisons and mesocycle objective text interpretation.
- Notifications, dashboard aggregation across workouts, and trainer actions on observations (dismiss, comment).
- Outbox, background or retried delivery of the event.

## Notes

Resolved decisions:

- **No primary exercise.** The model has no such concept, so `EXERCISE_SKIPPED` is an observation only and is never highlighted.
- **Dispatch after commit.** See Event Wiring; the completion and the observations are two commits, not atomic.
- **Snapshot.** Observations are generated only at completion; corrections made while `COMPLETED` do not recompute them.
- **Trainer-only.** Only workouts with a prescribing trainer get observations, and history only uses earlier sessions with the same trainer.
- **Visibility codes.** The observations route follows the existing summary spec: `403` for a hidden workout, `404` for an unknown or malformed id.
- **Perfect compliance highlight.** Eligible only when it ends a repeated-miss streak.
- **History read projection used for a write.** The invariants say no use case uses a projection to decide a mutation. Here the projection feeds a new aggregate, not a change to `Workout`, and hydrating every past `Workout` of the owner to read a few top values is not acceptable. This is an allowed deviation and must be added to the invariants as an explicit case.

Other notes:

- Handlers are auto-registered by `AddEventHandlers`; the service, repository, query, use case and options are registered in `Program.cs`.
- `IExerciseExecutionHistoryQuery` takes the owner, the trainer, the current workout id and date, the exercise ids and the derived depth, and returns per exercise the last executions with workout date, mesocycle week type, prescribed set count, `perSide` and recorded sets, plus the all-time max weight and best reps per weight over comparable executions. Backed by `IX_Workouts_UserId_WorkoutDate` and the `WorkoutExercises` exercise index; verify with `EXPLAIN` on a seeded database.
- `.stpr/template/src` parity must still hold; no template file is expected to change except `STPR_INVARIANTS.md`.
- No other deviation from the mapped references is allowed beyond the history projection and the handler-side commit above.
