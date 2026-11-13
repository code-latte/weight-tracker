## Change

Add `GET /users/me/exercises/{exerciseId}/last-performance`: the sets the caller recorded the last time they did an exercise.

## Objective

While recording a set, the client wants to show what the user did the last time ("last time: 10 x 40 kg"). Today the only way is to list every workout and dig through `setRecords` on the client. This endpoint returns that answer directly, resolved from the token, with a query built to stop at the first match instead of scanning the user's history.

## Capabilities

- `GET /users/me/exercises/{exerciseId}/last-performance` returns `200` with the date of the last workout in which the caller did the exercise and the sets recorded in it, in order.
- Each set has the same shape as a recorded set: `reps`, `durationSeconds` and `weight`, all nullable. A set done without weight returns `weight: null` with the rest of the structure unchanged; the client shows "done without weight". A timed set (plank) returns `durationSeconds` with `reps: null`, so the endpoint keeps working for the exercises added by `add-duration-and-optional-weight-set-records`.
- If the caller has never done the exercise, it returns `204 No Content` with no body.
- The route is protected and only ever reads the caller's own data. There is no user id in the route.

## Module Classification

- Workouts (read side) -> mutable aggregate, read-only view over its persisted state, no domain change -> `src/WeightTracker.Domain/Workouts` is untouched; the reference for the read is `src/WeightTracker.Infra/Repositories/WorkoutRepository.cs`
- Users (API) -> REST controller with protected routes -> `src/WeightTracker.Api/Endpoints/Users/UsersController.cs` (new action, next to `GET me`)
- Application -> read use case -> `src/WeightTracker.Application/UseCases/Users/GetUserTrainerUseCase.cs` (new use case in the same folder)
- Infra / Migrations -> one new index, no new table or column -> `src/WeightTracker.Migrations/Migrations/202611051049_AddMesocycleToWorkouts.cs`

`Workout` is a mutable aggregate, not a catalog. This change only reads it and never goes through `Workout` or its action methods.

## Canonical Template Mapping

- `GetMyExerciseLastPerformanceUseCase` -> `GetUserTrainerUseCase` (`src/WeightTracker.Application/UseCases/Users`) -> nearest read use case that works for the caller
- New action on `UsersController` -> `GetMe` and the `me` routes in the same controller -> inherits `AuthorizedController`, reads the caller from `CurrentUser`, every catch goes through `ExceptionMapper.Map`
- Read method on the repository -> `IWorkoutRepository` / `WorkoutRepository` -> mapping stays inside the repository. The method returns a small read model, not a `Workout` (see Notes)
- Response DTO -> `SetRecordResponse` in `src/WeightTracker.Api/Endpoints/Workouts/Dtos` -> same nullable `Reps` / `DurationSeconds` / `Weight` fields and docs style
- Index in the migration and in the EF configuration -> `IX_Workouts_MesocycleId_MesocycleWeekNumber` (migration) and its `HasIndex` in `WeightTrackerDbContext` -> composite index declared in both places
- Endpoint tests -> `test/WeightTracker.Integration/Workouts/SetRecordEndpointTest.cs` plus the closest existing `GET` endpoint test under `Users` -> arranged through the API, `401` matrix per route
- Use case unit test -> the unit test of `GetUserTrainerUseCase` -> Arrange/Act/Assert shape, repository mocked

## State And Mutability

- Nothing is written. No business state changes, so no aggregate method, no `Id.New()` and no `Save`.
- The only persistence change is a new index on `Workouts`. No column, no table, no data change.

## Business Rules

- "Last time the exercise was done" is the caller's most recent workout, by `WorkoutDate` (ties broken by the workout id, which is time-ordered), that is `COMPLETED` or `REVIEWED` and holds at least one set record for that exercise. Workouts still in `DRAFT`, `PUBLISHED` or `STARTED` do not count, so the answer is never the session the user is in the middle of.
- Only the caller's own workouts are read (`Workouts.UserId = CurrentUser.Id`), whoever prescribed them. A trainer asking gets their own history, never an athlete's.
- The sets returned are all the set records of that exercise in that one workout, ordered by `Position`. Sets from other workouts are never mixed in.
- The returned date is `Workouts.WorkoutDate`, the day the workout was done, as a `DateOnly`. It is not the time of any individual set.
- A `weight` of `null` is returned as `null`, not as `0`. A weight of `0` (bodyweight, tracked) is returned as `0`. The two are different and the client relies on it.
- An unknown `exerciseId` is `404`, with the same visibility rule as the existing exercise reads (a custom exercise of another user is not visible). An exercise that exists but was never done is `204`, not `404`.
- A malformed `exerciseId` is `400`, as the other routes with a guid in the path.

## Query And Index

The goal is one index-driven pass that stops at the first match, with no sort and no scan of the user's whole history.

