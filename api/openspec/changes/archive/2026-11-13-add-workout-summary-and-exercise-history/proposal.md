# Proposal

## Why

The app and the backoffice want to show how a session went and how an exercise progresses. Today a client would have to fetch whole workouts and recompute the stats itself, with rules (what "in target" means, what volume is, which set is the top one, how to estimate a one-rep max) that would drift between clients. Two read endpoints compute them once, on the server, with the rules kept in the domain.

Source brief: `.stpr/plan/openspec/proposal-change-add-workout-summary-and-exercise-history.md`.

## What Changes

- New protected route `GET /workouts/{workoutId}/summary`, `200 OK` with:
  - for the workout: `setsInTarget` (`inTarget` over `evaluated`) and `volume`;
  - per exercise, in workout order: `setsInTarget`, `volume`, `topSet` (the top set in this workout) and `previous` (workout id, workout date and top set of the previous execution of that exercise), each `null` when not applicable.
- New protected route `GET /users/{userId}/exercises/{exerciseId}/history?limit=`, `200 OK` with the last `limit` sessions in which the user did the exercise, newest first, each with `workoutId`, `workoutDate`, `maxWeight`, `volume` and `estimatedOneRepMax`. An empty list when there are none.
- New domain rules in `Domain/Workouts`: whether a set result meets its target, the volume of a set (reps × kg), the Epley estimated one-rep max of a set (`weight × (1 + reps / 30)`), and the top-set ordering.
- Two new read projections over the Workout tables, one for previous executions and one for the exercise history, so neither read hydrates `Workout` aggregates of the user's history.
- `400` for a `limit` outside `1..50`, through a new Application exception mapped in `ExceptionMapper`.
- New index on `WorkoutExercises (ExerciseId, WorkoutId)` through a FluentMigrator migration, so the reads stay fast for an exercise that is rare or never done instead of seq-scanning the table (see design.md, Risks). The reads otherwise use the existing `IX_Workouts_UserId_WorkoutDate_Id`, `WorkoutExercises (WorkoutId, ExerciseId)` and `SetRecords (WorkoutExerciseId)` indexes. It also benefits `exercise-last-performance`, whose behaviour does not change.
- No change to any existing route, response or write. Not breaking.

## Capabilities

### New Capabilities

- `workout-summary`: the stats of one workout: sets in target, volume, top set per exercise and the top set of each exercise's previous execution, with its visibility rules.
- `exercise-history`: a user's per-session history of one exercise: which sessions count, their order and limit, max weight, volume and estimated one-rep max, and who may read it.

### Modified Capabilities

None. `workout-details`, `workout-set-records` and `exercise-last-performance` keep their requirements; the new capabilities only read the data they define.

## Canonical Template Mapping

- `GetWorkoutSummaryUseCase` (`src/WeightTracker.Application/UseCases/Workouts`) -> `GetWorkoutByIdUseCase` -> same load by id, `WorkoutNotFoundException`, and `WorkoutVisibility.CanSee` gate before anything else is resolved.
- `GetUserExerciseHistoryUseCase` (`src/WeightTracker.Application/UseCases/Users`) -> `GetMyExerciseLastPerformanceUseCase` (exercise lookup, `ExerciseVisibility`, read projection) and `GetUserMesocyclesUseCase` (caller versus `{userId}` authorization, `UserNotFoundException`).
- Read projections `IExercisePreviousExecutionQuery` and `IExerciseHistoryQuery` (`src/WeightTracker.Application/Interfaces`), implemented in `src/WeightTracker.Infra/Queries` -> `IExerciseLastPerformanceQuery` / `ExerciseLastPerformanceQuery` and "Read projections" in `STPR_INVARIANTS.md` -> same finished-workout selection, index-driven, no-tracking, Application view records.
- Read models (`WorkoutSummaryView`, `ExerciseHistorySessionView` and the projection views) -> `ExerciseLastPerformanceView` -> Application view records next to the use case that consumes them.
- Domain rules (`SetTarget.IsMetBy`, set volume, estimated one-rep max, top-set comparer) -> `SetTarget` / `SetResult` / `SetRecord` in `src/WeightTracker.Domain/Workouts` -> behaviour lives with the value objects it reads.
- `InvalidHistoryLimitException` -> existing Application exceptions mapped to `400` in `ExceptionMapper`.
- Migration and EF index -> `202611131200_AddUserWorkoutDateIndexToWorkouts` and its `HasIndex` in `WeightTrackerDbContext` -> composite index declared in both places.
- Controller actions -> `WorkoutsController.GetById` and `UsersController.GetMyExerciseLastPerformance` -> `AuthorizedController`, `CurrentUser`, every catch through `ExceptionMapper.Map`, `ProducesResponseType` per status.
- Response DTOs -> `ExerciseLastPerformanceResponse` and `SetRecordResponse` -> same nullable `Reps` / `DurationSeconds` / `Weight` fields and docs style.
- Tests -> `WorkoutGetByIdEndpointTests`, `ExerciseLastPerformanceEndpointTests`, `ExerciseLastPerformanceQueryTests`, `SetTargetTests` and the use case unit tests under `test/WeightTracker.Unit/Application` -> auth matrix per route, Arrange/Act/Assert.

## Module Classification

- Workouts -> mutable aggregate, read here only; new pure calculation rules in its domain folder, no state or action-method change.
- Exercises -> mutable aggregate (system catalog plus custom exercises), read here only for existence, name and visibility.
- Users -> mutable aggregate, read here only for the existence of `{userId}`.
- Workouts API and Users API -> protected routes on existing controllers.

## Business Identifiers And Mutability

- No business identifier is created or changed. `workoutId`, `userId`, `exerciseId` and the returned workout ids are existing `Id` values and immutable. Nothing is generated, so `Id.New()` is not involved.

## Constraints And Deviations

- Previous executions and history are read through read projections, not `IWorkoutRepository`, because hydrating every workout of a user's history to read a few set values would defeat the purpose. The summarised workout itself is loaded as an aggregate through the repository.
- Projections return raw set values; the use cases turn them into `SetResult` / `Weight` and apply the domain rules, so volume, top set and estimated one-rep max are computed in one place. Aggregating in SQL is rejected because it would duplicate those rules.
- Authorization stays in the use cases, which scope the projections by owner and, for a trainer reading someone else's workouts, by prescribing trainer.
- A malformed `workoutId`, `userId` or `exerciseId` does not match the `:guid` route constraint and answers `404`, as in every other route.
- Invariant check: "Read projections", "Repository contracts" and "Persistence and Mapping" are respected; "Business authorization rules" for mutations do not apply because nothing is mutated. No conflict remains and no deviation from the template references is approved.

## Impact

- **API**: new action on `WorkoutsController` and on `UsersController`, new response DTOs, OpenAPI documents `200`, `400`, `401`, `403`, `404` as they apply.
- **Domain**: new rules on `SetTarget` and a small set-metrics helper plus top-set comparer in `Domain/Workouts`, with unit tests.
- **Application**: two use cases, two projection contracts, views, `InvalidHistoryLimitException`, registrations in `Program.cs`.
- **Infra**: two projections in `Infra/Queries`, and the new index declared in `WeightTrackerDbContext`. No change to repositories.
- **Migrations**: one new migration creating `IX_WorkoutExercises_ExerciseId_WorkoutId`, with a `Down()` that drops it. No table, column or data change.
- **Tests**: domain and use case unit tests; projection integration tests; endpoint integration tests for the auth matrix, scoping and computed values.
- **Clients**: additive only.
