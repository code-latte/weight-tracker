# Design

## Context

See `proposal.md` for the motivation and the specs for the behaviour. The current state that shapes the approach:

- `Exercise` is an `AggregateRoot` with only `Rehydrate`, no mutation methods, no owner. It spans `Exercises` and `ExerciseTranslations` through `ExerciseMapper`. `IExerciseRepository` has three reads (`GetAll`, `GetAllByIds`, `GetById`) and no `Save`.
- `ExercisesController` inherits `AuthorizedController`, and both reads are `[AllowAnonymous]`. `AuthorizedController.OnActionExecutionAsync` skips caller resolution for anonymous actions, so `CurrentUser` is `null` there. The JWT middleware still runs, so `User` is authenticated when a valid bearer token is sent.
- Workout scoping (`ADMIN` sees all; `TRAINER` sees owned or prescribed workouts; `USER` sees owned ones; hidden prescribed drafts are removed through `Workout.IsHiddenFrom`) lives as private methods in `GetAllWorkoutsUseCase`.
- `AddWorkoutExerciseUseCase` authorizes with `WorkoutEditAuthorizer` and then throws `ExerciseNotFoundException` when `GetById` returns `null`.
- `WorkoutExercises.ExerciseId` is a foreign key to `Exercises`, so exercises cannot be deleted while used.
- `IClock` (implemented by `SystemClock`) supplies the current time to Application.
- `.stpr/template/src` holds copies of `Exercise.cs`, `IExerciseRepository.cs` and `GetAllExercisesUseCase.cs`.

## Goals / Non-Goals

**Goals:**
- One `Exercise` aggregate and one table for system and custom exercises, so workouts do not change.
- Every visibility and ownership decision lives in Application or Domain, never in SQL that duplicates a domain rule.
- A bounded number of queries per request.

**Non-Goals:**
- Sharing, promoting, restoring or hard-deleting custom exercises.
- Any change to workout responses, workout persistence or other `[AllowAnonymous]` actions.

## Decisions

### D1. Domain: owner, archive and actions on `Exercise`

- **New members:** `Id? OwnerId` (immutable), `DateTime? ArchivedAtUtc`, `bool IsSystem => OwnerId is null` and `bool IsArchived => ArchivedAtUtc is not null`.
- **`Create(Id ownerId, ExerciseTranslation translation, Muscles muscle, ExerciseTypes type, Url? videoUrl)`:** `Id.New()`, `IsNew = true`, `FreeExerciseDbId = null`, a single translation.
- **`Rehydrate`:** gains trailing `Id? ownerId = null, DateTime? archivedAtUtc = null`, so the seed, the mapper and existing tests compile unchanged.
- **`UpdateDetails(ExerciseTranslation translation, Muscles muscle, ExerciseTypes type, Url? videoUrl)`:** replaces the translation of `translation.LanguageCode` or adds it.
- **`Archive(DateTime nowUtc)`:** mirrors `User.Deactivate`.

Both actions validate before mutating:
- System exercise: `SystemExerciseNotEditableException`, domain, mapped to `403`.
- Archived exercise: `ExerciseArchivedException`, domain, mapped to `409`.
- Muscle and type keep the existing `EnsureDefined` checks.

*Alternative:* a separate `CustomExercise` aggregate and table. Rejected because workouts reference exercises by a single `ExerciseId` foreign key, and two tables would make that reference polymorphic.

### D2. Application: shared workout scoping

Extract the private scoping of `GetAllWorkoutsUseCase` into an Application service, `Services/WorkoutVisibility`, with `Task<IReadOnlyList<Workout>> VisibleTo(Id callerId, Role callerRole, CancellationToken ct)`. It keeps the same repository calls, the same `IsHiddenFrom` filter and the same `UnauthorizedOperationException` for an unknown role. `GetAllWorkoutsUseCase` calls it, and its behaviour and tests stay unchanged. Exercise visibility reuses it, so the hidden-draft rule is not duplicated.

### D3. Application: exercise visibility and reads

