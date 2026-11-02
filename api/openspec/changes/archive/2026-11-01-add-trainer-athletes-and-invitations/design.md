# Design

## Context

See `proposal.md` for motivation and `specs/` for the required behavior. Current state that shapes the approach:

- `RefreshToken` (`Domain/Auth`) is the nearest aggregate to an invitation: it stores only a SHA-256 hex hash
  (`TokenHashMaxLength`, 128), has an absolute expiry and is revoked through behaviour. `JwtTokenService` generates
  the raw value with `RandomNumberGenerator` and hashes it; lifetime comes from `JwtOptions.RefreshTokenDays`.
- `Workout.ChangeStatus` is the nearest status-guarded action; status enums are stored as `int` (`AddStatusToWorkouts`).
- Tables and columns are PascalCase (`"RefreshTokens"`, `"UserId"`), not snake_case as the brief wrote.
- `User.CanTrain()` returns true for `TRAINER` and `ADMIN`; it is used for workout prescription and stays unchanged.
- `RequireRoleAttribute` on a route answers `401` before `403`, and `POST /users` already uses it for admin-only
  access. `AuthorizedController.CurrentUser` gives the use case the caller id and role.
- `IUserRepository` has `GetById`, `GetByEmail`, `ExistsByEmail`, `GetAll`, `Save`. No batch lookup.
- `ApiFactory.ResetDatabase` truncates a fixed list of tables and reseeds through `DatabaseSeeder`.
- Domain events are dormant (`STPR_INVARIANTS.md`).

## Goals / Non-Goals

**Goals:**

- Keep each lifecycle rule (transition and expiry) inside its aggregate; keep cross-aggregate rules (roles, one active
  trainer per athlete, duplicate pending invitation) in Application.
- Make `LinkAthleteToTrainerUseCase` callable without an HTTP caller, so a future event handler can reuse it.
- Back the "one active trainer per athlete" rule with the database, not only with a pre-check.

**Non-Goals:**

- A generic token service shared by refresh tokens and invitations. Refresh token code is not touched.
- Storing an `EXPIRED` status or running a background expiry job.

## Decisions

### Layer mapping

| Layer | Change |
| --- | --- |
| Domain | `TrainerAthletes/TrainerAthlete`, `TrainerAthleteStatus { ACTIVE = 1, ENDED = 2 }`; `TrainerInvitations/TrainerInvitation`, `TrainerInvitationStatus { PENDING = 1, ACCEPTED = 2, REJECTED = 3, REVOKED = 4 }`; `User.CanBeTrainer()` (`Role == TRAINER && IsActive`) and `User.CanBeAthlete()` (`Role == USER && IsActive`); `Common/HashedTokens.MaxLength` (128), which `RefreshToken.TokenHashMaxLength` and `TrainerInvitation.TokenHashMaxLength` both point to; domain exceptions `InvalidTrainerAthleteStatusTransitionException`, `InvalidTrainerInvitationStatusTransitionException`, `TrainerInvitationExpiredException`, `InvalidTrainerAthleteStateException`, `InvalidTrainerInvitationStateException` |
| Application | `UseCases/TrainerInvitations/{InviteAthlete,AcceptInvitation,RejectInvitation,RevokeInvitation}UseCase`; `UseCases/TrainerAthletes/{LinkAthleteToTrainer,EndTrainerAthleteRelationship,GetTrainerAthletes}UseCase`; `ITrainerAthleteRepository`, `ITrainerInvitationRepository`, `IInvitationTokenService`; `IUserRepository.GetAllByIds`; application exceptions `TrainerAthleteNotFoundException`, `TrainerInvitationNotFoundException`, `AthleteAlreadyHasTrainerException`, `InvalidTrainerAthleteRoleException`, `PendingInvitationAlreadyExistsException`, `InvalidInvitationTokenException`; `GetAllUsersUseCase` drops `callerId` and the trainer branch; `TrainerAthletesNotImplementedException` deleted |
| Infra | `TrainerAthleteEntity`, `TrainerInvitationEntity`, their configuration in `WeightTrackerDbContext`; `TrainerAthleteRepository`, `TrainerInvitationRepository`; `TrainerAthleteMapper`, `TrainerInvitationMapper`; `InvitationTokenService` + `TrainerInvitationOptions` (section `TrainerInvitations`); `OpaqueToken`, shared by `InvitationTokenService` and `JwtTokenService`; `UserRepository.GetAllByIds`; seed |
| Migrations | `CreateTrainerAthletesTable_<ts>`, `CreateTrainerInvitationsTable_<ts>` |
| Api | `Endpoints/TrainerInvitations/TrainerInvitationsController` (`[Route("athlete-invitation")]`) and `Endpoints/TrainerAthletes/TrainerAthletesController` (`[Route("trainer-athletes")]`), with Dtos; `ExceptionMapper` entries; `GET /users` `[RequireRole(Role.ADMIN)]`; `appsettings.json` key `TrainerInvitations:ExpirationDays` (7); `Program.cs` registrations |

