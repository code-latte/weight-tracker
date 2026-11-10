## Change

Record on each workout when it was last completed (`CompletedAt`) and when it was reviewed (`ReviewedAt`), both nullable UTC dates.

## Objective

Workout responses say whether a workout is `COMPLETED` or `REVIEWED`, but not when that happened. Clients need these dates to sort and show history ("completed 2 days ago") and to build the trainer's review queue later.

`ReviewedAt` already exists. It was added with the `REVIEWED` status (`AddReviewToWorkouts_202611071300`, column `ReviewedAtUtc`), is set by `Workout.MarkReviewed` and is already exposed in the responses. This change adds `CompletedAt` in the same way and confirms the rule for both dates.

## Capabilities

- Add `Workout.CompletedAt` (`DateTime?`, UTC), persisted in a new nullable column `Workouts.CompletedAtUtc`.
- Set `CompletedAt` every time the workout reaches `COMPLETED`, through the lifecycle (`ChangeStatus`) or an admin override (`OverrideStatus`). A later completion overwrites the earlier date.
- Keep `ReviewedAt` as it is: it is set when the workout reaches `REVIEWED` through `MarkReviewed`. `REVIEWED` is terminal, so the first time is also the last.
- Expose `completedAt` next to `reviewedAt` in `WorkoutResponse`, `WorkoutByIdResponse` and `WorkoutListItemResponse`.

## Module Classification

- Domain: Workouts -> mutable aggregate (`Workout` root) -> `src/WeightTracker.Domain/Workouts/Workout.cs`
- Application: status change -> mutable aggregate use case -> `src/WeightTracker.Application/UseCases/Workouts/ChangeWorkoutStatusUseCase.cs`
- Infra / Migrations: Workouts persistence -> mutable aggregate persistence -> `WorkoutEntity`, `WorkoutMapper`, `WorkoutRepository`, `WeightTrackerDbContext`, a new FluentMigrator migration
- API: Workouts -> workout response DTOs in `src/WeightTracker.Api/Endpoints/Workouts/Dtos/`

Workouts is a real mutable aggregate, not a catalog.

## Canonical Template Mapping

- `CompletedAt` on the aggregate -> `Workout.ReviewedAt` / `MarkReviewed` -> same shape: a nullable UTC date set by a status-changing method, with the time passed in by the caller.
- Time source in the use case -> `ReviewWorkoutUseCase` (`IClock.UtcNow`) -> the nearest status use case that already records a time.
- Column, entity, mapper and repository copy -> `ReviewedAtUtc` in `WorkoutEntity`, `WorkoutMapper` and `WorkoutRepository` (tracked-entity copy) -> the existing review date.
- Migration -> `202611071300_AddReviewToWorkouts` -> it adds nullable `timestamptz` columns to `Workouts`.
- Response fields -> `ReviewedAt` in the three workout response DTOs -> same position and docs style.
- Aggregate tests -> `.stpr/template/test/WeightTracker.Unit/Domain/Workouts/WorkoutTests`.

## State And Mutability

- `Workout` gains `CompletedAt` (`DateTime?`, UTC), null until the workout is first completed.
- `ChangeStatus(target)` and `OverrideStatus(target)` take a `changedAtUtc` parameter. When the target is `COMPLETED`, `CompletedAt` becomes that time, truncated with `ToStoredPrecision` like `ReviewedAt`. Any other target leaves `CompletedAt` untouched. Time is always passed in by the caller, never read inside the domain.
- `ChangeWorkoutStatusUseCase` takes `IClock` and passes `_clock.UtcNow` to both methods.
- Leaving `COMPLETED` (only possible for an admin through `OverrideStatus`) does **not** clear `CompletedAt`. The date means "last time it was completed", not "completed now". The status says the rest.
- `ReviewedAt` keeps its current behavior: set together with `ReviewedBy` by `MarkReviewed`, never cleared.
- `Workout.Create` and `CreateFromTemplate` start `CompletedAt` as null. `Workout.Rehydrate` takes it.
- Workouts copied into templates do not carry `CompletedAt`.

## Business Rules

