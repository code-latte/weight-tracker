## Change

Add the athlete's current trainer to each item of `GET /users`

## Objective

The backoffice needs an admin screen ("Asignación de entrenadores") that lists every athlete and shows whether they
have a trainer right now, so the admin can assign one or end the current relationship. Today `GET /users` returns no
relationship data, and `GET /trainer-athletes/{trainerId}` is scoped per trainer, so the front would have to call it
once per trainer and join the results itself, mixing current and past relationships. The API already knows the rule
"an athlete has at most one `ACTIVE` relationship", so it should answer the question directly.

## Capabilities

- `GET /users` (`ADMIN`) returns, on every item, a new `Trainer` property:
  - `null` for users with role `TRAINER` or `ADMIN`;
  - `null` for a `USER` with no `ACTIVE` relationship;
  - the trainer's details for a `USER` with an `ACTIVE` relationship.
- Nothing else changes in the endpoint: same route, same role gate, same order, same failures.

## Module Classification

- Users (application) -> mutable aggregate use case (read) -> `src/WeightTracker.Application/UseCases/Users/GetAllUsersUseCase.cs`
- TrainerAthletes (application port / infra) -> mutable aggregate, existing repository, new read method, no schema
  change -> `src/WeightTracker.Application/Interfaces/ITrainerAthleteRepository.cs`,
  `src/WeightTracker.Infra/Repositories/TrainerAthleteRepository.cs`
- Users (API) -> REST controller, existing action, response shape extended -> `src/WeightTracker.Api/Endpoints/Users/UsersController.cs`,
  `src/WeightTracker.Api/Endpoints/Users/Dtos/UserSummaryResponse.cs`
- Users (domain) and TrainerAthletes (domain) -> no change expected.

Neither area is a catalog. `User` and `TrainerAthlete` keep changing through their own endpoints; this change only
adds read data to an existing read path.

## Canonical Template Mapping

- `GetAllUsersUseCase` composing users with their active trainer ->
  `src/WeightTracker.Application/UseCases/TrainerAthletes/GetTrainerAthletesUseCase.cs` and its read model
  `TrainerAthleteView.cs` -> nearest use case that joins relationships with user data in Application
  (`GetAllByIds`), without N+1 reads, and returns an Application read model
- New repository read (active relationships for a set of athletes, or all active ones) ->
  `ITrainerAthleteRepository` / `TrainerAthleteRepository` existing reads -> same repository shape, `AsNoTracking`,
  mapping to domain inside the repository
- Response DTO -> `src/WeightTracker.Api/Endpoints/Users/Dtos/UserSummaryResponse.cs` (`From`) plus
  `UserProfileResponse.From(User)` for the nested trainer -> existing DTO factories
- Tests -> `test/WeightTracker.Unit/Application/Users/GetAllUsersUseCaseTests.cs`,
  `test/WeightTracker.Integration/Users/ListUsersEndpointTests.cs`,
  `test/WeightTracker.Integration/Repositories/TrainerAthleteRepositoryTests.cs`,
  `test/WeightTracker.Integration/OpenApi/OpenApiDocumentTests.cs`

## State And Mutability

- Nothing changes state. `GET /users` still only reads, now from `users` and `trainer_athletes`.
- No new tables, columns or indexes, so no migration. The existing partial unique index on
  `trainer_athletes(user_id) WHERE status = 'ACTIVE'` already guarantees one active relationship per athlete.

## Business Rules

- An athlete's current trainer is the trainer of their single `ACTIVE` `TrainerAthlete`. `ENDED` relationships are
  ignored.
- `Trainer` is always `null` for users whose role is `TRAINER` or `ADMIN`, whatever data exists.
- A `USER` with no `ACTIVE` relationship gets `Trainer: null`.
- The trainer is returned whatever the trainer's own activation state (`IsActive`); hiding deactivated trainers is
  not this endpoint's decision.
- The list order (email ascending), the admin-only gate and the `401` / `403` behaviour are unchanged.
- The data is read with a bounded number of queries (no query per user).

## Roles And Visibility

- `ADMIN`: `200` with every user and their `Trainer`.
- `TRAINER`, `USER`: `403`, unchanged.
- Unauthenticated or invalid token: `401`, unchanged.
- The nested `Trainer` exposes only `Id`, `Email`, `Name`, `Surname` and `Role`. Never the password hash, refresh
  tokens, activation data or the trainer's own trainer.

## Business Identifiers And Mutability

- `User.Id` and `User.Email` are not affected.
- `TrainerAthlete.Id` is not exposed or affected by this change.

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## Out Of Scope

- Changes to `GET /trainer-athletes`, `GET /trainer-athletes/{trainerId}`, `POST /trainer-athletes/{trainerId}`
  or `PUT /trainer-athletes/{trainerId}/end`.
- Relationship history, start date or relationship id on `GET /users`.
- Filtering `GET /users` by role, by "has trainer", search or pagination.
- `GET /users/me`, `POST /users`, activate / deactivate: their responses do not gain `Trainer`, even if they share
  the DTO (see Notes).
- Any front change. That is a separate plan in `front/` after the OpenAPI snapshot refresh
  (`front/.stpr/plan/openspec/proposal-change-add-backoffice-trainer-assignment.md`).

## Notes

- **Nested type (decided).** `Trainer` is typed `UserProfileResponse?` (`Id`, `Email`, `Name`, `Surname`, `Role`),
  the existing public user DTO. Do not nest `UserSummaryResponse` in itself: it would make the schema recursive and
  expose a trainer's activation state and a meaningless nested `Trainer`.
- **Where the DTO changes.** `UserSummaryResponse` is also returned by `PUT /users/{id}/activate|deactivate`. Prefer
  a dedicated list item DTO (for example `UserListItemResponse`, the summary fields plus `Trainer`) used only by
  `GET /users`, so the other responses stay as they are. If the proposal keeps `UserSummaryResponse` instead, those
  endpoints must fill `Trainer` consistently or the field must be documented as list-only; decide at proposal time
  and state it in the spec.
- **Application result.** `GetAllUsersUseCase.Execute` returns a small Application read model per user (for example
  `UserWithTrainerView(User User, User? Trainer)`, beside `TrainerAthleteView`) instead of bare `User`, so the controller maps without querying.
  The use case reads all users, reads the `ACTIVE` relationships of the `USER` ones (one query), resolves the
  trainers with `IUserRepository.GetAllByIds` (one query) and pairs them.
- `[ProducesResponseType]` on `GET /users` changes to the new item type; `401` and `403` stay.
- Tests to add or update:
  - Unit `GetAllUsersUseCase`: a `USER` with an active relationship gets its trainer; a `USER` without one gets
    `null`; `TRAINER` and `ADMIN` users get `null`; a non-admin caller still throws
    `UnauthorizedOperationException` and never calls a repository.
  - Repository: the new read returns only `ACTIVE` relationships, ignores `ENDED` ones, and handles an empty id set.
  - Integration `GET /users` as the seeded `ADMIN`: in Arrange, link a `USER` to a `TRAINER` with
    `POST /trainer-athletes/{trainerId}`, link and then end another one; assert the first has `trainer` with exactly
    `id`, `email`, `name`, `surname`, `role`, the ended one and unlinked users have `trainer: null`, and every
    `TRAINER` / `ADMIN` has `trainer: null`. Order by email is unchanged. Existing `401` / `403` tests stay green.
  - OpenAPI document test: the `GET /users` item schema has a nullable `trainer` property referencing the user
    profile schema.
- OpenAPI snapshot changes (the `GET /users` item schema); the front refreshes it in its own plan.
- No deviations from the mapped template references are allowed.
