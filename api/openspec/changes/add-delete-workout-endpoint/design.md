# Design

## Context

See `proposal.md` for motivation and `specs/workout-deletion/spec.md` for the behaviour contract.

Observed today:

- `WorkoutEditAuthorizer.EnsureCanEdit` combines a private permission check (`CanEdit`: admin always; prescribing trainer when `TrainerId` is set; otherwise the owner) with a status lock that lets admins through on non-reviewed workouts.
- `IWorkoutRepository` has no removal operation. `WorkoutRepository.GetById` records the row version in `_loadedVersions`, and `Save` sets it as the original `Version` value so EF checks concurrency on commit. `UnitOfWork.SaveChangesAsync` translates EF's concurrency exception into `WorkoutConcurrencyConflictException`, which `ExceptionMapper` maps to `409`.
- FKs already cascade: `FK_WorkoutExercises_Workout` (`Rule.Cascade`), `WorkoutExerciseSets → WorkoutExercises` and `FK_SetRecords_WorkoutExercise` both cascade. The EF model configures the aggregate's children with `DeleteBehavior.Cascade`. No other table references `Workouts`.
- `WorkoutLockedException`, `UnauthorizedOperationException` and `WorkoutNotFoundException` already map to `409`, `403` and `404`.

## Goals / Non-Goals

**Goals:**
- One delete use case that reuses the existing edit permission without inheriting the admin's status bypass.
- A delete that respects the optimistic concurrency token.

**Non-Goals:**
- Changing the edit rule or any existing `EnsureCanEdit` caller.
- Schema changes.

## Decisions

### Layer mapping

- **Api**: `WorkoutsController.Delete` with `[HttpDelete("{workoutId:guid}")]`, `204` / `401` / `403` / `404` / `409` response types. It passes `CurrentUser` id and role to the use case and returns `NoContent()`. The catch delegates to `ExceptionMapper.Map`. Mirrors `RemoveExercise`.
- **Application**: `DeleteWorkoutUseCase.Execute(callerId, callerRole, workoutId, ct)`. Steps: `GetById` → `WorkoutNotFoundException`; `authorizer.EnsureCanModify(...)`; `workout.EnsureCanBeDeleted()`; `_workouts.Delete(workout, ct)`; `_unitOfWork.SaveChangesAsync(ct)`. Mirrors `RemoveWorkoutExerciseUseCase`.
- **Domain**: `Workout.EnsureCanBeDeleted()` throws a new domain `WorkoutDeletionLockedException(Id, Status)` unless `Status == DRAFT`, mapped to `409` in `ExceptionMapper`. It mirrors `WorkoutPlacementLockedException`: `WorkoutLockedException` lives in Application and cannot be thrown by the aggregate. It is a guard, not a state change, because deletion is not a transition.
- **Infrastructure**: `WorkoutRepository.Delete` loads the tracked entity with its aggregate tables, applies the `_loadedVersions` original `Version` (same as `Save`), and calls `_context.Workouts.Remove(tracked)`. Children go with it through the configured cascade. If the row is already gone, it does nothing and the commit succeeds. That race is not observable as a conflict, because the use case already returned 404 for a missing workout at read time.

### Authorization placement: reuse the edit permission (chosen)

Make the permission half of `WorkoutEditAuthorizer` public as `EnsureCanModify(workout, callerId, callerRole, operation)`, which throws `UnauthorizedOperationException`. `EnsureCanEdit` calls `EnsureCanModify` and then applies its lock, so its behaviour is unchanged. The draft-only lock for deletion lives in the aggregate.

- *Alternative: a new `WorkoutDeleteAuthorizer`.* Rejected by the user. It would duplicate the permission set, and the two rules could drift apart.
- *Alternative: call `EnsureCanEdit` from the delete use case.* Rejected: it lets admins through on non-draft workouts, which contradicts the spec.

### Cross-module dependencies

None beyond the Workouts domain area. Mesocycle placement is a column on `Workouts`, so deleting a placed draft removes its placement and leaves the mesocycle untouched.

## Risks / Trade-offs

- [Making `EnsureCanModify` public invites misuse by edit use cases, which would skip the lock] → Its XML doc states that it checks permission only and that content edits must use `EnsureCanEdit`. The invariants rule names both entry points.
- [Hard delete loses data permanently] → Limited to `DRAFT`, which never has set records or a review.
- [Concurrent publish while deleting] → The row-version check refuses the delete with `409`.

## Migration Plan

No schema migration. Deploy as a normal API release. Rollback means removing the endpoint, and no data change needs reverting.