- **`GetAllExercisesUseCase.Execute(AuthenticatedUser? caller, ct)`:**
  - `null` caller: `IExerciseRepository.GetSystemAndOwnedBy(null)`, which returns system exercises only.
  - `ADMIN`: `GetAll()`.
  - `USER` / `TRAINER`:
    1. `GetSystemAndOwnedBy(caller.Id)`;
    2. `WorkoutVisibility.VisibleTo(...)`, collecting the referenced exercise ids not yet loaded;
    3. `GetAllByIds(those ids)`, keeping only custom exercises (system ones are already in step 1).

    The result is the union of steps 1 and 3, with three queries at most.
- **`GetExerciseByIdUseCase.Execute(Id id, AuthenticatedUser? caller, ct)`:** loads by id, then `EnsureVisible`:
  - system: visible to everyone;
  - custom with no caller: `ExerciseNotFoundException`;
  - `ADMIN` or owner: visible;
  - otherwise visible only if a workout from `VisibleTo(...)` uses it, else `ExerciseNotFoundException`.
- **`ExerciseVisibility`** (Application service) holds `EnsureVisible` and `IsVisible`, so `GetExerciseById`, update, archive and `AddWorkoutExercise` share one rule.
- *Alternative:* one SQL query joining `WorkoutExercises` and `Workouts` with the hidden-draft condition. Rejected because it copies `Workout.IsHiddenFrom` into Infra.

### D4. Application: writes and authorization

- **`ExerciseEditAuthorizer.EnsureCanEdit(Exercise, Id callerId, Role callerRole, string operation)`:**
  - system exercise: `SystemExerciseNotEditableException`, for admins too;
  - `ADMIN`: allowed;
  - owner: allowed;
  - otherwise: `UnauthorizedOperationException`.

  It follows the shape of `WorkoutEditAuthorizer`.
- **`CreateExerciseUseCase(callerId, callerRole, translation, muscle, type, videoUrl)`:** an `ADMIN` gets `UnauthorizedOperationException` before any write. Otherwise it calls `Exercise.Create(callerId, ...)`, then `Save`, then `SaveChangesAsync` once.
- **`UpdateExerciseUseCase` / `ArchiveExerciseUseCase`:**
  1. load the exercise (`null` → `ExerciseNotFoundException`);
  2. `ExerciseVisibility.EnsureVisible` (`404` before `403`, so existence is not leaked);
  3. `ExerciseEditAuthorizer.EnsureCanEdit`;
  4. the aggregate action (archive uses `IClock.UtcNow`);
  5. `Save`, then `SaveChangesAsync` once.

### D5. Application: usage rule in `AddWorkoutExerciseUseCase`

After the existing `EnsureCanEdit` on the workout:
1. load the exercise (`null` → `ExerciseNotFoundException`);
2. `ExerciseVisibility.EnsureVisible` for the caller (`404`);
3. if it is custom:
   - archived → `ExerciseArchivedException` (`409`);
   - `OwnerId != (workout.TrainerId ?? workout.UserId)` → `ExerciseNotUsableInWorkoutException` (Application, `409`).

`UpdateWorkoutExerciseUseCase` doesn't change, so entries with archived exercises stay editable.

### D6. Infra and persistence

- **Migration** `AddOwnerAndArchiveToExercises_<timestamp>`:
  - `Exercises.OwnerId`: `AsGuid().Nullable()`, foreign key `FK_Exercises_Owner` to `Users(Id)` with no cascade, index `IX_Exercises_OwnerId`;
  - `Exercises.ArchivedAtUtc`: `AsDateTime().Nullable()`, with the same mapping the existing `DeactivatedAtUtc` column uses;
  - `Down()` drops the index, the foreign key and both columns.
- **EF:** `ExerciseEntity.OwnerId` (`Guid?`) and `ArchivedAtUtc` (`DateTime?`) with `HasColumnName`. `ExerciseMapper` maps both ways.
- **`IExerciseRepository`:**
  - `Save(Exercise, ct)`: insert on `IsNew`. Otherwise load the tracked entity with its translations, copy the scalars and replace the translations, as `WorkoutRepository` does with sets.
  - `GetSystemAndOwnedBy(Id? ownerId, ct)`: `OwnerId == null || OwnerId == ownerId`, `AsNoTracking`, translations included.

  No `SaveChanges` in the repository.
