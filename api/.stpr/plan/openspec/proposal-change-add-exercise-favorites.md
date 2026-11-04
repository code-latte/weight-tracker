## Change

Let any user mark exercises as favorites, so the front can filter the exercise list by them

## Objective

The exercise list keeps growing (system catalog plus custom exercises), and users repeat the same handful of
exercises. A user of any role should be able to mark exercises as favorites, remove them, and list them with the date
each one was added. The front then filters its picker by favorites. Nothing else about exercises or workouts changes.

## Capabilities

- `PUT /exercise-favorites/{exerciseId}`: the caller marks an exercise they can see as a favorite. Idempotent.
- `DELETE /exercise-favorites/{exerciseId}`: the caller removes an exercise from their favorites. Idempotent.
- `GET /exercise-favorites`: the caller lists their favorites, each with `exerciseId` and `createdAtUtc`, newest first.

## Module Classification

Favorites are a new domain area, `ExerciseFavorites`.

- **ExerciseFavorites (domain)** -> **mutable aggregate root, `ExerciseFavorite`** ->
  `src/WeightTracker.Domain/Auth/RefreshToken.cs` and `src/WeightTracker.Domain/TrainerAthletes/TrainerAthlete.cs`.
  These are small aggregates that reference `User` (and another aggregate) by `Id` only.
- **ExerciseFavorites (application)** -> three use cases, plus the existing `ExerciseVisibility` service ->
  `src/WeightTracker.Application/UseCases/TrainerAthletes/`
- **ExerciseFavorites (port / infra)** -> new `IExerciseFavoriteRepository`, `ExerciseFavoriteRepository` and
  `ExerciseFavoriteMapper` -> `ITrainerAthleteRepository`, `TrainerAthleteRepository`, `TrainerAthleteMapper`
- **ExerciseFavorites (API)** -> new `ExerciseFavoritesController` under `Endpoints/ExerciseFavorites/` ->
  `src/WeightTracker.Api/Endpoints/TrainerAthletes/TrainerAthletesController.cs`
- **Persistence** -> one migration creating `FavoriteExercises` ->
  `src/WeightTracker.Migrations/Migrations/202610301437_CreateTrainerAthletesTable.cs`
- **Exercises, Users, Workouts** -> no change.

It is not a catalog: favorites are created and removed by user actions.

**Why an aggregate and not an application-only table.** The question was whether favorites, which have almost no
rules, could be managed in Application without a domain type. The common DDD answer is that a record with almost no
behaviour may still be its own small aggregate when it has its own lifecycle and identity. That applies here: a user
creates a favorite and deletes it, independently of the user and of the exercise. This repository settles it:

- **Existing precedent.** Every persisted concept is an `AggregateRoot` with its own repository and mapper.
  `RefreshToken` is the closest case: little behaviour, referenced by `UserId`, still an aggregate.
  No table is managed from Application today.
- **Invariants and checklist.** "Repositories take and return domain types only; no EF entity leaks past the
  repository" and "`DbContext` is reached directly only from seed code and the integration test bootstrap". An
  application-only table would need either a repository returning a non-domain type or Application touching EF. Both
  break the contract.
- **Alternatives rejected:**
  - *A favorite collection inside `User`:* every toggle would load and save the whole user, and the list is
    unbounded. It also mixes an unrelated concern into the credentials aggregate.
  - *A collection inside `Exercise`:* favorites are per user, and system exercises are read-only.

**What the aggregate holds.** The aggregate is deliberately thin: `Create` validates nothing beyond its value types,
and it has no mutators. The rules that do exist sit in the layer that owns them:
- Uniqueness per user and exercise is enforced by the database index and checked first in Application.
- "The exercise must exist and be visible to the caller" is a check against the database, which lives in Application
  through `ExerciseVisibility`.

## Canonical Template Mapping

- `ExerciseFavorite` aggregate (`Create` with `IsNew = true`, `Rehydrate`, private constructor, `Id.New()`) ->
  `RefreshToken.cs` -> the nearest aggregate with no business behaviour that references `User` by `Id`
- `IExerciseFavoriteRepository` / `ExerciseFavoriteRepository` / `ExerciseFavoriteMapper` ->
  `ITrainerAthleteRepository`, `TrainerAthleteRepository`, `TrainerAthleteMapper` -> a single-table aggregate keyed
  by a pair of user and other ids, with `AsNoTracking` reads and mapping inside the repository
- Use cases -> `UseCases/TrainerAthletes/` (one public `Execute`, caller passed explicitly, one `SaveChangesAsync`)
- Exercise existence and visibility -> `Services/ExerciseVisibility.cs` -> the same check that guards
  `GET /exercises/{id}` and adding an exercise to a workout
- Controller and DTOs -> `TrainerAthletesController.cs` (`AuthorizedController`, `[ProducesResponseType]`,
  `ExceptionMapper.Map` in every catch)
- Migration -> `202610301437_CreateTrainerAthletesTable.cs` -> a new table with foreign keys to `Users` and a unique
  index
- Tests -> `test/WeightTracker.Unit/Domain/TrainerAthletes/`, `test/WeightTracker.Unit/Application/TrainerAthletes/`,
  `test/WeightTracker.Integration/TrainerAthletes/`, `test/WeightTracker.Integration/Repositories/TrainerAthleteRepositoryTests.cs`

## State And Mutability

