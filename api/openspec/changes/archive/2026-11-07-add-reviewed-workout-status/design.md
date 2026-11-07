# Design

## Context

See proposal.md for the motivation and the brief. The current code that shapes the approach:

- `Workout.IsLocked` (`STARTED` or `FINISHED`) drives two different rules:
  - the edit lock in `WorkoutEditAuthorizer`;
  - the placement lock in `Workout.EnsurePlacementCanChange`.
- `Workout.CanTransitionTo` is the lifecycle used both by `ChangeStatus` and by `WorkoutStatusAuthorizer`, which returns "allowed" for moves outside the lifecycle so the aggregate can answer 409.
- Recording is gated by `Workout.RecordingEntry` (`Status == STARTED`, `WorkoutNotStartedException`). Who may record is decided by `WorkoutRecordingAuthorizer`. `AddSetRecord` already receives the time from `IClock`; update, remove and reorder do not take a time.
- `WorkoutRepository.GetById` loads with `AsNoTracking`. `Save` re-reads the tracked row and copies scalar fields onto it. There is no concurrency token anywhere in the repo.
- `WorkoutStatus` is stored as its integer value, and `FINISHED = 4`.

## Goals / Non-Goals

**Goals:**
- Keep the domain free of persistence concerns, so no version number goes on `Workout`.
- Make the two meanings of "locked" (prescription edit vs. placement) separate and explicit.
- Keep every permission rule in its existing Application service. No rule is restated in use cases.

**Non-Goals:**
- No generic auditing or history of status changes.
- No change to read visibility (`IsHiddenFrom`).

## Decisions

### Domain

- **Status enum.**
  - Rename `FINISHED` to `COMPLETED` and keep the value `4`.
  - Add `REVIEWED = 5`.
  - No data migration for the rename.
- **Two locks.**
  - `IsLocked` becomes the prescription lock: `Status != DRAFT`.
  - A new `IsExecuted` (`STARTED`, `COMPLETED`, `REVIEWED`) replaces `IsLocked` in `EnsurePlacementCanChange`.
  - *Alternative:* a single `IsLocked = Status != DRAFT` for both. Rejected, because it would block admins from moving `PUBLISHED` workouts between mesocycle weeks, a behavior change nobody asked for.
- **Lifecycle.**
  - `CanTransitionTo` keeps only the status-route moves: `DRAFT↔PUBLISHED`, `PUBLISHED→STARTED`, `STARTED→COMPLETED`. `COMPLETED→REVIEWED` is not in it.
  - `ChangeStatus(REVIEWED)` and every move out of `REVIEWED` therefore fall through to `InvalidWorkoutStatusTransitionException` (409).
  - `OverrideStatus` refuses `REVIEWED` as source or target with the same exception.
  - *Alternative:* add `COMPLETED→REVIEWED` to `CanTransitionTo`. Rejected because:
    - `WorkoutStatusAuthorizer` would then judge it as a role question (403 for the trainer on the status route instead of 409);
    - the review metadata could be skipped.
- **`Workout.MarkReviewed(Id reviewerId, DateTime reviewedAtUtc, DateTime? expectedResultsUpdatedAt)`.** It checks, in this order:
  1. `Status == COMPLETED`, otherwise new `WorkoutReviewNotAllowedException` (409);
  2. `TrainerId is not null`, otherwise the same exception;
  3. `expectedResultsUpdatedAt == ResultsUpdatedAt`, otherwise new `WorkoutResultsChangedException` (409).

  Only then does it set `Status`, `ReviewedAt` and `ReviewedBy`. Every input is validated before any state changes.
- **Results timestamp.**
  - `UpdateSetRecord`, `RemoveSetRecord` and `ReorderSetRecords` gain a `DateTime updatedAtUtc` parameter.
  - `AddSetRecord` uses its `registeredAtUtc`.
  - `ResultsUpdatedAt` is set only after the entry-level operation succeeds, so a refused change leaves it untouched.
  - The value is truncated to microseconds when assigned, so the in-memory value equals the value Postgres returns. The review's equality check depends on this.
- **Recording statuses.**
  - `RecordingEntry` allows `STARTED` or `COMPLETED`.
  - `WorkoutNotStartedException` is renamed to `WorkoutResultsLockedException`. It keeps 409 and its message names both statuses.
- **Rehydrate.** `Workout.Rehydrate` takes `reviewedAt`, `reviewedBy` and `resultsUpdatedAt`. It refuses a `REVIEWED` workout without review metadata, and review metadata on a non-`REVIEWED` workout, with `InvalidWorkoutStatusException`, so a stored inconsistency surfaces instead of being carried.

### Application

- **Edit lock.** `WorkoutEditAuthorizer` keeps its order: permission (403), then lock (409). The lock check becomes:
  - non-admin and `IsLocked`, or
  - any caller and `Status == REVIEWED`.

  In both cases it throws `WorkoutLockedException`. The admin's `REVIEWED` lock lives here, next to the existing lock, rather than in each aggregate content method.
- **Review permission.**
  - `WorkoutStatusAuthorizer` gains `EnsureCanReview(workout, callerId, callerRole)`: admin, or `workout.TrainerId == callerId`, otherwise `UnauthorizedOperationException` (403).
  - It goes in the same service because a review is a status change with its own rule.
  - *Alternative:* a new `WorkoutReviewAuthorizer`. Rejected as one more service for a three-line rule.
