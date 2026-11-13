## Change

Record on each workout when it was last started (`StartedAt`), a nullable UTC date, and backfill it for workouts that are already started, completed or reviewed.

## Objective

Workout responses say when a workout was completed (`completedAt`) but not when it was started, so clients cannot show how long a session took or when it began. This change adds `StartedAt` with the same shape and rules as `CompletedAt`, and fills it for existing workouts with a plausible estimate so history screens are not empty.

## Capabilities

- Add `Workout.StartedAt` (`DateTime?`, UTC), persisted in a new nullable column `Workouts.StartedAtUtc`.
- Set `StartedAt` every time the workout reaches `STARTED`, through the lifecycle (`ChangeStatus`) or an admin override (`OverrideStatus`). A later start overwrites the earlier date.
- Expose `startedAt` next to `completedAt` in every response that returns `completedAt`: `WorkoutResponse` (both records in `WorkoutResponse.cs`), `WorkoutByIdResponse` and `WorkoutListItemResponse`.
- Backfill `StartedAtUtc` for existing rows in the migration (see Persistence).

## Module Classification

- Domain: Workouts -> mutable aggregate (`Workout` root) -> `src/WeightTracker.Domain/Workouts/Workout.cs`
- Application: status change -> mutable aggregate use case -> `src/WeightTracker.Application/UseCases/Workouts/ChangeWorkoutStatusUseCase.cs` (already passes `IClock.UtcNow`; no change expected)
- Infra / Migrations: Workouts persistence -> mutable aggregate persistence -> `WorkoutEntity`, `WorkoutMapper`, `WorkoutRepository`, `WeightTrackerDbContext`, a new FluentMigrator migration
- API: Workouts -> workout response DTOs in `src/WeightTracker.Api/Endpoints/Workouts/Dtos/`

Workouts is a real mutable aggregate, not a catalog.

## Canonical Template Mapping

- `StartedAt` on the aggregate -> `Workout.CompletedAt` / `SetStatus` -> identical shape: a nullable UTC date set inside `SetStatus` when the target status is reached, with the time passed in by the caller and truncated with `ToStoredPrecision`.
- Column, entity, mapper and repository copy -> `CompletedAtUtc` in `WorkoutEntity`, `WorkoutMapper`, `WorkoutRepository` (tracked-entity copy) and `WeightTrackerDbContext`.
- Migration -> `202611101200_AddCompletedAtToWorkouts` for the column; an existing migration using `Execute.Sql` (e.g. `202611121200_AddDurationToSetRecords`) for the backfill.
- Response fields -> `CompletedAt` in the workout response DTOs -> same position (right before `completedAt`) and docs style.
- Aggregate tests -> `.stpr/template/test/WeightTracker.Unit/Domain/Workouts/WorkoutTests` (the existing `CompletedAt` cases).

## State And Mutability

- `Workout` gains `StartedAt` (`DateTime?`, UTC), null until the workout is first started.
- `SetStatus` sets `StartedAt = ToStoredPrecision(changedAtUtc)` when the target is `STARTED`, exactly as it does for `COMPLETED`. Any other target leaves `StartedAt` untouched. `ChangeStatus` and `OverrideStatus` keep their signatures; their `changedAtUtc` docs mention `StartedAt` too.
- Leaving `STARTED` (completing it, or an admin override back to `PUBLISHED`/`DRAFT`) does **not** clear `StartedAt`. It means "last time it was started".
- `Workout.Create` and `CreateFromTemplate` start `StartedAt` as null. `Workout.Rehydrate` takes it as a new optional parameter after `completedAt`.
- Workouts copied into templates do not carry `StartedAt`.

## Business Rules

- `StartedAt` is written only when the status becomes `STARTED`. Set record changes, content edits and reviews do not touch it.
- Re-starting a workout (admin override back to `STARTED`) overwrites `StartedAt` with the new time.
- `Rehydrate` does **not** require `StartedAt` for `STARTED`, `COMPLETED` or `REVIEWED` workouts, and does not enforce `StartedAt <= CompletedAt`, because some existing rows cannot be backfilled and admin overrides can reorder the dates.
- The lifecycle, permission checks, exceptions and the review concurrency check on `ResultsUpdatedAt` do not change.

## Roles And Visibility

- No permission changes. `StartedAt` is set by whoever is allowed to start the workout today (the athlete through the lifecycle, an admin through the override).
- Everyone who can read the workout sees `startedAt`. `IsHiddenFrom` does not change.

## Business Identifiers And Mutability

- No new business identifiers and no new generated ids.

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## Persistence

New column on `Workouts`:

| Column | Type | Notes |
|---|---|---|
| `StartedAtUtc` | timestamptz | nullable, UTC |

- New FluentMigrator migration, e.g. `AddStartedAtToWorkouts_<timestamp>`. `Up()` adds the column and then runs the backfill. `Down()` drops the column. No EF Core migration.
- `WeightTrackerDbContext` maps the column, `WorkoutMapper` round-trips it and `WorkoutRepository` copies it onto the tracked entity, like `CompletedAtUtc`.

