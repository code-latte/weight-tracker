## Change

Add `GET /users/{userId}/trainer`, which returns the trainer of a user's active trainer-athlete relationship, or `204 No Content` when there is none. A `USER` may call it only for themselves, and an `ADMIN` for anyone.

## Objective

The app needs to know who the athlete's trainer is, for example to show "Your trainer: …" right after login or to decide whether trainer-only flows apply. Today the athlete can only work it out indirectly, from the `trainer` of a prescribed workout. Admins also need to look up a single user's trainer without listing every user.

## Capabilities

- `GET /users/{userId}/trainer` returns the public profile of the trainer of that user's active relationship with `200`, or `204 No Content` with an empty body when there is none.

## Module Classification

- Users -> mutable aggregate, read here only -> `src/WeightTracker.Domain/Users`
- TrainerAthletes -> mutable aggregate, read here only -> `src/WeightTracker.Domain/TrainerAthletes`
- Application: user trainer read -> read use case composing two aggregates -> `src/WeightTracker.Application/UseCases/Users`
- API: Users -> `src/WeightTracker.Api/Endpoints/Users/UsersController.cs`

Neither aggregate changes. This change only reads them. No domain, persistence or migration work.

## Canonical Template Mapping

- Endpoint -> `GET /users/{userId}/mesocycles` in `UsersController` -> the same user-scoped read sub-resource:
  - a `[RequireRole]` gate;
  - a use case that checks the caller before looking the user up;
  - 403 and 404 responses.
- `GetUserTrainerUseCase` (new) -> `GetUserMesocyclesUseCase` for the shape:
  - explicit `callerId`, `callerRole` and `userId`;
  - `UnauthorizedOperationException`, then `UserNotFoundException`.

  It follows `GetAllUsersUseCase` for how the trainer is resolved (`ITrainerAthleteRepository.GetAllActiveByUserIds`, then `IUserRepository.GetAllByIds`).
- Response -> reuse `UserProfileResponse` -> it is already the public profile used for `trainer` in `UserListItemResponse` and in workout responses. No new DTO.
- Tests:
  - `GetUserMesocyclesUseCaseTests` (unit);
  - the `GET /users/{userId}/mesocycles` cases in `MesocycleReadEndpointsTests` (integration).

## State And Mutability

- No state changes. The endpoint is read-only.
- The trainer comes from the user's single active `TrainerAthlete` relationship. Ended or pending relationships are ignored.

## Business Rules

- The response is the trainer of the user's active trainer-athlete relationship.
- The response is `204 No Content` (no trainer) when:
  - the user has no relationship, or only ended or pending ones;
  - the user's role is `TRAINER` or `ADMIN`, since only `USER`s can be athletes. An admin may ask about such a user and gets `204`; relationships are not queried for them, as in `GetAllUsersUseCase`.
- A user has at most one active trainer (already enforced by the trainer-athletes rules), so no ordering or tie-breaking is needed.
- The trainer is returned whatever their activation state, consistent with the workout list and the admin user list ("listed people are resolved regardless of their state").
- A deactivated user can still be looked up by an admin.

## Roles And Visibility

| Caller | `userId` | Result |
|---|---|---|
| `USER` | their own id | `200` with the trainer, or `204` |
| `USER` | any other id, existing or not | `403` |
| `TRAINER` | any id, including their own | `403` |
| `ADMIN` | an existing user | `200` with the trainer, or `204` |
| `ADMIN` | an unknown id | `404` |
| no token / invalid token | — | `401` |

- The route carries `[RequireRole(Role.USER, Role.ADMIN)]`. The use case also checks that a `USER` asks about themselves.
- The permission check runs before the user lookup. A `USER` asking about another id gets `403` whether that id exists or not, so the route never reveals which ids exist.
- The response exposes only the trainer's public profile (`id`, `email`, `name`, `surname`, `role`), the same `UserProfileResponse` used for `athlete` and `trainer` in workout responses.

## Business Identifiers And Mutability

- No new identifiers. `userId` and `trainer.id` are existing user `Id`s.

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## API

- `GET /users/{userId:guid}/trainer` returns `200` with the trainer as a bare `UserProfileResponse`:

  ```json
  { "id": "...", "email": "...", "name": "...", "surname": "...", "role": "TRAINER" }
  ```

- When there is no trainer it returns `204 No Content` with an empty body. The controller returns `NoContent()` explicitly rather than relying on `Ok(null)` being turned into `204`, so the behaviour does not depend on output-formatter configuration.
- `[ProducesResponseType]` declares `200` (`UserProfileResponse`), `204`, `401`, `403` (`ProblemDetails`) and `404` (`ProblemDetails`).
- `GET /users/me`, `PUT /users/me` and `UserProfileResponse` do not change.

## Out Of Scope

- Letting a trainer read who trains a given user.
- Pending invitations or relationship history.
- Changes to the `/users/me` routes.
- Front-end changes (regenerating the contract and showing the trainer).

## Testing

- **Unit:** `GetUserTrainerUseCaseTests`:
  - a `USER` asking about themselves with an active relationship gets the trainer;
  - a `USER` without a relationship, or with only an ended one, gets no trainer (`null` from the use case);
  - a `USER` asking about another id is refused before any repository call;
  - a `TRAINER` is refused, even for their own id;
  - an `ADMIN` gets the trainer of an athlete, and no trainer for a trainer or admin target without querying relationships;
  - an `ADMIN` asking about an unknown id gets `UserNotFoundException`;
  - a deactivated trainer is still returned.
- **Integration:** a new `UserTrainerEndpointTests`, mirroring the `GET /users/{userId}/mesocycles` cases. Cover:
  - `200` with the seeded trainer for the seeded user asking about themselves;
  - `204` with an empty body for a user with no relationship and for one whose relationship has ended (seeded ended relationship);
  - `200` for an admin asking about the seeded user, and `204` for an admin asking about the trainer;
  - `403` for the user asking about another user, existing and unknown;
  - `403` for the trainer asking about the seeded user and about themselves;
  - `404` for an admin asking about an unknown id;
  - `401` without a token and with an invalid token.
- Run the full `dotnet test WeightTracker.slnx` with Docker, because the change adds an endpoint.

## Notes

- **Decided:** no trainer is `204 No Content` with an empty body, not a `200` wrapper with `trainer: null`. Clients treat `204` as "this user has no trainer".
- Reuse `ITrainerAthleteRepository.GetAllActiveByUserIds([userId])` rather than adding a new repository method. If the plan prefers a dedicated `GetActiveByUserId`, that needs a new repository method and test.
- The use case returns `User?`, the trainer or `null`. The controller maps a trainer to `UserProfileResponse.From(trainer)` with `Ok`, and `null` to `NoContent()`.
- Register `GetUserTrainerUseCase` in `Program.cs`, and mention the new route in the `UsersController` summary.
- No deviations from the mapped template references are allowed.
