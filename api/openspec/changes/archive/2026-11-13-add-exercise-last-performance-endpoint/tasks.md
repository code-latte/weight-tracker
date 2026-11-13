# Tasks

## 1. Index and migration

- [x] 1.1 Add the FluentMigrator migration after `202611121200_AddDurationToSetRecords` that creates `IX_Workouts_UserId_WorkoutDate_Id` on `Workouts` (`UserId` ascending, `WorkoutDate` descending, `Id` descending) and whose `Down()` deletes it, mirroring `202611051049_AddMesocycleToWorkouts`; verify `dotnet build WeightTracker.slnx` has no warnings
- [x] 1.2 Declare the same index in `WeightTrackerDbContext` (`HasIndex(...).IsDescending(false, true, true).HasDatabaseName(...)`) and verify no EF Core migration was generated and the build is clean
- [x] 1.3 Run the migration up, down and up on a throwaway Postgres and verify with `\d "Workouts"` that the index exists after each up and is gone after the down

## 2. Repository read

- [x] 2.1 Add the read models `ExerciseLastPerformanceView` (date, prescribed effort, sets) and `LastPerformanceSetView` in `src/WeightTracker.Application/UseCases/Users`, following `UserWithTrainerView`, with XML docs; verify the build
- [x] 2.2 Add the read projection contract `IExerciseLastPerformanceQuery` in `src/WeightTracker.Application/Interfaces` with `Get(Id userId, Id exerciseId, CancellationToken)`, returning `ExerciseLastPerformanceView?`, with XML docs stating the finished-workout and ordering rules; verify the build
- [x] 2.3 Implement it in `src/WeightTracker.Infra/Queries/ExerciseLastPerformanceQuery.cs`, registered in `Program.cs`, as the two no-tracking statements of design.md (effort read from the reference `WorkoutExercises` row, status values from `WorkoutStatus`, order by `WorkoutDate` then `Id` descending, sets by `Position`, step 2 skipped when step 1 finds nothing); verify the build
- [x] 2.4 Add query integration tests in `test/WeightTracker.Integration/Queries/ExerciseLastPerformanceQueryTests.cs`, arranged through the workout repository like `WorkoutRepositoryTests`, covering: latest finished workout wins, effort of the reference workout, tie on date broken by the newer workout, unfinished workouts ignored, finished workout without records for the exercise skipped, other users excluded, sets in position order, null weight versus `0`, a timed set, and `null` when never done; verify they pass
- [x] 2.5 Run `EXPLAIN (ANALYZE, BUFFERS)` of step 1 on a Postgres seeded with a few thousand workouts for one user and verify a commonly done exercise uses `IX_Workouts_UserId_WorkoutDate_Id` with no sort node; record the measured limit for rare and never-done exercises in design.md (Risks)

## 3. Use case

- [x] 3.1 Add `GetMyExerciseLastPerformanceUseCase` in `src/WeightTracker.Application/UseCases/Users`, following `GetExerciseByIdUseCase`: load the exercise, throw `ExerciseNotFoundException` when missing, `ExerciseVisibility.EnsureVisible`, then `IExerciseLastPerformanceQuery.Get`; XML docs with `<returns>` and `<exception>`; register it in `Program.cs` and verify the build
- [x] 3.2 Add unit tests in `test/WeightTracker.Unit/Application/Users` mirroring the `GetExerciseByIdUseCase` tests: returns the view, returns `null` when never done, throws for a missing exercise and for a hidden one without reading workouts; Arrange/Act/Assert; verify they pass

## 4. Endpoint

- [x] 4.1 Add `ExerciseLastPerformanceResponse` and `LastPerformanceSetResponse` in `src/WeightTracker.Api/Endpoints/Users/Dtos` with `From` mapping, `Effort` as the enum name, and XML docs (nullable fields documented like `SetRecordResponse`); verify the build
- [x] 4.2 Add the `GET me/exercises/{exerciseId:guid}/last-performance` action to `UsersController`: `NoContent()` for `null`, `Ok(...)` otherwise, catch through `ExceptionMapper.Map`, `ProducesResponseType` for `200`, `204`, `401` and `404`, and update the controller summary; verify `/openapi/v1.json` shows the route with those responses
- [x] 4.3 Add `test/WeightTracker.Integration/Users/ExerciseLastPerformanceEndpointTests.cs`, mirroring `UserProfileEndpointTests` and arranging workouts and set records through the API as `SetRecordEndpointTest` does: `401` without and with an invalid token, `404` unknown exercise, `404` hidden custom exercise, `404` malformed id, `204` never done, `204` only in a `STARTED` workout, `200` with the latest finished workout and its prescribed effort, `REVIEWED` counts, another user's records excluded, trainer does not get the athlete's records, null weight versus `0`, a timed set; Arrange/Act/Assert; verify they pass

## 5. Review and verification

- [x] 5.1 Compare the new use case, read projection, controller action, DTOs, migration and tests against their mapped references in proposal.md (Canonical Template Mapping) and "Read projections" in `STPR_INVARIANTS.md`, and verify `IWorkoutRepository` is unchanged and no deviation remains
- [x] 5.2 Run the template drift check from `AGENTS.md` and verify no `DRIFT:` lines (sync the template copy if a canonical file changed)
- [x] 5.3 Run the full `dotnet test WeightTracker.slnx` with Docker running and verify unit and integration suites pass with no warnings
- [x] 5.4 Run `openspec validate add-exercise-last-performance-endpoint --strict` and verify it passes

## Workflow follow-up

- Archive the change with `/opsx:archive` after review, syncing `exercise-last-performance` into the main specs.