- `CompletedAt` is written only when the status becomes `COMPLETED`. No other operation, including set record changes and content edits, touches it.
- Re-completing a workout (admin moves it away and it later reaches `COMPLETED` again) overwrites `CompletedAt` with the new time.
- `ReviewedAt` is written only by `MarkReviewed`. Its rules (both review fields set or both null, and both set exactly when the status is `REVIEWED`) do not change.
- `Rehydrate` does **not** require `CompletedAt` to be set for `COMPLETED` or `REVIEWED` workouts, because existing rows have no date (see Persistence). It also accepts a `CompletedAt` with any status, since leaving `COMPLETED` keeps the date.
- The lifecycle, permission checks and exceptions do not change.

## Roles And Visibility

- No permission changes. `CompletedAt` is set by whoever is allowed to complete the workout today (the athlete through the lifecycle, an admin through the override).
- Everyone who can read the workout sees `completedAt` and `reviewedAt`. `IsHiddenFrom` does not change.

## Business Identifiers And Mutability

- No new business identifiers and no new generated ids.

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## Persistence

New column on `Workouts`:

| Column | Type | Notes |
|---|---|---|
| `CompletedAtUtc` | timestamptz | nullable, UTC |

- New FluentMigrator migration, e.g. `AddCompletedAtToWorkouts_<timestamp>`. `Down()` drops the column. No EF Core migration.
- `ReviewedAtUtc` already exists, so it needs no migration.
- No backfill. Workouts that are already `COMPLETED` or `REVIEWED` keep `CompletedAtUtc` null, because the real completion time is unknown.
- `WeightTrackerDbContext` maps the column, `WorkoutMapper` round-trips it and `WorkoutRepository` copies it onto the tracked entity, like `ReviewedAtUtc`.

## API

- No new routes. `PUT /workouts/{workoutId}/status` keeps its contract and returns as today.
- `WorkoutResponse`, `WorkoutByIdResponse` and `WorkoutListItemResponse` add `completedAt` (`DateTime?`, UTC) next to `reviewedAt`.
- Additive change. The front-end contract (`front/core/openapi/v1.json`, `front/core/src/contract/generated/api.ts`) must be regenerated in the front change.

## Out Of Scope

- `StartedAt`, `PublishedAt` or a full status history.
- Backfilling `CompletedAtUtc` for existing workouts.
- Sorting or filtering workout lists by these dates.
- The trainer's pending-review queue.
- Renaming the domain or API property `ReviewedAt` to `ReviewedAtUtc`. The repo convention is `...At` in domain and API and `...AtUtc` in the column; `CompletedAt` follows it.
- Front-end and backoffice screens.

## Testing

- **Unit tests:**
  - `WorkoutTests`: `ChangeStatus(COMPLETED, t)` sets `CompletedAt` to `t`; other lifecycle moves leave it unchanged.
  - `WorkoutTests`: `OverrideStatus(COMPLETED, t)` sets it; overriding away from `COMPLETED` keeps it; completing again overwrites it.
  - `WorkoutTests`: a refused transition leaves `CompletedAt` unchanged; `MarkReviewed` does not change `CompletedAt`; `Create` and `CreateFromTemplate` start it null; `Rehydrate` keeps it, including null on a `COMPLETED` workout.
  - `ChangeWorkoutStatusUseCaseTests`: the clock time reaches the aggregate for both the lifecycle and the admin path.
  - `WorkoutMapperTests`: round-trip with `CompletedAt` set and null.
- **Integration tests:**
  - `PUT /workouts/{id}/status` to `COMPLETED`, then `GET /workouts/{id}` returns a non-null `completedAt`; the list routes return it too.
  - `WorkoutRepositoryTests`: `CompletedAt` persists on update.
  - The existing auth matrix for `PUT /workouts/{id}/status` does not change, so no new auth cases are needed.
- Run the full `dotnet test WeightTracker.slnx` with Docker.

## Notes

- Update the curated template copies (`Workout.cs`, `WorkoutTests.cs`, and any other that changes) so they stay byte-identical to `src`.
- Update `STPR_INVARIANTS.md` only if it lists workout fields or the status rule's side effects. Add that reaching `COMPLETED` records `CompletedAt`.
- **Decided:** `CompletedAt` is not cleared when the workout leaves `COMPLETED`, so it always means "last time completed".
- **Decided:** no backfill and no `Rehydrate` invariant on `CompletedAt`, so existing rows still load.
- No other deviations from the mapped template references are allowed.
