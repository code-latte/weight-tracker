# Tasks

## 1. Domain

- [x] 1.1 Add `StartedAt` (`DateTime?`, UTC) to `Workout` before `CompletedAt`, documented like it; add optional `startedAt` to `Rehydrate` after `completedAt` with no status invariant; `Create` and `CreateFromTemplate` start it null. Verify the solution builds.
- [x] 1.2 In `SetStatus`, set `StartedAt = ToStoredPrecision(changedAtUtc)` only when the target is `STARTED`; update the `changedAtUtc` docs on `ChangeStatus` and `OverrideStatus`. Verify the existing `WorkoutTests` still pass.
- [x] 1.3 Add `WorkoutTests`, mirroring the existing `CompletedAt` cases: lifecycle `PUBLISHED → STARTED` sets `StartedAt`; publish, unpublish and complete keep it; `OverrideStatus(STARTED)` sets it; overriding away from `STARTED` keeps it; re-starting overwrites it; refused transitions keep it; set record changes and `MarkReviewed` keep it; `Create`/`CreateFromTemplate` start null; `Rehydrate` keeps it, including null on a `STARTED` workout. Arrange/Act/Assert labelled. Verify with `dotnet test test/WeightTracker.Unit`.
- [x] 1.4 Review: compare `Workout` and `WorkoutTests` changes against the `CompletedAt` handling and `.stpr/template/test/WeightTracker.Unit/Domain/Workouts/WorkoutTests`; copy the changed files into `.stpr/template/` and verify the drift check reports nothing.

## 2. Application

- [x] 2.1 Add `ChangeWorkoutStatusUseCaseTests` asserting the mocked clock time reaches `StartedAt` on the lifecycle path and on the admin path. Verify with `dotnet test test/WeightTracker.Unit`.
- [x] 2.2 Review: confirm `ChangeWorkoutStatusUseCase` needed no change and the new tests match the existing `CompletedAt` cases.

## 3. Persistence

- [x] 3.1 Add migration `AddStartedAtToWorkouts_<timestamp>`: add nullable `timestamptz` `StartedAtUtc` to `Workouts`, then run the two backfill `UPDATE`s from design.md with `Execute.Sql`; `Down()` deletes the column. Shape it like `AddCompletedAtToWorkouts_202611101200` and the `Execute.Sql` use in `AddDurationToSetRecords_202611121200`. Verify the migrator runs up and `--down` against the development database.
- [x] 3.2 Verify the backfill manually on the development database: `COMPLETED`/`REVIEWED` rows with `CompletedAtUtc` have `CompletedAtUtc - StartedAtUtc` between 50 and 75 minutes; `STARTED` rows with `ResultsUpdatedAtUtc` have a 15 to 20 minute gap; all other rows are null; offsets differ across rows; `ResultsUpdatedAtUtc` and `CompletedAtUtc` are unchanged.
- [x] 3.3 Add `WorkoutEntity.StartedAtUtc`, map it in `WeightTrackerDbContext`, round-trip it in `WorkoutMapper`, and copy it onto the tracked entity in `WorkoutRepository`. Verify the build.
- [x] 3.4 Update `WorkoutMapperTests` (round-trip with `StartedAt` set and null) and `WorkoutRepositoryTests` (`StartedAt` persists on update). Verify with `dotnet test`.
- [x] 3.5 Review: compare the migration, entity, mapper and repository changes against the `CompletedAtUtc` handling; confirm no EF Core migration was generated.

## 4. API

- [x] 4.1 Add `startedAt` before `completedAt` in both records of `WorkoutResponse`, `WorkoutByIdResponse` and `WorkoutListItemResponse`, with `<param>` docs stating it may be an estimate for older workouts. Verify the build and that `/openapi/v1.json` exposes `startedAt` (update `OpenApiDocumentTests` if it asserts the schema fields).
- [x] 4.2 Add integration tests in `WorkoutStatusChangeEndpointTests`, mirroring its existing `completedAt` cases: athlete starts → `startedAt` set; admin `DRAFT → STARTED` → set; athlete completes → kept; admin `STARTED → PUBLISHED` → kept; refused change → unchanged; re-start overwrites. Authenticate only through `AuthenticatedClientFor`. The route's auth matrix is unchanged.
- [x] 4.3 Add retrieval assertions: a new workout returns `startedAt` `null`; `GET /workouts/{id}` and `GET /workouts` return `startedAt` for a started workout; recording a set keeps it (closest `SetRecord*EndpointTests`); reviewing keeps it (`WorkoutReviewEndpointTests`).
- [x] 4.4 Review: compare the DTO changes with the existing `CompletedAt` fields and the tests with their closest existing cases.

## 5. Invariants and verification

- [x] 5.1 Update the workout status rule in `.stpr/template/STPR_INVARIANTS.md`: reaching `STARTED` records `StartedAt` with the request time; it is kept when leaving `STARTED`. Verify the text matches the specs.
- [x] 5.2 Run `dotnet build WeightTracker.slnx` (warnings as errors) and the full `dotnet test WeightTracker.slnx` with Docker running; all tests pass.
- [x] 5.3 Run the template drift check from `AGENTS.md`; it reports no `DRIFT` lines.

## Workflow follow-up

- Regenerate the front-end contract (`front/core/openapi/v1.json`, `front/core/src/contract/generated/api.ts`) in a separate front change.
- Archive the change after review.
