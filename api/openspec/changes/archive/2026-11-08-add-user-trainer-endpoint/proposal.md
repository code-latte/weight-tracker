# Proposal

## Why

The app needs to know who an athlete's trainer is, for example to show "Your trainer: …" right after login or to decide whether trainer-only flows apply. Today the athlete can only work it out indirectly, from the `trainer` of a prescribed workout. Admins can only see it by listing every user. A direct lookup for one user removes both workarounds.

Source brief: `.stpr/plan/openspec/proposal-change-add-user-trainer-endpoint.md`.

## What Changes

- New route `GET /users/{userId}/trainer`:
  - `200` with the public profile of the trainer of that user's active trainer-athlete relationship;
  - `204 No Content` with an empty body when the user has no active trainer;
  - a `USER` may call it only for their own id, and an `ADMIN` for any user. Trainers are refused (`403`), even for their own id;
  - an admin asking about an unknown id gets `404`. A `USER` asking about any other id gets `403`, whether it exists or not.
- No change to `/users/me`, `UserProfileResponse`, the domain or persistence.

## Capabilities

### New Capabilities

- `user-trainer`: looking up the current trainer of one user: who may ask, what is returned, and when the answer is "no trainer".

### Modified Capabilities

- None. `trainer-athletes` keeps owning how relationships are created, ended and listed. This change only reads the active relationship.

### Canonical template references

- Route and controller action -> `GET /users/{userId}/mesocycles` in `UsersController`: the same user-scoped read sub-resource, with a `[RequireRole]` gate and 403/404 responses.
- `GetUserTrainerUseCase` -> `GetUserMesocyclesUseCase` for the shape (explicit caller and target, permission before lookup). It follows `GetAllUsersUseCase` for resolving the trainer of the active relationship.
- Response -> the existing `UserProfileResponse`, already the public profile used for `trainer` in `UserListItemResponse` and in workout responses.
- Tests -> `GetUserMesocyclesUseCaseTests` (unit) and the `GET /users/{userId}/mesocycles` cases in `MesocycleReadEndpointsTests` (integration).

Users and TrainerAthletes are mutable aggregates. This change only reads them, so they are not catalogs and gain no mutation.

### Business identifiers

- No new identifiers. `userId` and the trainer's `id` are existing user `Id`s and stay immutable.

## Impact

- **Application:** new `GetUserTrainerUseCase` in `UseCases/Users`. It reuses `IUserRepository.GetById` / `GetAllByIds` and `ITrainerAthleteRepository.GetAllActiveByUserIds`, so no repository changes are needed.
- **API:** a new action in `UsersController`, an updated controller summary, and registration in `Program.cs`.
- **No domain, persistence, migration or seed changes.**
- **Front-end:** the generated contract gains one route, in a separate front change.

### Conflicts with the template or invariants

- None. The route follows the business authorization pattern in `STPR_INVARIANTS.md`: permission in Application, checked before the existence lookup for callers who may only see themselves.
- Answering "no trainer" with `204` is a new response style for this API. Existing reads return `200` with a body or `404`. This is a deliberate choice recorded in the brief.

### Out of scope

- Letting a trainer read who trains a given user.
- Pending invitations or relationship history.
- Changes to the `/users/me` routes.
- Front-end work.
