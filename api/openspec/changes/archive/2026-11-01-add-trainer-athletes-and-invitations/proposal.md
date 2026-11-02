# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-trainer-athletes-and-invitations.md`.

## Why

Trainers and athletes have no relationship today, so `GET /users` answers `501` for trainers through
`TrainerAthletesNotImplementedException`. Trainers need to invite athletes and see who they prepare. An athlete needs a
single active trainer at a time, and the history of past relationships must be kept so the same pair can work together
again later.

## What Changes

- New domain area **TrainerAthletes**: aggregate `TrainerAthlete` (`TrainerId`, `UserId`, `Status`, `StartedAtUtc`,
  `FinishedAtUtc`) with `End`, and enum `TrainerAthleteStatus` (`ACTIVE`, `ENDED`).
- New domain area **TrainerInvitations**: aggregate `TrainerInvitation` (`TrainerId`, `InviteEmail`, optional `UserId`,
  `TokenHash`, `Status`, `CreatedAtUtc`, `ExpiresAtUtc`, optional `AcceptedAtUtc`) with `Revoke`, `Accept` and
  `Reject`, and enum `TrainerInvitationStatus` (`PENDING`, `ACCEPTED`, `REJECTED`, `REVOKED`).
- New endpoints:
  - `POST /athlete-invitation` (`TRAINER`): invite an email; returns the raw invitation token once.
  - `PUT /athlete-invitation/{invitationId}/revoke` (the inviting `TRAINER`).
  - `PUT /athlete-invitation/{invitationId}/accept` and `/reject` (the invited `USER`, body `{ "token": "..." }`).
  - `GET /athlete-invitation` (`ADMIN`: every invitation; `TRAINER`: only their own; `USER`: `403`).
  - `POST /trainer-athletes/{trainerId}` (`ADMIN`, body `{ "athleteId": "..." }`): link directly.
  - `PUT /trainer-athletes/{trainerId}/end` (`ADMIN`, that trainer or that athlete, body `{ "athleteId": "..." }`).
  - `GET /trainer-athletes` (`TRAINER`): own active athletes.
  - `GET /trainer-athletes/{trainerId}` (`ADMIN`): that trainer's active athletes.
- Two FluentMigrator migrations: `TrainerAthletes` and `TrainerInvitations` tables.
- **BREAKING**: `GET /users` becomes admin-only. A `TRAINER` now gets `403` instead of `501`; trainers use
  `GET /trainer-athletes`. `TrainerAthletesNotImplementedException` and its mapping are removed.
- Accepting an invitation does **not** create the relationship. No domain events are raised (they stay dormant).

## Capabilities

### New Capabilities
- `trainer-athletes`: the trainer–athlete relationship lifecycle (link, end, list), its role and uniqueness rules, and
  who may act on it.
- `trainer-invitations`: trainers inviting an email to become their athlete, and the invitation lifecycle (revoke,
  accept, reject, expiry, token check).

### Modified Capabilities
- `user-administration`: `GET /users` is restricted to `ADMIN`; the "Trainer listing is not implemented yet"
  requirement is removed.

## Canonical Template References

