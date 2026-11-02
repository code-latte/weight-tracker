# Tasks

## 1. Domain: TrainerAthletes

- [x] 1.1 Add `TrainerAthleteStatus` (`ACTIVE = 1`, `ENDED = 2`) in `src/WeightTracker.Domain/TrainerAthletes`, mirroring `WorkoutStatus`; verify `dotnet build WeightTracker.slnx` succeeds
- [x] 1.2 Add `InvalidTrainerAthleteStatusTransitionException` in `src/WeightTracker.Domain/Exceptions` following the existing domain exception shape; verify the build succeeds
- [x] 1.3 Add aggregate `TrainerAthlete` (private constructor, `Create(trainerId, userId, nowUtc)` with `Id.New()` and `IsNew = true`, `Rehydrate`, `End(nowUtc)` guarded on `ACTIVE`), mirroring `Workout`; verify the build succeeds
- [x] 1.4 Add `test/WeightTracker.Unit/Domain/TrainerAthletes/TrainerAthleteTests.cs` replicating `WorkoutTests` (labelled Arrange/Act/Assert): create is `ACTIVE` with no end date and new; rehydrate is not new; `End` sets `ENDED` and `FinishedAtUtc`; ending twice throws and keeps the first date; verify the tests pass

## 2. Domain: TrainerInvitations and user role questions

- [x] 2.1 Add `TrainerInvitationStatus` (`PENDING = 1`, `ACCEPTED = 2`, `REJECTED = 3`, `REVOKED = 4`) in `src/WeightTracker.Domain/TrainerInvitations`; verify the build succeeds
- [x] 2.2 Add `InvalidTrainerInvitationStatusTransitionException` and `TrainerInvitationExpiredException` in `Domain/Exceptions`; verify the build succeeds
- [x] 2.3 Add aggregate `TrainerInvitation` mirroring `RefreshToken` (`TokenHashMaxLength`, `Create`, `Rehydrate`, `Revoke`/`Accept`/`Reject(nowUtc)` sharing one pending-and-not-expired guard, `Accept` sets `AcceptedAtUtc`, `IsPendingAt(nowUtc)`, `MatchesToken(hash)` with a fixed-time comparison); no `AddDomainEvent`; verify the build succeeds
- [x] 2.4 Add `CanBeTrainer()` (`TRAINER` and active) and `CanBeAthlete()` (`USER` and active) to `User`, leaving `CanTrain()` unchanged; verify the build succeeds
- [x] 2.5 Add `test/WeightTracker.Unit/Domain/TrainerInvitations/TrainerInvitationTests.cs`: create `PENDING` with and without `UserId`; each action from `PENDING`; each action from every non-pending status throws and keeps the status; each action at and after expiry throws; `IsPendingAt` before/at expiry; `MatchesToken` true/false; verify the tests pass
- [x] 2.6 Extend `test/WeightTracker.Unit/Domain/Users/UserTests.cs` for `CanBeTrainer` and `CanBeAthlete` across the three roles and deactivated users; verify the tests pass
- [x] 2.7 Review both domain areas against `Workout`, `RefreshToken` and the aggregate lifecycle section of `STPR_INVARIANTS.md`; record any deviation or confirm none

## 3. Persistence

