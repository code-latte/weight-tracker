# Proposal

## Why

Set records already exist in the domain and in persistence (`SetRecord`, `RepCount`, `Weight`, the `SetRecords` table, `Workout.AddSetRecord`), but no use case or endpoint exposes them, so athletes cannot log what they actually performed. This change wires the Application and API layers on top and adds the domain actions that are still missing: edit, remove and reorder.

## What Changes

- New endpoints on `WorkoutsController`, all authenticated:
  - `POST   /workouts/{workoutId}/exercises/{exerciseId}/set-records` with `{ reps, weight }`
  - `PUT    /workouts/{workoutId}/exercises/{exerciseId}/set-records/{setRecordId}` with `{ reps, weight }`
  - `DELETE /workouts/{workoutId}/exercises/{exerciseId}/set-records/{setRecordId}`
  - `PUT    /workouts/{workoutId}/exercises/{exerciseId}/set-records/order` with `{ setRecordIds: [...] }`
- Each returns the updated workout. The workout detail response (mutations and `GET /workouts/{id}`) now lists each exercise's set records (`id`, `reps`, `weight`, `registeredAtUtc`) in their order.
- New domain actions on `Workout` / `WorkoutExercise`: edit reps and weight of a set record, remove a set record, reorder the set records of one exercise. Every action, including add, is allowed only while the workout is `STARTED`.
- **Order becomes explicit.** Set records are no longer ordered by registration time: a new record is appended last and reorder sets the order. The stored `Position` is the source of truth.
- `RegisteredAtUtc` of a new record comes from the server clock, not from the request.
- New recording authorization rule: `ADMIN` always, otherwise only the workout's owner (`UserId`) whose role is `USER`. Every caller with role `TRAINER` is rejected with 403, including the prescribing trainer and a trainer who owns a self-planned workout.
- `STPR_INVARIANTS.md` §Business authorization rules extends the current "adding a set record" note into a "Recording sets" rule covering add, edit, remove and reorder.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `workout-set-records`: ordering changes from registration time to explicit position; adds edit, remove and reorder; adds the HTTP routes, the recording authorization rule, the auth matrix and set records in the workout response.

## Module Classification

- Workouts -> mutable aggregate (`Workout` → `WorkoutExercise` → `SetRecord`) -> `.stpr/template/src/WeightTracker.Domain/Workouts`. Truly mutable, not catalog-like.
- Recording authorization -> auth/authorization support -> pattern of `src/WeightTracker.Application/Services/WorkoutEditAuthorizer.cs` (the pattern, not its rule).

## Canonical Template Mapping

- Add / edit / remove set record use cases -> `AddWorkoutExerciseUseCase`, `UpdateWorkoutExerciseUseCase`, `RemoveWorkoutExerciseUseCase` -> same aggregate and same load → authorize → mutate → save → commit flow.
- Reorder set records use case and domain action -> `ReorderWorkoutExercisesUseCase` + `Workout.ReorderExercises` -> same "every id exactly once, in the new order" semantics one level deeper.
- Recording authorizer -> `WorkoutEditAuthorizer` / `WorkoutStatusAuthorizer` -> one shared Application service per rule, never restated inline.
- Endpoints and DTOs -> `WorkoutsController` (`src/WeightTracker.Api/Endpoints/Workouts`) and `ReorderWorkoutExercisesRequest` / `AddOrUpdateExerciseRequest` -> authenticated routes, `CurrentUser` to the use case, `ExceptionMapper.Map` in every catch.
- Persistence -> `WorkoutRepository` / `WorkoutMapper` -> already map `SetRecords`; reconciliation is extended to update values of edited records.
- Tests -> `WorkoutTests`, `WorkoutExerciseTests`, `ReorderWorkoutExercisesUseCaseTests`, `WorkoutMapperTests`, `WorkoutRepositoryTests`, `WorkoutExerciseReorderEndpointTests` / `WorkoutExerciseUpdateEndpointTests`.

## Business Identifiers And Mutability

- `SetRecord.Id`: generated with `Id.New()`, immutable after creation.
- `WorkoutExercise` is addressed by its catalog `exerciseId` within the workout, as in existing routes. Unchanged.
- No unique business identifier becomes mutable.

## Conflicts With Template Or Invariants

- The existing invariant note says adding a set record is not an edit and the edit rule does not apply. This change keeps that and replaces the note with an explicit recording rule. The new rule rejects the prescribing trainer, who is allowed by the edit rule. That is intended: recording belongs to the athlete.
- The existing `workout-set-records` spec orders set records by registration time. This change replaces that with explicit order (MODIFIED requirement).

## Out Of Scope

- Changing set records after `FINISHED`, or an ADMIN bypass of the `STARTED` window. This is deferred to a later change.
- Changing `RegisteredAtUtc` after creation.
- Set records for targets that are not rep-based.
- Front-end and backoffice screens; history or progress reads across workouts.

## Impact

- Domain: `Workout`, `WorkoutExercise`, new exceptions (`SetRecordNotFoundException`, set record order mismatch).
- Application: four use cases in `UseCases/Workouts`, a new recording authorizer in `Services`.
- API: four routes, request DTOs, set records in `WorkoutExerciseDetailResponse`, `ExceptionMapper` entries, `Program.cs` registrations.
- Infra: `WorkoutRepository.ReconcileSetRecords` updates reps and weight as well as position. No new migration; the `Position` column already exists.
- Docs: `.stpr/template/STPR_INVARIANTS.md`, and `.stpr/template` copies of changed canonical files.
