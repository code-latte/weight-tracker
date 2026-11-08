# Tasks

## 1. Application: user trainer lookup

- [x] 1.1 Add `GetUserTrainerUseCase` in `src/WeightTracker.Application/UseCases/Users`, shaped like `GetUserMesocyclesUseCase`. The steps:
  - permission first (`ADMIN`, or `USER` asking about their own id, otherwise `UnauthorizedOperationException`);
  - load the target or throw `UserNotFoundException`;
  - return `null` for a non-`USER` target without querying relationships;
  - otherwise use `GetAllActiveByUserIds([userId])` to return the trainer of the active relationship, or `null`.
- [x] 1.2 Add `test/WeightTracker.Unit/Application/Users/GetUserTrainerUseCaseTests.cs`, modelled on `GetUserMesocyclesUseCaseTests` with strict mocks. Cover:
  - a user reading their own trainer;
  - a user with no active relationship gets `null`;
  - a user asking about another id is refused before any repository call;
  - a trainer is refused, also for their own id;
  - an admin gets an athlete's trainer;
  - an admin gets `null` for a trainer or admin target without the relationship repository being called;
  - an admin asking about an unknown id gets `UserNotFoundException`;
  - a deactivated trainer is still returned.
- [x] 1.3 Review the use case and its tests against `GetUserMesocyclesUseCase` / `GetAllUsersUseCase` and their tests (permission before lookup, no inline duplication of the trainer-athletes rules).

## 2. API

- [x] 2.1 Add `GET /users/{userId:guid}/trainer` to `UsersController`:
  - `[RequireRole(Role.USER, Role.ADMIN)]`;
  - `Ok(UserProfileResponse.From(trainer))`, or an explicit `NoContent()` when there is no trainer;
  - the catch delegates to `ExceptionMapper.Map`;
  - `[ProducesResponseType]` for 200 (`UserProfileResponse`), 204, 401, 403 and 404.
- [x] 2.2 Mention the route in the `UsersController` summary, and inject the use case in the controller.
- [x] 2.3 Register `GetUserTrainerUseCase` in `Program.cs`.
- [x] 2.4 Review the action against `GET /users/{userId}/mesocycles` (attributes, declared responses, exception handling).

## 3. Integration tests

- [x] 3.1 Add `test/WeightTracker.Integration/Users/UserTrainerEndpointTests.cs`, mirroring the `GET /users/{userId}/mesocycles` cases in `MesocycleReadEndpointsTests`. Authenticate only through `AuthenticatedClientFor`, and arrange state only through the API. Cover:
  - **200:**
    - the seeded user reads their own trainer (seeded trainer's `id`, `email`, `name`, `surname`, `role`, and no other fields);
    - an admin reads the seeded user's trainer;
    - the user's trainer was deactivated by an admin and the user still gets it.
  - **204 with an empty body:**
    - the seeded user after their active relationship is ended through `PUT /trainer-athletes/{trainerId}/end`;
    - a user created through `POST /users` who never had a trainer;
    - an admin asking about the seeded trainer.
  - **403:**
    - the seeded user asking about the seeded trainer, an existing other user;
    - the seeded user asking about an unknown id;
    - the seeded trainer asking about the seeded user and about themselves.
  - **404:** an admin asking about an unknown id.
  - **401:** without a token and with an invalid token.
- [x] 3.2 Review the new tests against the closest existing endpoint tests (structure, labelled `// Arrange` / `// Act` / `// Assert` phases, exactly one Act, nothing depending on another test's writes).

## 4. Verification

- [x] 4.1 Run `dotnet build WeightTracker.slnx` (warnings are errors), then the full `dotnet test WeightTracker.slnx` with Docker running. Fix any failure before closing.
- [x] 4.2 Run the template drift check from `AGENTS.md`. If a curated copy under `.stpr/template` was touched, refresh it until the check prints nothing.
