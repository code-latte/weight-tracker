# Proposal

## Why

A prescribing trainer has no way to say "I have looked at what the athlete did", and once a workout is finished nothing protects its results. We need a final `REVIEWED` status, set by the trainer (or an admin) on completed workouts, that locks the prescription and the results and records who reviewed it and when. At the same time `FINISHED` is renamed to `COMPLETED` and the lifecycle is tightened: a published workout must go back to draft before its prescription changes, and the athlete may correct results after completing the workout.

Source brief: `.stpr/plan/openspec/proposal-change-add-reviewed-workout-status.md`.

## What Changes

- **BREAKING:** the status `FINISHED` is renamed to `COMPLETED` in the domain, the API (request and response strings) and the specs. The stored value stays `4`, so stored data does not change.
- New status `REVIEWED` (stored value `5`). It is terminal, and only a `COMPLETED` workout with a prescribing trainer can reach it.
- New route `PUT /workouts/{workoutId}/review` with body `{ "resultsUpdatedAt": <UTC timestamp or null> }`:
  - allowed to the prescribing trainer and to admins;
  - records `reviewedAt` and `reviewedBy`;
  - refused with `409` when the results changed after the time the reviewer sent.
  - Responds `200` with the whole workout.
- `PUT /workouts/{workoutId}/status` never reaches `REVIEWED` and never leaves it, also for admins (`409`).
- Workouts track `resultsUpdatedAt`. Every set record add, edit, remove or reorder updates it.
- **Behavior change:** a `PUBLISHED` workout is locked against content edits for non-admins. The trainer must unpublish it first.
- **Behavior change:** set records may be added, edited, removed or reordered while the workout is `STARTED` or `COMPLETED`, no longer only `STARTED`.
- A `REVIEWED` workout refuses every content edit and every set record change, for every caller including admins.
- Workout responses (by id and in lists) carry `reviewedAt`, `reviewedBy` and `resultsUpdatedAt`.
- Saving a workout is protected by an optimistic concurrency token. A concurrent write surfaces as `409`.

## Capabilities

### New Capabilities

- `workout-review`: marking a completed, prescribed workout as reviewed:
  - the review route, its permissions and review metadata;
  - the stale-results check and concurrency protection;
  - the `REVIEWED` lock on content and results.

### Modified Capabilities

- `workout-status`: status set renamed and extended (`COMPLETED`, `REVIEWED`). The lifecycle adds `COMPLETED → REVIEWED`, reachable only through the review route. The admin override cannot set or leave `REVIEWED`.
- `workout-editing`: the edit lock now covers every status except `DRAFT` for non-admins, so `PUBLISHED` is no longer editable. Admins keep editing in every status except `REVIEWED`.
- `workout-set-records`: recording is allowed in `STARTED` and `COMPLETED` and refused in `REVIEWED`. Every change updates `resultsUpdatedAt`.
- `workout-mesocycle-placement`: the placement lock covers `STARTED`, `COMPLETED` and `REVIEWED` (rename plus the new status). `PUBLISHED` stays movable by admins.
- `workout-retrieval`: responses carry `reviewedAt`, `reviewedBy` and `resultsUpdatedAt`, and status strings use `COMPLETED` / `REVIEWED`.

### Canonical template references

- `workout-review` → `ChangeWorkoutStatusUseCase` + `WorkoutStatusAuthorizer` + `PUT /workouts/{workoutId}/status` in `WorkoutsController` (nearest status-changing flow).
- `workout-status` → `src/WeightTracker.Domain/Workouts/Workout.cs` (`ChangeStatus`, `CanTransitionTo`, `OverrideStatus`) and `WorkoutStatus.cs`.
- `workout-editing` → `WorkoutEditAuthorizer`.
- `workout-set-records` → `Workout.RecordingEntry` and the set record methods, plus `WorkoutRecordingAuthorizer`.
- `workout-mesocycle-placement` → `Workout.EnsurePlacementCanChange`.
- `workout-retrieval` → `WorkoutResponse` and `WorkoutMapper`.
- Migration → `202611071200_CreateSetRecordsTable` (naming and timestamp convention).

Workouts is a mutable aggregate, not a catalog.

### Business identifiers

- No new unique business identifiers and no new generated ids.
- `reviewedBy` references a user id. It is written once by the review and is immutable in this change.

## Impact

- **Domain:**
  - `WorkoutStatus`, `Workout` (new fields, `MarkReviewed`, split edit and placement locks, recording statuses, `resultsUpdatedAt` on set record methods);
  - new and renamed domain exceptions.
- **Application:**
  - `WorkoutStatusAuthorizer`, `WorkoutEditAuthorizer`;
  - new `ReviewWorkoutUseCase`;
  - set record use cases (pass the clock to the update, remove and reorder methods);
  - a new concurrency exception.
- **Infra:**
  - `WorkoutEntity`, `WorkoutMapper`, `WeightTrackerDbContext` (new columns, `xmin` concurrency token);
  - `UnitOfWork` (translates concurrency failures).
- **Migrations:** one FluentMigrator migration adding `ReviewedAtUtc`, `ReviewedBy` and `ResultsUpdatedAtUtc` to `Workouts`.
- **API:** `WorkoutsController` (review route), `ReviewWorkoutRequest`, `WorkoutResponse`, `ChangeWorkoutStatusRequest` docs, `ExceptionMapper`, `Program.cs` registration.
- **Docs:** `.stpr/template/STPR_INVARIANTS.md` (edit, recording and status rules, and the review rule) and the curated template copies.
- **Front-end:** the generated contract must be regenerated for `FINISHED` → `COMPLETED` and the new fields. That belongs to a separate front change.

### Deviations from the brief and conflicts with the template

- **Review response:** the brief says `204`. This proposal returns `200` with the whole workout, for parity with `PUT /workouts/{id}/status` and the set record routes.
- **Placement lock:** the brief makes `IsLocked` mean "not `DRAFT`". That flag also drives the mesocycle placement lock, so placement would become locked for `PUBLISHED` workouts, admins included. This proposal keeps two locks:
  - the edit lock (everything except `DRAFT`);
  - the placement lock (`STARTED`, `COMPLETED`, `REVIEWED`).
- **Invariants:** `STPR_INVARIANTS.md` currently states that a `PUBLISHED` workout is editable and that admins bypass every lock. Both statements change and are rewritten as part of this change.

### Out of scope

- The trainer's pending-review queue endpoint.
- Unlocking a reviewed workout and auditing it.
- Effort, observations and skipped sets as results.
- Front-end work.
