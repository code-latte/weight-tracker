# Tasks

## 1. Domain: completion event

- [x] 1.1 Add `PullDomainEvents()` to `AggregateRoot` (returns a copy and clears); verify a unit test in `test/WeightTracker.Unit/Domain/Common` shows a second pull returns nothing
- [x] 1.2 Add `WorkoutCompletedEvent(Id WorkoutId)` in `Domain/Workouts` and raise it in `Workout.SetStatus` when the target is `COMPLETED` (design "Layer map"); verify new `WorkoutTests` cases: raised once on `ChangeStatus` and on `OverrideStatus` to `COMPLETED`, never for `DRAFT`, `PUBLISHED`, `STARTED`, never by `MarkReviewed`, and not raised by `Rehydrate`
- [x] 1.3 Review: compare the changes with `Workout` / `WorkoutTests` (doc comments, naming, Arrange / Act / Assert); verify by reading the diff

## 2. Domain: observation model

- [x] 2.1 Add `ObservationCode` (17 codes in spec order), `ObservationSeverity`, `HighlightPriority`, `ObservationEvidence` and `ObservationThresholds` in `Domain/Workouts/Observations`, with domain exceptions for invalid values; verify unit tests pin the full code list and reject thresholds out of range (counts < 2, percents outside (0, 100))
- [x] 2.2 Add `WorkoutObservation` (entity) and `WorkoutObservations` (aggregate root identified by `WorkoutId`, `Create` / `Rehydrate` / `Replace`, `IsNew`) enforcing: at most 3 highlighted, ranks unique and contiguous from 1, highlight requires priority, one observation per `(code, exerciseId)`; verify unit tests cover each invariant, `Replace` swapping the whole list with new ids, and `Rehydrate` keeping ids
- [x] 2.3 Add the new domain exceptions to `ExceptionMapper` only if any can reach a controller; verify `ExceptionMapper` tests if a mapping was added
- [x] 2.4 Review: compare the aggregate and its tests with `Workout` / `WorkoutExercise` / `WorkoutTests`; verify by reading the diff

## 3. Domain: rules and highlight selection

- [x] 3.1 Add `ExerciseExecution` (history input) and the `WorkoutObservationGenerator` skeleton: positional expansion of prescriptions, comparability filter, deload/recovery gate, skip on missing history or zero baseline (spec requirements "Only comparable executions are compared", "A deload or recovery workout only gets compliance observations", "Rules without enough history are skipped", "Sets are matched to the prescription by position"); verify unit tests for every scenario of those requirements
- [x] 3.2 Implement compliance rules `WORKOUT_INCOMPLETE`, `EXERCISE_SKIPPED`, `REPS_BELOW_TARGET`, `REPS_ABOVE_TARGET`, `ALL_TARGETS_MET` with their evidence; verify unit tests for every scenario of their spec requirements
- [x] 3.3 Implement comparison rules `WEIGHT_INCREASE`/`DECREASE`, `WEIGHT_PERSONAL_RECORD`, `REPS_PERSONAL_RECORD`, `VOLUME_INCREASE`/`DECREASE`, `REPS_INCREASE`/`DECREASE` with their evidence (decimal percents rounded to 2 places); verify unit tests for every scenario of their spec requirements
- [x] 3.4 Implement streak rules `REPEATED_TARGET_MISS`, `WEIGHT_PLATEAU`, `WEIGHT_UPTREND`, `WEIGHT_DOWNTREND`, including skipped non-comparable executions inside a streak; verify unit tests for every scenario of their spec requirements
- [x] 3.5 Implement priority assignment and selection (order by priority, magnitude, position; one per exercise; cap 3; never padded) (spec "Highlight eligibility", "Highlight selection"); verify unit tests for every scenario of both requirements, plus `EXERCISE_SKIPPED` never highlighted
- [x] 3.6 Review: compare the generator with `TopSetComparer` (stateless, no I/O, doc comments) and check no threshold is hardcoded; verify by reading the diff

## 4. Persistence

- [x] 4.1 Add migration `202611131637_CreateWorkoutObservationsTable.cs` (columns, FKs, check constraint, unique and partial unique indexes as in design) with a `Down()` that drops the table; verify the migrator runs up and down against the compose database
- [x] 4.2 Add `WorkoutObservationEntity`, its `WeightTrackerDbContext` configuration (`jsonb`, lengths, `.ValueGeneratedNever()`, indexes matching the migration) and `WorkoutObservationsMapper`; verify `test/WeightTracker.Unit/Infra/Mappers` round-trip tests, including `Data` with numbers and arrays and the null exercise
- [x] 4.3 Add `IWorkoutObservationsRepository` (`GetByWorkoutId`, `Save`) and `WorkoutObservationsRepository` (insert on `IsNew`, delete-and-insert rows on replace); verify an integration test in `test/WeightTracker.Integration/Repositories` saves, reloads and replaces
- [x] 4.4 Review: compare with `WorkoutRepository`, `WorkoutMapper` and `CreateWorkoutsTable`; verify the EF configuration matches the migration

## 5. History read projection

