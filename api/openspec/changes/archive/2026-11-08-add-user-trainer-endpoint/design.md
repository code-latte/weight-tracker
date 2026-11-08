# Design

## Context

See proposal.md for the motivation. The current code that shapes the approach:

- `UsersController` already serves a user-scoped read, `GET /users/{userId}/mesocycles`:
  - `[RequireRole(Role.TRAINER, Role.ADMIN)]`;
  - its catch delegates to `ExceptionMapper.Map`;
  - `GetUserMesocyclesUseCase` checks the role, then loads the target with `IUserRepository.GetById` (`UserNotFoundException`, 404).
- `GetAllUsersUseCase` already resolves "the trainer of a user's active relationship":
  - `ITrainerAthleteRepository.GetAllActiveByUserIds`, then `IUserRepository.GetAllByIds`;
  - it only queries relationships for `Role.USER`.
- The trainer-athletes rules guarantee at most one active relationship per athlete.
- `RequireRoleAttribute` rejects other roles with 403 after the caller is resolved, so a deleted caller still gets 401.

## Goals / Non-Goals

**Goals:**
- Keep the permission rule in Application, with the role attribute only as a coarse gate, as for the mesocycles route.
- Reuse existing repository methods. No new persistence code.

**Non-Goals:**
- No change to the Users or TrainerAthletes aggregates, mappers or repositories.
- No generic "user sub-resource authorizer" service. A single route does not justify one.

## Decisions

### Application

- **`GetUserTrainerUseCase.Execute(Id callerId, Role callerRole, Id userId, CancellationToken)` returns `Task<User?>`.** The steps are:
  1. **Permission.** The caller must be an `ADMIN`, or a `USER` whose `callerId == userId`. Anyone else gets `UnauthorizedOperationException` ("GET /users/{userId}/trainer"). This runs before any repository call, so a `USER` probing other ids learns nothing.
  2. **Load the target.** `IUserRepository.GetById(userId)`, or `UserNotFoundException`. Only an admin can reach this step with an unknown id, because a `USER` asking about themselves exists by definition.
  3. **Non-athletes.** If the target's role is not `USER`, return `null` without querying relationships.
  4. **Find the relationship.** `ITrainerAthleteRepository.GetAllActiveByUserIds([userId])`. If it returns none, return `null`.
  5. **Load the trainer.** `IUserRepository.GetById(trainerId)`. If it is somehow missing, return `null` rather than fail, which matches `GetAllUsersUseCase`, where a missing trainer simply isn't shown.
- **Why `User?` and not a view.** The response is only the trainer, so the use case returns it directly. `UserWithTrainerView` would carry the target user for nothing.
- *Alternative:* add `ITrainerAthleteRepository.GetActiveByUserId(Id)`. Rejected for now: `GetAllActiveByUserIds` with one id already does the job, and a new method needs its own repository test.

### API

- **Action.** `GetTrainer(Guid userId, CancellationToken)` on `UsersController`, routed `[HttpGet("{userId:guid}/trainer")]`, with `[RequireRole(Role.USER, Role.ADMIN)]`.
  - It returns `Ok(UserProfileResponse.From(trainer))` when there is a trainer, and an explicit `NoContent()` when there isn't. It does not rely on `Ok(null)` being turned into 204 by the output formatter.
  - The catch delegates to `ExceptionMapper.Map`.
- **Declared responses.** `[ProducesResponseType<UserProfileResponse>(200)]`, `[ProducesResponseType(204)]`, `[ProducesResponseType(401)]`, `[ProducesResponseType<ProblemDetails>(403)]` and `[ProducesResponseType<ProblemDetails>(404)]`.
- **Exception mapping.** `UnauthorizedOperationException` (403) and `UserNotFoundException` (404) are already mapped, so `ExceptionMapper` does not change.
- **Docs.** Mention the route in the `UsersController` summary, next to the mesocycles one.

### Wiring

- Register `GetUserTrainerUseCase` as scoped in `Program.cs`.

## Risks / Trade-offs

- **Unusual status code.** [Trade-off] `204` for "no trainer" is a different status code for a normal answer. Clients must treat it as "none", not as an error or an empty JSON body. → Documented in the declared responses and the spec. The generated front-end contract will show both `200` and `204`.
- **A gate in two places.** [Risk] `[RequireRole]` and the use case both encode "USER or ADMIN", so they could drift. → The use case is the authority and has unit tests for every role. The attribute only keeps trainers out early, exactly like the mesocycles route.

## Migration Plan

No data or schema changes. Deploy the API. To roll back, deploy the previous build.