Cross-area dependencies: both use case groups read `IUserRepository`. `InviteAthleteUseCase` also reads
`ITrainerAthleteRepository` to refuse inviting an already linked athlete. TrainerInvitations never writes
TrainerAthletes.

### Aggregates

- `TrainerAthlete.Create(Id trainerId, Id userId, DateTime nowUtc)` → `ACTIVE`, `StartedAtUtc = nowUtc`,
  `FinishedAtUtc = null`. `End(DateTime nowUtc)` throws `InvalidTrainerAthleteStatusTransitionException` unless
  `ACTIVE`, then sets `ENDED` and `FinishedAtUtc`. The aggregate does not know roles; it only holds ids.
- `TrainerAthlete.Rehydrate` rejects contradictory stored state with `InvalidTrainerAthleteStateException`: `ACTIVE`
  with a `FinishedAtUtc`, `ENDED` without one, or an end before the start. This mirrors `User.Rehydrate`.
- `TrainerInvitation.Create(Id trainerId, Email inviteEmail, Id? userId, string tokenHash, DateTime nowUtc,
  DateTime expiresAtUtc)` → `PENDING`. `Revoke(nowUtc)`, `Accept(nowUtc)`, `Reject(nowUtc)` share one private guard:
  not `PENDING` → `InvalidTrainerInvitationStatusTransitionException`; `nowUtc >= ExpiresAtUtc` →
  `TrainerInvitationExpiredException`. Both map to `409`. `Accept` sets `AcceptedAtUtc`.
- `TrainerInvitation.Create` and `Rehydrate` share two guards that throw `InvalidTrainerInvitationStateException`: the
  token hash must be non-empty and at most `HashedTokens.MaxLength`, and `ExpiresAtUtc` must be after the creation
  time. `Rehydrate` also requires `AcceptedAtUtc` exactly when the status is `ACCEPTED`.
- Both state exceptions map to `400`, next to `InvalidUserActivationStateException`. Coming from a stored row, the
  mappers wrap them in `PersistenceMappingException` (`500`), so corrupt data surfaces as a server fault.
- `IsPendingAt(DateTime nowUtc)` answers "pending and not expired", used by the duplicate-invitation check so the
  expiry rule is not restated in Application.
- `MatchesToken(string tokenHash)` compares with `CryptographicOperations.FixedTimeEquals` over the hash bytes;
  the use case hashes the raw token through `IInvitationTokenService` first. Alternative considered: compare in the
  use case — rejected so the aggregate owns its secret check.
- No `AddDomainEvent` call anywhere.

### Authorization and check order

Role gates use `RequireRoleAttribute` on the route (as `POST /users` does). Ownership rules, which need loaded data,
live in the use case and throw `UnauthorizedOperationException` (`403`).

