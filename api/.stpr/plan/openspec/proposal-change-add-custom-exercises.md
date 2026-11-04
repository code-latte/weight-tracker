## Change

Let trainers and athletes create their own exercises, next to the shared system catalog

## Objective

Exercises are a system-wide catalog, maintained through migrations, and every trainer uses the same list. Trainers
need exercises the catalog does not have (their own variants, equipment, drills), and athletes planning their own
workouts need the same. This change lets a `TRAINER` or a `USER` create, edit and archive exercises they own. They
use them in their workouts, and the system catalog stays exactly as it is.

## Capabilities

- `POST /exercises`: a `TRAINER` or `USER` creates an exercise they own, named only in the language the front sends.
- `PUT /exercises/{id}`: the owner or an `ADMIN` edits a custom exercise.
- `DELETE /exercises/{id}`: the owner or an `ADMIN` archives a custom exercise. It stops being offered for new
  workouts, and workouts that already use it keep it.
- `GET /exercises` stays public. Anonymous callers get the system catalog. Authenticated callers also get their own
  exercises and the custom exercises used in workouts they can see.
- `GET /exercises/{id}` stays public for system exercises. A custom exercise is returned only to callers who may see
  it.
- Adding an exercise to a workout accepts a custom exercise when it belongs to the workout's editor of record.

## Module Classification

- Exercises (domain) -> **mutable aggregate** (reclassified, no longer a read-only catalog) ->
  `src/WeightTracker.Domain/Workouts/Workout.cs` for `Create` / `Rehydrate` / `IsNew` and action methods, and
  `src/WeightTracker.Domain/Users/User.cs` (`Deactivate` / `DeactivatedAtUtc`) for the archive action
- Exercises (application) -> mutable aggregate use cases: create, update, archive, plus caller-scoped reads ->
  `src/WeightTracker.Application/UseCases/Workouts/` (`CreateWorkoutUseCase`, `UpdateWorkoutUseCase`) and
  `src/WeightTracker.Application/Services/WorkoutEditAuthorizer.cs` for the edit rule
- Exercises (port / infra) -> existing `IExerciseRepository` gains `Save` and caller-scoped reads ->
  `src/WeightTracker.Infra/Repositories/WorkoutRepository.cs` (`Save` with `IsNew`, multi-table mapping),
  `src/WeightTracker.Infra/Mappers/ExerciseMapper.cs`
- Exercises (API) -> REST controller gains protected writes; the reads stay `[AllowAnonymous]` with optional caller
  resolution -> `src/WeightTracker.Api/Endpoints/Workouts/WorkoutsController.cs`
- Workouts (application) -> `AddWorkoutExerciseUseCase` gains the custom-exercise usage rule ->
  `src/WeightTracker.Application/Services/WorkoutEditAuthorizer.cs` (rule spanning two aggregates lives in Application)
- Persistence -> one FluentMigrator migration on `Exercises` ->
  `src/WeightTracker.Migrations/Migrations/202610291200_AddActivationToUsers.cs`

**Reclassification.** The system exercises keep being read-only: no API path may create, edit or archive an exercise
whose `OwnerId` is null. The module as a whole is a mutable aggregate, because custom exercises change through user
actions. This replaces the "read-only catalog" classification in the `exercises` spec and in
`proposal-change-exercises-read-only-catalog.md`.

## Canonical Template Mapping

- `Exercise.Create`, `UpdateDetails`, `Archive` -> `Workout.cs` (`Create` with `IsNew = true`, action methods that
  validate before mutating) and `User.Deactivate` (timestamped state change, rejected when already applied) -> nearest
  aggregate action references
- `ExerciseEditAuthorizer` -> `WorkoutEditAuthorizer.cs` -> same shape: shared Application service, never inline
- Create / update / archive use cases -> `CreateWorkoutUseCase.cs`, `UpdateWorkoutUseCase.cs` -> one
  `SaveChangesAsync` per use case, caller passed explicitly
- Custom exercise usage in workouts -> `WorkoutEditAuthorizer` + `AddWorkoutExerciseUseCase` -> cross-aggregate rule
  in Application
