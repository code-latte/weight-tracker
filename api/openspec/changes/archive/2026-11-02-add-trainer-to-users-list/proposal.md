# Proposal

## Why

The backoffice needs an admin screen ("Asignación de entrenadores") that lists every athlete and shows whether they have a trainer right now, so the admin can assign one or end the current relationship. Today `GET /users` returns no relationship data and `GET /trainer-athletes/{trainerId}` is scoped per trainer, so the front would have to call it once per trainer and join the results itself. The API already enforces "an athlete has at most one `ACTIVE` relationship", so it should answer the question directly.

## What Changes

- `GET /users` (`ADMIN` only) returns, on every item, a new `trainer` property:
  - `null` for users whose role is `TRAINER` or `ADMIN`;
  - `null` for a `USER` with no `ACTIVE` relationship;
  - the trainer's public profile (`id`, `email`, `name`, `surname`, `role`) for a `USER` with an `ACTIVE` relationship, whatever the trainer's own activation state.
- `GET /users` items get their own response type (the existing summary fields plus `trainer`). `PUT /users/{id}/activate` and `PUT /users/{id}/deactivate` keep returning the current summary shape, without `trainer`.
- Route, role gate, order (email ascending) and `401` / `403` behaviour are unchanged.
- The data is read with a bounded number of queries, never one per user.
- The OpenAPI document changes for the `GET /users` item schema. Additive for clients; not breaking.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `user-administration`: the listed-user shape gains `trainer`, and a new requirement defines how an athlete's current trainer is resolved. Canonical template reference: `GetTrainerAthletesUseCase` + `TrainerAthleteView` / `TrainerAthleteViewLoader` for joining relationships with user data in Application; existing `TrainerAthleteRepository` reads for the new repository method; `UserSummaryResponse.From` and `UserProfileResponse.From(User)` for the DTOs.

## Domain Areas And Template Mapping

| Area | Classification | Change | Template reference |
| --- | --- | --- | --- |
| Users (application) | mutable aggregate, read use case | `GetAllUsersUseCase` returns a read model per user with its current trainer | `UseCases/TrainerAthletes/GetTrainerAthletesUseCase.cs`, `TrainerAthleteView.cs` |
| TrainerAthletes (port / infra) | mutable aggregate, existing repository | new read: active relationships for a set of athletes | existing `ITrainerAthleteRepository` / `TrainerAthleteRepository` reads (`AsNoTracking`, mapping inside the repository) |
| Users (API) | REST controller, existing action | new list item DTO, `[ProducesResponseType]` updated | `Endpoints/Users/Dtos/UserSummaryResponse.cs`, `UserProfileResponse.cs` |
| Users / TrainerAthletes (domain) | — | no change | — |

Neither area is a catalog: `User` and `TrainerAthlete` keep changing through their own endpoints. This change only adds read data to an existing read path. No conflict with `.stpr/template/STPR_INVARIANTS.md` or the template was found.

## Business Identifiers

- `User.Id` and `User.Email`: immutable for this change; not affected.
- `TrainerAthlete.Id`: immutable; not exposed or affected.

## Out Of Scope

- Changes to `GET /trainer-athletes`, `GET /trainer-athletes/{trainerId}`, `POST /trainer-athletes/{trainerId}` or `PUT /trainer-athletes/{trainerId}/end`.
- Relationship history, start date or relationship id on `GET /users`.
- Filtering, search or pagination on `GET /users`.
- `GET /users/me`, `POST /users`, activate / deactivate responses.
- Any front change (separate plan in `front/` after the OpenAPI snapshot refresh).

## Acceptance Criteria

- An `ADMIN` calling `GET /users` gets every user, ordered by email, each with `trainer` set as described above; an `ENDED` relationship never produces a trainer.
- The nested `trainer` contains exactly `id`, `email`, `name`, `surname`, `role`.
- `USER` / `TRAINER` callers still get `403`; missing or invalid token still gets `401`.
- Activate / deactivate responses are unchanged.
- No migration; the full `dotnet test WeightTracker.slnx` passes with Docker running.

## Impact

- Code: `GetAllUsersUseCase` (+ new read model), `ITrainerAthleteRepository` / `TrainerAthleteRepository`, `UsersController.GetAll`, new list item DTO.
- Database: reads `trainer_athletes` in addition to `users`; no schema change. The partial unique index on `trainer_athletes(user_id) WHERE status = 'ACTIVE'` already guarantees one active relationship per athlete.
- API contract / OpenAPI: `GET /users` item schema gains nullable `trainer`.
- Tests: unit `GetAllUsersUseCaseTests`, integration `ListUsersEndpointTests`, `TrainerAthleteRepositoryTests`, `OpenApiDocumentTests`.