- [x] 3.1 Add migration `CreateTrainerAthletesTable_<YYYYMMDDHHmm>` (file `<ts>_CreateTrainerAthletesTable.cs`, non-round time) per design: FKs to `Users`, `IX_TrainerAthletes_TrainerId`, partial unique `UQ_TrainerAthletes_UserId_Active` via `Execute.Sql`, `Down()` dropping index then table; mirror `202610080952_CreateRefreshTokensTable`; verify `test/WeightTracker.Integration/Migrations/MigrationsTests.cs` passes up and down
- [x] 3.2 Add migration `CreateTrainerInvitationsTable_<YYYYMMDDHHmm>` per design: FKs to `Users` (`UserId` nullable), `InviteEmail` length aligned with `Users.Email`, unique `TokenHash` (128), `IX_TrainerInvitations_TrainerId_InviteEmail`, reversing `Down()`; verify the migration tests pass
- [x] 3.3 Add `TrainerAthleteEntity` and `TrainerInvitationEntity` in `Infra/Entities` and their configuration in `WeightTrackerDbContext` (table/column names, lengths, `.ValueGeneratedNever()`); verify the build succeeds and no EF Core migration was generated
- [x] 3.4 Add `TrainerAthleteMapper` and `TrainerInvitationMapper` (`MapToEntity` / `MapToDomain` via `Rehydrate`) and extend `test/WeightTracker.Unit/Infra/Mappers/AggregateMapperTests.cs` with round-trips (active and ended relationship; invitation with and without `UserId` and `AcceptedAtUtc`); verify the tests pass
- [x] 3.5 Add `ITrainerAthleteRepository` (`GetActiveByTrainerIdAndUserId(trainerId, userId)`, `ExistsActiveByUserId`, `GetAllActiveByTrainerId`, `Save`) and `ITrainerInvitationRepository` (`GetById`, `GetAllPendingByTrainerIdAndEmail`, `Save`; the aggregate's `IsPendingAt` decides whether one is still live) in `Application/Interfaces`, and add `GetAllByIds` to `IUserRepository`; verify the build succeeds
- [x] 3.6 Implement `TrainerAthleteRepository`, `TrainerInvitationRepository` and `UserRepository.GetAllByIds` mirroring `WorkoutRepository` (`IsNew` branching, mapping inside); register them in `Program.cs`; add `"TrainerAthletes"` and `"TrainerInvitations"` to the `ApiFactory.ResetDatabase` truncate list
- [x] 3.7 Add `test/WeightTracker.Integration/Repositories/TrainerAthleteRepositoryTests.cs` and `TrainerInvitationRepositoryTests.cs` (save/load, active filters, the partial unique index rejecting a second active row for one athlete), and extend `UserRepositoryTests` for `GetAllByIds`; verify the tests pass
- [x] 3.8 Seed in `SeedData` / `DatabaseSeeder` through the domain: one active relationship, one ended relationship, one pending invitation to a registered `USER` with a known raw token, one expired pending invitation and one to an unregistered email, all with deterministic ids; verify the integration suite still passes
- [x] 3.9 Review persistence against `CreateRefreshTokensTable`, `WorkoutRepository` and `WorkoutMapper`; confirm EF configuration matches the migrations and no deviation exists

## 4. Invitation token service

- [x] 4.1 Add `IInvitationTokenService` with `GeneratedInvitationToken` in `Application/Interfaces`, and `InvitationTokenService` + `TrainerInvitationOptions` (section `TrainerInvitations`, `ExpirationDays`) in `Infra/Services`, mirroring `JwtTokenService.GenerateRefreshToken` / `HashRefreshToken`; add `TrainerInvitations:ExpirationDays: 7` to `appsettings.json`; register in `Program.cs`; verify the build succeeds
- [x] 4.2 Add unit tests for `InvitationTokenService` (hash is deterministic, raw differs from hash, expiry = now + configured days); verify the tests pass

## 5. Application: TrainerAthletes use cases

- [x] 5.1 Add `AthleteAlreadyHasTrainerException`, `InvalidTrainerAthleteRoleException` and `TrainerAthleteNotFoundException` in `Application/Exceptions`; verify the build succeeds
- [x] 5.2 Add `LinkAthleteToTrainerUseCase` (no caller parameter) with the check order in design; verify the build succeeds
- [x] 5.3 Add `EndTrainerAthleteRelationshipUseCase` (permission before lookup) mirroring `ChangeWorkoutStatusUseCase`; verify the build succeeds
- [x] 5.4 Add `GetTrainerAthletesUseCase` returning the relationship plus athlete user, ordered by email, mirroring the `Exercises` read use cases; verify the build succeeds
- [x] 5.5 Register the three use cases in `Program.cs`; verify the API starts in the integration fixture
- [x] 5.6 Add unit tests in `test/WeightTracker.Unit/Application/TrainerAthletes` with mocked ports: link happy path saves and commits; unknown trainer/athlete; wrong roles; deactivated users; athlete already active; end by admin/trainer/athlete; end by other `403` before lookup; end with no active `404`; list ordering and unknown trainer `404`; no save on any failure; verify the tests pass
- [x] 5.7 Review the use cases against `ChangeWorkoutStatusUseCase` and the validation-location rules of `STPR_INVARIANTS.md`; confirm no deviation

## 6. Application: TrainerInvitations use cases

- [x] 6.1 Add `TrainerInvitationNotFoundException`, `PendingInvitationAlreadyExistsException` and `InvalidInvitationTokenException` in `Application/Exceptions`; verify the build succeeds
- [x] 6.2 Add `InviteAthleteUseCase` with the check order in design (uses `IUserRepository`, `ITrainerAthleteRepository`, `ITrainerInvitationRepository`, `IInvitationTokenService`, `IClock`), returning the invitation and the raw token; verify the build succeeds
- [x] 6.3 Add `RevokeInvitationUseCase`, `AcceptInvitationUseCase` and `RejectInvitationUseCase` (ownership `403`, then token, then aggregate action); confirm accept creates no relationship; verify the build succeeds
- [x] 6.4 Register the four use cases in `Program.cs`; verify the API starts in the integration fixture
- [x] 6.5 Add unit tests in `test/WeightTracker.Unit/Application/TrainerInvitations`: invite registered `USER` (sets `UserId`), unregistered (null), `TRAINER`/`ADMIN`/deactivated email `409`, linked athlete `409`, duplicate pending `409`, expired or revoked previous does not block, malformed email; revoke by owner / other trainer; accept and reject by invited user, other user, null `UserId`, wrong token; accept never touches `ITrainerAthleteRepository`; no save on failure; verify the tests pass
- [x] 6.6 Review the use cases against `ChangeWorkoutStatusUseCase` and `RefreshTokenUseCase`; confirm no deviation and no `AddDomainEvent` call

## 7. API: trainer-athletes routes

- [x] 7.1 Add `Endpoints/TrainerAthletes/TrainerAthletesController` inheriting `AuthorizedController` with `POST {trainerId:guid}` (`ADMIN`, `201`), `PUT {trainerId:guid}/end` (authenticated, `200`), `GET` (`TRAINER`), `GET {trainerId:guid}` (`ADMIN`), DTOs per design, `[ProducesResponseType]` for every status, catches through `ExceptionMapper.Map`; verify the build succeeds
- [x] 7.2 Map `TrainerAthleteNotFoundException` `404`, `AthleteAlreadyHasTrainerException` `409`, `InvalidTrainerAthleteRoleException` `409`, `InvalidTrainerAthleteStatusTransitionException` `409` in `ExceptionMapper` and extend `test/WeightTracker.Integration/Common/ExceptionMapperTests.cs`; verify the tests pass
- [x] 7.3 Add `test/WeightTracker.Integration/TrainerAthletes/` endpoint tests replicating `AuthEndpointsTests` and `CreateUserEndpointTests`, one class per route, covering every scenario in `specs/trainer-athletes/spec.md` (each route: `401` no token, `401` invalid token, role `403`s, `404`, `409`, `400`, happy path with state checked through the API); authenticate only via `AuthenticatedClientFor`; verify the tests pass
- [x] 7.4 Add scoped-read tests for both `GET` routes replicating `WorkoutScopingTests`: assert ended relationships and other trainers' athletes are excluded, order by email and exact item fields; verify the tests pass
- [x] 7.5 Review the controller against `WorkoutsController` and the `POST /users` admin route; confirm no deviation

## 8. API: athlete-invitation routes

- [x] 8.1 Add `Endpoints/TrainerInvitations/TrainerInvitationsController` (`[Route("athlete-invitation")]`) with `POST` (`TRAINER`, `201` with token), `PUT {invitationId:guid}/revoke` (`TRAINER`), `PUT {invitationId:guid}/accept` and `/reject` (`USER`, `[Required] Token`), DTOs per design, `[ProducesResponseType]`, catches through `ExceptionMapper.Map`; verify the build succeeds
- [x] 8.2 Map `TrainerInvitationNotFoundException` `404`, `PendingInvitationAlreadyExistsException` `409`, `InvalidInvitationTokenException` `403`, `InvalidTrainerInvitationStatusTransitionException` `409`, `TrainerInvitationExpiredException` `409` in `ExceptionMapper` and extend `ExceptionMapperTests`; verify the tests pass
- [x] 8.3 Add `test/WeightTracker.Integration/TrainerInvitations/` endpoint tests, one class per route, covering every scenario in `specs/trainer-invitations/spec.md`, including: token present only in the creation response; expiry equals creation plus configured days; accepted invitation leaves `GET /trainer-athletes` unchanged; expired seeded invitation `409`; verify the tests pass
- [x] 8.4 Review the controller against `WorkoutsController`; confirm no deviation

## 9. GET /users becomes admin-only

- [x] 9.1 Remove the `Role.TRAINER` branch and the `callerId` parameter from `GetAllUsersUseCase` and update its caller in `UsersController`; change `GET /users` to `[RequireRole(Role.ADMIN)]`; verify the build succeeds
- [x] 9.2 Delete `TrainerAthletesNotImplementedException`, its `ExceptionMapper` entry and its case in `ExceptionMapperTests`; verify the build succeeds
- [x] 9.3 Update `test/WeightTracker.Unit/Application/Users/GetAllUsersUseCaseTests.cs` (trainer caller throws `UnauthorizedOperationException`) and `test/WeightTracker.Integration/Users/ListUsersEndpointTests.cs` (trainer gets `403` with no user data); verify the tests pass
- [x] 9.4 Review against `specs/user-administration/spec.md`; confirm no remaining reference to the removed exception (`grep -rn TrainerAthletesNotImplemented src test` is empty)

## 10. Integration checks

- [x] 10.1 Update `test/WeightTracker.Integration/OpenApi/OpenApiDocumentTests.cs` for the eight new routes (secured, declared responses) and `GET /users` without `501`; verify the tests pass
- [x] 10.2 Run `dotnet build WeightTracker.slnx` (warnings as errors) and `dotnet test WeightTracker.slnx` with Docker running; verify both pass
- [x] 10.3 Run the template parity check from `AGENTS.md`; verify it prints no `DRIFT` line
- [x] 10.4 Walk the `AGENTS.md` quality checklist and confirm every item applies or is satisfied

## 11. List invitations

- [x] 11.1 Add `GetAll` and `GetAllByTrainerId` (ordered by `CreatedAtUtc` desc, then `Id`) to `ITrainerInvitationRepository` and `TrainerInvitationRepository`; extend `TrainerInvitationRepositoryTests` (all invitations; only the trainer's; order); verify the tests pass
- [x] 11.2 Add `GetTrainerInvitationsUseCase` (admin → all, trainer → own, otherwise `UnauthorizedOperationException`), register it in `Program.cs`, and add `GetTrainerInvitationsUseCaseTests` with mocked repository for each role; verify the tests pass
- [x] 11.3 Add `GET /athlete-invitation` to `TrainerInvitationsController` with `[RequireRole(Role.ADMIN, Role.TRAINER)]`, `200` list of `InvitationResponse`, `[ProducesResponseType]` for `200`, `401`, `403`; verify the build succeeds
- [x] 11.4 Add `ListInvitationsEndpointTests` replicating `ListUsersEndpointTests`: `401` no token, `401` invalid token, `403` `USER` with no data, admin sees both trainers' invitations, trainer sees only theirs (asserting the other trainer's are excluded), newest first, exact item fields and no token or hash; extend `OpenApiDocumentTests`; verify the tests pass
- [x] 11.5 Review against `GetAllUsersUseCase`, `UsersController.GetAll` and the scoped-read rule; run the full `dotnet test WeightTracker.slnx` and the parity check; verify both pass

## 12. Audit follow-up

- [x] 12.1 Rename the new repository methods to the contract table (`GetActiveByTrainerIdAndUserId`, `ExistsActiveByUserId`, `GetAllActiveByTrainerId`, `GetAllPendingByTrainerIdAndEmail`, `IUserRepository.GetAllByIds`), with lists returning `IEnumerable<T>`; update callers, mocks and repository tests; verify the build and tests pass
- [x] 12.2 Make `TrainerAthlete.Rehydrate` and `TrainerInvitation.Rehydrate` reject contradictory state with `InvalidTrainerAthleteStateException` / `InvalidTrainerInvitationStateException`, mapped to `400` in `ExceptionMapper`; add domain, mapper and `ExceptionMapperTests` cases; verify the tests pass
- [x] 12.3 Make `TrainerInvitation.Create` reject an empty or oversized token hash and an expiry not after creation; add domain tests; verify the tests pass
- [x] 12.4 Share token hashing: `Domain/Common/HashedTokens.MaxLength` for both `TokenHashMaxLength` constants, and `Infra/Services/OpaqueToken` for raw-token generation and SHA-256 hashing in `JwtTokenService` and `InvitationTokenService`; verify the auth and invitation tests pass
- [x] 12.5 Run `dotnet build WeightTracker.slnx`, the full `dotnet test WeightTracker.slnx` with Docker and the parity check; verify all pass

## 13. Second audit follow-up

- [x] 13.1 Fail startup when `TrainerInvitations:ExpirationDays` is below `TrainerInvitationOptions.MinimumExpirationDays`, mirroring the `Jwt:SigningKey` check in `Program.cs`; add `TrainerInvitationOptionsStartupTests` booting the API with `0` and `-1`; verify the tests pass
- [x] 13.2 Cover the "every status" part of listing: `ListInvitationsEndpointTests.Get_ShouldListInvitationsInEveryStatus` for admin and trainer (accepted, rejected, revoked, pending) and `TrainerInvitationRepositoryTests.GetAllByTrainerId_ShouldIncludeAnsweredInvitations`; verify the tests pass
- [x] 13.3 Run `dotnet build WeightTracker.slnx`, the full `dotnet test WeightTracker.slnx` with Docker and the parity check; verify all pass

## Workflow follow-up

- Refresh the front OpenAPI snapshot and move trainer screens from `GET /users` to `GET /trainer-athletes` in a separate front plan.
- Domain events change: `InvitationAccepted` → `LinkAthleteToTrainerUseCase`, and signup linking of pending invitations.
- Archive the change after review.
