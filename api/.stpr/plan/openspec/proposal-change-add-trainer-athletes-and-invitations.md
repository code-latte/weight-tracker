## Change

Add the trainer–athlete relationship and the trainer invitation system

## Objective

Today there is no link between a trainer and the athletes they prepare. `GetAllUsersUseCase` even throws
`TrainerAthletesNotImplementedException` for trainers because of it. This change introduces two domain areas,
and trainers will list their athletes through the new `GET /trainer-athletes` instead of `GET /users`:

- **TrainerAthletes**: the relationship itself, with a lifecycle (active, then ended), so a trainer can
  have many athletes over time and an athlete has at most one trainer at a time.
- **TrainerInvitations**: the way a trainer asks someone (registered or not) to become their athlete, and the way
  that person accepts or rejects it.

Both areas are independent aggregates. Accepting an invitation does **not** create the relationship in this
change: that will be wired later through domain events (see Out Of Scope).

## Capabilities

- `POST /athlete-invitation`: a trainer invites an email to become their athlete.
- `PUT /athlete-invitation/{invitationId}/revoke`: the inviting trainer revokes a pending invitation.
- `PUT /athlete-invitation/{invitationId}/accept`: the invited user accepts, sending the invitation token in the body.
- `PUT /athlete-invitation/{invitationId}/reject`: the invited user rejects, sending the invitation token in the body.
- `POST /trainer-athletes/{trainerId}`: an admin links an athlete (`UserId` in the body) to a trainer directly.
- `PUT /trainer-athletes/{trainerId}/end`: ends the active relationship between that trainer and the athlete
  (`AthleteId` wrapped in the body, e.g. `{ "athleteId": "..." }`). Callable by an admin, the trainer or the athlete.
- `GET /trainer-athletes`: a trainer lists their own athletes.
- `GET /trainer-athletes/{trainerId}`: an admin lists the athletes of a given trainer.
- `GET /users` becomes admin-only: the not-implemented trainer branch of `GetAllUsersUseCase` is removed.

## Module Classification

Domain areas, not modules (the architecture is layer-first; each area is a slice across the layer projects).

- TrainerAthletes (domain) -> mutable aggregate -> `.stpr/template/src/WeightTracker.Domain/Workouts` (aggregate shape) + `src/WeightTracker.Domain/Workouts/WorkoutStatus.cs` (status enum)
- TrainerInvitations (domain) -> mutable aggregate holding a hashed secret -> `src/WeightTracker.Domain/Auth/RefreshToken.cs` (only the hash is stored, expiry, revocation)
- TrainerAthletes / TrainerInvitations (application) -> use cases + business authorization -> `src/WeightTracker.Application/UseCases/Workouts` and `src/WeightTracker.Application/Services/WorkoutStatusAuthorizer.cs`
- TrainerAthletes / TrainerInvitations (infra) -> new repositories, mappers, EF entities -> `src/WeightTracker.Infra/Repositories/WorkoutRepository.cs`, `src/WeightTracker.Infra/Mappers/WorkoutMapper.cs`
- TrainerAthletes / TrainerInvitations (API) -> REST controllers with protected routes -> `src/WeightTracker.Api/Endpoints/Workouts/WorkoutsController.cs`, `AuthorizedController`, `RequireRoleAttribute`

- Users (application + API) -> existing mutable aggregate, read path narrowed -> `src/WeightTracker.Application/UseCases/Users/GetAllUsersUseCase.cs`, `src/WeightTracker.Api/Endpoints/Users/UsersController.cs`

Neither area is a catalog. Both hold business state that changes through use (status transitions, end dates,
acceptance dates). `GET` endpoints over them are reads of mutable aggregates.

## Canonical Template Mapping