- **Seed: no change.** Seeded workouts are asserted by exact entry lists and exact id sets in many tests, and adding custom exercises there would ripple through them. Integration tests create custom exercises in Arrange through `POST /exercises` and attach them with `POST /workouts/{id}/exercises`, as the seeded trainer on `TrainedWorkoutId` (published) or `PrescribedDraftWorkoutId` (hidden draft). This follows the "write through the API" pattern of the existing endpoint tests.

### D7. API

- **`[OptionalCaller]` attribute** (`Endpoints/OptionalCallerAttribute.cs`, next to `RequireRoleAttribute`). In `AuthorizedController.OnActionExecutionAsync`, for anonymous actions carrying it:
  - no `Authorization` header: continue with `CurrentUser = null`;
  - header present and a valid `sub` claim: resolve `CurrentUser` as for protected actions (a deleted user → `401` through `ExceptionMapper`);
  - header present but the principal is not authenticated: `401`.

  Anonymous actions without the attribute keep today's path exactly.
- **`ExercisesController`:**
  - reads: `[AllowAnonymous, OptionalCaller]`, passing `CurrentUser`;
  - new protected actions: `POST` (`201`, `CreatedAtAction(GetById)`), `PUT {id}` (`200`) and `DELETE {id}` (`204`), each with `[ProducesResponseType]` for `400/401/403/404/409` as they apply, and `ExceptionMapper.Map` in every catch.
- **DTOs:**
  - `CreateExerciseRequest` / `UpdateExerciseRequest` in `Endpoints/Exercises/Dtos/`: `[Required] LanguageCode`, `[Required] Name`, `[Required] Muscle` and `Type` (string-enum converters as on existing DTOs), optional `VideoUrl`. Structural validation only, with value objects built in the controller `try`.
  - `ExerciseResponse` gains `Guid? OwnerId` and `DateTime? ArchivedAtUtc`, always serialized.
- **`ExceptionMapper`:** `SystemExerciseNotEditableException` → `403`; `ExerciseArchivedException` and `ExerciseNotUsableInWorkoutException` → `409`.
- **`Program.cs`:** registers the three use cases, `ExerciseEditAuthorizer`, `ExerciseVisibility` and `WorkoutVisibility`.

### D8. Template and specs

- Update the three template copies this change touches with the code: `.stpr/template/src/WeightTracker.Domain/Exercises/Exercise.cs`, `.../Application/Interfaces/IExerciseRepository.cs` and `.../UseCases/Exercises/GetAllExercisesUseCase.cs`.
- Reword the `ExerciseUseCase` row and the quick-selection rule in `.stpr/template/README.md` to "caller-scoped read use case".
- At archive, edit the `exercises` main spec `## Purpose` directly so it no longer says "read-only catalog". A delta cannot change Purpose.

## Risks / Trade-offs

- [Optional caller resolution weakens the anonymous path] → It is opt-in per action, and integration tests prove other `[AllowAnonymous]` routes, such as `POST /auth/login` with a stale header, behave as before.
- [Authenticated `GET /exercises` loads the caller's visible workouts with their sets] → This reuses the same query as `GET /workouts`. A dedicated projection is deferred until it is measured.
- [Tests need more Arrange steps] → A shared helper in the exercise test base creates and attaches a custom exercise through the API.
- [Clients without a token lose their custom exercises in the picker] → This is documented in the proposal (clients must send the token), and the front plan covers it.

## Migration Plan

1. Deploy the migration. It is additive, and existing rows become system exercises.
2. Deploy the API. Old clients keep working, because anonymous reads return the same catalog.
3. Rollback: `Down()` drops `OwnerId` and `ArchivedAtUtc` but keeps the rows, so every custom exercise turns into a system exercise visible to everyone, and archived ones become active again. Delete custom exercises and their workout entries first if that is not acceptable.
