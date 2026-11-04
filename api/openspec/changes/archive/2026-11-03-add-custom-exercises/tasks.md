# Tasks

## 1. Domain: owned and archivable exercises

- [x] 1.1 Add `OwnerId`, `ArchivedAtUtc`, `IsSystem`, `IsArchived`, `Create`, `UpdateDetails` and `Archive` to `Exercise`, with trailing optional `ownerId` / `archivedAtUtc` on `Rehydrate` (D1). Add `SystemExerciseNotEditableException` and `ExerciseArchivedException` in `src/WeightTracker.Domain/Exceptions`. Verify `dotnet build WeightTracker.slnx` passes with the seed and existing tests unchanged.
- [x] 1.2 Extend `test/WeightTracker.Unit/Domain/Exercises/ExerciseTests.cs`:
  - `Create` sets the owner, one translation, no dataset id and `IsNew`;
  - `UpdateDetails` replaces a same-language translation and adds a new language while keeping the others;
  - `Archive` sets the date;
  - system and archived exercises reject both actions and stay unchanged.

  Verify the tests pass.
- [x] 1.3 Review: compare the actions against `Workout.cs` and `User.Deactivate` (validation before mutation, XML exception docs, private constructor). Copy `Exercise.cs` to `.stpr/template/src/WeightTracker.Domain/Exercises/Exercise.cs` and verify `cmp` reports no drift for it.

## 2. Persistence

- [x] 2.1 Add the migration `src/WeightTracker.Migrations/Migrations/<timestamp>_AddOwnerAndArchiveToExercises.cs` (D6), mirroring `202610291200_AddActivationToUsers.cs`: nullable `OwnerId` with `FK_Exercises_Owner` and `IX_Exercises_OwnerId`, nullable `ArchivedAtUtc`, and a symmetric `Down()`. Verify that up and `--down` both succeed against a throwaway Postgres container, not the compose database.
- [x] 2.2 Add `OwnerId` / `ArchivedAtUtc` to `ExerciseEntity` and `WeightTrackerDbContext`, and map both ways in `ExerciseMapper`. Extend the exercise mapper unit tests with a round-trip of a custom archived exercise and of a system exercise. Verify the tests pass.
- [x] 2.3 Add `Save` and `GetSystemAndOwnedBy` to `IExerciseRepository` and `ExerciseRepository` (D6). Add integration repository tests:
  - insert a new custom exercise;
  - update one, replacing a translation and adding another;
  - archive one;
  - `GetSystemAndOwnedBy(null)` returns system only, and `GetSystemAndOwnedBy(owner)` adds only that owner's exercises.

  Copy `IExerciseRepository.cs` to its `.stpr/template` counterpart. Verify the tests pass and `cmp` reports no drift.
- [x] 2.4 Review: compare the migration, EF configuration, mapper and `Save` against `AddActivationToUsers`, `ExerciseMapper` and `WorkoutRepository.Save` (column names, foreign key without cascade, children replaced, no `SaveChanges` in the repository).

## 3. Application: visibility, authorization and use cases

- [x] 3.1 Extract `WorkoutVisibility` from `GetAllWorkoutsUseCase` (D2) and make the use case call it. Verify `GetAllWorkoutsUseCaseTests` still pass unchanged apart from construction, and add `WorkoutVisibility` unit tests for each role, the hidden draft and the unknown role.
- [x] 3.2 Add `ExerciseVisibility` and `ExerciseEditAuthorizer` (D3, D4), with unit tests:
  - system exercise visible to anonymous callers; custom exercise invisible to anonymous callers;
  - owner and admin see a custom exercise; a workout-visible caller sees it; a hidden-draft-only caller does not;
  - edit allowed for the owner and an admin, rejected for others, and rejected for a system exercise even for an admin.

  Verify the tests pass.
- [x] 3.3 Change `GetAllExercisesUseCase` and `GetExerciseByIdUseCase` to take an optional caller (D3). Unit tests cover anonymous, `USER`, `TRAINER` and `ADMIN` results, assert the excluded exercises, and check the bounded repository calls. Copy `GetAllExercisesUseCase.cs` to its template counterpart. Verify the tests pass and there's no drift.
- [x] 3.4 Add `CreateExerciseUseCase`, `UpdateExerciseUseCase` and `ArchiveExerciseUseCase` (D4), each with one `SaveChangesAsync`. Unit tests cover:
  - admin create rejected before any write;
  - visibility checked before ownership (`404` before `403`);
  - archive uses `IClock`;
  - nothing saved on any failure.

  Verify the tests pass.
- [x] 3.5 Add the usage rule to `AddWorkoutExerciseUseCase` (D5) and `ExerciseNotUsableInWorkoutException` in `src/WeightTracker.Application/Exceptions`. Extend `AddWorkoutExerciseUseCaseTests`:
  - an own custom exercise is accepted for the prescribing trainer and for a self-planned owner;
  - someone else's exercise is `ExerciseNotUsableInWorkoutException`;
  - an archived exercise is `ExerciseArchivedException`;
  - an invisible exercise is `ExerciseNotFoundException`;
  - system exercises are unchanged.

  Verify the tests pass.
