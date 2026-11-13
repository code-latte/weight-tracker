## Why

After an athlete completes a workout prescribed by a trainer, the trainer has to read every set to see what went well and what did not. The system already knows the prescription and the athlete's earlier sessions with that trainer, so it can state the measurable facts (observations) and point at the few that deserve attention (at most 3 highlights). It is also the first producer for the domain event dispatcher added by `add-event-dispatcher`.

## What Changes

- `Workout` raises a `WorkoutCompletedEvent` every time it reaches `COMPLETED`, through the lifecycle or an admin override.
- `ChangeWorkoutStatusUseCase` dispatches the workout's events **after** the completion is committed, then they are cleared. `AggregateRoot` gains a public `PullDomainEvents()`.
- A handler for `WorkoutCompletedEvent` delegates to an application service that, for workouts with a prescribing trainer only, compares what the athlete did against the prescription and against their earlier finished workouts prescribed by the same trainer, selects at most 3 highlights and stores the result.
- 17 observation rules with stable codes, a severity and numeric evidence (JSON). Highlights are chosen by priority (CRITICAL, HIGH, MEDIUM, LOW), magnitude and at most one per exercise; never padded.
- Comparisons only use comparable executions (same exercise, result kind and per-side flag, outside `DELOAD` and `RECOVERY` weeks). A workout in a deload or recovery week only gets compliance observations. Rules without enough history are skipped.
- New table `WorkoutObservations` (FluentMigrator). A second completion of the same workout replaces its observations.
- New endpoint `GET /workouts/{workoutId}/observations` returning `observations` and `highlights`.
- Thresholds live in configuration (`WorkoutObservations` section) and are validated at startup.
- Observations are a snapshot at completion: set record corrections made later do not recompute them.
- If generation fails, the completion stays committed, the workout has no observations and the request ends in `500`.

## Capabilities

### New Capabilities
- `workout-observations`: generation of observations and highlights when a trainer-prescribed workout is completed, their rules, evidence and storage, and the read route that returns them.

### Modified Capabilities
- `domain-event-dispatch`: events raised by an aggregate are dispatched only after its change is committed, and are cleared once dispatched. Today no producer exists, so this is the first requirement on when dispatch happens.

## Canonical Template References

- `WorkoutCompletedEvent`, raising it in `Workout`, `PullDomainEvents` -> `src/WeightTracker.Domain/Workouts/Workout.cs`, `src/WeightTracker.Domain/Common/AggregateRoot.cs`, `WorkoutTests`.
- `WorkoutObservations` aggregate and `WorkoutObservation` entity -> `Workout` / `WorkoutExercise` (Create / Rehydrate / IsNew). Mutable aggregate, not a catalog and not a non-domain record: it has invariants and is read by trainers and athletes.
- `WorkoutObservationGenerator` domain service and `ObservationThresholds` value object -> `src/WeightTracker.Domain/Workouts/TopSetComparer.cs`.
- `WorkoutObservationService` + `GenerateWorkoutObservationsOnCompleted` handler -> `src/WeightTracker.Application/Services/WorkoutStatusAuthorizer.cs` shape, `src/WeightTracker.Application/Common/Dispatcher/IEventHandler.cs`.
- `IExerciseExecutionHistoryQuery` read projection -> `IExercisePreviousExecutionQuery` / `Infra/Queries/ExercisePreviousExecutionQuery.cs`.
- `IWorkoutObservationsRepository`, repository and mapper -> `IWorkoutRepository`, `WorkoutRepository`, `WorkoutMapper`.
- `GetWorkoutObservationsUseCase` + `WorkoutsController.GetObservations` -> `GetWorkoutSummaryUseCase` + `WorkoutsController.GetSummary`.
- Migration -> `202610080931_CreateWorkoutsTable.cs`.
- Tests -> `WorkoutTests`, `WorkoutStatusAuthorizerTests`, `WorkoutMapperTests`, `WorkoutSummaryEndpointTests`, `WorkoutStatusChangeEndpointTests`, `WorkoutScopingTests`.

## Business Identifiers And Mutability

- `WorkoutObservation.Id`: generated with `Id.New()`, immutable.
- `WorkoutObservations` (aggregate root): identified by its `WorkoutId`, one per workout, immutable. It reuses the workout's existing `Id`, so nothing new is generated for it.
- Observation `code`: stable public business code; once released it is never renamed or reused with another meaning.
- No existing identifier changes.

## Conflicts With Template Or Invariants

- **Second commit in one request.** The invariants say a use case calls `SaveChangesAsync` exactly once. Dispatch after commit means the observations need their own commit, made once by `WorkoutObservationService` as the entry point of its operation. `STPR_INVARIANTS.md` must record this.
- **Read projection feeding a write.** The invariants say no use case uses a projection to decide a mutation. The history projection feeds a new aggregate, never a change to `Workout`, and hydrating every earlier `Workout` of the athlete to read a few values is not acceptable. Recorded as an explicit allowed case.
- **"Domain events: dispatcher without producers"** in `STPR_INVARIANTS.md` is no longer true and must be rewritten.
- No other deviation from the mapped references is allowed.

## Impact

- Domain: `AggregateRoot`, `Workout`, new observation types and domain service.
- Application: `ChangeWorkoutStatusUseCase` (takes `IEventDispatcher`), new service, handler, query and repository contracts, read use case.
- Infra: new EF entity and `DbContext` configuration (`jsonb` column), repository, mapper, query.
- Migrations: `CreateWorkoutObservationsTable`.
- Api: new action and DTOs on `WorkoutsController`, `WorkoutObservations` configuration in `appsettings.json` and `Program.cs` registrations.
- Docs: `STPR_INVARIANTS.md`.
- Behaviour of existing routes is unchanged, except that a failure while generating observations turns a successful completion into a `500` response with the completion already saved.

## Out Of Scope

- Observations for workouts without a prescribing trainer.
- Recomputing after set record corrections or later workouts; backfilling earlier workouts.
- Duration-based rules, estimated one-rep max, effort-based comparisons, interpreting mesocycle objective text.
- Notifications, cross-workout dashboards, dismissing or commenting observations.
- Outbox, background or retried delivery of events.