| Use case | Route role gate | Use case order |
| --- | --- | --- |
| `InviteAthlete(callerId, email)` | `TRAINER` | parse `Email` (`400`) → load trainer, `CanBeTrainer` (`409`) → `GetByEmail`: if found and not `CanBeAthlete` → `409`; if found and has active relationship → `409` → pending duplicate for `(callerId, email)` → `409` → generate token → `Create` → save → commit |
| `RevokeInvitation(callerId, id)` | `TRAINER` | load (`404`) → `TrainerId == callerId` else `403` → `Revoke` (`409`) → save → commit |
| `Accept/RejectInvitation(callerId, id, rawToken)` | `USER` | load (`404`) → `UserId == callerId` (null fails) else `403` → `MatchesToken(hash)` else `InvalidInvitationTokenException` (`403`) → `Accept`/`Reject` (`409`) → save → commit |
| `LinkAthleteToTrainer(trainerId, athleteId)` | `ADMIN` | load trainer, athlete (`404`) → `CanBeTrainer`, `CanBeAthlete` else `InvalidTrainerAthleteRoleException` (`409`) → `ExistsActiveByUserId(athleteId)` → `AthleteAlreadyHasTrainerException` (`409`) → `Create` → save → commit |
| `EndTrainerAthleteRelationship(caller, trainerId, athleteId)` | any authenticated | `ADMIN` or `caller == trainerId` or `caller == athleteId` else `403` → `GetActiveByTrainerIdAndUserId(trainerId, athleteId)` (`404`) → `End` → save → commit |
| `GetTrainerAthletes(trainerId)` | `TRAINER` (own id) / `ADMIN` (route id) | `GetById(trainerId)` (`404`) → `GetAllActiveByTrainerId` → `GetAllByIds` → join, order by email |

- `LinkAthleteToTrainer` takes no caller: the admin restriction is the HTTP entry point's, and the future event
  handler will call it after an accepted invitation. Alternative considered: pass the caller and check `ADMIN` in the
  use case — rejected because the event handler has no admin caller.
- `GetTrainerAthletes` takes only `trainerId`; `GET /trainer-athletes` passes `CurrentUser.Id`. For a known user that
  is not a trainer the result is an empty list (they have no relationships).
- `GetTrainerAthletesUseCase` returns `IReadOnlyList<TrainerAthleteView>` (relationship + athlete `User`), a small
  application record, so the controller does not query users. Ordering by `Email` is done in the use case.

### Persistence

- `TrainerAthletes`: `Id` (PK), `TrainerId` (FK `Users`), `UserId` (FK `Users`), `Status` int, `StartedAtUtc`
  timestamptz, `FinishedAtUtc` timestamptz null. Indexes: `IX_TrainerAthletes_TrainerId`; partial unique
  `UQ_TrainerAthletes_UserId_Active ON "TrainerAthletes" ("UserId") WHERE "Status" = 1` via `Execute.Sql`, dropped
  explicitly in `Down()` before the table.
- `TrainerInvitations`: `Id` (PK), `TrainerId` (FK `Users`), `InviteEmail` (length aligned with `Users.Email`),
  `UserId` (nullable FK `Users`), `TokenHash` (128, unique), `Status` int, `CreatedAtUtc`, `ExpiresAtUtc`,
  `AcceptedAtUtc` null. Index `IX_TrainerInvitations_TrainerId_InviteEmail`. No unique on pending pairs: an expired
  pending row must not block a new invitation, so the duplicate rule is Application-only.
- FKs use `OnDelete(Rule.None)`: history must not disappear silently with a user. Users are never deleted today.
- EF configuration mirrors the migrations, keys `.ValueGeneratedNever()`. No EF Core migration.
- The partial unique index turns a concurrent double link into a `DbUpdateException`. It stays a `500`; the
  application pre-check covers normal traffic. Alternative considered: translate the unique violation to `409` in the
  repository — deferred, since no other repository does it today.