- Caller-scoped `GET /exercises` -> `GetAllWorkoutsUseCase.cs` -> role- and ownership-scoped read
- Repository `Save` -> `WorkoutRepository.Save` -> tracked update vs. insert on `IsNew`, child rows replaced
- Migration -> `202610291200_AddActivationToUsers.cs` -> adds nullable columns to an existing table
- Endpoints and DTOs -> `WorkoutsController` (`[ProducesResponseType]`, `ExceptionMapper.Map` in every catch) and
  `Endpoints/Workouts/Dtos/CreateWorkoutRequest.cs` (structural validation only)
- Tests -> `test/WeightTracker.Unit/Domain/Workouts/WorkoutTests.cs`,
  `test/WeightTracker.Unit/Application/Workouts/*UseCaseTests.cs`,
  `test/WeightTracker.Integration/Exercises/ExerciseReadEndpointsTests.cs`,
  `test/WeightTracker.Integration/Workouts/WorkoutExerciseAddEndpointTests.cs`

## State And Mutability

- `Exercises` gains `OwnerId` (`uuid`, nullable, FK to `Users(Id)` without cascade, index `IX_Exercises_OwnerId`) and
  `ArchivedAtUtc` (`timestamptz`, nullable).
- Existing rows keep `OwnerId = NULL` and `ArchivedAtUtc = NULL`, so the whole current catalog becomes the system
  catalog unchanged.
- `ExerciseTranslations` does not change. A custom exercise is created with exactly one translation.
- Custom exercises change through create, update and archive. System exercises change only through migrations.
- Exercises are never hard-deleted through the API, because `WorkoutExercises.ExerciseId` references them.

## Business Rules

- **Ownership.** `OwnerId` is set on creation to the caller and never changes. `null` means a system exercise.
- **Who creates.** `TRAINER` and `USER` may create. `ADMIN` may not (`403`).
- **Creation fields.** `languageCode`, `name`, `muscle`, `type`, optional `videoUrl`. `FreeExerciseDbId` is always
  `null`. Exactly one translation is created, in `languageCode`. The usual value object rules apply (`ExerciseName`,
  `LanguageCode`, `Muscles`, `ExerciseTypes`, `Url`).
- **No name uniqueness.** Two exercises may share a name, whoever owns them.
- **Who edits and archives.** The owner, or an `ADMIN`, through `ExerciseEditAuthorizer`. Anyone else gets `403`.
- **System exercises are protected by the aggregate.** `UpdateDetails` and `Archive` reject a system exercise
  whatever the caller's role: `403` through the authorizer, and a domain exception as a second guard.
- **Edit.** `PUT` replaces `muscle`, `type` and `videoUrl`, and sets the name of the given `languageCode`. That
  replaces the translation if it exists and adds it if not. Other languages are kept.
- **Archive.** Sets `ArchivedAtUtc`. Archiving an archived exercise, or editing an archived exercise, is `409`.
- **Visibility.** A custom exercise is visible to:
  - its owner;
  - any `ADMIN`;
  - any caller who can see a workout that uses it.
  Everyone else gets `404` (not `403`), so the exercise's existence is not leaked.
- **`GET /exercises`** returns:
  - anonymous caller: system exercises only;
  - authenticated caller: system exercises, their own (archived included) and the custom exercises used in workouts
    they can see (archived included);
  - `ADMIN`: system exercises and every custom exercise.

  The response carries `ownerId` and `archivedAtUtc`, so the front can mark custom exercises and hide archived ones
  from the picker. Workout details return exercise ids only, so the client must be able to resolve every exercise in
  the workouts it can see.
- **Use in a workout.** A custom exercise can be added to a workout only when its `OwnerId` equals the workout's
  editor of record: `TrainerId` when set, otherwise `UserId`. This matches the workout edit rule.
  - The exercise is invisible to the caller: `404`, as an unknown catalog exercise is today.
  - It is visible but owned by someone else (for example an admin adding trainer A's exercise to trainer B's
    workout): `409`.
  - It is archived: `409`.
  - Exercises already in a workout stay there, and their sets and effort stay editable, even when archived or when
    the owner is deactivated.
- **Deactivated owners.** Their exercises keep existing and stay visible through workouts. The owner cannot sign in to
  manage them.

## Roles And Visibility

| Action | Anonymous | USER | TRAINER | ADMIN |
| --- | --- | --- | --- | --- |
| `GET /exercises` | system | system + own + used in visible workouts | same as USER | everything |
| `GET /exercises/{id}` | system only | system, own, used in visible workouts | same as USER | everything |
| `POST /exercises` | `401` | creates own | creates own | `403` |
| `PUT /exercises/{id}` | `401` | own only | own only | any custom |
| `DELETE /exercises/{id}` | `401` | own only | own only | any custom |

