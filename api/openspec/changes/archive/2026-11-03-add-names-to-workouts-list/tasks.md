# Tasks

## 1. Use case resolves owners and trainers

- [x] 1.1 Add `Users` (`IReadOnlyDictionary<Id, User>`) to `WorkoutListDTO`, add `IUserRepository` to `GetAllWorkoutsUseCase`, and read the referenced users once as in design.md, throwing `InvalidOperationException` when one is missing. Verify `dotnet build WeightTracker.slnx` passes.
- [x] 1.2 Update `test/WeightTracker.Unit/Application/Workouts/GetAllWorkoutsUseCaseTests.cs` with a strict `IUserRepository` mock: users are read once with exactly the distinct owner and trainer ids (no `null`); a workout without trainer adds no trainer id; an empty list still calls with an empty id set; a missing user throws; an unknown role throws `UnauthorizedOperationException` before any read; existing scoping tests still pass. Verify the tests pass.
- [x] 1.3 Review: compare the users join and its tests against the existing exercise join in the same use case (distinct ids, one call, dictionary by `Id`, XML docs on `WorkoutListDTO`).

## 2. Endpoint returns the list item DTO

- [x] 2.1 Add `WorkoutListItemResponse` in `src/WeightTracker.Api/Endpoints/Workouts/Dtos/` and use it in `WorkoutsController.GetAll`, with `[ProducesResponseType<IReadOnlyList<WorkoutListItemResponse>>]`. Leave `POST /workouts` on `WorkoutResponse`. Verify the build passes.
- [x] 2.2 Extend `test/WeightTracker.Integration/Workouts/WorkoutResponseShapeTests.cs`: a prescribed workout has `athlete` / `trainer` matching `userId` / `trainerId` with exactly `id`, `email`, `name`, `surname`, `role`; a self-planned workout has `trainer` `null`; the `POST /workouts` body has no `athlete` or `trainer`. Verify the tests pass.
- [x] 2.3 Extend `test/WeightTracker.Integration/Workouts/WorkoutScopingTests.cs`: a USER sees themselves as `athlete` and their trainer as `trainer`; a TRAINER still sees the athlete after `PUT /trainer-athletes/{trainerId}/end`; an ADMIN still sees an athlete deactivated with `PUT /users/{id}/deactivate`. Existing scoping, `401` and `403` tests stay green. Verify the tests pass.
- [x] 2.4 Extend `test/WeightTracker.Integration/OpenApi/OpenApiDocumentTests.cs` following `ListUsers_ShouldDocumentANullableTrainerOnEachItem`: the `GET /workouts` item references `WorkoutListItemResponse`, whose `athlete` and nullable `trainer` reference `UserProfileResponse`; `POST /workouts` `201` still references `WorkoutResponse`. Verify the test passes.
- [x] 2.5 Review: compare the DTO, controller action and tests against `UserListItemResponse`, `UsersController.GetAll` and the `add-trainer-to-users-list` tests (naming, XML docs, `ExceptionMapper.Map` in the catch).

## 3. Integration check

- [x] 3.1 Run the template drift check from `AGENTS.md` and verify it prints nothing.
- [x] 3.2 With Docker running, run `dotnet build WeightTracker.slnx` and `dotnet test WeightTracker.slnx` and verify both suites pass with no warnings.
- [x] 3.3 Run `openspec validate add-names-to-workouts-list --strict` and verify it passes.

## Workflow follow-up

- The front refreshes its OpenAPI snapshot and switches its list type to `WorkoutListItemResponse` in `show-workout-athlete-and-trainer-names`.
- Archive the change after review.
