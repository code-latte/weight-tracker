# Proposal

## Why

Workout responses say when a workout was completed (`completedAt`) but not when it was started, so clients cannot show when a session began or how long it took. Existing workouts also need a plausible start date so history screens are not empty from day one.

Source brief: `.stpr/plan/openspec/proposal-change-add-workout-started-date.md`.

## What Changes

- New `Workout.StartedAt` (`DateTime?`, UTC), null until the workout is first started.
- Every time the workout reaches `STARTED`, through the lifecycle (`ChangeStatus`) or an admin override (`OverrideStatus`), `StartedAt` is set to the server time of the request. A later start overwrites it.
- Leaving `STARTED` (completing it, or an admin override) keeps `StartedAt`: it means "last time started".
- `ChangeStatus` and `OverrideStatus` keep their signatures. They already receive `changedAtUtc` from `ChangeWorkoutStatusUseCase` (`IClock`).
- New nullable column `Workouts.StartedAtUtc` (FluentMigrator), backfilled with an estimate in the same migration:
  - `COMPLETED` or `REVIEWED` rows with `CompletedAtUtc`: `CompletedAtUtc` minus a random 50 min to 1 h 15 min;
  - `STARTED` rows with `ResultsUpdatedAtUtc`: `ResultsUpdatedAtUtc` minus a random 15 to 20 min;
  - every other row stays `null`.
- `startedAt` added to `WorkoutResponse`, `WorkoutByIdResponse` and `WorkoutListItemResponse`, right before `completedAt`. Additive, not breaking.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `workout-status`: reaching `STARTED` records the start time, overwritten on every new start and kept when the workout leaves `STARTED`; existing workouts receive an estimated start time.
- `workout-retrieval`: the review and results metadata in workout responses gains `startedAt`.

## Canonical Template References

- `workout-status`, domain -> `Workout.CompletedAt` set in `SetStatus` in `src/WeightTracker.Domain/Workouts/Workout.cs`, and its cases in `WorkoutTests` (`.stpr/template/test/WeightTracker.Unit/Domain/Workouts`) -> same aggregate, same "nullable UTC date recorded when a status is reached" shape.
- `workout-status`, use case -> `ChangeWorkoutStatusUseCase` already passes `IClock.UtcNow`; only its tests change.
- `workout-status`, persistence -> `CompletedAtUtc` in `WorkoutEntity`, `WorkoutMapper`, `WorkoutRepository`, `WeightTrackerDbContext`; column migration shaped like `AddCompletedAtToWorkouts_202611101200`; SQL backfill shaped like the `Execute.Sql` use in `AddDurationToSetRecords_202611121200`.
- `workout-retrieval` -> `CompletedAt` in the workout response DTOs -> same position and doc style.

## Classification And Identifiers

- Workouts is a mutable aggregate (`Workout` root), not a catalog.
- No new business identifiers and no new generated ids. Existing identifiers keep their mutability.

## Conflicts With Template Or Invariants

None. `STPR_INVARIANTS.md` gains one line in the status rule: reaching `STARTED` records `StartedAt`.

The backfill writes estimated, not real, dates. This is an explicit product decision from the brief and differs from the `CompletedAt` change, which refused to backfill guesses.

## Impact

- Domain: `Workout` (`StartedAt`, `SetStatus`, `Rehydrate`).
- Application: no code change; `ChangeWorkoutStatusUseCaseTests` gain assertions.
- Infra / Migrations: `WorkoutEntity`, `WorkoutMapper`, `WorkoutRepository`, `WeightTrackerDbContext`, new migration with backfill.
- API: workout response DTOs; OpenAPI document gains `startedAt`.
- Front end: contract regeneration in a separate front change.
- Template copies (`Workout.cs`, `WorkoutTests.cs`) re-synced.
