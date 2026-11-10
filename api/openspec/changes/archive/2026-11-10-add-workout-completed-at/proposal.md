# Proposal

## Why

Workout responses tell whether a workout is `COMPLETED` or `REVIEWED`, but not when it was completed. Clients need that date to sort and show history, and the trainer's review queue will need it later. `reviewedAt` already exists and already satisfies the requested rule (it is set when `REVIEWED` is reached, which is terminal), so the missing piece is the completion date.

Source brief: `.stpr/plan/openspec/proposal-change-add-workout-completed-and-reviewed-dates.md`.

## What Changes

- New `Workout.CompletedAt` (`DateTime?`, UTC), null until the workout is first completed.
- Every time the workout reaches `COMPLETED`, through the lifecycle (`ChangeStatus`) or an admin override (`OverrideStatus`), `CompletedAt` is set to the server time of the request. A later completion overwrites it.
- Leaving `COMPLETED` (admin override only) keeps `CompletedAt`: it means "last time completed".
- `ChangeStatus` and `OverrideStatus` take the change time from the caller. `ChangeWorkoutStatusUseCase` reads it from `IClock`.
- New nullable column `Workouts.CompletedAtUtc` (FluentMigrator). No backfill: existing completed or reviewed workouts keep `null`.
- `completedAt` added to `WorkoutResponse`, `WorkoutByIdResponse` and `WorkoutListItemResponse`, next to `reviewedAt`. Additive, not breaking.
- `reviewedAt` behavior is unchanged.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `workout-status`: reaching `COMPLETED` records the completion time, overwritten on every new completion and kept when the workout leaves `COMPLETED`.
- `workout-retrieval`: the review and results metadata in workout responses gains `completedAt`.

`workout-review` does not change: its requirement "The review records who reviewed and when" already covers `reviewedAt`.

## Canonical Template References

- `workout-status`, domain -> `Workout.ReviewedAt` / `MarkReviewed` in `src/WeightTracker.Domain/Workouts/Workout.cs` and `WorkoutTests` (`.stpr/template/test/WeightTracker.Unit/Domain/Workouts`) -> same aggregate, same "nullable UTC date set by a status method with the time passed in" shape.
- `workout-status`, use case -> `ReviewWorkoutUseCase` (`IClock.UtcNow`) applied to `ChangeWorkoutStatusUseCase` -> nearest status use case that already records a time.
- `workout-status`, persistence -> `ReviewedAtUtc` in `WorkoutEntity`, `WorkoutMapper`, `WorkoutRepository`, `WeightTrackerDbContext`; migration shaped like `AddReviewToWorkouts_202611071300` -> nullable `timestamptz` column on `Workouts`.
- `workout-retrieval` -> `ReviewedAt` in the three workout response DTOs -> same position and doc style.

## Classification And Identifiers

- Workouts is a mutable aggregate (`Workout` root), not a catalog.
- No new business identifiers and no new generated ids. Existing identifiers keep their mutability.

## Conflicts With Template Or Invariants

None. `STPR_INVARIANTS.md` gains one line in the status rule: reaching `COMPLETED` records `CompletedAt`.

## Impact

- Domain: `Workout` (`CompletedAt`, `ChangeStatus`, `OverrideStatus`, `Rehydrate`).
- Application: `ChangeWorkoutStatusUseCase` takes `IClock`.
- Infra / Migrations: `WorkoutEntity`, `WorkoutMapper`, `WorkoutRepository`, `WeightTrackerDbContext`, new migration.
- API: three workout response DTOs; OpenAPI document gains `completedAt`.
- Front end: contract regeneration in a separate front change.
- Template copies (`Workout.cs`, `WorkoutTests.cs`) re-synced.
