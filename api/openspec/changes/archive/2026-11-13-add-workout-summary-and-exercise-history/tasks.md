# Tasks

## 1. Domain rules

- [x] 1.1 Add `SetTarget.IsMetBy(SetResult)` with one override per target kind as in design.md (Domain rules), with XML docs; extend `test/WeightTracker.Unit/Domain/Workouts/SetTargetTests.cs` with reps below/at/above n, range below min/at min/above max, `MaxReps` always met, duration and minutes boundaries, and kind mismatch; verify the tests pass
- [x] 1.2 Add `SetMetrics.Volume` and `SetMetrics.EstimatedOneRepMax` in `src/WeightTracker.Domain/Workouts`, with XML docs; add `SetMetricsTests` covering weighted reps, `0` kg, `null` weight, timed set, the Epley estimate for 10 reps at 60 kg (`80`) and 1 rep at 60 kg (`62`), and two-decimal rounding; verify they pass
- [x] 1.3 Add `TopSetComparer` in `src/WeightTracker.Domain/Workouts` (weight desc with `null` below `0`, reps desc, seconds desc, position asc); add `TopSetComparerTests` for each tie-break; verify they pass
- [x] 1.4 Add `WorkoutExercise.SetsInTarget()` (expand prescriptions by count, match records by position, apply `IsMetBy`); extend `WorkoutExerciseTests` with extra records, missing records, mixed prescriptions and no records (`0/0`); verify they pass and `dotnet build WeightTracker.slnx` has no warnings

## 2. Read projections

- [x] 2.1 Add `IExercisePreviousExecutionQuery` in `src/WeightTracker.Application/Interfaces` and its view records next to `GetWorkoutSummaryUseCase` in `src/WeightTracker.Application/UseCases/Workouts`, following `IExerciseLastPerformanceQuery` / `ExerciseLastPerformanceView`, with XML docs stating the finished-workout, anchor and `prescribedBy` rules; verify the build
- [x] 2.2 Implement `src/WeightTracker.Infra/Queries/ExercisePreviousExecutionQuery.cs` as in design.md (one `LIMIT 1` walk per exercise anchored before the summarised workout, then one read of set records by `Position`, `AsNoTracking`), registered in `Program.cs`; verify the build
- [x] 2.3 Add `test/WeightTracker.Integration/Queries/ExercisePreviousExecutionQueryTests.cs` mirroring `ExerciseLastPerformanceQueryTests`: latest earlier finished workout wins, later and same-anchor workouts excluded, unfinished excluded, finished without records skipped, other owners excluded, `prescribedBy` excludes the athlete's own workouts, tie on date broken by id, sets in position order, exercises never done absent; verify they pass
- [x] 2.4 Add `IExerciseHistoryQuery` in `src/WeightTracker.Application/Interfaces` and its view records next to `GetUserExerciseHistoryUseCase` in `src/WeightTracker.Application/UseCases/Users`, with XML docs; implement `src/WeightTracker.Infra/Queries/ExerciseHistoryQuery.cs` (statement 1 limited and ordered `WorkoutDate desc, Id desc`, statement 2 set records by `Position`, skipped when empty), registered in `Program.cs`; verify the build
- [x] 2.5 Add `test/WeightTracker.Integration/Queries/ExerciseHistoryQueryTests.cs`: order and `limit`, unfinished excluded, finished without records skipped, other users excluded, `prescribedBy` scoping, empty result; verify they pass
- [x] 2.6 Run `EXPLAIN (ANALYZE, BUFFERS)` of both projections' first statement on a Postgres seeded with a few thousand workouts for one user, verify commonly done exercises use `IX_Workouts_UserId_WorkoutDate_Id` with no sort node, and record the result in design.md (Risks); the rare and never-done cases seq-scan `WorkoutExercises`, which the index of 2.7 and 2.8 fixes
- [x] 2.7 Add the FluentMigrator migration after `202611131300_AddStartedAtToWorkouts` that creates `IX_WorkoutExercises_ExerciseId_WorkoutId` on `WorkoutExercises` (`ExerciseId`, `WorkoutId`) and whose `Down()` deletes it, mirroring `202611131200_AddUserWorkoutDateIndexToWorkouts`; declare the same index in `WeightTrackerDbContext` (`HasIndex(...).HasDatabaseName(...)`) and verify the build is clean and no EF Core migration was generated
- [x] 2.8 Run the migration up, down and up on a throwaway Postgres and verify with `\d "WorkoutExercises"` that the index exists after each up and is gone after the down; then repeat the `EXPLAIN (ANALYZE, BUFFERS)` of 2.6 for a rare and a never-done exercise and verify both use the new index with no seq scan of `WorkoutExercises`, recording the result in design.md (Risks)

## 3. Workout summary