- **`ReviewWorkoutUseCase`.** It mirrors `ChangeWorkoutStatusUseCase`:
  1. load the workout, or `WorkoutNotFoundException` (404);
  2. `EnsureCanReview`;
  3. `workout.MarkReviewed(callerId, _clock.UtcNow, expectedResultsUpdatedAt)`;
  4. save and commit;
  5. return the workout.
- **Set record use cases** pass `_clock.UtcNow` to the update, remove and reorder methods. `WorkoutRecordingAuthorizer` does not change.
- **Concurrency exception.** New `WorkoutConcurrencyConflictException` in `Application/Exceptions` (409).

### Infrastructure

- **Columns.** `WorkoutEntity` gains `ReviewedAtUtc`, `ReviewedBy` and `ResultsUpdatedAtUtc`. `WorkoutMapper` maps them both ways, and `Save` copies them onto the tracked row.
- **Optimistic concurrency.**
  - Map Postgres `xmin` as a row version on `WorkoutEntity`: a `uint Version` property with column `xmin`, type `xid`, configured with `IsRowVersion()`. It lives only on the EF entity; the mapper never copies it into the domain.
  - `Save` re-reads the tracked row, so on its own the token would only cover the gap between that re-read and the commit. To cover the gap since `GetById`:
    - the scoped `WorkoutRepository` remembers the `xmin` it loaded per workout id;
    - in `Save` it sets the tracked entry's `xmin` original value to that remembered value.
  - Every set record change now also updates `ResultsUpdatedAtUtc`, so the parent row is always written and its `xmin` always checked.
  - *Alternative:* a `Version` property on the `Workout` aggregate. Rejected because it leaks persistence into the domain.
  - *Alternative:* a shadow property on the entity. Rejected because the repository reads the loaded version from the entity, which is simpler with a real property and still stays inside Infra.
  - *Alternative:* a conditional `UPDATE … WHERE ResultsUpdatedAtUtc = @expected`. Rejected because it only protects the review, not concurrent set record writes.
- **Translating the failure.** `UnitOfWork.SaveChangesAsync` catches `DbUpdateConcurrencyException` and throws `WorkoutConcurrencyConflictException`. Today only workouts carry a token. If other aggregates get one later, the exception can be generalised then.
- **Migration.** `<yyyyMMddHHmm>_AddReviewToWorkouts` adds:
  - `ReviewedAtUtc` (timestamp, nullable);
  - `ReviewedBy` (uuid, nullable, FK → `Users.Id`);
  - `ResultsUpdatedAtUtc` (timestamp, nullable).

  It uses the same timestamp type as `CreateSetRecordsTable`, and `Down()` drops the three columns. `xmin` is a system column and needs nothing. No EF Core migration.

### API

- **Review route.**
  - `PUT /workouts/{workoutId}/review` in `WorkoutsController`, with body `ReviewWorkoutRequest { DateTime? ResultsUpdatedAt }`.
  - The property is nullable and not `[Required]`, because `null` is a meaningful value.
  - A missing body or an unparseable timestamp is a 400 from model binding.
  - It returns 200 with `WorkoutResponse`, like `PUT /status`. The catch delegates to `ExceptionMapper.Map`.
- **Responses.** Every workout response adds `ReviewedAt`, `ReviewedBy` and `ResultsUpdatedAt`: `WorkoutResponse` and `WorkoutDetailResponse` (create, update, status, review and set record routes), `WorkoutByIdResponse` (`GET /workouts/{id}`) and `WorkoutListItemResponse` (`GET /workouts`).
- **Status request docs.** `ChangeWorkoutStatusRequest` docs list the five statuses (numbers 1 to 5) and note that `REVIEWED` is reached through the review route.
- **ExceptionMapper.** It adds 409 for `WorkoutReviewNotAllowedException`, `WorkoutResultsChangedException`, `WorkoutResultsLockedException` (renamed) and `WorkoutConcurrencyConflictException`.

### Invariants and template

- Rewrite the editing, recording and status rules in `STPR_INVARIANTS.md`, and add a short review rule (who may review, metadata, stale-results check, concurrency).
- Update the curated copies (`Workout.cs`, `WorkoutTests.cs` and any other touched file under `.stpr/template`) so the drift check stays clean.

## Risks / Trade-offs

- **Breaking clients.** [Risk] Existing clients send or compare `FINISHED`. → Called out as **BREAKING**; the front change regenerates the contract. The numeric value `4` still works on the status route.
- **More 409s.** [Risk] The concurrency token now applies to every workout save, so two quick edits from different tabs can 409 where they used to be last-write-wins. → This is the intended protection. Integration tests run serially, so they are unaffected.
- **Timestamp equality.** [Risk] Exact equality on `resultsUpdatedAt` breaks if precision or `DateTimeKind` differ between memory, the database and JSON. → Truncate to microseconds in the domain, keep UTC throughout, and add a round-trip test that reads `resultsUpdatedAt` over HTTP and reviews with it.
- **Published workouts are no longer editable.** [Risk] Trainers who edited published workouts now get 409. → Documented as a behavior change. Unpublish, edit and publish again is a status-route sequence that already exists.
- **Admins cannot reopen reviewed workouts.** [Trade-off] An admin can no longer fix a mistaken review. → Deliberate until the audited unlock operation exists (out of scope).

## Migration Plan

1. Deploy the migration (additive, nullable columns). Existing rows stay valid: no row is `REVIEWED` and all new columns are `null`.
2. Deploy the API. Status `4` is read as `COMPLETED`.
3. Roll back with the migration's `Down()` (drops the columns) and the previous API build. No row can be `REVIEWED` until the new API runs. If any exists at rollback, set it back to `4` first.
