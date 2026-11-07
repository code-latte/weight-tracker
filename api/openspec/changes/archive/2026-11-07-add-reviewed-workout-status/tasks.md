# Tasks

## 1. Domain: statuses, locks and lifecycle

- [x] 1.1 Rename `WorkoutStatus.FINISHED` to `COMPLETED` (value `4`) and add `REVIEWED = 5`. Update every reference in `src`, `test` and the seed.
- [x] 1.2 Split the locks in `Workout`: `IsLocked` becomes `Status != DRAFT`; add `IsExecuted` (`STARTED`, `COMPLETED`, `REVIEWED`) and use it in `EnsurePlacementCanChange`.
- [x] 1.3 Restrict `CanTransitionTo` to `DRAFT↔PUBLISHED`, `PUBLISHED→STARTED` and `STARTED→COMPLETED`. Make `OverrideStatus` refuse `REVIEWED` as source or target with `InvalidWorkoutStatusTransitionException`.
- [x] 1.4 Add `ReviewedAt`, `ReviewedBy` and `ResultsUpdatedAt` to `Workout`. Extend `Rehydrate`, and make it reject inconsistent review metadata with `InvalidWorkoutStatusException`. `Create` and `CreateFromTemplate` start the new fields as null.
- [x] 1.5 Add `WorkoutReviewNotAllowedException` and `WorkoutResultsChangedException` to `Domain/Exceptions`.
- [x] 1.6 Add `Workout.MarkReviewed(reviewerId, reviewedAtUtc, expectedResultsUpdatedAt)`. Check `COMPLETED`, then `TrainerId`, then stale results before changing any state.
- [x] 1.7 Allow recording in `STARTED` or `COMPLETED` in `RecordingEntry`, and rename `WorkoutNotStartedException` to `WorkoutResultsLockedException` with an updated message.
- [x] 1.8 Add `updatedAtUtc` to `UpdateSetRecord`, `RemoveSetRecord` and `ReorderSetRecords`. Have every set record method set `ResultsUpdatedAt` (truncated to microseconds) only after the entry operation succeeds. `AddSetRecord` uses `registeredAtUtc`.
- [x] 1.9 Unit tests in `WorkoutTests`:
  - lifecycle moves, allowed and refused, including `REVIEWED` on `ChangeStatus` and `OverrideStatus`;
  - `IsLocked` and `IsExecuted` per status, and placement allowed on `PUBLISHED` but refused on `COMPLETED` and `REVIEWED`;
  - `MarkReviewed`: happy path, not `COMPLETED`, no trainer, stale and null-equals-null results, and nothing changing on failure;
  - recording allowed on `COMPLETED` and refused on `REVIEWED`;
  - `ResultsUpdatedAt` set by each set record method and untouched on failure;
  - `Rehydrate` round trip, and rejection of inconsistent metadata.
- [x] 1.10 Review the domain changes against `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs` and the invariants (action semantics guarded, inputs validated before mutation).

## 2. Application: permissions and use cases

- [x] 2.1 Update `WorkoutEditAuthorizer` so its lock check is (non-admin and `IsLocked`) or `Status == REVIEWED`, still after the permission check. Update its remarks.
- [x] 2.2 Add `EnsureCanReview` to `WorkoutStatusAuthorizer` (admin or prescribing trainer, otherwise `UnauthorizedOperationException`). Update `IsForward` to `STARTED→COMPLETED` and the remarks.
- [x] 2.3 Add `WorkoutConcurrencyConflictException` to `Application/Exceptions`.
- [x] 2.4 Add `ReviewWorkoutUseCase`, mirroring `ChangeWorkoutStatusUseCase` and using `IClock` for `reviewedAtUtc`.
- [x] 2.5 Pass `_clock.UtcNow` to the update, remove and reorder set record calls in their use cases (inject `IClock` where it is missing).
- [x] 2.6 Unit tests:
  - `WorkoutEditAuthorizer`: `PUBLISHED` locked for non-admins, admin allowed on `PUBLISHED` and `COMPLETED` but refused on `REVIEWED`, and 403 before 409;
  - `WorkoutStatusAuthorizer.EnsureCanReview` for each role (admin, prescribing trainer, athlete, unrelated trainer, stranger);
  - `ReviewWorkoutUseCaseTests`: not found, forbidden, the domain refusals, and success saving and committing once;
  - set record use case tests updated for the time parameter.
- [x] 2.7 Review the use case and authorizers against `ChangeWorkoutStatusUseCase` / `WorkoutStatusAuthorizer` and their tests.

## 3. Infrastructure and migration

- [x] 3.1 Add the migration `<yyyyMMddHHmm>_AddReviewToWorkouts`:
  - columns `ReviewedAtUtc` (timestamp, nullable), `ReviewedBy` (uuid, nullable, FK → `Users.Id`) and `ResultsUpdatedAtUtc` (timestamp, nullable), using the timestamp type of `CreateSetRecordsTable`;
  - a `Down()` that drops all three.