For every role, system exercises are never writable through the API.

## Business Identifiers And Mutability

- `Exercise.Id` is a unique technical identifier, generated with `Id.New()` on creation and immutable.
- `Exercise.OwnerId` is a reference to `User.Id`. It is immutable after creation and not unique.
- `Exercise.FreeExerciseDbId` stays unique where set and is always `null` for custom exercises.
- Exercise names are not identifiers and are not unique.

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## Out Of Scope

- Sharing a custom exercise with other users, or a trainer sharing it with their athletes for their own planning.
- Promoting a custom exercise to the system catalog.
- Hard deletion, and restoring an archived exercise.
- Creating or editing several translations at once.
- Searching, filtering or paginating `GET /exercises`.
- Any front change. The front plan follows after the OpenAPI snapshot refresh.

## Notes

- **Optional authentication on public reads (decided).** `[AllowAnonymous]` actions don't resolve `CurrentUser` today
  (`AuthorizedController`). Both reads need an opt-in way to resolve the caller when a bearer token is present:
  - no token: anonymous;
  - valid token: authenticated;
  - present but invalid or expired token: `401`. Otherwise a client with an expired token would silently lose its
    custom exercises instead of refreshing.

  This is the one deviation from the public-endpoint test rule in `AGENTS.md`. These two routes get an invalid-token
  case, and the opt-in must not change any other `[AllowAnonymous]` action.
- **Template impact (decided).** `Exercise.cs` and `GetAllExercisesUseCase.cs` are canonical copies in
  `.stpr/template/src`, and `.stpr/template/README.md` names `ExerciseUseCase` as the read-only use case reference.
  Both copies are updated with the code. The README entry becomes "caller-scoped read use case", because no read-only
  module remains. The `exercises` spec's read-only purpose and its "Exercises cannot be written through the API"
  requirement are replaced, not patched.
- **Domain shape.** `Exercise` gains `Id? OwnerId`, `DateTime? ArchivedAtUtc`, `bool IsSystem`, `bool IsArchived`.
  - `Create(ownerId, translation, muscle, type, videoUrl)` sets `IsNew = true`.
  - `UpdateDetails(translation, muscle, type, videoUrl)` and `Archive(nowUtc)` reject system and archived exercises.
  - The constructor keeps validating duplicate translation languages.
- **Repository.** `IExerciseRepository` gains:
  - `Save(Exercise)`: insert on `IsNew`, otherwise update the row and replace the translations, as `WorkoutRepository`
    does with sets;
  - `GetVisibleTo(callerId, role)`: system, plus owned, plus used in workouts the caller can see. The workout part
    reuses the existing workout scoping (owner, or trainer, minus hidden prescribed drafts).

  Keep the number of queries bounded. Never query per exercise.
- **Exceptions.** An invisible custom exercise reuses the existing `ExerciseNotFoundException` (`404`). New:
  `ExerciseArchivedException` (`409`), `SystemExerciseNotEditableException` (`403`, domain guard),
  `ExerciseNotUsableInWorkoutException` (`409`). Each gets an explicit mapping in `ExceptionMapper`.
- **Seed.** Add one custom exercise owned by the seeded trainer, used in a prescribed workout of the seeded user, and
  one owned by the seeded user. That lets integration tests cover visibility through workouts.
- **Tests:**
  - Unit:
    - `Exercise.Create` / `UpdateDetails` / `Archive`, including rejections for system and archived exercises;
    - `ExerciseEditAuthorizer` branches;
    - each new use case;
    - the usage rule in `AddWorkoutExerciseUseCase`;
    - the mapper round-trip with owner and archive.
  - Integration, per route:
    - `POST`, `PUT` and `DELETE` each with `401` no token, `401` invalid token, `403`, `404`, `400` and `409` where
      they apply;
    - `GET /exercises` for anonymous, user, trainer and admin, asserting what is excluded (another user's custom
      exercises);
    - `GET /exercises/{id}` returning `404` for an invisible custom exercise;
    - adding a custom exercise to a workout: allowed for the editor of record, `409` for someone else's, `409` when
      archived;
    - an archived exercise still read back inside existing workouts.
  - OpenAPI: the new write routes and the `ownerId` / `archivedAtUtc` fields.
- No other deviations from the mapped template references are allowed.
