# Change Proposal: Delete Workout

## Change

Add `DELETE /workouts/{workoutId}` so authorized callers can remove a workout while it is still a `DRAFT`.

## Objective

Workouts can be created and edited but never removed, so mistaken or abandoned drafts stay in athletes' and trainers' lists forever. Restricting deletion to drafts keeps every published, executed or reviewed workout safe from loss.

## Capabilities

- An authorized caller can permanently delete a `DRAFT` workout, including its exercises, set prescriptions and superseries.
- Deleting a workout in any other status is refused.
- Callers without delete permission are refused before learning the workout's status.

## Module Classification

- Workouts -> mutable aggregate -> `.stpr/template/src/WeightTracker.Domain/Workouts`
- Workout delete authorization -> auth/authorization support -> `src/WeightTracker.Application/Services/WorkoutEditAuthorizer.cs`

Workouts are fully mutable; nothing here is catalog-like.

## Canonical Template Mapping

- Delete use case -> `src/WeightTracker.Application/UseCases/Workouts/RemoveWorkoutExerciseUseCase.cs` -> same load / authorize / mutate / save / commit shape and exception set.
- Delete permission -> `WorkoutEditAuthorizer` -> the existing business authorization service for the Workouts domain area; its permission half is reused, permission checked before status.
- Draft-only guard -> `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs` -> aggregate action methods protect action semantics.
- Endpoint -> `src/WeightTracker.Api/Endpoints/Workouts/WorkoutsController.cs` -> authenticated route, `CurrentUser` to use case, `ExceptionMapper.Map` in catch.
- Repository removal -> `src/WeightTracker.Infra/Repositories/WorkoutRepository.cs` -> multi-table aggregate load; now also removal across its tables.
- Integration tests -> `.stpr/template/test/WeightTracker.Integration/Workouts/WorkoutEndpointTest.cs`, `test/WeightTracker.Integration/Workouts/WorkoutStatusChangeEndpointTests.cs` -> per-route auth matrix structure.
- Unit tests -> `.stpr/template/test/WeightTracker.Unit/Domain/Workouts/WorkoutTests.cs`, `test/WeightTracker.Unit/Application/Workouts/` -> aggregate guard and use-case branches.

## State And Mutability

- A deleted workout is removed from `workouts`, along with its `workout_exercises` and `workout_exercise_sets` rows (hard delete, no soft delete).
- Drafts never have set records (recording is only allowed in `STARTED`/`COMPLETED`), but the delete must not leave orphan rows regardless.
- A draft linked to a mesocycle week loses that link with it; the mesocycle itself is untouched.

## Business Rules

- Only a `DRAFT` workout can be deleted, for **every caller, `ADMIN` included**. This intentionally differs from the edit rule, where an admin may edit non-draft workouts.
- A permitted caller deleting a non-draft workout gets `WorkoutLockedException` (409).
- The permission check runs first: a caller who may not delete gets 403 and never learns the status.
- A missing workout returns 404 (`WorkoutNotFoundException`).
- The aggregate owns the draft-only guard through a delete-intent method (e.g. `Workout.EnsureCanBeDeleted()`) that throws `WorkoutLockedException` unless the workout is `DRAFT`. The use case does not compare status inline.

## Roles And Visibility

- `ADMIN`: may delete any workout (still `DRAFT` only).
- Prescribing trainer (`TrainerId == caller`): may delete when the workout has a trainer.
- Owner (`UserId == caller`): may delete only when the workout has no `TrainerId`. The owner of a prescribed workout may **not** delete it.
- Anyone else: `UnauthorizedOperationException` (403).

The permission set is identical to the edit rule's; only the status lock differs (no admin bypass).

## Business Identifiers And Mutability

- `Workout.Id` is the immutable technical identifier. No identifiers are introduced or changed.

## Out Of Scope

- Deleting `PUBLISHED`, `STARTED`, `COMPLETED` or `REVIEWED` workouts; the trainer or owner unpublishes to `DRAFT` first through the existing status rule.
- Soft delete, undo or audit trail.
- Bulk delete.
- Deleting workout templates or mesocycles.
- Frontend (app or backoffice) changes.

## Notes

- **Authorization placement:** reuse the permission half of `WorkoutEditAuthorizer`. Expose its existing permission check (today the private `CanEdit`) as a public method that throws `UnauthorizedOperationException` without applying the status lock (e.g. `EnsureCanModify`); `EnsureCanEdit` calls it and then applies the edit lock as today, so edit behaviour is unchanged. The delete use case calls the permission method and then `workout.EnsureCanBeDeleted()` for the draft-only lock. No new authorizer is introduced. Document a "Deleting a workout" rule in `.stpr/template/STPR_INVARIANTS.md` stating it shares the edit permission and that the aggregate enforces the draft-only lock for every caller.
- **Persistence:** `IWorkoutRepository` has no delete today; add `Delete(Workout, CancellationToken)` and keep mapping inside the repository. `workout_exercise_sets` cascades from `workout_exercises`, but `workout_exercises` → `workouts` and `set_records` → workouts do not cascade. The repository removes the child rows explicitly; no schema change or migration is required.
- **Concurrency:** the delete respects the workout's row-version token like `Save`, so a draft published concurrently is not removed.
- **Response:** `204 No Content` on success.
- **Tests:**
  - Unit: `EnsureCanBeDeleted` per status; the newly public permission method per role branch; `EnsureCanEdit` behaviour unchanged; use-case branches (404, 403 per forbidden role, 409 including admin, success calls `Delete` and commits).
  - Integration (`DELETE /workouts/{workoutId}`): 401 no token, 401 invalid token; 403 for a stranger, the owner of a prescribed workout and a non-prescribing trainer; 404; 409 for a non-draft (admin included); 204 for admin, prescribing trainer and owner without trainer, then assert `GET /workouts/{id}` returns 404 and the workout is excluded from the list.
  - Repository: delete removes the workout and all its child rows.
- **Wiring:** register the use case in `Program.cs`.
- **Deviations:** none beyond the intentional admin lock described in Business Rules.
