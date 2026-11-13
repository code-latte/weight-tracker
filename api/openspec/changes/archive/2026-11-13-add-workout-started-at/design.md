# Design

## Context

See proposal.md for motivation. `Workout` already records `CompletedAt` in the private `SetStatus(target, changedAtUtc)`, which both `ChangeStatus` and `OverrideStatus` call after their checks pass. `ChangeWorkoutStatusUseCase` already passes `IClock.UtcNow`. `ResultsUpdatedAt` is the optimistic concurrency token for reviews (`MarkReviewed` compares it). `Status` is stored as the integer value of `WorkoutStatus` (`STARTED = 3`, `COMPLETED = 4`, `REVIEWED = 5`). `ToStoredPrecision` truncates to microseconds, which matches `timestamptz`.

## Goals / Non-Goals

**Goals:**
- Record `StartedAt` inside the aggregate as part of the status action, mirroring `CompletedAt` in every layer.
- Give existing workouts an estimated start date in the same migration that adds the column.

**Non-Goals:**
- Any change to lifecycle, permissions, exceptions, `CompletedAt` or `ReviewedAt`.
- Any change to method signatures: the time is already passed in.
- A computed duration field.

## Decisions

### Layer mapping

- **Domain** (`Workout`):
  - `public DateTime? StartedAt { get; private set; }`, documented like `CompletedAt` and declared just before it.
  - `SetStatus` sets `StartedAt = ToStoredPrecision(changedAtUtc)` when `target == STARTED`, next to the existing `COMPLETED` branch. Other targets leave it untouched. The `changedAtUtc` docs on `ChangeStatus` and `OverrideStatus` mention `StartedAt`.
  - `Rehydrate` gains an optional `DateTime? startedAt = null` after `completedAt`, so existing callers keep compiling. `Create` and `CreateFromTemplate` leave it null; templates never carry it.
- **Application**: no change. `ChangeWorkoutStatusUseCase` already passes the clock time to both paths.
- **Infrastructure**: `WorkoutEntity.StartedAtUtc` (`DateTime?`), mapped in `WeightTrackerDbContext` next to `CompletedAtUtc`; `WorkoutMapper` both directions; `WorkoutRepository` copies it onto the tracked entity.
- **Migrations**: new `AddStartedAtToWorkouts_<timestamp>`. `Up()` adds nullable `timestamptz` `StartedAtUtc`, then runs the backfill with `Execute.Sql`. `Down()` deletes the column.
- **Api**: `startedAt` placed just before `completedAt` in `WorkoutResponse` (both records), `WorkoutByIdResponse` and `WorkoutListItemResponse`, with matching `<param>` docs. Controller and requests unchanged.

No cross-module dependencies are added. Repository pattern and the current authorization flow (`WorkoutStatusAuthorizer` before the aggregate call) are unchanged.

### Backfill in SQL, per row

```sql
UPDATE "Workouts"
SET "StartedAtUtc" = "CompletedAtUtc" - (interval '50 minutes' + random() * interval '25 minutes')
WHERE "Status" IN (4, 5) AND "CompletedAtUtc" IS NOT NULL;

UPDATE "Workouts"
SET "StartedAtUtc" = "ResultsUpdatedAtUtc" - (interval '15 minutes' + random() * interval '5 minutes')
WHERE "Status" = 3 AND "ResultsUpdatedAtUtc" IS NOT NULL;
```

`random()` is volatile, so Postgres evaluates it per row. Status integers are written literally, as the migration must not depend on the domain enum.

Alternative: backfill in C# through the repository. Rejected: migrations run before the application and do not load the domain; a set-based `UPDATE` is atomic with the column addition.

### Estimated dates despite the `CompletedAt` precedent

The `CompletedAt` change refused to backfill guesses. Here the brief explicitly asks for estimates, so the history screens have a start time. The estimate stays within plausible bounds of a real date the workout already has.

### Keep `StartedAt` when leaving `STARTED`

Same reasoning as `CompletedAt`: the field means "last time started" and the status says the rest. Clearing it on completion would also erase the start of every finished workout.

### No `Rehydrate` invariant on `StartedAt`

Alternatives: require it for `STARTED`/`COMPLETED`/`REVIEWED`, or require `StartedAt <= CompletedAt`. Rejected: rows without a source date keep `null`, and an admin override can start a workout after its last completion.

## Risks / Trade-offs

- [Clients treat backfilled dates as real] → Response docs say it may be an estimate for older workouts; the spec records the rule.
- [Admin overrides produce `startedAt` after `completedAt`] → Accepted; both mean "last time", and no invariant relies on their order.
- [Backfill is not covered by automated tests, because the integration suite migrates an empty database] → Verify manually against the development database before deploying (counts per status, offsets within bounds, `ResultsUpdatedAtUtc` unchanged).
- [Template drift: `Workout.cs` and `WorkoutTests.cs` are curated copies] → Re-sync and run the drift check.

## Migration Plan

- Deploy the migration with the code. The column is nullable and the backfill only writes it, so the previous code keeps working.
- Rollback: `--down` on that migration drops the column; the backfilled estimates are lost, which is acceptable.
