## Change

Add two stats reads: `GET /workouts/{workoutId}/summary` (sets in target, volume and top set against the previous execution) and `GET /users/{userId}/exercises/{exerciseId}/history?limit=` (max weight, volume and estimated 1RM per session).

## Objective

The app and the backoffice want to show how a session went and how an exercise is progressing. Today the client would have to fetch whole workouts and recompute everything, with rules (what "in target" means, what volume is, which set is the top one) that would drift between clients. These endpoints compute the stats once, on the server, with the rules defined in the domain.

## Capabilities

- `GET /workouts/{workoutId}/summary` returns `200` with:
  - for the workout: sets in target over sets evaluated, and total volume;
  - per exercise, in workout order: sets in target over sets evaluated, volume, the top set of this workout and the top set of the previous execution of that exercise (with its workout id and date), or `null` when there is none.
- `GET /users/{userId}/exercises/{exerciseId}/history?limit=` returns `200` with the last `limit` sessions in which the user did the exercise, newest first, each with workout id, workout date, max weight, volume and estimated one-rep max. An empty list when there are none.
- Both routes are protected and read-only.

## Module Classification

- Workouts (domain) -> mutable aggregate, read-only use of it plus new pure calculation rules -> `src/WeightTracker.Domain/Workouts` (`SetTarget`, `SetResult`, `SetRecord`, `WorkoutExercise`). No state, no action method changes.
- Workouts (API) -> REST controller, protected route -> `src/WeightTracker.Api/Endpoints/Workouts/WorkoutsController.cs` (new action next to `GET {id}`)
- Users (API) -> REST controller, protected route -> `src/WeightTracker.Api/Endpoints/Users/UsersController.cs` (new action next to `GET me/exercises/{exerciseId}/last-performance`)
- Application -> read use cases -> `src/WeightTracker.Application/UseCases/Workouts/GetWorkoutByIdUseCase.cs` and `src/WeightTracker.Application/UseCases/Users/GetMyExerciseLastPerformanceUseCase.cs`
- Infra -> repository read models, no migration expected -> `src/WeightTracker.Infra/Repositories/WorkoutRepository.cs` (the last-performance read)

`Workout` is a mutable aggregate, not a catalog. This change never calls its action methods and never saves it.

## Canonical Template Mapping

- `GetWorkoutSummaryUseCase` -> `GetWorkoutByIdUseCase` -> same load-by-id, `WorkoutNotFoundException`, and `WorkoutVisibility.CanSee` gate before anything else is resolved
- `GetUserExerciseHistoryUseCase` -> `GetMyExerciseLastPerformanceUseCase` (query shape, exercise visibility, read model) and `GetUserTrainerUseCase` / `GetUserMesocyclesUseCase` (caller vs `{userId}` authorization)
- Previous-execution and history reads -> the last-performance method on `IWorkoutRepository` / `WorkoutRepository` -> index-driven, `AsNoTracking`, projection to a read model, mapping inside the repository
- In-target, volume and top-set rules -> `SetTarget` / `SetResult` records in `src/WeightTracker.Domain/Workouts` -> behaviour lives with the value objects it reads
- Controller actions -> `GetById` in `WorkoutsController` and `GetMyExerciseLastPerformance` in `UsersController` -> `AuthorizedController`, `CurrentUser`, every catch through `ExceptionMapper.Map`, `ProducesResponseType` per status
- DTOs -> `ExerciseLastPerformanceResponse` and `SetRecordResponse` -> same nullable fields and XML docs style
- Tests -> `test/WeightTracker.Integration` tests of `GET /workouts/{id}` and of last-performance; unit tests of `GetWorkoutByIdUseCase`, `GetMyExerciseLastPerformanceUseCase` and of the `Domain/Workouts` value objects

## State And Mutability

