# Tasks

## 1. Domain: `WorkoutTemplate` aggregate

- [x] 1.1 Add `Domain/Workouts/WorkoutTemplate.cs` (`Create`, `Rehydrate` with superseries validation, `IsNew`, `UpdateDetails`, `AddExercise`, `UpdateExercise`, `RemoveExercise`, `ReorderExercises`, `CreateSuperSeries`, `BreakSuperSeries`, snapshot `Exercises`), reusing `WorkoutExercise` and the workout value objects and exceptions, with no lock; verify `dotnet build WeightTracker.slnx` passes
- [x] 1.2 Add `test/WeightTracker.Unit/Domain/Workouts/WorkoutTemplateTests` mirroring `WorkoutTests` case by case (create, rehydrate, every content operation and its failures, superseries rules, snapshot not mutable) without lock cases; verify the unit tests pass
- [x] 1.3 Review `WorkoutTemplate` and its tests against `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs` and `.stpr/template/test/WeightTracker.Unit/Domain/Workouts/WorkoutTests`; verify no deviation is left beyond the accepted duplication

## 2. Domain: workout from template

- [x] 2.1 Add `Workout.CreateFromTemplate(template, userId, workoutDate)` per design (template trainer, `DRAFT`, entries copied in order with new ids and the same superseries ids); verify the build passes
- [x] 2.2 Add `CreateFromTemplate` cases to `WorkoutTests`: content copied, new entry ids, `DRAFT`, trainer equals owner refused, empty template, later template edits do not change the workout; verify the unit tests pass
- [x] 2.3 Copy the changed `Workout.cs` and `WorkoutTests` into `.stpr/template` and run the parity loop from `AGENTS.md`; verify it prints no `DRIFT`

## 3. Persistence

- [x] 3.1 Add migrations `CreateWorkoutTemplatesTable`, `CreateWorkoutTemplateExercisesTable`, `CreateWorkoutTemplateExerciseSetsTable` mirroring the current workout tables (all later columns included), with FKs, index, unique constraint and reversing `Down()`; verify the migrator runs up, `--down` and up again against a throwaway Postgres container (never the compose database, since `--down` drops the whole schema)
- [x] 3.2 Add EF entities and `WeightTrackerDbContext` configuration (table and column names, lengths, `.ValueGeneratedNever()`, `HasOne<UserEntity>().WithMany()` without navigation); verify the build passes
- [x] 3.3 Add `IWorkoutTemplateRepository` (`GetById`, `GetAll`, `GetAllByTrainerId`, `Save`, `Remove`), `WorkoutTemplateMapper` and `WorkoutTemplateRepository` (save by `IsNew`, inner rows replaced, ordered by name then id); verify with a `WorkoutTemplateMapper` round-trip unit test (empty, with sets, with superseries)
- [x] 3.4 Seed one template for the seeded trainer (several exercises, one superseries) in `SeedData`/`DatabaseSeeder` (a second trainer and their template are arranged through the API in tests), add the three tables to the integration truncate list and to `RemoveUser`; verify the integration suite seeds a fresh database without errors
- [x] 3.5 Add `test/WeightTracker.Integration/Repositories/WorkoutTemplateRepositoryTests.cs` mirroring `WorkoutRepositoryTests` (new aggregate across three tables, updated details, surviving row id, remove and re-add in one save, replaced sets and order, superseries ungrouped on removal, no duplicate root, cascading `Remove`, unknown id, filtered list order); verify they pass with Docker
- [x] 3.6 Review repository, mapper, EF config, migrations and seed against `WorkoutRepository`, `WorkoutMapper`, `CreateWorkoutsTable`/`CreateWorkoutExercisesTable`/`CreateWorkoutExerciseSetsTable` and `DatabaseSeeder`; verify no deviation is left

## 4. Application: template management

- [x] 4.1 Add `WorkoutTemplateNotFoundException` and `InvalidWorkoutTemplateTrainerException`, and `WorkoutTemplateEditAuthorizer` (admin, owning trainer, otherwise 404); verify with unit tests for admin, owner, other trainer
- [x] 4.2 Implement `CreateWorkoutTemplateUseCase` (trainer self, trainer with `trainerId` 400, admin without `trainerId` 400, admin naming admin/user/deactivated/unknown 400); verify with unit tests per branch, asserting no save on failure
- [x] 4.3 Implement `GetWorkoutTemplateByIdUseCase`, `UpdateWorkoutTemplateUseCase`, `DeleteWorkoutTemplateUseCase` (404 unknown, 404 other trainer, admin allowed); verify with unit tests
- [x] 4.4 Implement `GetAllWorkoutTemplatesUseCase` (trainer own only and filter ignored, admin all, admin filter); verify with unit tests asserting excluded templates
- [x] 4.5 Review the management use cases and authorizer against `CreateWorkoutUseCase`, `UpdateWorkoutUseCase`, `GetWorkoutByIdUseCase`, `GetAllExercisesUseCase` and `WorkoutEditAuthorizer`; verify no deviation is left

