# Tasks

## 1. Domain

- [x] 1.1 Add `ExerciseFavorite` in `src/WeightTracker.Domain/ExerciseFavorites/` (D1), following `RefreshToken.cs`, and add `test/WeightTracker.Unit/Domain/ExerciseFavorites/ExerciseFavoriteTests.cs`:
  - `Create` sets a fresh id, the user, the exercise, the date and `IsNew`;
  - `Rehydrate` restores every field with `IsNew` false.

  Verify `dotnet build WeightTracker.slnx` and the tests pass.

## 2. Persistence

- [x] 2.1 Add the migration `src/WeightTracker.Migrations/Migrations/<YYYYMMDDHHmm>_CreateFavoriteExercisesTable.cs` (D3), mirroring `202610301437_CreateTrainerAthletesTable.cs`. Add migration tests in `MigrationsTests`:
  - `Up` creates the table and the unique index;
  - a duplicate `(UserId, ExerciseId)` insert is a unique violation;
  - an unknown `ExerciseId` is a foreign key violation;
  - `Down` removes the table.

  Verify the tests pass.
- [x] 2.2 Add `ExerciseFavoriteEntity`, its `WeightTrackerDbContext` configuration and `ExerciseFavoriteMapper`. Add a round-trip test in `test/WeightTracker.Unit/Infra/Mappers/AggregateMapperTests.cs`, including UTC kind on `CreatedAtUtc`. Verify the tests pass.
- [x] 2.3 Add `IExerciseFavoriteRepository` and `ExerciseFavoriteRepository` (D3), and add `test/WeightTracker.Integration/Repositories/ExerciseFavoriteRepositoryTests.cs`:
  - save then read by user and exercise;
  - `GetAllByUser` is newest first and excludes other users;
  - `Remove` deletes the row.

  Verify the tests pass.
- [x] 2.4 Add `"FavoriteExercises"` to `ApiFactory.ResetDatabase` and delete favorites in `ApiFactory.RemoveUser` (D5). Verify that a test which marks a favorite and then removes the user passes. Task 3.3 covers this.
- [x] 2.5 Review: compare the migration, entity, mapper and repository against the trainer-athlete references (column names, `timestamptz`, `ValueGeneratedNever`, mapping inside the repository, no `SaveChanges`). Check that the `Remove` XML doc states why this aggregate is deleted rather than changed.

## 3. Application and API

- [x] 3.1 Add `MarkExerciseFavoriteUseCase`, `UnmarkExerciseFavoriteUseCase` and `GetExerciseFavoritesUseCase` (D2), registered in `Program.cs`. Add unit tests in `test/WeightTracker.Unit/Application/ExerciseFavorites/`:
  - mark creates once and commits once;
  - re-mark returns the stored favorite without saving;
  - an unknown or invisible exercise is `ExerciseNotFoundException` with nothing saved;
  - an archived exercise is `ExerciseArchivedException` with nothing saved;
  - unmark removes and commits once;
  - unmark of a non-favorite commits nothing;
  - unmark of an archived favorite succeeds;
  - list returns the repository order.

  Verify the tests pass.
- [x] 3.2 Add `ExerciseFavoritesController` and `ExerciseFavoriteResponse` (D4). Verify the build passes.
- [x] 3.3 Add `test/WeightTracker.Integration/ExerciseFavorites/ExerciseFavoritesEndpointTests.cs` covering every scenario in `specs/exercise-favorites/spec.md`:
  - per route: `401` without a token, with an invalid token, and with a removed user (one of them after marking a favorite);
  - mark by `USER`, `TRAINER` and `ADMIN`;
  - idempotent mark keeps `createdAtUtc`;
  - `404` for an unknown exercise and for an invisible custom exercise, and `409` for an archived one;
  - an athlete marks the trainer's exercise through a published workout;
  - idempotent unmark, unmark `404`, and unmark after archiving;
  - list newest first, empty list, other users excluded, admin sees only their own, archived favorite still listed.

  Custom exercises and workouts are set up through the API with `AuthenticatedClientFor`. Verify the tests pass.
- [x] 3.4 Extend `OpenApiDocumentTests`: the three routes carry the `Bearer` requirement and their response codes. Verify the test passes.
- [x] 3.5 Review: compare the use cases, controller, DTO and tests against `UseCases/TrainerAthletes/`, `TrainerAthletesController` and the trainer-athlete endpoint tests (caller passed explicitly, `ExceptionMapper.Map` in every catch, labelled Arrange/Act/Assert with one Act).

## 4. Verification

- [x] 4.1 Run the template drift check from `AGENTS.md` and verify it prints nothing.
- [x] 4.2 With Docker running, run `dotnet build WeightTracker.slnx` and `dotnet test WeightTracker.slnx` and verify both suites pass with no warnings.
- [x] 4.3 Run `openspec validate add-exercise-favorites --strict` and verify it passes.

## Workflow follow-up

- The front refreshes its OpenAPI snapshot and adds the favorites filter to its exercise picker.
- Archive the change after review.