- [x] 3.2 Add the three properties to `WorkoutEntity` and configure them in `WeightTrackerDbContext` with matching column names. Map `xmin` as the `uint Version` row version property of `WorkoutEntity`.
- [x] 3.3 Map the new fields both ways in `WorkoutMapper`, and copy them onto the tracked row in `WorkoutRepository.Save`.
- [x] 3.4 Have `WorkoutRepository` remember the `xmin` loaded by `GetById` per workout id, and set it as the tracked entry's original `xmin` in `Save`.
- [x] 3.5 Have `UnitOfWork.SaveChangesAsync` translate `DbUpdateConcurrencyException` into `WorkoutConcurrencyConflictException`.
- [x] 3.6 Tests:
  - `WorkoutMapperTests` round trip with review metadata and `ResultsUpdatedAt`;
  - `WorkoutRepositoryTests`: the new columns persist;
  - two scopes load the same workout, both save, and the second commit throws `WorkoutConcurrencyConflictException`, both for two set record changes and for a review racing a set record correction.
- [x] 3.7 Review the persistence changes against `WorkoutMapper`, `WorkoutRepository` and the `CreateSetRecordsTable` migration (EF configuration matches the migration, mapping stays inside the repository).

## 4. API

- [x] 4.1 Add `ReviewWorkoutRequest { DateTime? ResultsUpdatedAt }` and `PUT /workouts/{workoutId}/review` in `WorkoutsController`, returning 200 with `WorkoutResponse`. The catch delegates to `ExceptionMapper.Map`.
- [x] 4.2 Add `ReviewedAt`, `ReviewedBy` and `ResultsUpdatedAt` to `WorkoutResponse`, `WorkoutDetailResponse`, `WorkoutByIdResponse` and `WorkoutListItemResponse`, and update the `ChangeWorkoutStatusRequest` docs (five statuses, `REVIEWED` only through the review route).
- [x] 4.3 Map `WorkoutReviewNotAllowedException`, `WorkoutResultsChangedException`, `WorkoutResultsLockedException` and `WorkoutConcurrencyConflictException` to 409 in `ExceptionMapper`. Remove the old `WorkoutNotStartedException` entry.
- [x] 4.4 Register `ReviewWorkoutUseCase` in `Program.cs`.
- [x] 4.5 Review the controller, DTOs and mapping against the `PUT /workouts/{workoutId}/status` endpoint.

## 5. Integration tests

- [x] 5.1 Add `WorkoutReviewEndpointTests`, modelled on `WorkoutStatusChangeEndpointTests`. Reach `COMPLETED` through the status route as the athlete. Cover:
  - 200 for the trainer and for an admin, with `reviewedAt`, `reviewedBy` and `status` checked through a later `GET`;
  - 401 without a token and with an invalid token;
  - 403 for the athlete, an unrelated trainer and a stranger (including a stranger on a `STARTED` workout);
  - 404 for an unknown workout;
  - 409 for not `COMPLETED`, already `REVIEWED`, no trainer, and stale `resultsUpdatedAt` (athlete corrects a set record after the read);
  - 400 for a malformed timestamp.
- [x] 5.2 In `WorkoutStatusChangeEndpointTests`, rename `FINISHED` to `COMPLETED` and add `REVIEWED` through the status route: 409 for the trainer, the athlete and an admin, and 409 for an admin leaving `REVIEWED`.
- [x] 5.3 Update the edit endpoint tests (exercise add, update, remove and reorder, superseries, workout update): 409 for the trainer on `PUBLISHED`, editing allowed after unpublishing, admin allowed on `PUBLISHED` and `COMPLETED`, admin 409 on `REVIEWED`.
- [x] 5.4 Update the set record endpoint tests: corrections allowed on `COMPLETED`, 409 on `REVIEWED` for the athlete and an admin, and `resultsUpdatedAt` exposed and moving after each change.
- [x] 5.5 Update `WorkoutMesocyclePlacementEndpointTests` and `WorkoutGetByIdEndpointTests` / `WorkoutResponseShapeTests` for `COMPLETED` / `REVIEWED`, and for the new response fields (null on a new workout) on `GET /workouts/{id}` and on `GET /workouts` (`WorkoutScopingTests`).
- [x] 5.6 Add `ExceptionMapperTests` cases for the four 409 mappings.

## 6. Invariants, template and verification

- [x] 6.1 Rewrite the editing, recording and status rules in `.stpr/template/STPR_INVARIANTS.md`, and add the review rule (permissions, metadata, stale-results check, concurrency).
- [x] 6.2 Update the AGENTS.md quality checklist line about the workout edit rule if its wording no longer matches.
- [x] 6.3 Refresh the curated copies under `.stpr/template/src` and `.stpr/template/test` that this change touched, and run the drift check from AGENTS.md until it prints nothing.
- [x] 6.4 Run `dotnet build WeightTracker.slnx` (warnings are errors), then the full `dotnet test WeightTracker.slnx` with Docker running. Fix any failure before closing.