- Nothing is written. No `Id.New()`, no `Save`, no aggregate action method.
- No new table or column. The history and previous-execution reads are served by the existing `IX_Workouts_UserId_WorkoutDate`, `WorkoutExercises(WorkoutId, ExerciseId)` unique and `SetRecords(WorkoutExerciseId)` indexes. If `EXPLAIN` shows otherwise, a migration is added and justified in the design.

## Business Rules

Volume

- Volume of a set = reps × kg. Volume of an exercise = sum over its set records; of a workout = sum over its exercises.
- Only sets with a reps result and a non-null weight contribute. A timed set or a set with `weight: null` contributes nothing; a set at `0` kg contributes `0`.
- When no set of an exercise contributes, its volume is `null` (not applicable), not `0`. The workout volume is `null` only when every exercise volume is `null`.
- `perSide` does not multiply the volume: one recorded set is one set.

Sets in target

- Prescriptions are expanded by `count` into an ordered list of prescribed sets; set record `i` (by `Position`) is matched to prescribed set `i`.
- A set is in target when its result meets the matched target:
  - `Reps(n)`: reps ≥ n
  - `RepsRange(min, max)`: reps ≥ min (going above max still counts)
  - `Duration(s)`: seconds ≥ s
  - `Minutes(m)`: seconds ≥ m × 60
  - `MaxReps`: always met, whatever the reps
- Sets evaluated = recorded sets that have a matched prescribed set. Records beyond the prescribed sets and prescribed sets without a record are not evaluated. The response gives `inTarget` and `evaluated`, so the client can show "7/9".
- A result whose kind does not match its target (reps against a time target) is not in target. The aggregate already prevents it; the rule is total anyway.
- The rule is `SetTarget.IsMetBy(SetResult)` in the domain, unit-tested per target kind and boundary.

Top set

- The top set of an exercise in a workout is its heaviest set; ties broken by most reps, then longest duration, then lowest `Position`. A `null` weight ranks below `0`.
- It is returned with the set's `reps`, `durationSeconds` and `weight`, same shape as `SetRecordResponse`. `null` when the exercise has no set records in that workout.
- The ordering is a domain rule (a comparer next to `SetRecord`), shared by the summary and by any later stat.

Previous execution

- For each exercise of the summarised workout: the owner's most recent workout before it (by `WorkoutDate`, ties by workout id) that is `COMPLETED` or `REVIEWED`, contains that exercise and has at least one set record for it, and that the caller may see. Same selection as last-performance, anchored before the summarised workout instead of "now", and on the workout owner instead of the caller.
- Its top set is computed with the same rule. `null` when there is none.

Summary scope

- Any visible workout can be summarised, whatever its status. A `STARTED` workout gives a partial summary; a workout without set records gives `0/0` and `null` volumes and top sets.

History

- A session is a workout of `{userId}` that is `COMPLETED` or `REVIEWED`, contains the exercise, has at least one set record for it, and that the caller may see. Ordered by `WorkoutDate` desc, ties by workout id desc; the first `limit` are returned.
- Max weight of a session = the highest non-null weight among its sets of that exercise, `null` when none has weight. Volume as defined above.
- Estimated one-rep max of a session = the highest Epley estimate among its sets, `1RM = weight × (1 + reps / 30)`. Only sets with a reps result and a non-null weight are estimated; `null` when none is. The formula is applied as given, also for a single rep (60 kg × 1 → 62 kg). It lives in the domain next to the volume rule.
- `limit` is optional, default `10`, accepted `1..50`. Outside that range or not an integer is `400`.

## Roles And Visibility