- [x] 3.1 Add `GetWorkoutSummaryUseCase` and `WorkoutSummaryView` in `src/WeightTracker.Application/UseCases/Workouts`, following `GetWorkoutByIdUseCase` and the Summary flow in design.md (visibility gate first, no exercise lookup, `prescribedBy` resolved for a trainer who is not the owner, totals as specified), with XML docs; register it in `Program.cs` and verify the build
- [x] 3.2 Add unit tests in `test/WeightTracker.Unit/Application/Workouts` mirroring the `GetWorkoutByIdUseCase` tests: missing workout, invisible workout without reading projections, mixed exercises with totals, no records, previous present and absent, `prescribedBy` passed for a trainer and not for owner or admin, workout volume `null` when all are `null`; Arrange/Act/Assert; verify they pass
- [x] 3.3 Add `WorkoutSummaryResponse`, `ExerciseSummaryResponse`, `SetsInTargetResponse` and `PreviousExecutionResponse` in `src/WeightTracker.Api/Endpoints/Workouts/Dtos` with `From` mappers and XML docs like `SetRecordResponse`, reusing `LastPerformanceSetResponse` for the top sets because it has the same fields; verify the build
- [x] 3.4 Add `GET {workoutId:guid}/summary` to `WorkoutsController`, catch through `ExceptionMapper.Map`, `ProducesResponseType` for `200`, `401`, `403`, `404`; verify `/openapi/v1.json` shows the route with those responses
- [x] 3.5 Add `test/WeightTracker.Integration/Workouts/WorkoutSummaryEndpointTests.cs` mirroring `WorkoutGetByIdEndpointTests`, arranging set records through the API as `SetRecordEndpointTest` does: `401` without and with an invalid token, `404` unknown and malformed id, `403` another user's workout and unpublished prescribed draft, `200` for owner, prescribing trainer and admin, values asserted (in target per kind including range above max and max reps, volume with `null` weight and `0` kg, top set), `STARTED` workout partial summary, previous execution picks the latest earlier finished workout and excludes later, unfinished, other users' and, for the trainer, the athlete's own workouts; Arrange/Act/Assert; verify they pass

## 4. Exercise history

- [x] 4.1 Add `InvalidHistoryLimitException` in `src/WeightTracker.Application/Exceptions` and map it to `400` in `ExceptionMapper`; verify the build
- [x] 4.2 Add `GetUserExerciseHistoryUseCase` and `ExerciseHistorySessionView` in `src/WeightTracker.Application/UseCases/Users`, following `GetMyExerciseLastPerformanceUseCase` and `GetUserMesocyclesUseCase` and the order in design.md (limit, `USER` self check, user, exercise visibility, `prescribedBy`, projection, metrics), with XML docs; register it in `Program.cs` and verify the build
- [x] 4.3 Add unit tests in `test/WeightTracker.Unit/Application/Users`: default and out-of-range limit, user asking for another user throws without lookups, unknown user, unknown and hidden exercise, `prescribedBy` per role, max weight / volume / estimate per session including `null` cases; Arrange/Act/Assert; verify they pass
- [x] 4.4 Add `ExerciseHistorySessionResponse` in `src/WeightTracker.Api/Endpoints/Users/Dtos` with `From` and XML docs; add `GET {userId:guid}/exercises/{exerciseId:guid}/history` with `[FromQuery] int? limit` to `UsersController`, catch through `ExceptionMapper.Map`, `ProducesResponseType` for `200`, `400`, `401`, `403`, `404`; verify `/openapi/v1.json` shows the route with those responses
- [x] 4.5 Add `test/WeightTracker.Integration/Users/ExerciseHistoryEndpointTests.cs` mirroring `ExerciseLastPerformanceEndpointTests`: `401` without and with an invalid token, `400` for `limit=0`, `limit=51` and `limit=abc`, `403` user asking for another user, `404` unknown user, unknown exercise, hidden custom exercise and malformed id, `200` empty list, sessions newest first truncated by `limit` and default `10`, max weight / volume / estimated one-rep max asserted, unfinished excluded, other users excluded, trainer gets only prescribed sessions (athlete's own excluded), admin gets all; Arrange/Act/Assert; verify they pass

## 5. Review and verification

- [x] 5.1 Compare the domain rules, projections, use cases, controller actions, DTOs and tests against their mapped references in proposal.md (Canonical Template Mapping) and "Read projections" in `STPR_INVARIANTS.md`; verify `IWorkoutRepository` and `ExerciseLastPerformanceQuery` are unchanged and no deviation remains
- [x] 5.2 Run the template drift check from `AGENTS.md` and verify no `DRIFT:` lines (sync the template copy if a canonical file changed)
- [x] 5.3 Run the full `dotnet test WeightTracker.slnx` with Docker running and verify unit and integration suites pass with no warnings
- [x] 5.4 Run `openspec validate add-workout-summary-and-exercise-history --strict` and verify it passes

## Workflow follow-up

- Archive the change with `/opsx:archive` after review, syncing `workout-summary` and `exercise-history` into the main specs.
