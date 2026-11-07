# Tasks

## 1. Domain: explicit order and new set record actions

- [x] 1.1 Change `WorkoutExercise.SetRecords` to return records in list order, not sorted by `RegisteredAtUtc`. Update `WorkoutExerciseTests` / `WorkoutTests` so that set records added at 10:05, 10:01, 10:03 come back in that order, and verify they pass
- [x] 1.2 Add `SetRecordNotFoundException` and `SetRecordOrderMismatchException` in `WeightTracker.Domain/Exceptions`, following `WorkoutExerciseOrderMismatchException`; verify the build passes
- [x] 1.3 Add `internal SetRecord.Change(RepCount, Weight)`, keeping `Id` and `RegisteredAtUtc` immutable, and `Workout.UpdateSetRecord` → `WorkoutExercise.UpdateSetRecord`. Cover with unit tests: success keeps id, time and position; not `STARTED` → `WorkoutNotStartedException`; unknown exercise; unknown set record; nothing changes on failure
- [x] 1.4 Add `Workout.RemoveSetRecord` → `WorkoutExercise.RemoveSetRecord`. Cover with unit tests: middle record removed and the rest keep their order; not `STARTED`; unknown exercise; unknown set record
- [x] 1.5 Add `Workout.ReorderSetRecords` → `WorkoutExercise.ReorderSetRecords`, mirroring `ReorderExercises`. Cover with unit tests: reverse order keeps values; other exercises untouched; missing, extra, repeated or foreign id → `SetRecordOrderMismatchException` with order unchanged; not `STARTED`
- [x] 1.6 Add a unit test that `AddSetRecord` refuses a `FINISHED` workout for any caller (the status lives in the aggregate). Run `dotnet test test/WeightTracker.Unit` and verify it passes
- [x] 1.7 Review: compare `Workout` / `WorkoutExercise` / `SetRecord` changes against `.stpr/template/src/WeightTracker.Domain/Workouts`, copy the changed canonical files (`Workout.cs`, `WorkoutExercise.cs`, `WorkoutTests.cs`) into `.stpr/template`, and verify the drift check in `AGENTS.md` prints nothing

## 2. Infra: persist edited values

- [x] 2.1 Extend `WorkoutRepository.ReconcileSetRecords` to copy `Reps` and `Weight` as well as `Position` to surviving rows, never `RegisterDatetimeUtc`. Verify with new `WorkoutRepositoryTests`: edit persists, remove deletes only that row, reorder reloads in the new order
- [x] 2.2 Extend `WorkoutMapperTests` so that a reordered and edited exercise round-trips its set records in list order, and verify they pass
- [x] 2.3 Review: compare against `WorkoutRepository` / `WorkoutMapper` references in `.stpr/template/README.md`; confirm no migration was added

## 3. Application: recording authorizer and use cases

- [x] 3.1 Add `WorkoutRecordingAuthorizer.EnsureCanRecord` in `Application/Services`: `ADMIN` allowed, `TRAINER` rejected, owner allowed, others rejected. Cover with unit tests for admin, owner, prescribing trainer, trainer owning a self-planned workout, and a non-owner user
- [x] 3.2 Add `AddSetRecordUseCase` using `IClock.UtcNow`. Cover with unit tests mirroring `AddWorkoutExerciseUseCaseTests`: success stamps the clock time; workout not found; 403 branches; authorization runs before the status check (a non-owner on a `DRAFT` workout gets `UnauthorizedOperationException`)
- [x] 3.3 Add `UpdateSetRecordUseCase`, building `RepCount` / `Weight` before touching the aggregate. Cover with unit tests mirroring `UpdateWorkoutExerciseUseCaseTests`
- [x] 3.4 Add `RemoveSetRecordUseCase`. Cover with unit tests mirroring `RemoveWorkoutExerciseUseCaseTests`
- [x] 3.5 Add `ReorderSetRecordsUseCase`. Cover with unit tests mirroring `ReorderWorkoutExercisesUseCaseTests`
- [x] 3.6 Replace the "Adding a set record" note in `.stpr/template/STPR_INVARIANTS.md` with the "Recording sets" rule from design.md, and verify it names all four actions, the roles, 403-before-409 and the `STARTED` window
- [x] 3.7 Review: compare the authorizer with `WorkoutEditAuthorizer` and the use cases with `ReorderWorkoutExercisesUseCase`. Run `dotnet test test/WeightTracker.Unit` and verify it passes

## 4. API: endpoints, DTOs and response

- [x] 4.1 Add `AddOrUpdateSetRecordRequest` and `ReorderSetRecordsRequest` DTOs with required fields, mirroring `ReorderWorkoutExercisesRequest`
- [x] 4.2 Add `SetRecordResponse` and `SetRecords` to `WorkoutExerciseDetailResponse`. Extend `WorkoutResponseShapeTests` / `WorkoutGetByIdEndpointTests` to assert ordered set records on one exercise and an empty list on the others
- [x] 4.3 Add the four routes to `WorkoutsController`, each with `ProducesResponseType` for 200/400/401/403/404/409 and `ExceptionMapper.Map` in the catch. Map `SetRecordNotFoundException` → 404 and `SetRecordOrderMismatchException` → 409 in `ExceptionMapper`, and extend `ExceptionMapperTests` for both
- [x] 4.4 Register the authorizer and the four use cases in `Program.cs`, and verify the API starts with `dotnet build WeightTracker.slnx`
- [x] 4.5 Add `SetRecordAddEndpointTests`, mirroring `WorkoutExerciseAddEndpointTests`: 200 owner, 200 admin, 401 no token, 401 invalid token, 403 prescribing trainer, 403 trainer owning a self-planned workout, 403 other user, 403 not 409 for a non-owner on a `DRAFT` workout, 404 workout, 404 exercise, 409 not started, 409 non-rep target, 400 missing field, 400 invalid reps or weight
- [x] 4.6 Add `SetRecordUpdateEndpointTests` with the same per-route matrix, plus a 404 for an unknown set record and a check that id, time and position are kept
- [x] 4.7 Add `SetRecordRemoveEndpointTests` with the per-route matrix (no 400 body case), plus a 404 for an unknown set record and a check that the remaining order is kept
- [x] 4.8 Add `SetRecordReorderEndpointTests`, mirroring `WorkoutExerciseReorderEndpointTests`: the per-route matrix, 409 for missing, repeated or foreign ids, 400 for a missing list, and a reorder that survives a `GET /workouts/{id}`
- [x] 4.9 Review: compare the controller routes, DTOs and tests against `WorkoutsController` and the closest existing endpoint tests; confirm every test has labelled `// Arrange` / `// Act` / `// Assert` with one Act and authenticates through `AuthenticatedClientFor`

## 5. Verification

- [x] 5.1 With Docker running, run `dotnet test WeightTracker.slnx` and verify unit and integration suites pass
- [x] 5.2 Run the `.stpr/template` drift check from `AGENTS.md` and verify it prints no `DRIFT:` lines
- [x] 5.3 Walk the quality checklist in `AGENTS.md` and verify every item applies or is satisfied
- [x] 5.4 Run `openspec validate add-set-record-endpoints --strict` and verify it is valid

## Workflow follow-up

- Archive the change after review, which merges the delta into `openspec/specs/workout-set-records/spec.md`.