- [x] 3.6 Register the new services and use cases in `Program.cs`. Verify the build passes and the API starts in the integration host (any integration test passes).
- [x] 3.7 Review: compare `WorkoutVisibility`, `ExerciseVisibility`, `ExerciseEditAuthorizer` and the use cases against `WorkoutEditAuthorizer`, `GetAllWorkoutsUseCase`, `CreateWorkoutUseCase` and `UpdateWorkoutUseCase` (shared services, caller passed explicitly, exception docs).

## 4. API

- [x] 4.1 Add `[OptionalCaller]` and its handling in `AuthorizedController` (D7). Integration tests:
  - `GET /exercises` with a garbage token is `401`;
  - with a token of a removed user is `401`;
  - without a header the result is anonymous;
  - `POST /auth/login` with a garbage `Authorization` header still succeeds, proving other anonymous actions are unaffected.

  Verify the tests pass.
- [x] 4.2 Map `SystemExerciseNotEditableException` (`403`), `ExerciseArchivedException` and `ExerciseNotUsableInWorkoutException` (`409`) in `ExceptionMapper`, with cases in `ExceptionMapperTests`. Verify the tests pass.
- [x] 4.3 Add `ownerId` / `archivedAtUtc` to `ExerciseResponse`, and make both reads `[AllowAnonymous, OptionalCaller]` passing `CurrentUser`. Rewrite `ExerciseReadEndpointsTests` for the `exercises` delta:
  - the three "should not write" tests are removed with their requirement;
  - add anonymous system-only, user, trainer and admin lists, asserting exclusions;
  - add athlete visibility through a published workout and exclusion through a hidden draft;
  - add an archived own exercise still listed, and a `404` for an invisible custom exercise by id.

  Custom exercises are created in Arrange through the API. Verify the tests pass.
- [x] 4.4 Add `CreateExerciseRequest` / `UpdateExerciseRequest` and the `POST`, `PUT {id}` and `DELETE {id}` actions with `[ProducesResponseType]` (D7). Add `test/WeightTracker.Integration/Exercises/ExerciseWriteEndpointsTests.cs` covering every `custom-exercises` scenario, with per route:
  - `401` without a token, with an invalid token, and with a removed user;
  - `403`, `404`, `409` and `400` where they apply;
  - success for `TRAINER` and `USER`, and admin edit or archive of a custom exercise;
  - admin `403` on system exercises and on create.

  Verify the tests pass.
- [x] 4.5 Extend `WorkoutExerciseAddEndpointTests` for the `workout-exercises` delta:
  - the prescribing trainer adds their own custom exercise (`201`);
  - the self-planned owner adds theirs (`201`);
  - an admin adds another owner's exercise (`409`);
  - an archived exercise is `409`;
  - an invisible exercise is `404`.

  Extend `WorkoutExerciseUpdateEndpointTests` so that an entry whose exercise was archived stays updatable (`200`). Extend the workout list tests so that an archived exercise still shows its translations. Verify the tests pass.
- [x] 4.6 Extend `OpenApiDocumentTests`:
  - the three write routes carry the `Bearer` requirement and their response codes;
  - `ExerciseResponse` documents a nullable `ownerId` and `archivedAtUtc`.

  Verify the test passes.
- [x] 4.7 Review: compare `ExercisesController`, its DTOs and tests against `WorkoutsController`, `CreateWorkoutRequest` and the workout endpoint tests (structural validation only, `ExceptionMapper.Map` in every catch, Arrange/Act/Assert with one Act).

## 5. Template, docs and verification

- [x] 5.1 Reword the `ExerciseUseCase` row and the read-only quick-selection rule in `.stpr/template/README.md` to "caller-scoped read use case" (D8), and update the `ExercisesController` class summary that calls exercises a read-only catalog. Verify by reading both files.
- [x] 5.2 Run the template drift check from `AGENTS.md` and verify it prints nothing.
- [x] 5.3 With Docker running, run `dotnet build WeightTracker.slnx` and `dotnet test WeightTracker.slnx` and verify both suites pass with no warnings.
- [x] 5.4 Run `openspec validate add-custom-exercises --strict` and verify it passes.

## Workflow follow-up

- At archive, edit the `## Purpose` of `openspec/specs/exercises/spec.md` directly so it no longer describes a read-only catalog (a delta cannot change Purpose).
- Mark `.stpr/plan/openspec/proposal-change-exercises-read-only-catalog.md` as superseded by this change.
- The front refreshes its OpenAPI snapshot, sends the token on exercise reads, marks custom exercises and hides archived ones from its picker.
