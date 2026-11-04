# Proposal

## Why

The exercise list keeps growing (system catalog plus custom exercises), and users repeat the same handful of exercises. Users of every role need to mark favorites so the front can filter its picker by them.

Source brief: `.stpr/plan/openspec/proposal-change-add-exercise-favorites.md`.

## What Changes

- New `PUT /exercise-favorites/{exerciseId}`: the caller marks an exercise they can see as a favorite. It is idempotent: a repeat keeps the original `createdAtUtc`. It responds `200` with `{ exerciseId, createdAtUtc }`.
- New `DELETE /exercise-favorites/{exerciseId}`: the caller removes it from their favorites. It is idempotent and responds `204`.
- New `GET /exercise-favorites`: the caller's own favorites, newest first, as `{ exerciseId, createdAtUtc }`.
- An unknown exercise, or one the caller cannot see, is `404`. Marking an archived exercise is `409`. Every route requires authentication.
- New table `FavoriteExercises`, with a unique `(UserId, ExerciseId)` pair. No existing table, response or route changes.

## Capabilities

### New Capabilities

- `exercise-favorites`: marking, removing and listing a user's favorite exercises, and who may favorite which exercise.

### Modified Capabilities

None. `exercises`, `custom-exercises` and workout capabilities keep their requirements. The visibility rule is reused, not changed.

## Canonical Template Mapping

| Area | Classification | Change | Template reference |
| --- | --- | --- | --- |
| ExerciseFavorites (domain) | **mutable aggregate root** `ExerciseFavorite` | `Create` / `Rehydrate`, no mutators | `Domain/Auth/RefreshToken.cs`, `Domain/TrainerAthletes/TrainerAthlete.cs` |
| ExerciseFavorites (application) | use cases | mark, unmark and list use cases; reuse `ExerciseVisibility` | `UseCases/TrainerAthletes/`, `Services/ExerciseVisibility.cs` |
| ExerciseFavorites (port / infra) | new repository and mapper | `GetByUserAndExercise`, `GetAllByUser`, `Save`, `Remove` | `ITrainerAthleteRepository`, `TrainerAthleteRepository`, `TrainerAthleteMapper` |
| ExerciseFavorites (API) | REST controller | `ExerciseFavoritesController`, response DTO | `Endpoints/TrainerAthletes/TrainerAthletesController.cs` |
| Persistence | migration | `FavoriteExercises` table | `Migrations/202610301437_CreateTrainerAthletesTable.cs` |

Classification rationale: favorites are an aggregate, not a table managed from Application.
- Every persisted concept in this codebase is an aggregate root with its own repository and mapper. `RefreshToken` is the closest precedent: almost no behaviour, referenced by `UserId`.
- The invariants require repositories to take and return domain types only, and allow direct `DbContext` access only from seed code and the test bootstrap.
- Nesting favorites in `User` or `Exercise` was rejected in the brief.

No conflict with `.stpr/template/STPR_INVARIANTS.md` or the template was found. One operation has no existing reference: `Remove`, the first repository method that deletes an aggregate. It is justified because a favorite has no history worth keeping. The only `.stpr/template` copy touched is `ApiFactory.cs`, for the test bootstrap.

**Business identifiers:**
- `ExerciseFavorite.Id` comes from `Id.New()` and is immutable.
- `(UserId, ExerciseId)` is unique, and both are immutable references.

## Impact

- **Domain**: new `Domain/ExerciseFavorites/ExerciseFavorite.cs`.
- **Application**:
  - new `IExerciseFavoriteRepository`;
  - `MarkExerciseFavoriteUseCase`, `UnmarkExerciseFavoriteUseCase`, `GetExerciseFavoritesUseCase`;
  - reuses `ExerciseVisibility`, `IExerciseRepository` and `IClock`.
- **Infra**: `ExerciseFavoriteEntity`, the `WeightTrackerDbContext` configuration, `ExerciseFavoriteMapper`, `ExerciseFavoriteRepository`, and one migration.
- **API**: `ExerciseFavoritesController`, `ExerciseFavoriteResponse`, and `Program.cs` registrations. No new exception mappings.
- **Tests**: unit (aggregate, use cases, mapper) and integration (repository, endpoints, OpenAPI, migration).
- **Clients**: the front refreshes its OpenAPI snapshot and filters its picker by joining `GET /exercise-favorites` with `GET /exercises`.
