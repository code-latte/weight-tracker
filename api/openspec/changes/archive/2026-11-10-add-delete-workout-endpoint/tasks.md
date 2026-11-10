# Tasks

## 1. Domain guard

- [x] 1.1 Add `Workout.EnsureCanBeDeleted()` throwing a new domain `WorkoutDeletionLockedException` (mapped to `409` in `ExceptionMapper`) unless the status is `DRAFT`, with XML docs matching the aggregate's style; verify `dotnet build WeightTracker.slnx` passes
- [x] 1.2 Add `WorkoutTests` cases (Arrange/Act/Assert, one Act each): `DRAFT` passes; `PUBLISHED`, `STARTED`, `COMPLETED` and `REVIEWED` throw `WorkoutDeletionLockedException`; verify the unit tests pass

## 2. Authorization

- [x] 2.1 Expose the permission check of `WorkoutEditAuthorizer` as public `EnsureCanModify(workout, callerId, callerRole, operation)` throwing `UnauthorizedOperationException`, make `EnsureCanEdit` call it before its lock, and document that content edits must use `EnsureCanEdit`; verify the existing `WorkoutEditAuthorizerTests` still pass unchanged
- [x] 2.2 Add `EnsureCanModify` cases to `test/WeightTracker.Unit/Application/Services/WorkoutEditAuthorizerTests.cs`: admin, prescribing trainer and owner without trainer pass on a non-draft workout; the owner of a prescribed workout, an unrelated trainer and an unrelated user throw; verify the tests pass
- [x] 2.3 Add a "Deleting a workout" rule to `.stpr/template/STPR_INVARIANTS.md` (edit permission via `EnsureCanModify`, draft-only for every caller including `ADMIN`, permission before status, hard delete); verify it sits beside the edit and status rules

## 3. Persistence

- [x] 3.1 Add `Delete(Workout, CancellationToken)` to `IWorkoutRepository` and implement it in `WorkoutRepository`: load the tracked aggregate, apply the `_loadedVersions` original `Version` as `Save` does, remove it; verify the build passes
- [x] 3.2 Add `WorkoutRepositoryTests` cases: deleting a draft with exercises, sets and a superseries leaves no `Workouts`, `WorkoutExercises` or `WorkoutExerciseSets` rows; a delete after the row version changed raises `WorkoutConcurrencyConflictException` on commit and keeps the row; verify the integration tests pass with Docker running

## 4. Use case

- [x] 4.1 Add `DeleteWorkoutUseCase` in `src/WeightTracker.Application/UseCases/Workouts/` mirroring `RemoveWorkoutExerciseUseCase` (load → 404, `EnsureCanModify` with `"DELETE /workouts/{workoutId}"`, `EnsureCanBeDeleted`, `Delete`, commit) and register it in `Program.cs`; verify the build passes
- [x] 4.2 Add `test/WeightTracker.Unit/Application/Workouts/DeleteWorkoutUseCaseTests.cs` mirroring `RemoveWorkoutExerciseUseCaseTests`: 404 for a missing workout; 403 for each forbidden caller with no delete or commit; 409 for a non-draft (admin included) with no delete; success calls `Delete` and `SaveChangesAsync`; a forbidden caller on a published workout gets 403; verify the tests pass

## 5. Endpoint

- [x] 5.1 Add `Delete` to `WorkoutsController` (`[HttpDelete("{workoutId:guid}")]`, `204`/`401`/`403`/`404`/`409` response types, `ExceptionMapper.Map` in the catch) mirroring `RemoveExercise`; verify the route appears in `/openapi/v1.json` in Development
- [x] 5.2 Add `test/WeightTracker.Integration/Workouts/WorkoutDeleteEndpointTests.cs`, following `WorkoutExerciseRemoveEndpointTests` and `WorkoutEndpointTest`, with tokens only from `AuthenticatedClientFor`: 401 with no token; 401 with an invalid token; 404 for an unknown id; 403 for the owner of a prescribed workout, an unrelated trainer and an unrelated user; 403 rather than 409 for a forbidden caller on a published workout; 409 for admin and prescribing trainer on non-draft workouts, the workout still existing; 204 for admin, prescribing trainer and owner without trainer, then `GET /workouts/{id}` returns 404 and `GET /workouts` excludes it; verify the tests pass

## 6. Review and verification

- [x] 6.1 Review `workout-deletion` against its mapped references (`RemoveWorkoutExerciseUseCase`, `WorkoutEditAuthorizer`, the `Workout` template aggregate, `WorkoutsController.RemoveExercise`, the `WorkoutRepository` concurrency handling) and the AGENTS.md quality checklist; record any deviation
  - Deviation recorded: the aggregate throws the domain `WorkoutDeletionLockedException` (409) instead of the Application-layer `WorkoutLockedException`, which the Domain cannot reference; see design.md.
- [x] 6.2 Run the template parity check from AGENTS.md and confirm there is no `DRIFT:` output, updating `.stpr/template` copies if a canonical file changed
- [x] 6.3 Run `dotnet build WeightTracker.slnx` and the full `dotnet test WeightTracker.slnx` with Docker running; verify both succeed with no warnings
