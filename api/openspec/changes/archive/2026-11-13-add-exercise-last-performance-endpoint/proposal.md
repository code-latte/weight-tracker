# Proposal

## Why

While recording a set, the client wants to show what the user did the last time they did that exercise ("last time: 10 x 40 kg"). Today the only way is to list every workout and dig through its `setRecords` on the client, which reads the user's whole history to answer one question. A dedicated read, resolved from the token and backed by an index, answers it directly.

Source brief: `.stpr/plan/openspec/proposal-change-add-exercise-last-performance-endpoint.md`.

## What Changes

- New protected route `GET /users/me/exercises/{exerciseId}/last-performance`.
- `200 OK` with the date of the caller's last finished workout that holds set records for the exercise, the effort that workout prescribed for the exercise (same names as the workout responses), and those sets in order. Each set has `reps`, `durationSeconds` and `weight`, all nullable, the same values a recorded set has. A set done without weight returns `weight: null`. A timed set returns `durationSeconds` with `reps: null`.
- `204 No Content` when the caller has never done the exercise in a finished workout.
- `404` for an exercise that does not exist or the caller may not see, and for a malformed id (the route uses the `:guid` constraint like the other routes); `401` without a valid token.
- "Last time" means the caller's own most recent `COMPLETED` or `REVIEWED` workout, by workout date, that holds at least one set record for the exercise. Unfinished workouts (`DRAFT`, `PUBLISHED`, `STARTED`) never count, so the answer is never the session in progress.
- New index on `Workouts (UserId, WorkoutDate DESC, Id DESC)` through a FluentMigrator migration, so the read walks the caller's workouts newest first and stops at the first match. No index on `WorkoutExercises.ExerciseId` for now (see design.md, Risks).
- The route has no `/api` prefix, unlike the request: the API has none, and `GET /users/me` already lives at `/users/me`.
- No change to set record writes, workout responses or any existing route. Not breaking.

## Capabilities

### New Capabilities

- `exercise-last-performance`: the caller's last recorded performance of one exercise: which workout counts as the last one, what is returned, and the empty and error outcomes.

### Modified Capabilities

None. `workout-set-records` keeps its requirements; this capability only reads the set records it defines.

## Canonical Template Mapping

- Use case `GetMyExerciseLastPerformanceUseCase` (`src/WeightTracker.Application/UseCases/Users`) -> `GetExerciseByIdUseCase` (`src/WeightTracker.Application/UseCases/Exercises`) -> nearest read use case that resolves an exercise and enforces `ExerciseVisibility` before answering.
- Read model `ExerciseLastPerformanceView` -> `UserWithTrainerView` (`src/WeightTracker.Application/UseCases/Users`) -> existing Application read model returned by a use case.
- Read projection `IExerciseLastPerformanceQuery` (`src/WeightTracker.Application/Interfaces`) and `ExerciseLastPerformanceQuery` (`src/WeightTracker.Infra/Queries`) -> "Read projections" in `STPR_INVARIANTS.md` -> its own read-only contract over the Workout tables, so `IWorkoutRepository` keeps returning aggregates only.
- Controller action -> `UsersController.GetMe` -> `me` route on the same controller, inherits `AuthorizedController`, every catch goes through `ExceptionMapper.Map`.
- Response DTO -> `SetRecordResponse` (`src/WeightTracker.Api/Endpoints/Workouts/Dtos`) -> same nullable `Reps` / `DurationSeconds` / `Weight` fields and docs style.
- Migration and EF index -> `202611051049_AddMesocycleToWorkouts` and its `HasIndex` in `WeightTrackerDbContext` -> composite index declared in both places.
- Tests -> `test/WeightTracker.Integration/Workouts/SetRecordEndpointTest.cs` (arranging set records through the API) and the use case unit tests under `test/WeightTracker.Unit/Application` -> auth matrix per route, Arrange/Act/Assert.

## Module Classification

- Workouts -> mutable aggregate, read here only; no domain change.
- Exercises -> mutable aggregate (system catalog plus custom exercises), read here only for existence and visibility.
- Users API -> protected `me` routes on `UsersController`.

## Business Identifiers And Mutability

- No business identifier is created or changed. `exerciseId`, workout ids and set record ids are existing `Id` values and immutable. Nothing is generated, so `Id.New()` is not involved.

## Constraints And Deviations

- Only the caller's own workouts are read (`UserId` is the caller), whoever prescribed them. A trainer gets their own history, never an athlete's.
- The read never hydrates the `Workout` aggregate: loading whole workouts to read a few values would defeat the purpose. It is a read projection under `STPR_INVARIANTS.md` ("Read projections"), not a repository method, so "Repository contracts" (domain types only) and "DDD building blocks" (a repository never loads a fragment) are not touched.
- Invariant check: "Repository contracts" and "DDD building blocks" are respected through the read projection; "Persistence and Mapping" allows its `DbContext` access in `Infra/Queries`; "Business authorization rules" do not apply because nothing is mutated. No conflict remains and no deviation from the template references is approved.

## Impact

- **API**: new action on `UsersController`, new response DTOs, OpenAPI documents `200`, `204`, `401` and `404`.
- **Application**: new use case, read model and `IExerciseLastPerformanceQuery`, registration in `Program.cs`. `IWorkoutRepository` does not change.
- **Infra**: `ExerciseLastPerformanceQuery` in the new `Infra/Queries` folder (no-tracking projection), new index in `WeightTrackerDbContext`.
- **Migrations**: new migration creating `IX_Workouts_UserId_WorkoutDate`, with a `Down()` that drops it. No table, column or data change.
- **Tests**: use case unit tests; integration tests for the auth matrix, scoping, the finished-workout rule and the set shapes.
- **Clients**: additive only. No existing response changes.