- [x] 5.1 Add `IExerciseExecutionHistoryQuery` and its view next to the service, and `ExerciseExecutionHistoryQuery` in `Infra/Queries` (`AsNoTracking`, filters by owner, trainer, finished status, strictly before, excluding the current workout, depth per exercise, records over all comparable sessions, current week type) (design "History read projection"); verify integration tests in `test/WeightTracker.Integration/Queries`: another trainer's and self-created sessions excluded, later and unfinished excluded, current workout excluded, depth respected, records looking past depth, week type returned
- [x] 5.2 Run `EXPLAIN (ANALYZE, BUFFERS)` on a database seeded with enough workouts for the planner to use `IX_Workouts_UserId_WorkoutDate`; verify by recording the plan summary under this task
  - EXPLAIN (ANALYZE, BUFFERS) on a throwaway Postgres 17 with 91,700 finished workouts, 458,500 workout exercises and 1,375,500 set records. The athlete had 2,000 finished sessions, 1,500 of them with the trainer. Queries mirror the LINQ translation, depth 6.
  - Sessions, popular exercise (150 matching sessions): 4.9 to 9.1 ms. Sessions, rare exercise (25 matches): 1.5 to 2.6 ms. Records, popular: 2.5 ms. Records, rare: 1.4 ms.
  - The plan does not stop at the first matches. With the trainer filter, Postgres bitmap-ANDs `IX_Workouts_UserId` and `IX_Workouts_TrainerId`, probes `IX_WorkoutExercises_ExerciseId_WorkoutId` once per session of the athlete with that trainer (1,493 probes here) and sorts top-N. Cost therefore grows with the athlete's sessions with that trainer, never with the size of the database. Accepted for now: 1,500 sessions is about four years of daily training with one trainer. If it ever matters, an index on `Workouts (UserId, TrainerId, WorkoutDate DESC, Id DESC)` restores the early stop.
- [x] 5.3 Review: compare with `IExercisePreviousExecutionQuery` / `ExercisePreviousExecutionQuery`; verify by reading the diff

## 6. Application: generation on completion

- [x] 6.1 Add `WorkoutObservationsOptions` (Infra/Services, `SectionName = "WorkoutObservations"`), defaults in `appsettings.json`, and in `Program.cs` build `ObservationThresholds` from it and register it as a singleton, failing fast on invalid values; verify an integration test in `test/WeightTracker.Integration/Configuration` shows startup fails with `TrendCount` 0
- [x] 6.2 Add `WorkoutObservationService.Generate` (design "The service owns its own commit") and register it, the repository and the query in `Program.cs`; verify unit tests with mocks: no trainer → nothing saved; trainer → generator output saved and committed once; existing observations replaced
- [x] 6.3 Add `GenerateWorkoutObservationsOnCompleted : IEventHandler<WorkoutCompletedEvent>` delegating to the service (auto-registered, not in `Program.cs`); verify a unit test that it calls the service with the event's workout id
- [x] 6.4 Change `ChangeWorkoutStatusUseCase` to take `IEventDispatcher` and run `Save` → `SaveChangesAsync` → `DispatchAsync(workout.PullDomainEvents())`; verify unit tests: dispatch happens after commit, the completion event is dispatched only on completion, nothing is dispatched when saving throws, a dispatch exception propagates after the commit
- [x] 6.5 Update `.stpr/template/STPR_INVARIANTS.md` "Domain events: dispatcher without producers": `Workout` raises `WorkoutCompletedEvent`, dispatch after commit, clear with `PullDomainEvents()`, a writing handler commits its own unit of work once; and "Read projections": the history projection feeding a new aggregate as the allowed case; verify the section reads consistently with the code
- [x] 6.6 Add integration tests in `test/WeightTracker.Integration/Workouts` driving everything through the API: athlete completes a prescribed workout → observations exist; admin completes → observations exist; workout without trainer → empty; publish/start → empty; set record correction after completion → unchanged; admin reopen and recomplete → replaced; deload week → compliance only; history across several prescribed workouts produces a downtrend and a weight record with the spec evidence; verify they pass with Docker running
- [x] 6.7 Review: compare the service with `WorkoutStatusAuthorizer` and the use case change with the existing use cases; verify by reading the diff

## 7. Read endpoint

- [x] 7.1 Add `GetWorkoutObservationsUseCase` and its view (404 unknown, 403 not visible via `WorkoutVisibility`, empty when none) and register it; verify unit tests modeled on the `GetWorkoutSummaryUseCase` tests
- [x] 7.2 Add `WorkoutsController.GetObservations` (`GET /workouts/{workoutId:guid}/observations`), response DTOs with XML docs, `data` as a raw JSON object, `ProducesResponseType` for `200`, `401`, `403`, `404`, catch through `ExceptionMapper.Map`; verify `/openapi/v1.json` lists the route and its responses
- [x] 7.3 Add `test/WeightTracker.Integration/Workouts/WorkoutObservationsEndpointTests.cs` modeled on `WorkoutSummaryEndpointTests`: `401` without token, `401` invalid token, `404` unknown, `404` malformed id, `403` unrelated trainer (assert nothing returned), `200` for athlete, prescribing trainer and admin, `200` empty for a not completed workout, response shape and ordering, highlight ids present in observations; verify they pass with Docker running
- [x] 7.4 Review: compare the use case, action and tests with `GetWorkoutSummaryUseCase`, `GetSummary` and `WorkoutSummaryEndpointTests`; verify by reading the diff

## 8. Full verification

- [x] 8.1 Run the template parity check from `AGENTS.md`; verify it prints no `DRIFT:` line
- [x] 8.2 Run `dotnet build WeightTracker.slnx` and `dotnet test WeightTracker.slnx` with Docker running; verify both pass, including the integration suite
- [x] 8.3 Walk through the `AGENTS.md` quality checklist (migration `Down()`, EF matches migration, `Id.New()` only, mapper round-trip, `Program.cs` registrations, `ExceptionMapper`, auth matrix, Arrange / Act / Assert); verify each applicable item holds

## Workflow follow-up

- Archive the change after review, then confirm `openspec/specs/workout-observations/spec.md` was created with its Purpose and `domain-event-dispatch` gained the new requirement.
