# Design

## Context

See proposal.md - Why. Today `GetAllUsersUseCase.Execute(Role, CancellationToken)` returns `IEnumerable<User>` and `UsersController.GetAll` maps each one with `UserSummaryResponse.From`. `UserSummaryResponse` is also returned by `PUT /users/{id}/activate|deactivate`. The nearest existing join of relationships with user data is `GetTrainerAthletesUseCase`, which reads relationships, resolves users with `IUserRepository.GetAllByIds` through `TrainerAthleteViewLoader`, and returns the Application read model `TrainerAthleteView`. `ITrainerAthleteRepository` has no read that spans several athletes.

## Goals / Non-Goals

**Goals:**
- Resolve every athlete's current trainer in Application, with a fixed number of queries (three: users, active relationships, trainers).
- Leave the activate / deactivate contract untouched.

**Non-Goals:**
- Domain changes to `User` or `TrainerAthlete`.
- Any migration, index or EF configuration change.
- Reusing `TrainerAthleteViewLoader`: it resolves both sides of each relationship and throws when a user is missing, which is more than this read needs.

## Decisions

### Api: dedicated list item DTO

Add `UserListItemResponse(Guid Id, string Email, string Name, string Surname, string Role, bool IsActive, DateTime? DeactivatedAtUtc, UserProfileResponse? Trainer)` in `Endpoints/Users/Dtos/`, with `From(UserWithTrainerView)` built like `UserSummaryResponse.From` and the nested trainer through `UserProfileResponse.From(User)`. `GET /users` returns `IReadOnlyList<UserListItemResponse>` and its `[ProducesResponseType]` changes accordingly; `401` and `403` stay.

- Alternative: add `Trainer` to `UserSummaryResponse`. Rejected: activate / deactivate would either have to load the trainer or return a misleading `null`.
- Alternative: nest `UserSummaryResponse` as the trainer. Rejected: recursive schema, exposes the trainer's activation state and a meaningless nested `trainer`.

### Application: read model and use case

Add `UserWithTrainerView(User User, User? Trainer)` in `UseCases/Users/`, beside the use case, mirroring `TrainerAthleteView`. `GetAllUsersUseCase` takes `ITrainerAthleteRepository` in addition to `IUserRepository` and returns `IReadOnlyList<UserWithTrainerView>`:

1. Reject non-admin callers with `UnauthorizedOperationException` before touching any repository (unchanged).
2. `IUserRepository.GetAll` (already ordered by email; order preserved).
3. Collect the ids of `Role.USER` users; `ITrainerAthleteRepository.GetAllActiveByUserIds(ids)`.
4. Resolve the distinct trainer ids with `IUserRepository.GetAllByIds` (skip the call when there are none), index by `Id`.
5. Pair: `USER` with an active relationship → its trainer; everyone else → `null`.

- Alternative: pick trainers out of the already loaded `GetAll` result and skip step 4. Valid and one query cheaper, but the brief maps the join to the `GetTrainerAthletesUseCase` pattern (`GetAllByIds`), and deviations from mapped references are not allowed.
- A relationship whose trainer is missing from step 4 cannot happen given foreign keys; the pairing yields `null` rather than throwing, so a list read never fails on it.

### Infrastructure: repository read

Add to `ITrainerAthleteRepository`:

```csharp
/// <summary>Every active relationship of the given athletes, in no particular order. Ended ones are ignored.</summary>
Task<IEnumerable<TrainerAthlete>> GetAllActiveByUserIds(IReadOnlyCollection<Id> userIds, CancellationToken cancellationToken);
```

Implemented in `TrainerAthleteRepository` like `GetAllActiveByTrainerId`: `AsNoTracking`, filter `status == ACTIVE` and `userIds.Contains(user_id)`, map to domain inside the repository. Returns an empty result without querying when `userIds` is empty. Registration in `Program.cs` is unchanged (the repository is already registered; the use case gains a constructor dependency resolved by DI).

- Alternative: `GetAllActive()` with no filter. Rejected: reads relationships the caller does not need and is less reusable.

### Cross-area dependency

Users (Application) now depends on the `TrainerAthletes` port. Dependency direction stays Application → Application interface; Domain is untouched.

## Risks / Trade-offs

- [OpenAPI shape of a nullable `$ref` in .NET 10 may be `oneOf`/`anyOf` with `null` rather than `nullable: true`] → the OpenAPI test asserts that `trainer` exists and resolves to the `UserProfileResponse` schema, tolerating either form.
- [Three queries instead of one join] → bounded and independent of user count; acceptable for an admin list without pagination.
- [OpenAPI snapshot change for the front] → additive; the front refreshes it in its own plan.

## Migration Plan

No database migration. Deploy normally; rollback is a code revert.
