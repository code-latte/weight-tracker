## Change

Let trainers build reusable workout templates and create workouts for their athletes from them

## Objective

Today a trainer who prescribes the same session to several athletes, or repeats it across mesocycle weeks, has to
rebuild every exercise, set, effort and superseries by hand each time. This change adds **workout templates**: complete
workout content (name, description, exercises, sets, superseries) that belongs to a trainer, has no athlete, date,
status or mesocycle, and can be turned into a real workout for an athlete as often as needed.

A template is its own aggregate, not a workout with a null owner. A workout is a scheduled session with an owner, a
date, a lifecycle and a lock. A template has none of them, and making the workout owner optional would weaken
`Workout` invariants and force every workout read to exclude templates.

Creating a workout from a template **copies** the content. Later edits to the template never change workouts already
created from it, and deleting a template never affects them.

## Capabilities

- Create, read, list, update (name, description) and delete workout templates.
- Edit template content exactly as workout content is edited today: add, update and remove exercises, reorder them,
  create and break superseries.
- Create a workout for an athlete from a template, either standalone (with a date) or placed in a mesocycle week,
  following the same assignment and placement rules as `POST /workouts`.
- Admin can create a template on behalf of a trainer, and see and edit any template, without ever owning one.

## Module Classification

Domain areas, not modules (the architecture is layer-first; each area is a slice across the layer projects).

- Workouts (domain) -> new mutable aggregate root `WorkoutTemplate` in the existing area, reusing the `WorkoutExercise`
  inner entity and the workout value objects -> `.stpr/template/src/WeightTracker.Domain/Workouts`
- Workouts (domain) -> existing mutable aggregate `Workout`, one new factory to create a workout from a template ->
  `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs`
- Workout templates (application) -> mutable aggregate use cases, caller-scoped reads, a business authorization
  service -> `src/WeightTracker.Application/UseCases/Workouts`, `src/WeightTracker.Application/UseCases/Exercises`,
  `src/WeightTracker.Application/Services/WorkoutEditAuthorizer.cs`
- Workout templates (infra) -> new repository, mapper and EF entities over three tables ->
  `src/WeightTracker.Infra/Repositories/WorkoutRepository.cs`, `src/WeightTracker.Infra/Mappers/WorkoutMapper.cs`
- Workout templates (api) -> new protected controller -> `src/WeightTracker.Api/Endpoints/Workouts`

Templates are not a catalog. They are business state that changes through use (content edited, templates deleted),
and they are visible only to their trainer and admin.

## Canonical Template Mapping

- `WorkoutTemplate` aggregate -> `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs` -> same content
  operations, `Create` / `Rehydrate` / `IsNew`, private constructor, exercise list exposed as a copied snapshot
- Template exercises -> `src/WeightTracker.Domain/Workouts/WorkoutExercise.cs` -> reused as is: it holds no reference to
  its parent, so both aggregates can own it
- `Workout.CreateFromTemplate` -> `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs` (`Create`) -> new
  factory in the same aggregate
- Template use cases -> `src/WeightTracker.Application/UseCases/Workouts` (`CreateWorkoutUseCase`,
  `UpdateWorkoutUseCase`, `AddWorkoutExerciseUseCase`, `UpdateWorkoutExerciseUseCase`, `RemoveWorkoutExerciseUseCase`,
  `ReorderWorkoutExercisesUseCase`, `CreateSuperSeriesUseCase`, `BreakSuperSeriesUseCase`) -> same operations on
  the template aggregate
- List templates -> `src/WeightTracker.Application/UseCases/Exercises` (`GetAllExercisesUseCase`) -> read scoped by caller
- Create workout from template -> `src/WeightTracker.Application/UseCases/Workouts/CreateWorkoutUseCase.cs` -> reuses
  `WorkoutAssignmentValidator` and `MesocyclePlacementValidator` so the rules stay identical
- `WorkoutTemplateEditAuthorizer` -> `src/WeightTracker.Application/Services/WorkoutEditAuthorizer.cs` -> one shared rule
  for every template use case, called before touching the aggregate
- Repository and mapper -> `src/WeightTracker.Infra/Repositories/WorkoutRepository.cs` +
  `src/WeightTracker.Infra/Mappers/WorkoutMapper.cs` -> multi-table aggregate, `Save` branching on `IsNew`, mapping
  inside the repository
- EF configuration -> `src/WeightTracker.Infra/Persistence/WeightTrackerDbContext.cs`
- Migrations -> `src/WeightTracker.Migrations/Migrations/202610080931_CreateWorkoutsTable.cs`,
  `202610080938_CreateWorkoutExercisesTable.cs`, `202610080944_CreateWorkoutExerciseSetsTable.cs`