- `TrainerAthlete` aggregate -> `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs` -> `Create` / `Rehydrate` / `IsNew`, private constructor, status-guarded action method (`End`) like `Workout.ChangeStatus`
- `TrainerAthleteStatus` enum -> `src/WeightTracker.Domain/Workouts/WorkoutStatus.cs` -> nearest persisted status enum
- `TrainerInvitation` aggregate -> `src/WeightTracker.Domain/Auth/RefreshToken.cs` -> nearest aggregate that stores only a token hash, has an expiry and is revoked through behaviour
- `TrainerInvitationStatus` enum -> `src/WeightTracker.Domain/Workouts/WorkoutStatus.cs` -> nearest persisted status enum
- Invitation token generation and hashing -> the refresh token issuing in `src/WeightTracker.Infra/Services` -> same "raw value only in the response, hash in the database" mechanism
- Use cases -> `src/WeightTracker.Application/UseCases/Workouts/ChangeWorkoutStatusUseCase.cs` (mutations) and `src/WeightTracker.Application/UseCases/Exercises` (reads) -> one public `Execute`, load, authorize, act on aggregate, save, `SaveChangesAsync`
- Role/user existence checks -> `src/WeightTracker.Application/Services/WorkoutAssignmentValidator.cs` -> nearest application service that validates the users being linked
- Repositories and mappers -> `src/WeightTracker.Infra/Repositories/WorkoutRepository.cs` + `src/WeightTracker.Infra/Mappers/WorkoutMapper.cs` -> repository branching on `IsNew`, mapping kept inside repositories
- Migrations -> `src/WeightTracker.Migrations/Migrations/202610080952_CreateRefreshTokensTable.cs` and `202610080931_CreateWorkoutsTable.cs` -> create table with FK to `users`, indexes and a reversing `Down()`
- Controllers -> `src/WeightTracker.Api/Endpoints/Workouts/WorkoutsController.cs` -> inherits `AuthorizedController`, `CurrentUser` to the use case, every catch through `ExceptionMapper.Map`
- Endpoint tests -> `.stpr/template/test/WeightTracker.Integration/Auth/AuthEndpointsTests` (401 matrix) and `.stpr/template/test/WeightTracker.Integration/Workouts/WorkoutScopingTests` (scoped reads, asserting exclusions)
- Aggregate unit tests -> `.stpr/template/test/WeightTracker.Unit/Domain/Workouts/WorkoutTests` -> Arrange/Act/Assert, invariants, factory and rehydrate

## State And Mutability

- New table `trainer_athletes`: `id`, `trainer_id` (FK `users`), `user_id` (FK `users`), `status`, `started_at_utc`,
  `finished_at_utc` (nullable).
- New table `trainer_invitations`: `id`, `trainer_id` (FK `users`), `invite_email`, `user_id` (nullable FK `users`),
  `token_hash`, `status`, `created_at_utc`, `expires_at_utc`, `accepted_at_utc` (nullable).
- `TrainerAthleteStatus`: `ACTIVE`, `ENDED`. Created as `ACTIVE`; `End` moves it to `ENDED` and sets `FinishedAtUtc`.
- `TrainerInvitationStatus`: `PENDING`, `ACCEPTED`, `REJECTED`, `REVOKED`. Created as `PENDING`. Expiry is not a
  persisted status: an invitation past `ExpiresAtUtc` is expired by time, and the aggregate refuses to act on it.
- `users` is read only to check roles and resolve emails; it is not modified.
- Two FluentMigrator migrations, one per table. No data backfill.

## Business Rules

### TrainerAthletes

- The trainer must have role `TRAINER`. `ADMIN` does not qualify here, unlike `User.CanTrain()` for workouts.
- The athlete must have role `USER`.
- A trainer can have several active athletes.
- An athlete can have at most one `ACTIVE` relationship, with any trainer.
- A new relationship between the same trainer and athlete is allowed only when every previous one is `ENDED`.
  History is kept: ending never deletes the row.
- `End` is only valid on an `ACTIVE` relationship; ending an already `ENDED` one is a domain error (409).
- `PUT /trainer-athletes/{trainerId}/end` ends the single `ACTIVE` relationship for that trainer and athlete; if
  there is none, `404`.
- The "one active relationship per athlete" rule spans aggregates, so it is checked in Application before
  creating, and backed by a partial unique index on `trainer_athletes(user_id) WHERE status = 'ACTIVE'`.

### TrainerInvitations

- Only a `TRAINER` can send invitations.
- The invited email must be valid (`Email` value object).
- If the email belongs to a registered `USER`, the invitation stores that user's `UserId`. If it is not
  registered, `UserId` stays `null`.
- If the email belongs to a registered `TRAINER` or `ADMIN`, the invitation is rejected (409).
- A random token is generated on invite. Only its hash is stored in `TokenHash`; the raw token is returned once in
  the `POST` response, as refresh tokens are.
