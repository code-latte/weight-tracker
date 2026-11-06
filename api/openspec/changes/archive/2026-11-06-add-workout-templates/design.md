# Design

## Context

See `proposal.md` for motivation and the three delta specs for the required behaviour.

Current state that shapes the approach:

- `Workout` owns its content through `AddExercise`, `UpdateExercise`, `RemoveExercise`, `ReorderExercises`,
  `CreateSuperSeries` and `BreakSuperSeries`, each guarded by the lock. `WorkoutExercise` holds no parent reference.
  Its mutators (`Replace`, `JoinSuperSeries`, `LeaveSuperSeries`) are `internal` to the Domain assembly.
- Content use cases load the workout, call `WorkoutEditAuthorizer.EnsureCanEdit`, act, then save.
  `AddWorkoutExerciseUseCase` additionally checks that the exercise exists, is visible (`ExerciseVisibility`), is not
  archived, and that a custom exercise is owned by the editor of record.
- `CreateWorkoutUseCase` uses `WorkoutAssignmentValidator` (`EnsureOwnerExists`, `EnsureCanBeAssigned`,
  `EnsureCanTrain`) and `MesocyclePlacementValidator` (`EnsureComplete`, `EnsureDateOrPlacement`, `EnsureCanPlace`,
  `ResolveWorkoutDate`). `EnsureCanTrain` accepts `TRAINER` or `ADMIN`. A template owner must be `TRAINER` only, so
  template creation checks the trainer with `User.CanBeTrainer()` instead.
- `AuthorizedController` resolves `CurrentUser` and applies `[RequireRole]` after authentication, so `401` comes before
  `403`.
- `POST /workouts` returns `WorkoutResponse.From(workout, exercises)`, where `exercises` is a dictionary used to show
  exercise data.

## Goals / Non-Goals

**Goals:**

- One authorization rule for templates (`WorkoutTemplateEditAuthorizer`), used by every template use case.
- Creating a workout from a template reuses the existing assignment and placement services, so its rules cannot drift
  from `POST /workouts`.
- No change to any existing route, table or response.

**Non-Goals:**

- No shared base type or content type between `Workout` and `WorkoutTemplate`.
- No link from `Workouts` to `WorkoutTemplates` in the schema.

## Decisions

### Layer mapping

| Layer | Change |
| --- | --- |
| Domain | `Workouts/WorkoutTemplate.cs` (aggregate); `Workout.CreateFromTemplate`. Template-specific exceptions only if a workout exception's message would mislead (see below). |
| Application | `Interfaces/IWorkoutTemplateRepository`; `Services/WorkoutTemplateEditAuthorizer`; `UseCases/WorkoutTemplates/`: `GetAllWorkoutTemplatesUseCase`, `GetWorkoutTemplateByIdUseCase`, `CreateWorkoutTemplateUseCase`, `UpdateWorkoutTemplateUseCase`, `DeleteWorkoutTemplateUseCase`, `AddWorkoutTemplateExerciseUseCase`, `UpdateWorkoutTemplateExerciseUseCase`, `RemoveWorkoutTemplateExerciseUseCase`, `ReorderWorkoutTemplateExercisesUseCase`, `CreateWorkoutTemplateSuperSeriesUseCase`, `BreakWorkoutTemplateSuperSeriesUseCase`, `CreateWorkoutFromTemplateUseCase`; `Exceptions/WorkoutTemplateNotFoundException`, `InvalidWorkoutTemplateTrainerException`. |
| Infrastructure | `Repositories/WorkoutTemplateRepository`, `Mappers/WorkoutTemplateMapper` (reusing the set-target and effort conversions of `WorkoutMapper`, made `internal`), EF entities `WorkoutTemplateEntity`, `WorkoutTemplateExerciseEntity`, `WorkoutTemplateExerciseSetEntity` and their `WeightTrackerDbContext` configuration; seed. |
| Migrations | `CreateWorkoutTemplatesTable`, `CreateWorkoutTemplateExercisesTable`, `CreateWorkoutTemplateExerciseSetsTable`. |
| Api | `Endpoints/WorkoutTemplates/WorkoutTemplatesController.cs` and `Dtos/`; `ExceptionMapper` entries; `Program.cs` registrations. |

Cross-area dependencies (Application only): WorkoutTemplates → Users (`IUserRepository`, via
`WorkoutAssignmentValidator` and the trainer check), → Exercises (`IExerciseRepository`, `ExerciseVisibility`),
→ Workouts (`IWorkoutRepository`), → Mesocycles (`MesocyclePlacementValidator`). In Domain, `WorkoutTemplate` and
`Workout` share the Workouts area, so no domain area references another.

### `WorkoutTemplate` is its own aggregate in the Workouts area

