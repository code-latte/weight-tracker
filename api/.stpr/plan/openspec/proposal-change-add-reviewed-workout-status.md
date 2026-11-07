## Change

Add a `REVIEWED` workout status that the trainer sets on completed workouts, rename `FINISHED` to `COMPLETED`, and restate the workout lifecycle rules.

## Objective

A trainer who prescribes a workout has no way to say "I have looked at what the athlete did". We need a final `REVIEWED` status, set by the prescribing trainer (or an `ADMIN`) once the athlete has completed the workout. It locks both the prescription and the results and records who reviewed it and when.

While we are here, `FINISHED` becomes `COMPLETED` because the name fits better, and the lifecycle rules are tightened. A `PUBLISHED` workout can no longer be edited without moving it back to `DRAFT`, and the athlete may correct results after completing the workout.

## Capabilities

- Rename `WorkoutStatus.FINISHED` to `COMPLETED`, keeping the stored value `4`.
- Add `WorkoutStatus.REVIEWED` with the stored value `5`.
- Mark a completed, prescribed workout as reviewed through the `Workout` aggregate. This records `ReviewedAt` and `ReviewedBy`.
- Track `ResultsUpdatedAt` on the workout. Every set record change updates it.
- Refuse a review based on outdated results. The trainer sends the `ResultsUpdatedAt` they saw, and the review fails if the results have changed since.
- Allow the athlete to correct set records while the workout is `COMPLETED`, not only while it is `STARTED`.
- Lock `PUBLISHED` workouts against content edits.
- Expose `REVIEWED`, `ReviewedAt`, `ReviewedBy` and `ResultsUpdatedAt` in the workout responses.

## Module Classification

- Domain: Workouts -> mutable aggregate (`Workout` root) -> `src/WeightTracker.Domain/Workouts`
- Application: workout status, edit and recording rules -> auth/authorization support -> `src/WeightTracker.Application/Services/WorkoutStatusAuthorizer.cs`, `WorkoutEditAuthorizer`, the set record authorizer
- Application: review use case -> mutable aggregate use case -> `src/WeightTracker.Application/UseCases/Workouts/ChangeWorkoutStatusUseCase.cs`
- Infra / Migrations: Workouts persistence -> mutable aggregate persistence -> `WorkoutEntity`, `WorkoutMapper`, `WeightTrackerDbContext`
- API: Workouts -> `src/WeightTracker.Api/Endpoints/Workouts/WorkoutsController.cs`

Workouts is a real mutable aggregate, not a catalog.

## Canonical Template Mapping

- `WorkoutStatus` rename and new value -> `src/WeightTracker.Domain/Workouts/WorkoutStatus.cs` -> the enum being changed.
- `Workout.MarkReviewed` -> `Workout.ChangeStatus` / `CanTransitionTo` -> the same lifecycle, plus review metadata.
- Results lock and `ResultsUpdatedAt` -> `Workout.RecordingEntry` and the set record methods -> the existing recording gate.
- Review permission -> `WorkoutStatusAuthorizer` -> it already decides who may make each status move.
- `ReviewWorkoutUseCase` -> `ChangeWorkoutStatusUseCase` -> the nearest status-changing use case.
- Endpoint -> `PUT /workouts/{workoutId}/status` in `WorkoutsController` -> same controller, same auth and exception handling.
- Migration -> `202611071200_CreateSetRecordsTable` for naming and timestamp convention. Use any existing "add column to `Workouts`" migration (e.g. `AddSuperSeriesIdToWorkoutExercises`) for the column shape.
- Aggregate tests -> `.stpr/template/test/WeightTracker.Unit/Domain/Workouts/WorkoutTests`.

## State And Mutability

- `Workout` gains:
  - `ReviewedAt` (`DateTime?`, UTC)
  - `ReviewedBy` (`Id?`)
  - `ResultsUpdatedAt` (`DateTime?`, UTC, null until the first set record change)
- `ReviewedAt` and `ReviewedBy` are both set or both null. They are set only by `MarkReviewed` and are never cleared in this change.
- `ResultsUpdatedAt` is set by `AddSetRecord`, `UpdateSetRecord`, `RemoveSetRecord` and `ReorderSetRecords`. The methods that do not take a time today gain an `updatedAtUtc` parameter. Time is always passed in by the caller, never read inside the domain.
- `Workout.Rehydrate` takes the three new fields. `Create` and `CreateFromTemplate` start them as null.
- Existing `FINISHED` rows need no data change because the stored integer stays `4`.