### Backfill

The backfill is an estimate, not the real start time. Each row gets its own random offset (Postgres `random()`, evaluated per row). `Status` is stored as the integer value of `WorkoutStatus`.

| Rows | `StartedAtUtc` |
|---|---|
| `COMPLETED` (4) or `REVIEWED` (5) with `CompletedAtUtc` not null | `CompletedAtUtc` minus a random duration between 50 min and 1 h 15 min |
| `STARTED` (3) with `ResultsUpdatedAtUtc` not null | `ResultsUpdatedAtUtc` minus a random duration between 15 min and 20 min |
| Everything else | stays null |

Sketch:

```sql
UPDATE "Workouts"
SET "StartedAtUtc" = "CompletedAtUtc" - (interval '50 minutes' + random() * interval '25 minutes')
WHERE "Status" IN (4, 5) AND "CompletedAtUtc" IS NOT NULL;

UPDATE "Workouts"
SET "StartedAtUtc" = "ResultsUpdatedAtUtc" - (interval '15 minutes' + random() * interval '5 minutes')
WHERE "Status" = 3 AND "ResultsUpdatedAtUtc" IS NOT NULL;
```

- Rows whose source date is null stay null: `COMPLETED`/`REVIEWED` workouts completed before `CompletedAtUtc` existed (it was not backfilled), and `STARTED` workouts with no set records yet.
- `DRAFT` and `PUBLISHED` rows stay null, even if an admin override left a `CompletedAtUtc` on them.
- The backfill touches only `StartedAtUtc`. It must not modify `ResultsUpdatedAtUtc` or `CompletedAtUtc`, so pending reviews keep their concurrency token.
- `timestamptz` stores microseconds, which matches `ToStoredPrecision`, so backfilled values round-trip without drift.
- `Down()` drops the column, so the backfill is lost on rollback. That is acceptable because it is an estimate.

## API

- No new routes. `PUT /workouts/{workoutId}/status` keeps its contract.
- `WorkoutResponse`, `WorkoutByIdResponse` and `WorkoutListItemResponse` add `startedAt` (`DateTime?`, UTC) right before `completedAt`.
- Additive change. The front-end contract (`front/core/openapi/v1.json`, `front/core/src/contract/generated/api.ts`) must be regenerated in the front change.

## Out Of Scope

- Workout duration as a computed field.
- Backfilling `CompletedAtUtc`, or estimating `StartedAtUtc` for rows without a source date.
- Sorting or filtering workout lists by `StartedAt`.
- A full status history.
- Front-end and backoffice screens.

## Testing

- **Unit tests:**
  - `WorkoutTests`: `ChangeStatus(STARTED, t)` sets `StartedAt` to `t`; other lifecycle moves (publish, unpublish, complete) leave it unchanged.
  - `WorkoutTests`: `OverrideStatus(STARTED, t)` sets it; overriding away from `STARTED` keeps it; starting again overwrites it.
  - `WorkoutTests`: a refused transition leaves `StartedAt` unchanged; `MarkReviewed` and set record changes do not change it; `Create` and `CreateFromTemplate` start it null; `Rehydrate` keeps it, including null on a `STARTED` workout.
  - `ChangeWorkoutStatusUseCaseTests`: the clock time reaches `StartedAt` for both the lifecycle and the admin path.
  - `WorkoutMapperTests`: round-trip with `StartedAt` set and null.
- **Integration tests:**
  - `PUT /workouts/{id}/status` to `STARTED`, then `GET /workouts/{id}` returns a non-null `startedAt`; the list routes return it too.
  - `WorkoutRepositoryTests`: `StartedAt` persists on update.
  - Backfill: not covered by an automated test, because the suite migrates an empty database before seeding. Verify it manually against the development database (rows per status get a value in the expected window, others stay null).
  - The existing auth matrix for `PUT /workouts/{id}/status` does not change, so no new auth cases are needed.
- Run the full `dotnet test WeightTracker.slnx` with Docker.

## Notes

- Update the curated template copies (`Workout.cs`, `WorkoutTests.cs`, and any other that changes) so they stay byte-identical to `src`.
- Update `STPR_INVARIANTS.md` only if it lists the side effects of status changes. Add that reaching `STARTED` records `StartedAt`.
- Naming follows the repo convention: `StartedAt` in domain and API, `StartedAtUtc` in the column.
- **Decided:** `StartedAt` is not cleared when the workout leaves `STARTED`, mirroring `CompletedAt`.
- **Decided:** "in progress" in the request means status `STARTED`; "finished" means `COMPLETED`.
- **Decided:** rows with a null source date are not backfilled, and `Rehydrate` has no invariant on `StartedAt`.
- No other deviations from the mapped template references are allowed.