`WorkoutTemplate : AggregateRoot` has `TrainerId`, `Name` (`WorkoutName`), `Description` (`WorkoutDescription?`) and
`List<WorkoutExercise>`, with `Create(trainerId, name, description)`, `Rehydrate(...)`, `UpdateDetails`, and the six
content methods with the same signatures and domain exceptions as `Workout`, without the lock check.
`Rehydrate` validates superseries exactly as `Workout.Rehydrate` does.

- *Alternative: a workout with a null owner.* Rejected in the proposal; it weakens `Workout` invariants and makes every
  workout read filter templates out.
- *Alternative: extract a shared `WorkoutContent` that both aggregates delegate to.* It would remove the duplication,
  but rewrites the template-backed `Workout.cs` and `WorkoutTests`. That is more risk than this feature needs, so it is
  deferred.
- *Alternative: put `WorkoutTemplate` in its own `Domain/WorkoutTemplates` area.* Rejected: it would reference
  `WorkoutExercise` and the workout value objects across areas, and `Workout.CreateFromTemplate` would reference it back.

Reusing `WorkoutExercise` keeps one definition of an exercise prescription. Each aggregate owns its own instances, and
they are never shared: `CreateFromTemplate` builds new ones.

### `Workout.CreateFromTemplate`

`static Workout CreateFromTemplate(WorkoutTemplate template, Id userId, DateTime workoutDate)` calls `Create(userId,
template.TrainerId, template.Name, template.Description, workoutDate)`, then copies every entry in order with
`WorkoutExercise.Create` (new `Id.New()`) and the same effort, sets, per side, rest and superseries id. The new workout
is `DRAFT`, so the lock never interferes. `TrainerCannotBeWorkoutOwnerException` still applies.

Copying superseries ids as they are is valid because they are unique within the template and the workout starts
empty. The factory restores them directly rather than replaying `CreateSuperSeries`, which would renumber them.

### Authorization

`WorkoutTemplateEditAuthorizer.EnsureCanEdit(template, callerId, callerRole)`:

1. `ADMIN` → allowed.
2. `TRAINER` and `template.TrainerId == callerId` → allowed.
3. Otherwise → `WorkoutTemplateNotFoundException` (404).

Regular users never reach it. The controller is `[RequireRole(TRAINER, ADMIN)]`, so they get `403` from the base
controller, after the `401` checks. Every by-id use case, including reads and create-from-template, loads the template,
throws `WorkoutTemplateNotFoundException` when it is missing, then calls the authorizer.

- *Alternative: `403` for another trainer's template, as `WorkoutEditAuthorizer` does.* Rejected: templates are private,
  and a `403` confirms that the id exists.

### Template creation

`CreateWorkoutTemplateUseCase` resolves the trainer from the role. A trainer becomes the owner, and a `trainerId` from a
trainer → `InvalidWorkoutTemplateTrainerException` (400). An admin needs a `trainerId`; the user is loaded and
`User.CanBeTrainer()` must hold. A missing `trainerId`, an unknown user, or one that cannot be a trainer →
`InvalidWorkoutTemplateTrainerException` (400). An unknown trainer is `400`, not `404`, matching the mesocycle
behaviour for an unknown trainer.

### Template content use cases

Each one mirrors its workout counterpart: load the template, authorize, act on the aggregate, save. The content
exceptions are reused as they are (`ExerciseAlreadyInWorkoutException`, `ExerciseNotInWorkoutException`,
`WorkoutExerciseOrderMismatchException`, superseries exceptions, `ExerciseNotFoundException`,
`ExerciseArchivedException`, `ExerciseIdMismatchException`). Their status mappings already match the spec. Their
messages say "workout", which is acceptable for a template; no template copies are added.

`AddWorkoutTemplateExerciseUseCase` applies the same exercise checks as `AddWorkoutExerciseUseCase`, with the
template's trainer as the editor of record. A custom exercise owned by someone else → `ExerciseNotUsableInWorkoutException`
(409), reused.

### Create workout from template

`CreateWorkoutFromTemplateUseCase.Execute(callerId, callerRole, templateId, userId, workoutDate, mesocycleId,
mesocycleWeekNumber)`:

1. `EnsureComplete` and `EnsureDateOrPlacement` (400).
2. Load the template (404), then `WorkoutTemplateEditAuthorizer.EnsureCanEdit`.
3. A missing `userId` → `InvalidWorkoutAssignmentException` (400). `EnsureOwnerExists` → 404. A non-`USER` owner →
   `UnauthorizedOperationException` (403) for a trainer, as `CreateWorkoutUseCase` does, and
   `InvalidWorkoutAssignmentException` (400) for an admin. Then `EnsureCanBeAssigned` → 400 for a deactivated owner.
4. `EnsureCanTrain(template.TrainerId)` → 400 when the template's trainer was deactivated since it was created.
5. With a placement, `EnsureCanPlace(ownerId, template.TrainerId, ...)` reuses the mesocycle existence, athlete and
   trainer match, week range and caller checks.
6. `ResolveWorkoutDate`, then `Workout.CreateFromTemplate`, then `LinkToMesocycle` when placed. Save the workout and
   commit once.