- Controller -> `src/WeightTracker.Api/Endpoints/Workouts/WorkoutsController.cs` -> inherits `AuthorizedController`,
  `CurrentUser` to use case, `ExceptionMapper` in every catch
- Seed -> `src/WeightTracker.Infra/Seed/DatabaseSeeder.cs` + `SeedData.cs`
- Tests -> `.stpr/template/test/WeightTracker.Unit/Domain/Workouts/WorkoutTests`,
  `.stpr/template/test/WeightTracker.Integration/Auth/AuthEndpointsTests`,
  `.stpr/template/test/WeightTracker.Integration/Workouts/WorkoutScopingTests`

## State And Mutability

- New table `WorkoutTemplates`: `Id` (PK), `TrainerId` (FK `Users`, required), `Name`, `Description` (nullable),
  with the same lengths as the workout columns. Index on `TrainerId`.
- New table `WorkoutTemplateExercises`: mirrors `WorkoutExercises` (exercise, order, effort, per side, rest,
  superseries id), FK to `WorkoutTemplates` with cascade delete, FK to `Exercises`.
- New table `WorkoutTemplateExerciseSets`: mirrors `WorkoutExerciseSets`, FK to `WorkoutTemplateExercises` with
  cascade delete.
- No change to the `Workouts` tables. Workouts created from a template are ordinary workouts and keep no link to it.
- Templates change through every content operation and can be hard-deleted. A template has no status and is never
  locked.

## Business Rules

- A template always belongs to exactly one trainer (`TrainerId`), whose role is `TRAINER` and who is active when
  the template is created.
- A template has a name and an optional description, with the same value objects and limits as a workout
  (`WorkoutName`, `WorkoutDescription`).
- Template content follows the same domain rules as workout content: no duplicate exercise, at least one set per
  exercise, valid effort and set targets, reorder requires the exact set of exercises, superseries rules unchanged.
- An exercise added to a template must exist, must not be archived, and when custom must be owned by the template's
  trainer (the same "editor of record" rule `AddWorkoutExerciseUseCase` applies, where the editor is the trainer).
- Creating a workout from a template:
  - The workout is a new `DRAFT` owned by the requested athlete. Its trainer is **always the template's trainer**,
    including when admin creates it. This keeps custom exercises valid for the workout's editor of record.
  - The owner must satisfy the same rules as a trainer-prescribed `POST /workouts`: exists, is a regular user, is
    active, is not the trainer.
  - Standalone needs a date. Placement needs both mesocycle and week, and must pass `MesocyclePlacementValidator`
    with the template's trainer as prescriber. The placed workout's date comes from the week, as today.
  - Name, description and all exercises are copied in order with their effort, sets, per side, rest and superseries
    grouping. Copied exercises get new ids.
  - The template's exercises are copied as they are. An exercise archived after it was added to the template is still
    copied, just as an existing workout keeps an archived exercise.
  - A template with no exercises can be used. The result is an empty draft workout.
- Deleting a template does not affect workouts already created from it.

## Roles And Visibility

- **Trainer**: creates templates for themselves only, and lists, reads, edits, deletes and uses only their own.
  Another trainer's template behaves as if it did not exist (`404`).
- **Admin**: lists and reads all templates, and edits, deletes and uses any of them. Admin creates a template only by
  naming the owning trainer (`trainerId` required). Admin is never the owner, and a template whose `trainerId` is an
  admin, a regular user or a deactivated trainer is refused.
- **Regular user (athlete)**: no access to any template route (`403`). Athletes only see the workouts created from
  templates, as ordinary workouts.
- A trainer who sends `trainerId` on create is refused (`400`), as `CreateWorkoutUseCase` refuses `trainerId` from
  anyone who is not an admin. A trainer's template is always their own.
- `GET /workout-templates` is a scoped read: a trainer gets only their own, and admin gets all with an optional
  `trainerId` filter.

Routes (all `[Authorize]`):

- `GET /workout-templates`, `GET /workout-templates/{id}`
- `POST /workout-templates`, `PUT /workout-templates/{id}`, `DELETE /workout-templates/{id}`
- `POST /workout-templates/{id}/exercises`, `PUT /workout-templates/{id}/exercises/{exerciseId}`,
  `DELETE /workout-templates/{id}/exercises/{exerciseId}`, `PUT /workout-templates/{id}/exercises/order`
- `POST /workout-templates/{id}/superseries`, `DELETE /workout-templates/{id}/superseries/{superSeriesId}`
- `POST /workout-templates/{id}/workouts` -> `201` with the same response shape as `POST /workouts`

## Business Identifiers And Mutability