- Summary: the caller must pass `WorkoutVisibility.CanSee` on the workout (admin any; trainer own or prescribed; user own, except prescribed drafts not yet published). Otherwise `403`. Unknown workout `404`.
- Previous execution is filtered by the same visibility: a trainer only gets previous executions among the workouts they could see, never an athlete's private workout.
- History:
  - `USER`: only `{userId}` = themselves, otherwise `403`.
  - `TRAINER`: themselves, or any user, scoped to the workouts of that user the trainer may see (in practice, the ones they prescribed). A user they never trained returns an empty list.
  - `ADMIN`: any user, every workout.
  - Unknown `{userId}` is `404`. Unknown `exerciseId`, or one not visible to the caller (another user's custom exercise), is `404`, same rule as last-performance.
- Malformed guid in either route is `400`. Missing or invalid token is `401`.

## Business Identifiers And Mutability

- No business identifier is created or changed. `workoutId`, `userId`, `exerciseId` and the returned workout ids are existing `Id` values, parsed through the shared `Id` value object and immutable.

## Out Of Scope

- Estimated one-rep max in the workout summary, relative intensity, tonnage per muscle, or any stat beyond the ones listed.
- History of time-based metrics (longest duration per session); history for timed exercises returns `null` max weight and `null` volume.
- Date-range filters, pagination beyond `limit`, and aggregation by week or mesocycle.
- Comparing the summary with the trainer's review, and caching or persisting computed stats.
- Changing `GET /users/me/exercises/{exerciseId}/last-performance` beyond extracting shared query code.

## Notes

- Response shapes (sketch):
  - Summary: `{ "workoutId", "setsInTarget": { "inTarget": 7, "evaluated": 9 }, "volume": 4250.0, "exercises": [ { "workoutExerciseId", "exerciseId", "exerciseName", "setsInTarget": {...}, "volume": 1200.0, "topSet": { "reps": 8, "durationSeconds": null, "weight": 60 }, "previous": { "workoutId", "workoutDate": "2026-11-06", "topSet": {...} } | null } ] }`
  - History: `[ { "workoutId", "workoutDate": "2026-11-10", "maxWeight": 62.5, "volume": 3100.0, "estimatedOneRepMax": 79.2 } ]`
- Volume and weights are `decimal` in kilograms, like `Weight`.
- The summary loads the workout aggregate (`GetById`) and computes everything from it in the domain. Previous executions come from one repository read per summary (all exercise ids at once), returning a read model with the set values of each previous execution, not a `Workout`. History comes from one repository read returning per-session set values; the use case applies the domain rules. Aggregating volume in SQL is rejected because it would duplicate the volume rule outside the domain.
- The repository reads return read models defined in Application, as last-performance already does. That is the only allowed deviation from the mapped references.
- Decisions to confirm before the proposal is created:
  1. Records are matched to prescriptions by position.
  2. Volume is `null` (not `0`) when no set contributes.
  3. Top set = heaviest, then most reps. An alternative is highest volume set (reps × kg).
  4. Previous execution and history count only `COMPLETED` and `REVIEWED` workouts and respect the caller's workout visibility, so a trainer sees only prescribed sessions.
  5. `limit` default `10`, max `50`.
- Docs: XML docs on the actions and DTOs; OpenAPI shows `200`, `400`, `401`, `403`, `404` per route.
- Tests:
  - Unit, domain: `SetTarget.IsMetBy` per kind and boundaries (reps below/at/above n, range below min, at min, above max, `MaxReps` always met); set volume with weight, `0` kg, `null` weight, timed set; Epley estimate with weight, `0` kg, `null` weight, timed set, best set chosen; top-set ordering and ties.
  - Unit, use cases: summary with mixed exercises, no records, extra records, previous present/absent; history with limit, empty, null weights, estimated 1RM per session. Repositories mocked.
  - Integration, summary: `401` without and with invalid token, `400` bad guid, `404` unknown workout, `403` invisible workout (another user's, athlete's unpublished draft), `200` with values asserted, previous execution picks the latest earlier finished workout and excludes later, unfinished and invisible ones.
  - Integration, history: `401` without and with invalid token, `400` bad guid and bad `limit`, `403` user asking for another user, `404` unknown user and unknown/invisible exercise, `200` ordered newest first and truncated by `limit`, max weight, volume and estimated 1RM asserted, unfinished workouts excluded, trainer only gets prescribed sessions (assert the athlete's own session is excluded), admin gets all.
  - Full `dotnet test WeightTracker.slnx` with Docker running before it is done.