## 5. Application: template content

- [x] 5.1 Implement `AddWorkoutTemplateExerciseUseCase` (exercise 404, not visible 404, archived 409, custom not owned by template trainer 409, duplicate 409) and `UpdateWorkoutTemplateExerciseUseCase`, `RemoveWorkoutTemplateExerciseUseCase`; verify with unit tests per branch
- [x] 5.2 Implement `ReorderWorkoutTemplateExercisesUseCase`, `CreateWorkoutTemplateSuperSeriesUseCase`, `BreakWorkoutTemplateSuperSeriesUseCase`; verify with unit tests for success, mismatch, unequal totals and unknown superseries
- [x] 5.3 Review the content use cases against `AddWorkoutExerciseUseCase`, `UpdateWorkoutExerciseUseCase`, `RemoveWorkoutExerciseUseCase`, `ReorderWorkoutExercisesUseCase`, `CreateSuperSeriesUseCase` and `BreakSuperSeriesUseCase`; verify no deviation is left

## 6. Application: workout from template

- [x] 6.1 Implement `CreateWorkoutFromTemplateUseCase` per design (placement pair and date checks, template 404 and authorizer, owner checks with 403 for trainer and 400 for admin, deactivated template trainer 400, `EnsureCanPlace`, `ResolveWorkoutDate`, `CreateFromTemplate`, `LinkToMesocycle`, single commit); verify with unit tests per branch, asserting no save on failure
- [x] 6.2 Review it against `CreateWorkoutUseCase`; verify the assignment and placement services are reused and not reimplemented

## 7. Api: `/workout-templates`

- [x] 7.1 Add DTOs in `Endpoints/WorkoutTemplates/Dtos` (`CreateWorkoutTemplateRequest`, `UpdateWorkoutTemplateRequest`, `WorkoutTemplateResponse`, `CreateWorkoutFromTemplateRequest`) with structural validation and `static From`; verify the build passes
- [x] 7.2 Add `WorkoutTemplatesController` (twelve routes, `[RequireRole(TRAINER, ADMIN)]`, `CurrentUser`, `ExceptionMapper.Map` in every catch, `[ProducesResponseType]`, `Location` headers); verify the build passes
- [x] 7.3 Map `WorkoutTemplateNotFoundException` (404) and `InvalidWorkoutTemplateTrainerException` (400) in `ExceptionMapper`, and register repository, authorizer and use cases in `Program.cs`; verify `ExceptionMapperTests` covers the new entries
- [x] 7.4 Add integration tests for `specs/workout-templates`: per route `401` no token, `401` invalid token, `403` user, `404` unknown and other trainer's template; create (trainer, admin with valid and invalid `trainerId`, blank name), update, delete; scoped list asserting exclusions and the admin filter; verify they pass with Docker
- [x] 7.5 Add integration tests for `specs/workout-template-content`: per route auth matrix, add/update/remove/reorder, custom and archived exercises, superseries create and break, editing still allowed after a workout from it started; verify they pass with Docker
- [x] 7.6 Add integration tests for `specs/workout-from-template`: auth matrix, standalone and placed creation with copied content in the `201` body and `Location`, owner errors (400/403/404), deactivated template trainer, placement mismatches, template edited and deleted afterwards leaving the workout intact; verify they pass with Docker
- [x] 7.7 Review the controller, DTOs and integration tests against `WorkoutsController`, `.stpr/template/test/WeightTracker.Integration/Auth/AuthEndpointsTests` and `.stpr/template/test/WeightTracker.Integration/Workouts/WorkoutScopingTests`; verify no deviation is left and every test has labelled Arrange / Act / Assert

## 8. Verification

- [x] 8.1 Run `dotnet build WeightTracker.slnx` and the full `dotnet test WeightTracker.slnx` with Docker running; verify both pass
- [x] 8.2 Run the `.stpr/template` parity loop and the `AGENTS.md` quality checklist; verify no `DRIFT` and every item holds
- [x] 8.3 Run `openspec validate add-workout-templates --strict`; verify it passes

## Workflow follow-up

- Archive the change after review.