- Step 1, find the workout: walk the caller's workouts newest first and take the first one with a matching `WorkoutExercises` row (`ExerciseId = @exerciseId`) that has at least one `SetRecords` row and whose status is `COMPLETED` or `REVIEWED`. `LIMIT 1`.
- Step 2, read its sets: select the `SetRecords` of that `WorkoutExercises` row ordered by `Position`. Per exercise per workout this is a handful of rows.
- Steps 1 and 2 may run as one statement or two sequential ones; two is acceptable if EF's translation of the nested projection is poor. Use `AsNoTracking` and project to the read model; never hydrate the `Workout` aggregate.

Index plan (all three joins are covered; one index is missing):

- `Workouts(UserId, WorkoutDate DESC)` -> new, `IX_Workouts_UserId_WorkoutDate`. It serves the filter and the order at once, so Postgres walks it in order and stops after the first hit. The existing `IX_Workouts_UserId` becomes redundant, but it stays: dropping it is outside this change.
- `WorkoutExercises(WorkoutId, ExerciseId)` unique -> already exists. It turns the per-workout probe into a point lookup.
- `SetRecords(WorkoutExerciseId)` -> already exists. It serves the "has records" check and step 2. Sorting a handful of rows by `Position` does not justify a composite index.
- Rejected: `WorkoutExercises(ExerciseId, ...)`. A popular exercise appears in many users' workouts, so starting from the exercise reads other users' rows. Starting from the caller keeps the work proportional to how recently they did it.

Migration:

- New FluentMigrator migration, next free timestamp after `202611121200_AddDurationToSetRecords`, named for example `AddUserWorkoutDateIndexToWorkouts`.
- `Up()` creates `IX_Workouts_UserId_WorkoutDate` on `Workouts` (`UserId` ascending, `WorkoutDate` descending). `Down()` deletes it. Both are safe and lossless.
- `WeightTrackerDbContext` declares the same index (`HasIndex` with `IsDescending(false, true)`) so EF and the migration agree. No EF Core migration is generated.
- Verify the plan with `EXPLAIN (ANALYZE, BUFFERS)` on a Postgres seeded with enough workouts to make the planner choose the index, and record the result in the change's design or tasks. A tiny test database will not prove it.

## Roles And Visibility

- Any authenticated user (`USER`, `TRAINER`, `ADMIN`) can read their own last performance.
- No caller can read another user's last performance through this route. The target is always `CurrentUser.Id`.
- A token whose user no longer exists ends with `401`, as `AuthorizedController` already does.

## Business Identifiers And Mutability

- No business identifier is created or changed. `exerciseId` and the workout ids are existing `Id` values, immutable.
- `exerciseId` in the route is parsed through the shared `Id` value object, like the other routes.

## Out Of Scope

- Reading another user's last performance (trainer viewing an athlete). It needs its own visibility rules.
- Returning more than the last time (history, personal bests, progression charts), and any per-exercise pagination.
- Choosing a different reference workout, for example a query parameter to exclude the current workout or to include unfinished ones.
- Comparing the last performance with the prescription, or any volume or one-rep-max calculation.
- Dropping the redundant `IX_Workouts_UserId`, and any change to set record writes or to the existing responses.

## Notes

- The request says `/api/users/me/...`, but the API has no `/api` prefix: `UsersController` is `[Route("users")]` and `GET /users/me` already exists. The route is `/users/me/exercises/{exerciseId}/last-performance`, next to it. If a gateway adds `/api`, that is outside the API.
- Response shape, flat like `SetRecordResponse`: `{ "workoutDate": "2026-11-10", "sets": [ { "reps": 10, "durationSeconds": null, "weight": null }, ... ] }`. `durationSeconds` is added to what was asked (reps and weight) because timed sets now exist; without it a plank would return two nulls.
- Two decisions to confirm when the proposal is created: (1) the date is `WorkoutDate` rather than the registration time of the sets (the latter would need ordering by set time, which no index can serve without scanning the history); (2) only `COMPLETED` and `REVIEWED` workouts count. If the client wants to see the previous session while the current one is `STARTED`, that is what excluding unfinished workouts gives. If it should count in-progress sets instead, say so before coding, because the current workout would then answer its own query.
- Repositories return aggregates today. This method returns a read model (`ExerciseLastPerformance`: date plus a list of set values, defined in Application) because hydrating a whole `Workout` to read a few values would defeat the optimisation. The mapping still stays inside `WorkoutRepository`. This is the only deviation from the mapped template references; no other deviation is allowed.
- Use `Id` for the identifiers in the read model and the use case. Nothing is generated.
- Docs: the controller action and the DTOs carry XML docs, and the OpenAPI document must show `200` and `204` and `401`, `400`, `404` (`ProducesResponseType`).
- Tests: use case unit tests (never done, done, weight null, timed set); integration tests per the auth matrix (`401` without and with an invalid token, `400` bad guid, `404` unknown exercise, `204` never done, `200` latest workout when several exist, another user's records excluded, unfinished workout excluded, null weight versus `0`, a timed set). Full `dotnet test WeightTracker.slnx` with Docker running before it is done.