- `WorkoutTemplate.Id` is a technical identifier generated with `Id.New()` and immutable.
- `WorkoutTemplate.TrainerId` is immutable after creation in this change.
- Copied `WorkoutExercise` ids are generated with `Id.New()`. They are never shared between a template and a workout.
- No new unique business identifier. Template names are not unique.

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## Out Of Scope

- Reassigning a template to another trainer.
- Saving an existing workout as a template.
- Keeping a link from a workout to its source template (`SourceTemplateId`), and any "created from" display.
- Sharing templates between trainers, or a global admin library.
- Mesocycle templates (whole plans made of workout templates).
- Creating several workouts from one template in one call, for example one per mesocycle week.
- Template versioning or propagating template edits to existing workouts.
- Any backoffice or app change.

## Notes

- Domain: `src/WeightTracker.Domain/Workouts/WorkoutTemplate.cs`. It lives in the Workouts area on purpose so it can
  own `WorkoutExercise` (whose `Replace`, `JoinSuperSeries` and `LeaveSuperSeries` are `internal`) and so
  `Workout.CreateFromTemplate` does not cross domain areas.
- The content operations on `WorkoutTemplate` duplicate the corresponding `Workout` methods without the lock check.
  Do not extract a shared base or content type in this change. That would rewrite `Workout.cs`, a template-backed file.
  Revisit only if the duplication becomes a maintenance problem.
- `Workout.CreateFromTemplate(WorkoutTemplate template, Id userId, DateTime workoutDate)` takes the trainer from the
  template and reuses `TrainerCannotBeWorkoutOwnerException`. The use case links the mesocycle afterwards with
  `LinkToMesocycle`, as `CreateWorkoutUseCase` does.
- New exceptions (names final at proposal time), each with an explicit `ExceptionMapper` entry:
  `WorkoutTemplateNotFoundException` (404), `InvalidWorkoutTemplateTrainerException` (400) when an admin names a
  non-trainer or a deactivated trainer. For content and ownership failures, reuse `UnauthorizedOperationException`, `ExerciseNotFoundException`,
  `ExerciseArchivedException`, `ExerciseAlreadyInWorkoutException` and the existing content exceptions. Decide at
  proposal time whether `ExerciseNotUsableInWorkoutException` is reused or a template variant is added.
- `WorkoutTemplateEditAuthorizer.EnsureCanEdit(callerId, callerRole, template)`: admin always; trainer only when owner,
  otherwise `WorkoutTemplateNotFoundException`; regular user `UnauthorizedOperationException`. Every template use case,
  including reads by id and create-from-template, goes through it.
- `IWorkoutTemplateRepository` in `Application/Interfaces` with `GetById`, `GetAll`, `GetAllByTrainerId`, `Save` and
  `Remove` (named as in `IExerciseFavoriteRepository`). Register the repository, the authorizer and the use cases in `Program.cs`.
- DTOs: template response mirrors the workout response without user, date, status and mesocycle fields. Reuse the
  existing exercise and set request DTOs.
- Add `WorkoutTemplateExerciseSets`, `WorkoutTemplateExercises` and `WorkoutTemplates` to the integration test
  truncate list (before `Users` and `Exercises`).
- Seed: one template for the seeded trainer with several exercises and one superseries. Tests that need a second
  trainer create them and their template through the API, so the existing user-list tests keep their baseline.
- Tests to add:
  - Unit, domain: `WorkoutTemplate` create/rehydrate and every content operation, mirroring `WorkoutTests` without the
    lock cases. `Workout.CreateFromTemplate` copies order, sets, effort, per side, rest and superseries, gives new
    exercise ids, starts as `DRAFT`, refuses trainer equals owner, and later template edits do not affect the workout.
  - Unit, application: authorizer branches (admin, owning trainer, other trainer, user), admin create with
    trainer/user/admin/deactivated `trainerId`, and create-from-template assignment and placement paths.
  - Unit, infra: `WorkoutTemplateMapper` round-trip (empty, with sets, with superseries).
  - Integration, repository: `WorkoutTemplateRepositoryTests` mirroring `WorkoutRepositoryTests`, including remove
    and re-add in one save and the cascading `Remove`.
  - Integration: every route carries `401` without token and `401` with invalid token, `403` for a regular user, `404`
    for a missing template and for another trainer's template, and `400` for structural errors. Scoped list asserts that
    a trainer does not see the other trainer's template, admin sees both, and the `trainerId` filter works. Create from
    template covers standalone, placed, invalid owner and the copied content in the response. Deleting a template
    leaves a workout created from it intact.
  - Full `dotnet test WeightTracker.slnx` with Docker.
- `.stpr/template/src` parity: `Workout.cs` and `WorkoutTests` are template copies, so update the template files
  together with `src` and `test`.
- No deviations from the mapped template references are allowed.
