# Design

## Context

See proposal.md for motivation. `Workout` already carries `ReviewedAt` (column `ReviewedAtUtc`), set by `MarkReviewed(reviewerId, reviewedAtUtc, ...)` with the time passed in by `ReviewWorkoutUseCase` from `IClock`. `ChangeStatus(target)` and `OverrideStatus(target)` take no time today, and `ChangeWorkoutStatusUseCase` has no clock. `Rehydrate` enforces that `ReviewedAt`/`ReviewedBy` are set exactly when the status is `REVIEWED`.

## Goals / Non-Goals

**Goals:**
- Record `CompletedAt` inside the aggregate, as part of the status action, so no caller can complete a workout without recording the date.
- Mirror the existing `ReviewedAt` shape in every layer.

**Non-Goals:**
- Any change to lifecycle, permissions, exceptions or `ReviewedAt`.
- Renaming `ReviewedAt` to `ReviewedAtUtc` in domain or API. Convention is `...At` in domain/API and `...AtUtc` for the column.

## Decisions

### Layer mapping

- **Domain** (`Workout`):
  - `public DateTime? CompletedAt { get; private set; }`, documented like `ReviewedAt`.
  - `ChangeStatus(WorkoutStatus target, DateTime changedAtUtc)` and `OverrideStatus(WorkoutStatus target, DateTime changedAtUtc)`. After the existing checks pass, when `target == COMPLETED`, set `CompletedAt = ToStoredPrecision(changedAtUtc)`. Other targets leave it untouched.
  - `Rehydrate` gains `DateTime? completedAt`. `Create` and `CreateFromTemplate` leave it null.
- **Application**: `ChangeWorkoutStatusUseCase` injects `IClock` (already registered) and passes `_clock.UtcNow` to `OverrideStatus` and `ChangeStatus`. No `Program.cs` change beyond what DI resolves.
- **Infrastructure**: `WorkoutEntity.CompletedAtUtc` (`DateTime?`), mapped in `WeightTrackerDbContext` next to `ReviewedAtUtc`; `WorkoutMapper` both directions; `WorkoutRepository` copies it onto the tracked entity. New migration `AddCompletedAtToWorkouts_<timestamp>` adding `CompletedAtUtc` as nullable `timestamptz`; `Down()` deletes it.
- **Api**: `completedAt` placed just before `reviewedAt` in `WorkoutResponse`, `WorkoutByIdResponse` and `WorkoutListItemResponse`, with matching `<param>` docs. Controller and requests unchanged.

No cross-module dependencies are added. Repository pattern and the current authorization flow (`WorkoutStatusAuthorizer` before the aggregate call) are unchanged.

### Time passed as a required parameter rather than an optional one

Alternative: `DateTime? changedAtUtc = null`. Rejected: an optional time lets a caller complete a workout without a date, which is exactly what the aggregate must prevent. Every call site (one use case plus tests) is updated.

### Keep `CompletedAt` when leaving `COMPLETED`

Alternative: clear it on leaving. Rejected: the requested semantics are "filled the last time the status is reached", and the status already tells whether the workout is completed now. Keeping it also preserves history when an admin corrects a workout.

### No `Rehydrate` invariant on `CompletedAt`

Alternative: require it for `COMPLETED`/`REVIEWED`. Rejected: existing rows have no date (no backfill), and keeping the date after leaving `COMPLETED` means it can appear with any status. Unlike the review pair, there is no consistency rule to enforce.

### No backfill

Alternative: backfill from `ResultsUpdatedAtUtc` or the workout date. Rejected: both are guesses, and a wrong date is worse than `null`.

## Risks / Trade-offs

- [Clients treat `completedAt != null` as "is completed"] → Response docs state it is the last completion time; status remains the source of truth.
- [Legacy completed workouts show `null`] → Documented in the spec; acceptable since the true time is unknown.
- [Template drift: `Workout.cs` and `WorkoutTests.cs` are curated copies] → Re-sync and run the drift check.

## Migration Plan

- Deploy the migration with the code; the column is nullable, so it is backward compatible.
- Rollback: `--down` on that migration drops the column; the previous code does not read it.