| Capability / piece | Reference | Why |
| --- | --- | --- |
| `TrainerAthlete` aggregate | `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs` | `Create` / `Rehydrate` / `IsNew`, status-guarded action |
| `TrainerInvitation` aggregate | `src/WeightTracker.Domain/Auth/RefreshToken.cs` | Stores only a token hash, expiry, revocation |
| Status enums | `src/WeightTracker.Domain/Workouts/WorkoutStatus.cs` | Persisted status enum |
| Invitation token generation/hash | `ITokenService.GenerateRefreshToken` / `JwtTokenService` | Raw value only in the response, SHA-256 hash stored |
| Mutating use cases | `src/WeightTracker.Application/UseCases/Workouts/ChangeWorkoutStatusUseCase.cs` | Load, authorize, act on aggregate, save, commit |
| Read use case | `src/WeightTracker.Application/UseCases/Exercises` | One `Execute`, read only |
| User role/existence checks | `src/WeightTracker.Application/Services/WorkoutAssignmentValidator.cs` | Validates the users being linked |
| Repositories / mappers | `src/WeightTracker.Infra/Repositories/WorkoutRepository.cs`, `src/WeightTracker.Infra/Mappers/WorkoutMapper.cs` | `IsNew` branching, mapping inside repositories |
| Migrations | `src/WeightTracker.Migrations/Migrations/202610080952_CreateRefreshTokensTable.cs` | Table with FK to `Users`, unique hash, `Down()` |
| Controllers | `src/WeightTracker.Api/Endpoints/Workouts/WorkoutsController.cs` + `AuthorizedController` + `RequireRoleAttribute` | `CurrentUser`, `ExceptionMapper.Map` in every catch |
| `GET /users` restriction | `src/WeightTracker.Api/Endpoints/Users/UsersController.cs` (`POST /users`) | Existing admin-only route |
| Endpoint tests | `.stpr/template/test/WeightTracker.Integration/Auth/AuthEndpointsTests`, `.stpr/template/test/WeightTracker.Integration/Workouts/WorkoutScopingTests` | `401` matrix; scoped reads asserting exclusions |
| Aggregate unit tests | `.stpr/template/test/WeightTracker.Unit/Domain/Workouts/WorkoutTests` | Arrange/Act/Assert, invariants, factories |

Both new domain areas are mutable aggregates, not catalogs: their state changes through use even where only `GET` is
exposed.

## Business Identifiers And Mutability

- `TrainerAthlete.Id` and `TrainerInvitation.Id`: technical identifiers from `Id.New()` in `Create`; immutable.
- `TrainerAthlete.TrainerId` / `UserId` and `TrainerInvitation.TrainerId`: references to `User` by `Id`; immutable.
- `TrainerInvitation.InviteEmail`: `Email` value object; immutable.
- `TrainerInvitation.UserId`: set at creation when the email is a registered `USER`, otherwise `null`; not changed by
  this change.
- `TrainerInvitation.TokenHash`: unique; immutable.
- `(TrainerId, UserId)` on `TrainerAthlete` is **not** unique: a pair can be linked again once previous relationships
  are `ENDED`. At most one `ACTIVE` relationship per athlete.

## Conflicts With Template Or Invariants

- None blocking. Two clarifications against the brief:
  - The brief named tables and columns in snake_case; the repository uses PascalCase (`"RefreshTokens"`, `"UserId"`),
    so the tables are `TrainerAthletes` and `TrainerInvitations` with PascalCase columns.
  - `User.CanTrain()` accepts `ADMIN` for workouts; this change requires role `TRAINER` exactly. A new aggregate
    question is added instead of reusing `CanTrain()`.
- Domain events stay dormant per `STPR_INVARIANTS.md`: no `AddDomainEvent` calls and no dispatcher.

## Out Of Scope

- Creating the relationship when an invitation is accepted (future `InvitationAccepted` event running
  `LinkAthleteToTrainerUseCase`).
- Linking an invitation's `UserId` when the invited email signs up (future signup event).
- Sending emails or notifications; listing or fetching invitations; background expiry.
- Using the relationship in workout authorization.
- Any front change.

## Impact

- **Domain**: `Domain/TrainerAthletes`, `Domain/TrainerInvitations`, new domain exceptions, a role question on `User`.
- **Application**: seven use cases in `UseCases/TrainerInvitations` and `UseCases/TrainerAthletes`, two repository
  contracts, an invitation token port, new application exceptions; `GetAllUsersUseCase` loses its trainer branch and
  `callerId`; `TrainerAthletesNotImplementedException` deleted; `IUserRepository.GetAllByIds` added.
- **Infra**: EF entities, configuration, repositories, mappers, invitation token service and options; seed data.
- **Migrations**: two new migrations.
- **API**: two controllers with DTOs, `ExceptionMapper` entries, `GET /users` admin-only, new config key
  `TrainerInvitations:ExpirationDays`.
- **Tests**: unit (aggregates, use cases, mappers), integration (every route's auth matrix, scoped reads, repositories,
  migrations, OpenAPI document), and `GET /users` updates.
- **Clients**: OpenAPI changes; trainers calling `GET /users` must switch to `GET /trainer-athletes`.
