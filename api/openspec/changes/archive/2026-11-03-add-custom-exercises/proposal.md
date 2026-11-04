# Proposal

## Why

Exercises are a system-wide catalog changed only through migrations, so every trainer works from the same list. Trainers need exercises the catalog lacks (their own variants, equipment and drills), and athletes who plan their own workouts need the same. This change lets trainers and athletes create, edit and archive their own exercises and use them in their workouts. The system catalog stays as it is.

Source brief: `.stpr/plan/openspec/proposal-change-add-custom-exercises.md`.

## What Changes

- **BREAKING (spec):** Exercises stop being a read-only catalog. The requirement "Exercises cannot be written through the API" is removed. System exercises (no owner) stay read-only through every API path.
- New `POST /exercises`: a `TRAINER` or `USER` creates an exercise they own, with a single translation in the language the front sends. `ADMIN` gets `403`.
- New `PUT /exercises/{id}`: the owner or an `ADMIN` edits a custom exercise. It replaces `muscle`, `type` and `videoUrl`, and sets the name for one language.
- New `DELETE /exercises/{id}`: the owner or an `ADMIN` archives a custom exercise. Archiving is a soft delete, and workouts that already use the exercise keep it.
- `GET /exercises` and `GET /exercises/{id}` stay public:
  - Anonymous callers see system exercises only.
  - Authenticated callers also see their own exercises and custom exercises used in workouts they can see.
  - `ADMIN` sees everything.
  - A present but invalid token on these two routes is `401`.
- Exercise responses gain `ownerId` (or `null`) and `archivedAtUtc` (or `null`).
- `POST /workouts/{workoutId}/exercises` accepts a custom exercise only when it belongs to the workout's editor of record. Someone else's exercise is `409`, an archived one is `409`, and an exercise the caller cannot see is `404`.
- Database: `Exercises` gains `OwnerId` and `ArchivedAtUtc`, both nullable. Existing rows become the system catalog.

## Capabilities

### New Capabilities

- `custom-exercises`: creating, editing and archiving exercises owned by a trainer or athlete. Covers ownership, who may change them, protection of system exercises, the single-language rule and archiving.

### Modified Capabilities

- `exercises`: the read-only catalog becomes a system catalog plus caller-scoped custom exercises. The read requirements gain visibility rules and the `ownerId` / `archivedAtUtc` fields, and "Exercises cannot be written through the API" is removed in favour of `custom-exercises`.
- `workout-exercises`: adding an exercise to a workout gains the custom-exercise usage rule.

## Canonical Template Mapping

| Area | Classification | Change | Template reference |
| --- | --- | --- | --- |
| Exercises (domain) | **mutable aggregate** (reclassified from read-only catalog) | `OwnerId`, `ArchivedAtUtc`, `Create`, `UpdateDetails`, `Archive` | `Domain/Workouts/Workout.cs` (Create / IsNew / action methods), `Domain/Users/User.cs` (`Deactivate`) |
| Exercises (application) | mutable aggregate use cases | create, update and archive use cases, caller-scoped reads, `ExerciseEditAuthorizer` | `UseCases/Workouts/CreateWorkoutUseCase.cs`, `UpdateWorkoutUseCase.cs`, `Services/WorkoutEditAuthorizer.cs`, `GetAllWorkoutsUseCase.cs` |
| Workouts (application) | mutable aggregate use case | usage rule in `AddWorkoutExerciseUseCase`; workout scoping extracted for reuse | `Services/WorkoutEditAuthorizer.cs` (rule across aggregates in Application) |
| Exercises (port / infra) | existing repository | `Save`, `GetSystemAndOwnedBy` | `Repositories/WorkoutRepository.cs` (`Save` on `IsNew`, children replaced), `Mappers/ExerciseMapper.cs` |
| Exercises (API) | REST controller | write routes; optional caller on public reads | `Endpoints/Workouts/WorkoutsController.cs`, `Endpoints/AuthorizedController.cs`, `Endpoints/RequireRoleAttribute.cs` |
| Persistence | migration | two nullable columns | `Migrations/202610291200_AddActivationToUsers.cs` |

The brief makes the reclassification explicit, which satisfies the stop case in `STPR_INVARIANTS.md`. The template impact is part of this change:
- `.stpr/template/src` copies of `Exercise.cs`, `IExerciseRepository.cs` and `GetAllExercisesUseCase.cs` are canonical, so they are updated with the code.
- The `.stpr/template/README.md` entry that names `ExerciseUseCase` as the read-only use case reference is reworded to "caller-scoped read use case".

One documented deviation from `AGENTS.md`: the two public exercise reads get an invalid-token `401` case.

**Business identifiers:**
- `Exercise.Id` comes from `Id.New()` on creation and is immutable.
- `Exercise.OwnerId` references `User.Id`. It is immutable and not unique.
- `FreeExerciseDbId` stays unique where set and is `null` for custom exercises.
- Names are not unique.

## Impact

- **Domain**: `Exercise` (new fields and methods), new exceptions.
- **Application**:
  - new `CreateExerciseUseCase`, `UpdateExerciseUseCase`, `ArchiveExerciseUseCase`, `ExerciseEditAuthorizer`, `WorkoutVisibility`;
  - changed `GetAllExercisesUseCase`, `GetExerciseByIdUseCase`, `GetAllWorkoutsUseCase` (uses `WorkoutVisibility`) and `AddWorkoutExerciseUseCase`;
  - `IExerciseRepository` gains `Save` and `GetSystemAndOwnedBy`.
- **Infra**: `ExerciseEntity`, `WeightTrackerDbContext`, `ExerciseMapper`, `ExerciseRepository`, a migration and the seed.
- **API**:
  - `ExercisesController` gets three new actions and caller-aware reads;
  - new request DTOs; `ExerciseResponse` gains `ownerId` and `archivedAtUtc` (workout responses are unchanged);
  - a new `[OptionalCaller]` attribute handled in `AuthorizedController`;
  - `ExceptionMapper` mappings;
  - `Program.cs` registrations.
- **Specs**: `exercises` (modified), `workout-exercises` (modified), `custom-exercises` (new).
- **Template**: three copies and one README entry, listed above.
- **Clients**: the front refreshes its OpenAPI snapshot, sends the token on exercise reads, and hides archived exercises from its picker.