- `ExpiresAtUtc` = `CreatedAtUtc` + a configurable lifetime read from `appsettings.json`
  (e.g. `TrainerInvitations:ExpirationDays`). Never hardcoded.
- `Revoke`, `Accept` and `Reject` are only valid on a `PENDING`, non-expired invitation. Any other state is a
  domain error (409). The aggregate protects the transition, not only the final state.
- `Accept` sets `AcceptedAtUtc`. `Reject` and `Revoke` only change `Status`.
- `Accept` and `Reject` require the raw token in the body; its hash must match `TokenHash`. A mismatch is
  rejected without revealing which part failed.
- Accepting an invitation does **not** create a `TrainerAthlete`. No domain event is raised in this change
  (domain events are dormant per `STPR_INVARIANTS.md`).

### Users listing

- `GetAllUsersUseCase` loses its `Role.TRAINER` branch. A trainer calling `GET /users` now gets `403`
  (`UnauthorizedOperationException`), like a `USER`. Trainers use `GET /trainer-athletes` instead.
- `UsersController` `GET /users` changes `[RequireRole(Role.ADMIN, Role.TRAINER)]` to `[RequireRole(Role.ADMIN)]`.
- The `callerId` parameter of `GetAllUsersUseCase.Execute`, kept only for the future trainer scope, is removed
  together with its doc comment.
- `TrainerAthletesNotImplementedException` is deleted, together with its `ExceptionMapper` entry and the tests
  that reference it (`GetAllUsersUseCaseTests`, `ExceptionMapperTests`).

## Roles And Visibility

| Route | ADMIN | TRAINER | USER |
| --- | --- | --- | --- |
| `POST /athlete-invitation` | 403 | yes | 403 |
| `PUT /athlete-invitation/{id}/revoke` | 403 | only the inviting trainer, else 403 | 403 |
| `PUT /athlete-invitation/{id}/accept` | 403 | 403 | only the invited user (`invitation.UserId == caller`), else 403 |
| `PUT /athlete-invitation/{id}/reject` | 403 | 403 | only the invited user (`invitation.UserId == caller`), else 403 |
| `POST /trainer-athletes/{trainerId}` | yes | 403 | 403 |
| `PUT /trainer-athletes/{trainerId}/end` | yes | only when `trainerId` is the caller, else 403 | only when the body `AthleteId` is the caller, else 403 |
| `GET /trainer-athletes` | 403 | own athletes | 403 |
| `GET /trainer-athletes/{trainerId}` | yes | 403 | 403 |

- Every route: `401` without token and with an invalid token.
- An invitation whose `UserId` is `null` cannot be accepted or rejected by anyone yet (403), since linking on
  signup is out of scope.
- Permission checks run before state checks, so a caller who may not act on an invitation or relationship gets
  `403` and never learns its status.
- `GET` lists return only `ACTIVE` relationships. Ended relationships and other trainers' athletes are excluded.
- Responses never expose `TokenHash`.

## Business Identifiers And Mutability

- `TrainerAthlete.Id` is the technical identifier, generated by `Id.New()` in `Create`, immutable.
- `TrainerInvitation.Id` is the technical identifier, generated by `Id.New()` in `Create`, immutable.
- `TrainerAthlete.TrainerId` / `UserId` and `TrainerInvitation.TrainerId` reference `User` by `Id`, immutable.
- `TrainerInvitation.InviteEmail` uses the `Email` value object and is immutable.
- `TrainerInvitation.UserId` is set at creation (registered user) or left `null`; it does not change in this
  change. A future signup-linking change must define how it becomes non-null.
- `TrainerInvitation.TokenHash` is unique and immutable.
- `(TrainerId, UserId)` is **not** unique: repeated relationships are allowed once previous ones are `ENDED`.

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## Out Of Scope

- Creating the `TrainerAthlete` when an invitation is accepted. It will be done later by an `InvitationAccepted`
  domain event handled by a dispatcher that runs `LinkAthleteToTrainerUseCase`. No dispatcher and no
  `AddDomainEvent` call in this change.
- Linking a pending invitation's `UserId` when the invited email signs up. It will be done later by a signup
  event.
- Sending invitation emails or any notification.
- Listing invitations (sent or received) and getting one by id.
- Expiring invitations through a background job; expiry is evaluated on use.
- Using the relationship in workout authorization (e.g. restricting which athletes a trainer can prescribe to).
- Any front change.