- New table `FavoriteExercises`:
  - `Id` (`uuid`, primary key);
  - `UserId` (`uuid`, not null, FK to `Users(Id)`, no cascade);
  - `ExerciseId` (`uuid`, not null, FK to `Exercises(Id)`, no cascade);
  - `CreatedAtUtc` (`timestamptz`, not null);
  - unique index `IX_FavoriteExercises_UserId_ExerciseId`.
- A favorite is created once and never updated. Removing it deletes the row.
- Users and exercises are never hard-deleted, so the foreign keys never block a delete.
- No existing table changes.

## Business Rules

- **Who.** Any authenticated user (`USER`, `TRAINER`, `ADMIN`) manages only their own favorites. No route acts on
  another user's favorites.
- **Which exercises.** Only an exercise the caller can see under the existing exercise visibility rule:
  - a system exercise;
  - one of their own custom exercises;
  - a custom exercise used in a workout they can see;
  - for an `ADMIN`, any exercise.

  An unknown exercise, or one the caller cannot see, is `404`.
- **Archived exercises.** Marking an archived exercise as a favorite is `409`, because it can no longer be added to
  workouts. A favorite whose exercise is archived later stays until the user removes it.
- **PUT is idempotent.** If the exercise is already a favorite, the response is the same success, and the original
  `CreatedAtUtc` is kept. `CreatedAtUtc` comes from `IClock` on creation.
- **DELETE is idempotent.** If the exercise is not a favorite, the response is the same success. The exercise must
  still exist and be visible (`404` otherwise), so the route doesn't confirm ids the caller cannot see.
- **GET** returns every favorite of the caller, newest first, as `{ exerciseId, createdAtUtc }`. It doesn't embed
  exercise data: the client already holds the exercises from `GET /exercises`.
- **Visibility changes after favoriting.** If an exercise stops being visible to the caller, for example a trainer's
  custom exercise once the athlete's workout no longer uses it, its favorite stays and is still listed. The client
  ignores ids it cannot resolve. No extra filtering query runs on `GET`.

## Roles And Visibility

| Route | Anonymous | USER | TRAINER | ADMIN |
| --- | --- | --- | --- | --- |
| `PUT /exercise-favorites/{exerciseId}` | `401` | own favorites, visible exercises | same | own favorites, any exercise |
| `DELETE /exercise-favorites/{exerciseId}` | `401` | own favorites | same | same |
| `GET /exercise-favorites` | `401` | own favorites only | same | own favorites only |

An `ADMIN` does not see other users' favorites.

## Business Identifiers And Mutability

- `ExerciseFavorite.Id`: technical identifier, generated with `Id.New()` and immutable.
- `(UserId, ExerciseId)`: unique pair and the business identity of a favorite. Both are references (`User.Id`,
  `Exercise.Id`) and immutable after creation. Changing either means deleting the favorite and creating another.

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## Out Of Scope

- Ordering favorites manually, grouping them, or adding notes.
- Seeing or managing other users' favorites, admins included.
- A `favorite` flag on `GET /exercises` items, and server-side filtering of `GET /exercises` by favorites. The front
  filters by joining the two lists.
- Workout favorites, or favorites of anything other than exercises.
- Any front change. That is a separate front plan after the OpenAPI snapshot refresh.

## Notes

- **Responses (decided):**
  - `PUT` returns `200 OK` with `{ exerciseId, createdAtUtc }` both when the favorite is created and when it already
    existed, so the client gets the stored date either way.
  - `DELETE` returns `204 No Content`.
  - `GET` returns `200 OK` with a list.

  Route ids that are not GUIDs don't match the routes, as on other endpoints.
- **Repository:**
  - `GetByUserAndExercise(userId, exerciseId)`;
  - `GetAllByUser(userId)`, ordered by `CreatedAtUtc` descending;
  - `Save(favorite)`: insert only, since there are no updates;
  - `Remove(favorite)`.

  `Remove` is the first repository method in this codebase that deletes an aggregate. That is a new operation with no
  existing reference, and it is justified because a favorite has no history worth keeping. It stages the delete, and
  the use case commits once, as with `Save`. No repository calls `SaveChanges`.
- **Concurrent PUT.** Two simultaneous PUTs for the same pair could both pass the existence check. The unique index
  rejects the second insert. The use case doesn't catch it, so the second request fails with `500`. This is accepted
  as very unlikely for a single user's clicks, and it is documented, not handled.
- **No new exception for the duplicate case.** Idempotency is resolved in the use case, by returning the existing
  favorite. The archived case reuses `ExerciseArchivedException` (`409`), and the not-found case reuses
  `ExerciseNotFoundException` (`404`).
- **Seed:** no change. Tests create favorites through the API.
- **Tests:**
  - Unit:
    - `ExerciseFavorite.Create` / `Rehydrate`;
    - each use case: idempotent PUT keeps the date, visibility checked, archived rejected, idempotent DELETE, nothing
      saved on failure;
    - the mapper round-trip.
  - Integration:
    - repository save, read, remove and the unique index;
    - for each route, `401` without a token, with an invalid token and with a removed user;
    - `404` for an unknown exercise and for an invisible custom exercise, and `409` for an archived one;
    - PUT twice keeps the first `createdAtUtc`, and DELETE twice is `204`;
    - `GET` returns only the caller's favorites, newest first, asserting that another user's favorites are excluded;
    - an `ADMIN` can favorite any exercise but lists only their own.
  - OpenAPI: the three routes carry the `Bearer` requirement and their response codes.
  - Migration: `Up` creates the table and unique index, and `Down` removes them.
- No other deviations from the mapped template references are allowed.