7. Load the catalog exercises of the copied entries with `IExerciseRepository.GetAllByIds` and return them with the
   workout as `WorkoutFromTemplateView(Workout, Exercises)`, so the controller builds `WorkoutResponse.From` with the
   copied exercises without touching a repository.

The template is only read, never saved, in this use case.

### Listing

`GetAllWorkoutTemplatesUseCase(callerId, callerRole, trainerIdFilter)`: a trainer → `GetAllByTrainerId(callerId)`, and
the filter is ignored. An admin → `GetAllByTrainerId(filter)` when a filter is given, otherwise `GetAll()`. The order
is name, then id, so results are stable. `IWorkoutTemplateRepository` has `GetById`, `GetAll`, `GetAllByTrainerId`,
`Save` and `Remove`. `Remove` follows the existing `IExerciseFavoriteRepository` naming for deletes.

### Persistence

- `WorkoutTemplates`: `Id` uuid PK, `TrainerId` uuid FK `Users` (no cascade), `Name` string(256) not null,
  `Description` string(1024) nullable, index on `TrainerId`.
- `WorkoutTemplateExercises`: same columns as `WorkoutExercises`, with `WorkoutTemplateId` FK cascade and
  `ExerciseId` FK `Exercises` (no cascade), `Position`, and a unique constraint on `(WorkoutTemplateId, ExerciseId)`
  mirroring `UQ_WorkoutExercises_WorkoutId_ExerciseId`.
- `WorkoutTemplateExerciseSets`: same columns as `WorkoutExerciseSets`, with the FK to `WorkoutTemplateExercises` set
  to cascade.
- Column names, lengths, nullability and checks are copied from the current workout tables, including the columns
  added by later migrations (effort, superseries, target max value, minutes, per side, rest). Each migration's `Down()`
  drops its table.
- EF entities are configured with `.ValueGeneratedNever()` and `HasOne<UserEntity>().WithMany()` without navigation.
  The repository saves by `IsNew` and reconciles inner rows on update, as `WorkoutRepository` does. `Remove` loads the
  stored root and removes only that row; the cascading foreign keys remove exercises and sets. It never re-maps the
  aggregate to delete it, because the mapper gives set rows fresh ids that were never stored.

### Api

`WorkoutTemplatesController : AuthorizedController`, `[Route("workout-templates")]`, with `[RequireRole(TRAINER, ADMIN)]`
on every action (the attribute targets methods only).
DTOs:

- `CreateWorkoutTemplateRequest(name, description?, trainerId?)`
- `UpdateWorkoutTemplateRequest(name, description?)`
- `WorkoutTemplateResponse(id, name, description, trainerId, exercises)`, whose exercise items reuse the shape the
  workout response uses for exercises.
- `CreateWorkoutFromTemplateRequest(userId?, workoutDate?, mesocycleId?, mesocycleWeekNumber?)`

The exercise, reorder and superseries requests reuse `AddOrUpdateExerciseRequest`, `ReorderWorkoutExercisesRequest`,
`CreateSuperSeriesRequest` and `SuperSeriesResponse`. `POST /workout-templates/{id}/workouts` returns `WorkoutResponse`
with `CreatedAtAction` to `WorkoutsController.GetById`. Every catch → `ExceptionMapper.Map`, and every action declares
`[ProducesResponseType]`.

### Seed and test arrangement

The seed adds one template for the seeded trainer: Sentadilla and Press Banca in superseries 1, then Plancha. There is
only one seeded trainer, and adding another would change the existing user-list tests, so tests that need a second
trainer or their template create them through `POST /users` and `POST /workout-templates`, as the mesocycle tests do.
`ApiFactory` truncates the three new tables, and `RemoveUser` deletes a removed trainer's templates.

### Folder naming

The aggregate lives in `Domain/Workouts` (it owns `WorkoutExercise`), while its use cases, controller and tests use
`WorkoutTemplates`. This is a deliberate exception to naming a slice the same way in every layer.

## Risks / Trade-offs

- [Content logic duplicated between `Workout` and `WorkoutTemplate`; a fix in one can miss the other] → The template
  unit tests mirror `WorkoutTests` case by case. The duplication is recorded as an accepted conflict, and extracting
  a shared type can be a separate change.
- [Workout exception messages mention "workout" when raised for a template] → Accepted. Status codes are right and
  the spec defines behaviour by status, not message.
- [Workout tables gain columns later and template tables drift] → Design records that template tables mirror workout
  tables. Future workout-column changes need a matching template migration, which the template unit and mapper
  round-trip tests will show.
- [A trainer deactivated after creating templates leaves orphan-like templates] → Admin can still read, edit and delete
  them. Using them is refused (`400`) until the trainer is reactivated.

## Migration Plan

Three additive migrations, applied by the migrator as usual. No data migration. Rollback runs `Down()` in reverse
order, dropping only the new tables. Workouts created from templates are unaffected by a rollback, since they keep no
link.