## Business Rules

### Lifecycle

| From | Allowed to |
|---|---|
| `DRAFT` | `PUBLISHED` |
| `PUBLISHED` | `DRAFT`, `STARTED` |
| `STARTED` | `COMPLETED` |
| `COMPLETED` | `REVIEWED`, only when `TrainerId` is not null |
| `REVIEWED` | none (terminal) |

- A `COMPLETED` workout without a trainer is terminal. Trying to review it throws a new domain exception, e.g. `WorkoutReviewNotAllowedException` (409), for every caller including an `ADMIN`.
- `REVIEWED` can only be reached through `MarkReviewed(reviewerId, reviewedAtUtc, expectedResultsUpdatedAt)`, never through `ChangeStatus` or `OverrideStatus`. That way `ReviewedAt` and `ReviewedBy` are always recorded.
- `OverrideStatus` (admin) may not set `REVIEWED`, and may not move a workout out of `REVIEWED`. Both throw a domain exception (409). Unlocking a reviewed workout needs its own audited operation, which is out of scope here.

### Content and results locks

- **Prescription (content edit):** editable only in `DRAFT`. `PUBLISHED`, `STARTED`, `COMPLETED` and `REVIEWED` are locked (`WorkoutLockedException`, 409). `Workout.IsLocked` becomes `Status != DRAFT`. This changes current behavior, where `PUBLISHED` is editable.
- An `ADMIN` keeps the right to edit content in any status except `REVIEWED`.
- **Results (set records):** allowed while `STARTED` or `COMPLETED`, and refused in every other status for every caller including an `ADMIN`. Rename `WorkoutNotStartedException` to something that fits both statuses, e.g. `WorkoutResultsLockedException` (409).
- A `REVIEWED` workout refuses both content edits and set record changes, for every caller including an `ADMIN`.

### Review

- The workout must be `COMPLETED` and have a `TrainerId`, or the review fails with 409.
- **Concurrency:** the caller sends the `ResultsUpdatedAt` they reviewed. If it does not equal the workout's current `ResultsUpdatedAt` (null equals null), the aggregate throws a new exception, e.g. `WorkoutResultsChangedException` (409), and nothing changes.
- To close the gap between load and save, `Workouts` gets an optimistic concurrency token (Postgres `xmin` mapped as a concurrency token). A concurrent save surfaces as a 409 through `ExceptionMapper`. This is the first concurrency token in the repo, so its exception mapping is new.

## Roles And Visibility

These replace the status-change rule in `STPR_INVARIANTS.md`:

- `ADMIN`:
  - may set any status except `REVIEWED`, skipping the lifecycle (`OverrideStatus`);
  - may review a workout through the review operation, under the same `COMPLETED` + `TrainerId` rule;
  - may not leave `REVIEWED`.
- Prescribing trainer (`TrainerId`):
  - may publish and unpublish (`DRAFT <-> PUBLISHED`);
  - may review (`COMPLETED -> REVIEWED`);
  - may view partial and final results;
  - may not change results.
- Athlete (`UserId`) of a prescribed workout:
  - may only start and complete it (`PUBLISHED -> STARTED -> COMPLETED`);
  - never sees it while it is a `DRAFT`;
  - may never review it.
- Athlete of a workout without a trainer may make every lifecycle move. There is no review step.
- Recording and correcting results keeps the current recording rule (owner unless their role is `TRAINER`, plus `ADMIN`). Only the allowed statuses change.
- Anyone else gets 403. A valid move the caller's role cannot make is 403, and a move outside the lifecycle is 409. Permission checks run before status checks.
- Reading does not change. `IsHiddenFrom` still hides only prescribed drafts from their athlete. Self-planned drafts stay visible to their owner.

## Business Identifiers And Mutability

- `ReviewedBy` references a user `Id`. It is written once by `MarkReviewed` and is immutable afterwards in this change.
- No new unique business identifiers and no new generated ids.

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## Persistence

New columns on `Workouts`:

| Column | Type | Notes |
|---|---|---|
| `ReviewedAtUtc` | timestamp | nullable, UTC |
| `ReviewedBy` | uuid | nullable, FK → `Users.Id` |
| `ResultsUpdatedAtUtc` | timestamp | nullable, UTC |

- `Status` stays an int. `4` is now `COMPLETED` and `5` is `REVIEWED`.
- `xmin` is mapped as a concurrency token in `WeightTrackerDbContext`. It needs no migration.
- `Down()` drops the three columns.
- Write a FluentMigrator migration only, no EF Core migration.

## API

- New route: `PUT /workouts/{workoutId}/review` with body `{ resultsUpdatedAt: string | null }`.
  - Returns `204` on success.
  - Handled by a new `ReviewWorkoutUseCase` that uses `WorkoutStatusAuthorizer` for the permission check.
- `PUT /workouts/{workoutId}/status` accepts `DRAFT`, `PUBLISHED`, `STARTED` and `COMPLETED`, and refuses `REVIEWED`.
- `WorkoutResponse` (by id and list) adds `reviewedAt`, `reviewedBy` and `resultsUpdatedAt`. Status strings now include `COMPLETED` and `REVIEWED`.
- **Breaking:** the status string `FINISHED` becomes `COMPLETED`. The front-end contract (`front/core/openapi/v1.json`, `front/core/src/contract/generated/api.ts`) must be regenerated in the front change.
- Every new exception gets an explicit `ExceptionMapper` entry.

## Out Of Scope

- The trainer's pending-review queue endpoint (completed, prescribed, not yet reviewed). This should be its own change.
- Unlocking a reviewed workout and its audit trail.
- Effort, observations and skipped sets as results. Today results are only set records (reps + weight).
- Front-end and backoffice screens.

## Testing

- **Unit tests:**
  - `WorkoutTests`: every lifecycle move, allowed and refused, including `COMPLETED -> REVIEWED` with and without a trainer.
  - `WorkoutTests`: `MarkReviewed` happy path (status, `ReviewedAt` and `ReviewedBy` set), stale `expectedResultsUpdatedAt`, and null-equals-null.
  - `WorkoutTests`: `ChangeStatus` and `OverrideStatus` refusing `REVIEWED` as source or target.
  - `WorkoutTests`: `PUBLISHED` locked for content; set record changes allowed in `COMPLETED` and refused in `REVIEWED`; each set record method updates `ResultsUpdatedAt`; the new fields survive `Rehydrate`.
  - `WorkoutStatusAuthorizer` tests for every role × move, including review by trainer, admin, athlete and stranger.
  - Edit and recording authorizer tests updated for the new statuses.
  - `WorkoutMapper` round-trip with the new fields.
- **Integration tests:**
  - `PUT /workouts/{id}/review`: `204` for trainer and admin; `401` with no token; `401` with an invalid token; `403` for athlete and stranger; `404` for a missing workout; `409` when not `COMPLETED`, when there is no trainer, and when results are stale; `400` for a malformed body.
  - Existing status, edit and set record route tests updated for the `PUBLISHED` lock, `COMPLETED` corrections and the `REVIEWED` lock.
  - `ExceptionMapperTests` for the new exceptions.
- Run the full `dotnet test WeightTracker.slnx` with Docker.

## Notes

- Update `STPR_INVARIANTS.md`:
  - editing rule: lock is now everything except `DRAFT`, and admins are also locked out of `REVIEWED`;
  - recording rule: `STARTED` or `COMPLETED`;
  - status rule: new lifecycle, roles and review operation;
  - add the concurrency rule for review.
- Keep the curated template copies (`Workout.cs`, `WorkoutTests.cs`, and others) in sync with `src`.
- Rename `FINISHED` everywhere: enum, DTO docs (`ChangeWorkoutStatusRequest`), tests, seed and invariants.
- **Decided:** admins keep their right to edit content in `PUBLISHED`, `STARTED` and `COMPLETED`, as they can today. Only `REVIEWED` locks them out.
- **Decided:** the review uses the dedicated route `PUT /workouts/{workoutId}/review` rather than `PUT /status` with `REVIEWED`, because it needs the `resultsUpdatedAt` concurrency check.
- No other deviations from the mapped template references are allowed.