## Notes

- Domain: `src/WeightTracker.Domain/TrainerAthletes/` (`TrainerAthlete`, `TrainerAthleteStatus`) and
  `src/WeightTracker.Domain/TrainerInvitations/` (`TrainerInvitation`, `TrainerInvitationStatus`).
- Application use cases, as requested:
  - `UseCases/TrainerInvitations/`: `InviteAthleteUseCase`, `AcceptInvitationUseCase`, `RejectInvitationUseCase`,
    `RevokeInvitationUseCase`.
  - `UseCases/TrainerAthletes/`: `LinkAthleteToTrainerUseCase`, `EndTrainerAthleteRelationshipUseCase`,
    `GetTrainerAthletesUseCase` (shared by both `GET` routes; the controller passes the trainer id).
  - `LinkAthleteToTrainerUseCase` must be callable without an HTTP caller in mind, since the future event handler
    will reuse it. Admin authorization for the `POST` route is decided at the controller/use-case boundary
    accordingly; settle the exact split in the proposal.
- New repository contracts in `Application/Interfaces`: `ITrainerAthleteRepository`
  (`GetActiveByTrainerAndUser`, `ExistsActiveForUser`, `GetActiveByTrainer`, `Save`) and
  `ITrainerInvitationRepository` (`GetById`, `Save`). Exact names at proposal time.
- New exceptions (expected), each with an explicit `ExceptionMapper` entry: `TrainerAthleteNotFoundException` (404),
  `TrainerInvitationNotFoundException` (404), `AthleteAlreadyHasTrainerException` (409),
  `InvalidTrainerAthleteStatusTransitionException` (409), `InvalidTrainerInvitationStatusTransitionException`
  (409), `TrainerInvitationExpiredException` (409), `InvalidInvitationTokenException` (403 or 400, decide at proposal
  time), `InvalidTrainerAthleteRoleException` (409 for wrong trainer/athlete role). Reuse `UserNotFoundException`
  and `UnauthorizedOperationException` where they fit.
- Route naming: the request used `/ahtlete-invitation` and `/trainer-athlete/{trainerId}`. Treated as typos and
  normalized to `/athlete-invitation` and `/trainer-athletes/{trainerId}`.
- Ending a relationship is a state change (`ACTIVE -> ENDED`), not a deletion: the row is kept, so the route is
  `PUT /trainer-athletes/{trainerId}/end` with `{ "athleteId": "..." }` in the body.
- The accept/reject body carries the raw token (e.g. `{ "token": "..." }`), not the hash. The server hashes it and
  compares with `TokenHash`.
- Deactivated users: a deactivated trainer cannot invite and a deactivated trainer or athlete cannot be linked
  (409). Confirm at proposal time.
- Duplicate pending invitations from the same trainer to the same email: rejected with 409. Inviting an athlete
  who already has an `ACTIVE` relationship: rejected with 409. Confirm at proposal time.
- Register both repositories, all use cases and any new services in `Program.cs`. Add the new aggregates to the
  integration test truncate list.
- Seed: add at least one `TRAINER`–`USER` active relationship, one ended relationship and one pending invitation
  in `SeedData` / `DatabaseSeeder` so scoped reads and transitions can be tested from the baseline.
- Tests to update: `GetAllUsersUseCaseTests` (trainer caller now expects `UnauthorizedOperationException`),
  `ExceptionMapperTests` (drop the removed exception case), and the `GET /users` integration tests (trainer gets `403`).
- Tests to add:
  - Unit, domain: `TrainerAthlete` create/rehydrate/`End` (and ending twice); `TrainerInvitation`
    create/rehydrate, every valid and invalid transition, expired invitation, token mismatch.
  - Unit, application: each use case with mocked repositories, covering role checks, ownership checks, the one
    active relationship rule, registered vs unregistered email.
  - Unit, infra: both mappers round-trip.
  - Integration, per route: `401` without token and with invalid token; `403` for every forbidden role and for
    non-owners; `404` for missing invitation/relationship; `409` for invalid transitions, expired invitation,
    athlete already linked, wrong roles; `400` for structural errors; happy paths with persisted state checked.
    `GET` routes assert excluded athletes (ended relationships, other trainers').
- OpenAPI snapshot changes (new paths); the front refreshes it in its own plan.
- No deviations from the mapped template references are allowed.
