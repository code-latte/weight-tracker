# Design

## Context

See `proposal.md` for the motivation and `specs/exercise-favorites/spec.md` for the behaviour. The current state that shapes the approach:

- Every persisted concept is an `AggregateRoot` with a repository in `Application/Interfaces` and an implementation plus mapper in `Infra`. `RefreshToken` and `TrainerAthlete` are small single-table aggregates that reference users by `Id`.
- `ExerciseVisibility.EnsureVisible(exercise, callerId, callerRole, ct)` already decides whether a caller may see an exercise, and throws `ExerciseNotFoundException` (`404`) otherwise. `ExerciseArchivedException` (`409`) and `ExerciseNotFoundException` are already mapped in `ExceptionMapper`.
- No repository deletes an aggregate today.
- Integration tests reset the database through an explicit `TRUNCATE` list and remove users through `ApiFactory.RemoveUser`, which deletes referencing rows table by table.
- `test/WeightTracker.Integration/Bootstrap/ApiFactory.cs` has a byte-identical copy under `.stpr/template/test`, so the test bootstrap change in D5 is copied there too. No other touched file has a template copy.

## Goals / Non-Goals

**Goals:**
- A thin aggregate, so favorites follow the same layering as everything else.
- Idempotent writes with a fixed number of queries.

**Non-Goals:**
- Any change to `GET /exercises`, workouts or exercise visibility.
- Handling the concurrent duplicate insert gracefully. It is accepted as `500` (see Risks).

## Decisions

### D1. Domain: `ExerciseFavorite` aggregate

`Domain/ExerciseFavorites/ExerciseFavorite.cs`, a `sealed class : AggregateRoot` with `Id UserId`, `Id ExerciseId` and `DateTime CreatedAtUtc`, all with private setters.

- `Create(Id userId, Id exerciseId, DateTime nowUtc)` uses `Id.New()` and sets `IsNew = true`.
- `Rehydrate(Id id, Id userId, Id exerciseId, DateTime createdAtUtc)`.
- There are no mutators and no domain exceptions, because the aggregate has no invariant beyond its typed fields.

*Alternative:* manage the table from Application with no domain type. Rejected because it breaks "repositories take and return domain types only" and the `DbContext` access rule (see proposal).

### D2. Application: use cases

All three take the caller explicitly (`Id callerId, Role callerRole`), like the trainer-athlete use cases.

- **`MarkExerciseFavoriteUseCase.Execute(callerId, callerRole, exerciseId, ct)` → `ExerciseFavorite`:**
  1. `IExerciseRepository.GetById` (`null` → `ExerciseNotFoundException`);
  2. `ExerciseVisibility.EnsureVisible`;
  3. an archived exercise → `ExerciseArchivedException`;
  4. `GetByUserAndExercise`: if found, return it unchanged without saving;
  5. otherwise `ExerciseFavorite.Create(callerId, exerciseId, IClock.UtcNow)`, `Save`, then `SaveChangesAsync` once.

  The existence check runs before the idempotency check. Re-marking an exercise that has since become archived is therefore `409`, which matches "archived exercises cannot be marked" in the spec.
- **`UnmarkExerciseFavoriteUseCase.Execute(callerId, callerRole, exerciseId, ct)`:**
  1. `GetById` (`null` → `404`);
  2. `EnsureVisible` (`404`);
  3. `GetByUserAndExercise`: if absent, return without committing;
  4. otherwise `Remove`, then `SaveChangesAsync` once.

  Archived exercises are not checked, so they stay removable.
- **`GetExerciseFavoritesUseCase.Execute(callerId, ct)` → `IReadOnlyList<ExerciseFavorite>`:** calls `GetAllByUser(callerId)`. No role check is needed, because a caller only ever reads their own favorites. There is no visibility filtering, as the spec requires.

### D3. Infra and persistence

- **Migration** `CreateFavoriteExercisesTable_<random time>`, with a timestamp after `202611031200` and a non-round minute as the invariants ask:
  - table `FavoriteExercises`: `Id` (`uuid` PK `PK_FavoriteExercises`), `UserId` and `ExerciseId` (`uuid`, not null), `CreatedAtUtc` (`timestamptz`, not null);
  - foreign keys `FK_FavoriteExercises_User` to `Users(Id)` and `FK_FavoriteExercises_Exercise` to `Exercises(Id)`, both `Rule.None`;
  - unique index `IX_FavoriteExercises_UserId_ExerciseId`;
  - `Down()` deletes the table.
- **EF:** `ExerciseFavoriteEntity` with `HasColumnName`, `ValueGeneratedNever()` on `Id`, `timestamptz` on `CreatedAtUtc`, and the unique index declared in the model.
- **`ExerciseFavoriteMapper`:** `MapToEntity` / `MapToDomain`, storing `CreatedAtUtc` as UTC like `UserMapper`, and wrapping `DomainException` in `PersistenceMappingException` like the other mappers.
- **`IExerciseFavoriteRepository` / `ExerciseFavoriteRepository`:**
  - `GetByUserAndExercise` and `GetAllByUser` (ordered by `CreatedAtUtc` descending, then `Id` for a stable order), both `AsNoTracking`;
  - `Save`: insert when `IsNew`, otherwise do nothing, since there are no updates;
  - `Remove(ExerciseFavorite)`: attaches an entity with the aggregate's id and marks it deleted. It stages the delete, and the use case commits.

  None of them calls `SaveChanges`.

### D4. API

- **`ExerciseFavoritesController : AuthorizedController`** with `[Route("exercise-favorites")]` and no `[RequireRole]`, because every role is allowed:
  - `[HttpPut("{exerciseId:guid}")]` → `200` with `ExerciseFavoriteResponse`;
  - `[HttpDelete("{exerciseId:guid}")]` → `204`;
  - `[HttpGet]` → `200` with a list.

  Each declares `[ProducesResponseType]` for its codes (`401`; `404` / `409` where they apply) and routes every catch through `ExceptionMapper.Map`.
- **`ExerciseFavoriteResponse(Guid ExerciseId, DateTime CreatedAtUtc)`** in `Endpoints/ExerciseFavorites/Dtos/`. There's no request body.
- **`Program.cs`:** registers `IExerciseFavoriteRepository` and the three use cases. No new exception mappings.

### D5. Test bootstrap

- Add `"FavoriteExercises"` to the `TRUNCATE` list in `ApiFactory.ResetDatabase`.
- Delete the user's favorites in `ApiFactory.RemoveUser` before the user row. Otherwise the new foreign key would make `RemoveUser` fail for a user with favorites.

## Risks / Trade-offs

- [Two concurrent `PUT`s for the same pair both pass the existence check] → The unique index rejects the second insert, which surfaces as `500`. This is accepted: it takes one user double-clicking within milliseconds, and the data stays correct.
- [A favorite outlives the exercise's visibility] → This is intended by the spec. It costs nothing on `GET`, and the client ignores ids it cannot resolve.
- [A deactivated user's favorites stay forever] → There's no hard delete of users, so this is harmless. The rows are small.
- [`Remove` is a new repository pattern] → It is scoped to this aggregate and documented in the interface. Future aggregates still default to state changes over deletes.

## Migration Plan

1. Deploy the migration. It is additive, with a new table only.
2. Deploy the API. Existing clients are unaffected.
3. Rollback: `Down()` drops the table and every favorite in it.
