# Tasks

## 1. Domain: week actions

- [x] 1.1 Replace `Mesocycle.DefineWeek` with `AddWeek` (out of range 409, duplicate 409) and `RedefineWeek` (missing 404); verify `dotnet build WeightTracker.slnx` reports no remaining `DefineWeek` caller in `src`
- [x] 1.2 Migrate `DefineWeek` usages in `MesocycleTests`, `AggregateMapperTests`, `MesocycleRepositoryTests` and `WorkoutRepositoryTests`, and add `AddWeek`/`RedefineWeek` cases from `specs/mesocycles` (duplicate refused and unchanged, undefined redefine refused and not created); verify the unit tests pass
- [x] 1.3 Review `Mesocycle` against `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs` (guarded action methods, exception docs); verify no deviation is left

## 2. Application: mesocycle reads

- [x] 2.1 Add `GetAll`, `GetAllByTrainerId`, `GetAllByAthleteId`, `GetAllByTrainerIdAndAthleteId` to `IMesocycleRepository` and `MesocycleRepository` (weeks included, `StartDate` desc then `Id`); verify with new cases in `MesocycleRepositoryTests` (filtering and order)
- [x] 2.2 Add `MesocycleView` and `MesocycleViewLoader` (one `GetAllByIds`, missing user → `InvalidOperationException`) and unit-test them in `test/WeightTracker.Unit/Application/Mesocycles`
- [x] 2.3 Add `MesocycleVisibility` (`VisibleTo`, `CanSee`) and unit-test every role branch, asserting excluded mesocycles
- [x] 2.4 Add `MesocycleNotFoundException` and implement `GetMesocycleByIdUseCase` (404, then 403 outside scope); verify with unit tests for athlete, trainer, admin, other trainer, other user and unknown id
- [x] 2.5 Implement `GetAllMesocyclesUseCase` and `GetUserMesocyclesUseCase` (USER 403, unknown user 404, trainer filtered by caller, admin all of the athlete); verify with unit tests per branch
- [x] 2.6 Review the read use cases and services against `GetAllExercisesUseCase`, `GetWorkoutByIdUseCase`, `WorkoutVisibility` and `TrainerAthleteViewLoader`; verify no deviation is left

## 3. Application: mesocycle writes

- [x] 3.1 Add `InvalidMesocycleAssignmentException` and `MesocycleAssignmentValidator` (`EnsureAthlete`: missing 404, not `CanBeAthlete` 400; `EnsureTrainer`: missing or not `CanBeTrainer` 400); verify with unit tests including admin-as-trainer and deactivated users
- [x] 3.2 Add `MesocycleEditAuthorizer` (admin, or trainer of the mesocycle) and unit-test admin, own trainer, other trainer and user
- [x] 3.3 Implement `CreateMesocycleUseCase` per design (role resolution, admin validation, trainer active relationship) and unit-test every branch, verifying `Save`/`SaveChangesAsync` are not called on failure
- [x] 3.4 Implement `UpdateMesocycleUseCase` (404, 403, details replaced, shrink below week 409) and unit-test it
- [x] 3.5 Implement `DefineMesocycleWeekUseCase` and `RedefineMesocycleWeekUseCase` and unit-test 404, 403, duplicate 409, out of range 409, missing week 404 and success
- [x] 3.6 Review the write use cases and services against `CreateWorkoutUseCase`, `UpdateWorkoutUseCase`, `AddWorkoutExerciseUseCase`, `WorkoutEditAuthorizer` and `WorkoutAssignmentValidator`; verify no deviation is left

## 4. Api: mesocycles resource

- [x] 4.1 Add DTOs in `Endpoints/Mesocycles/Dtos` (`CreateMesocycleRequest`, `UpdateMesocycleRequest`, `DefineMesocycleWeekRequest`, `RedefineMesocycleWeekRequest`, `MesocycleResponse`, `MesocycleWeekResponse`) with structural validation and `static From`; verify the build passes
- [x] 4.2 Add `MesocyclesController` (six routes, `RequireRole` on writes, `ExceptionMapper.Map`, `[ProducesResponseType]`) and `GET /users/{userId}/mesocycles` on `UsersController`; verify the build passes
- [x] 4.3 Map `MesocycleNotFoundException` (404) and `InvalidMesocycleAssignmentException` (400) in `ExceptionMapper`, register the use cases and services in `Program.cs`; verify `ExceptionMapperTests` covers the new entries
- [x] 4.4 Add integration tests in `test/WeightTracker.Integration/Mesocycles` for the reads (`specs/mesocycle-retrieval`): `401` without and with an invalid token per route, visibility per role with excluded mesocycles (second trainer and athlete created through `POST /users` and `POST /mesocycles`), `403`, `404`, response shape and ordering; verify they pass with Docker
- [x] 4.5 Add integration tests for the writes (`specs/mesocycle-management`): `401` matrix per route, `403` for users and other trainers, `404`, `400` structural and validation, `409` duplicate/out of range/shrink, `201` with `Location`, ended relationship still editable; verify they pass with Docker
- [x] 4.6 Review `MesocyclesController`, the `UsersController` action and the DTOs against `WorkoutsController`, `WorkoutListItemResponse` and `.stpr/template/test/WeightTracker.Integration/Workouts/WorkoutEndpointTest.cs`; verify no deviation is left

## 5. Workout placement over HTTP

- [x] 5.1 Add `IncompleteMesocyclePlacementException` (400), `MesocycleAthleteMismatchException` (409) and `MesocycleTrainerMismatchException` (409) with `ExceptionMapper` entries, and `MesocyclePlacementValidator` (404, 403 via `MesocycleEditAuthorizer`, athlete 409, trainer 409, out of range 409); verify with unit tests per check and their order
- [x] 5.2 Extend `CreateWorkoutUseCase` with the optional pair (incomplete 400, validator, `LinkToMesocycle` before save) and unit-test placed, standalone, incomplete, user 403 and mismatches
- [x] 5.3 Extend `UpdateWorkoutUseCase` per design (changed detection, validator on change or reassignment, link/unlink only on change) and unit-test move, unlink, unchanged on a locked workout, change on a locked workout 409, reassignment breaking the placement 409
- [x] 5.4 Add `MesocycleId` and `MesocycleWeekNumber` to `CreateWorkoutRequest`, `UpdateWorkoutRequest`, `WorkoutResponse` and `WorkoutDetailResponse`, wire them in `WorkoutsController` (`WeekNumber.From`), and register `MesocyclePlacementValidator`; verify `WorkoutResponseShapeTests` asserts the two fields and the list item shape is unchanged
- [x] 5.5 Add `test/WeightTracker.Integration/Workouts/WorkoutMesocyclePlacementEndpointTests.cs` covering every scenario in `specs/workout-mesocycle-placement`; verify they pass with Docker and existing workout integration tests still pass
- [x] 5.6 Review the workout changes against `CreateWorkoutUseCase`, `UpdateWorkoutUseCase` and `WorkoutAssignmentValidator`, and confirm the workout edit rule still runs before placement; verify no deviation is left

## 6. Verification

- [x] 6.1 Run `dotnet build WeightTracker.slnx` and the full `dotnet test WeightTracker.slnx` with Docker running; verify both succeed with no warnings
- [x] 6.2 Run the template parity check from `AGENTS.md`; verify it reports no `DRIFT`
- [x] 6.3 Walk the `AGENTS.md` quality checklist and `openspec validate add-mesocycles-usecases-endpoints --strict`; verify both pass

## Workflow follow-up

- Archive the change with `/opsx:archive` after review.
