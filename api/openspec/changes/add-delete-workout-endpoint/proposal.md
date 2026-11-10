# Proposal

## Why

Workouts can be created and edited but never removed, so mistaken or abandoned drafts stay in athletes' and trainers' lists forever. Allowing deletion only while a workout is a `DRAFT` removes that clutter without risking any published, executed or reviewed workout.

Source brief: `.stpr/plan/openspec/proposal-change-add-delete-workout-endpoint.md`.

## What Changes

- New endpoint `DELETE /workouts/{workoutId}` that permanently deletes a workout with its exercises, set prescriptions and superseries, returning `204 No Content`.
- Deletion is allowed only while the workout is `DRAFT`, for every caller, `ADMIN` included (`409 Conflict` otherwise). This intentionally differs from the edit rule, where an admin may edit non-draft workouts.
- Who may delete reuses the edit permission: `ADMIN` always; the prescribing trainer when the workout has one; the owner only when the workout has no trainer. Anyone else gets `403 Forbidden`, checked before the status so a forbidden caller never learns it.
- A missing workout returns `404 Not Found`.
- `WorkoutEditAuthorizer` exposes its permission check as a public method without the status lock; `EnsureCanEdit` keeps its current behaviour.
- `STPR_INVARIANTS.md` gains a "Deleting a workout" rule.

No breaking changes.

## Capabilities

### New Capabilities

- `workout-deletion`: who may delete a workout, in which status, and what disappears with it.

### Modified Capabilities

None. `workout-editing` requirements are unchanged; deletion only reuses its permission set.

## Module Classification

- Workouts -> mutable aggregate -> `.stpr/template/src/WeightTracker.Domain/Workouts` (fully mutable, not catalog-like)
- Workout delete authorization -> auth/authorization support -> `src/WeightTracker.Application/Services/WorkoutEditAuthorizer.cs`

## Canonical Template Mapping

- `workout-deletion` use case -> `src/WeightTracker.Application/UseCases/Workouts/RemoveWorkoutExerciseUseCase.cs` -> same load / authorize / mutate / persist / commit shape and exception set.
- `workout-deletion` permission -> `src/WeightTracker.Application/Services/WorkoutEditAuthorizer.cs` -> existing business authorization service whose permission half is reused.
- `workout-deletion` draft-only guard -> `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs` -> aggregate action methods protect action semantics.
- `workout-deletion` endpoint -> `src/WeightTracker.Api/Endpoints/Workouts/WorkoutsController.cs` -> authenticated route, `CurrentUser` to use case, `ExceptionMapper.Map` in catch.
- `workout-deletion` persistence -> `src/WeightTracker.Infra/Repositories/WorkoutRepository.cs` -> multi-table aggregate with row-version concurrency.
- `workout-deletion` tests -> `.stpr/template/test/WeightTracker.Integration/Workouts/WorkoutEndpointTest.cs`, `.stpr/template/test/WeightTracker.Unit/Domain/Workouts/WorkoutTests.cs` -> per-route auth matrix and aggregate test structure.

## Business Identifiers And Mutability

- `Workout.Id` is the immutable technical identifier. No business identifiers are introduced or changed.

## Conflicts With Template Or Invariants

None. The admin-included draft lock is a new, explicitly documented rule, not an exception to the edit rule.

## Impact

- Api: `WorkoutsController` new `DELETE` action.
- Application: new `DeleteWorkoutUseCase`; `WorkoutEditAuthorizer` public permission method; `IWorkoutRepository.Delete`.
- Domain: `Workout` delete guard.
- Infra: `WorkoutRepository.Delete`. Existing FKs already cascade (`WorkoutExercises → Workouts`, `WorkoutExerciseSets` and `SetRecords → WorkoutExercises`), so no migration.
- Wiring: `Program.cs` registration.
- Docs: `.stpr/template/STPR_INVARIANTS.md`.
- Tests: unit (aggregate, authorizer, use case) and integration (endpoint matrix, repository).