### Invitation token

`IInvitationTokenService` (Application) with `GeneratedInvitationToken Generate(DateTime nowUtc)` returning
`RawValue`, `Hash`, `ExpiresAtUtc`, and `string Hash(string rawValue)`. `InvitationTokenService` (Infra) reads
`TrainerInvitationOptions.ExpirationDays`. Raw-token generation (32 random bytes, Base64Url) and hashing (lowercase hex
SHA-256) live in the internal `Infra/Services/OpaqueToken`, which `JwtTokenService.GenerateRefreshToken` /
`HashRefreshToken` also call. Alternative considered: reuse `ITokenService` — rejected because its contract and
lifetime are refresh-token specific.
- `TrainerInvitations:ExpirationDays` is checked at startup in `Program.cs`, as `Jwt:SigningKey` is: a value below
  `TrainerInvitationOptions.MinimumExpirationDays` (1) stops the API with `InvalidOperationException`. Otherwise every
  invite would fail the aggregate's expiry guard and reach the trainer as a `400` for a server misconfiguration.
- The token hash stays a `string` in both `RefreshToken` and `TrainerInvitation`; only the length is shared, through
  `HashedTokens`. Alternative considered: a `TokenHash` value object in `Domain/Common` — rejected for now so the auth
  aggregate is not reworked in this change. As a result `RefreshToken.Create` still does not validate its hash; only
  `TrainerInvitation` does.

### API

- Request DTOs: `InviteAthleteRequest { Email }`, `InvitationTokenRequest { Token }`, `TrainerAthleteRequest
  { AthleteId }`, with `[Required]` so missing fields give `400`.
- Responses: `CreatedInvitationResponse` (includes `Token`), `InvitationResponse` (no token, no hash),
  `TrainerAthleteResponse`, `TrainerAthleteListItemResponse` (`id`, `athleteId`, `email`, `name`, `surname`,
  `startedAtUtc`). Creation routes return `StatusCode(201, ...)`, as `POST /users` does.
- Route ids use `{…:guid}`. Every catch delegates to `ExceptionMapper.Map`.

### Listing invitations

- `GET /athlete-invitation` with `[RequireRole(Role.ADMIN, Role.TRAINER)]`. `GetTrainerInvitationsUseCase(callerId,
  callerRole)` decides the scope, as `GetAllUsersUseCase` did before it became admin-only: `ADMIN` →
  `ITrainerInvitationRepository.GetAll`, `TRAINER` → `GetAllByTrainerId(callerId)`, anything else →
  `UnauthorizedOperationException` (`403`), so the rule holds even without the route attribute.
- Both repository methods return `IEnumerable<TrainerInvitation>` ordered by `CreatedAtUtc` descending, then `Id`,
  so ties (the seeded invitations share a creation time) still come back in a stable order.
- Items reuse `InvitationResponse`: no token, no hash. The stored `status` is returned as is, so an expired invitation
  still reads `PENDING`; its `expiresAtUtc` tells the client it can no longer be answered.
- No paging or filters; out of scope until the list grows.

## Risks / Trade-offs

- [Accepting an invitation creates nothing visible yet] → Specified and tested explicitly; the event-driven link is
  the next change.
- [Invitations to unregistered emails can never be answered until the signup event exists] → Specified (`403`);
  the trainer can revoke them.
- [Concurrent double link returns `500` instead of `409`] → Rare; DB index keeps data correct.
- [**BREAKING** `GET /users` for trainers: `501` → `403`] → No trainer feature consumed it (it was not implemented).
- [Token mismatch answers `403`, same as wrong user] → Intended: the response does not reveal which check failed.

## Migration Plan

- Two additive migrations; no backfill. `Down()` drops the partial index and each table. Rollback of the API keeps
  the tables unused.
- Add `TrainerInvitations:ExpirationDays` to `appsettings.json`; it is not a secret.
