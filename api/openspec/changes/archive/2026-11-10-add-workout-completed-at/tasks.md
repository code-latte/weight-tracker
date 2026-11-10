# Tasks

## 1. Domain

- [x] 1.1 Add `CompletedAt` (`DateTime?`, UTC) to `Workout`, documented like `ReviewedAt`; add `completedAt` to `Rehydrate` with no status invariant; `Create` and `CreateFromTemplate` start it null. Verify the solution builds.
- [x] 1.2 Add a required `DateTime changedAtUtc` parameter to `ChangeStatus` and `OverrideStatus`; set `CompletedAt = ToStoredPrecision(changedAtUtc)` only when the target is `COMPLETED` and the checks passed. Update existing callers in tests. Verify `WorkoutTests` compile and pass.
- [x] 1.3 Add `WorkoutTests`: lifecycle `STARTED → COMPLETED` sets `CompletedAt`; other lifecycle moves keep it; `OverrideStatus(COMPLETED)` sets it; overriding away from `COMPLETED` keeps it; re-completing overwrites it; refused transitions keep it; `MarkReviewed` keeps it; `Create`/`CreateFromTemplate` start null; `Rehydrate` keeps it, including null on a `COMPLETED` workout. Arrange/Act/Assert labelled. Verify with `dotnet test test/WeightTracker.Unit`.
- [x] 1.4 Review: compare `Workout` and `WorkoutTests` changes against the `ReviewedAt`/`MarkReviewed` pattern and `.stpr/template/test/WeightTracker.Unit/Domain/Workouts/WorkoutTests`; copy the changed files into `.stpr/template/` and verify the drift check reports nothing.

## 2. Application

- [x] 2.1 Inject `IClock` into `ChangeWorkoutStatusUseCase` and pass `_clock.UtcNow` to `OverrideStatus` and `ChangeStatus`. Verify the API project builds and DI resolves (`IClock` is already registered in `Program.cs`).
- [x] 2.2 Update `ChangeWorkoutStatusUseCaseTests` with a mocked `IClock`: the clock time reaches `CompletedAt` on the lifecycle path and on the admin path. Verify with `dotnet test test/WeightTracker.Unit`.
- [x] 2.3 Review: compare `ChangeWorkoutStatusUseCase` against `ReviewWorkoutUseCase` for clock usage and constructor shape.

## 3. Persistence

- [x] 3.1 Add migration `AddCompletedAtToWorkouts_<timestamp>` adding nullable `timestamptz` `CompletedAtUtc` to `Workouts`, `Down()` deleting it, shaped like `AddReviewToWorkouts_202611071300`. Verify the migrator runs up and `--down` against the development database.
- [x] 3.2 Add `WorkoutEntity.CompletedAtUtc`, map it in `WeightTrackerDbContext`, round-trip it in `WorkoutMapper`, and copy it onto the tracked entity in `WorkoutRepository`. Verify the build.
- [x] 3.3 Update `WorkoutMapperTests` (round-trip with `CompletedAt` set and null) and `WorkoutRepositoryTests` (`CompletedAt` persists on update). Verify with `dotnet test`.
- [x] 3.4 Review: compare the migration, entity, mapper and repository changes against the `ReviewedAtUtc` handling; confirm no EF Core migration was generated.

## 4. API

- [x] 4.1 Add `completedAt` before `reviewedAt` in `WorkoutResponse`, `WorkoutByIdResponse` and `WorkoutListItemResponse`, with `<param>` docs. Verify the build and that `/openapi/v1.json` exposes `completedAt` (update `OpenApiDocumentTests` if it asserts the schema fields).
- [x] 4.2 Add integration tests in `WorkoutStatusChangeEndpointTests`, mirroring its existing structure: athlete completes → `completedAt` set; admin `DRAFT → COMPLETED` → set; admin `COMPLETED → DRAFT` → kept; refused change → unchanged; re-completion overwrites. Authenticate only through `AuthenticatedClientFor`. The route's auth matrix is unchanged.
- [x] 4.3 Add retrieval assertions: a new workout returns `completedAt` `null`; `GET /workouts/{id}` and `GET /workouts` return `completedAt` for a completed workout; reviewing keeps it (`WorkoutReviewEndpointTests`).
- [x] 4.4 Review: compare the DTO changes with the existing `ReviewedAt` fields and the tests with their closest existing cases.

## 5. Invariants and verification

- [x] 5.1 Update the workout status rule in `.stpr/template/STPR_INVARIANTS.md`: reaching `COMPLETED` records `CompletedAt` with the request time; it is kept when leaving `COMPLETED`. Verify the text matches the specs.
- [x] 5.2 Run `dotnet build WeightTracker.slnx` (warnings as errors) and the full `dotnet test WeightTracker.slnx` with Docker running; all tests pass.
- [x] 5.3 Run the template drift check from `AGENTS.md`; it reports no `DRIFT` lines.

## Workflow follow-up

- Regenerate the front-end contract (`front/core/openapi/v1.json`, `front/core/src/contract/generated/api.ts`) in a separate front change.
- Archive the change after review.
