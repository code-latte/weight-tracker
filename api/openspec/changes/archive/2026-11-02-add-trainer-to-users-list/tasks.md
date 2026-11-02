# Tasks

## 1. Repository read for active relationships of several athletes

- [x] 1.1 Add `GetAllActiveByUserIds(IReadOnlyCollection<Id>, CancellationToken)` to `ITrainerAthleteRepository` and implement it in `TrainerAthleteRepository` mirroring `GetAllActiveByTrainerId` (`AsNoTracking`, mapping inside the repository, empty input returns empty without querying); verify `dotnet build WeightTracker.slnx` passes with warnings as errors
- [x] 1.2 Add cases to `test/WeightTracker.Integration/Repositories/TrainerAthleteRepositoryTests.cs` following its existing structure: returns only `ACTIVE` relationships of the given athletes, ignores `ENDED` ones and other athletes, empty id set returns empty; verify they pass
- [x] 1.3 Review: compare the new method and tests against the existing `TrainerAthleteRepository` reads and their tests; verify no deviation in naming, tracking or mapping

## 2. Use case returns users with their current trainer

- [x] 2.1 Add `UserWithTrainerView(User User, User? Trainer)` in `src/WeightTracker.Application/UseCases/Users/`, mirroring `TrainerAthleteView`; verify the build passes
- [x] 2.2 Change `GetAllUsersUseCase` to depend on `ITrainerAthleteRepository` and return `IReadOnlyList<UserWithTrainerView>` as described in design.md (admin check first, `GetAll`, active relationships of `USER` ids, trainers via `GetAllByIds`, pair preserving order); verify the build passes
- [x] 2.3 Update `test/WeightTracker.Unit/Application/Users/GetAllUsersUseCaseTests.cs`: `USER` with an active relationship gets its trainer; `USER` without one gets `null`; `TRAINER` and `ADMIN` get `null`; order of `GetAll` is preserved; non-admin caller throws `UnauthorizedOperationException` and no repository is called; each test with labelled Arrange / Act / Assert; verify the unit suite passes
- [x] 2.4 Review: compare `GetAllUsersUseCase` and `UserWithTrainerView` against `GetTrainerAthletesUseCase` / `TrainerAthleteView`; verify structure, naming and bounded queries match

## 3. Endpoint returns the list item DTO

- [x] 3.1 Add `UserListItemResponse` in `src/WeightTracker.Api/Endpoints/Users/Dtos/` with `From(UserWithTrainerView)`, nesting `UserProfileResponse.From(User)` for the trainer; switch `UsersController.GetAll` and its `[ProducesResponseType]` to it, keeping the catch delegating to `ExceptionMapper.Map`; leave `UserSummaryResponse` and the activate / deactivate actions untouched; verify the build passes
- [x] 3.2 Update `test/WeightTracker.Integration/Users/ListUsersEndpointTests.cs`: the item-shape test expects `trainer`; as the seeded `ADMIN`, in Arrange link one `USER` to a `TRAINER` with `POST /trainer-athletes/{trainerId}` and link then end another (`PUT /trainer-athletes/{trainerId}/end`); assert the first has `trainer` with exactly `id`, `email`, `name`, `surname`, `role`, the ended and unlinked users and every `TRAINER` / `ADMIN` have `trainer: null`, and order by email is unchanged; keep existing `401` (no token, invalid token) and `403` cases green; verify they pass
- [x] 3.3 Add a case where the linked trainer is deactivated (`PUT /users/{id}/deactivate`) and the athlete still lists that trainer; verify it passes
- [x] 3.4 In `test/WeightTracker.Integration/Users/UserActivationEndpointTests.cs`, assert the activate / deactivate response has no `trainer` property (only if no existing exact-shape assertion already covers it); verify it passes
- [x] 3.5 In `test/WeightTracker.Integration/OpenApi/OpenApiDocumentTests.cs`, assert the `GET /users` `200` item schema has a nullable `trainer` property resolving to `UserProfileResponse` (tolerating `nullable` or `oneOf`/`anyOf` with `null`); verify it passes
- [x] 3.6 Review: compare the DTO, controller action and tests against `UserSummaryResponse`, the existing `UsersController` actions and the closest existing endpoint tests; verify the AGENTS.md quality checklist items that apply

## 4. Integration check

- [x] 4.1 Run the template parity check from AGENTS.md and verify no `DRIFT` lines (update `.stpr/template` copies only if a curated file changed)
- [x] 4.2 Run `dotnet build WeightTracker.slnx` and the full `dotnet test WeightTracker.slnx` with Docker running; verify both pass

## Workflow follow-up

- Sync the delta into `openspec/specs/user-administration/spec.md` and archive the change after review.
- Refresh the front OpenAPI snapshot in the separate `front/` plan.
