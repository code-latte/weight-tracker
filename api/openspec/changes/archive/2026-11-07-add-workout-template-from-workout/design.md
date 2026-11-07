# Design

## Context

See `proposal.md` for motivation and `specs/workout-template-from-workout/spec.md` for the required behaviour.

Current state that shapes the approach:

- `WorkoutTemplate` (`Domain/Workouts`) owns `WorkoutExercise` entries, with `Create`, `Rehydrate` and the content
  operations. `Workout.CreateFromTemplate` already copies a template into a workout: new entries with
  `WorkoutExercise.Create`, superseries ids restored with the `internal` `JoinSuperSeries`.
- `Workout.TrainerId` is nullable. An admin may assign a `TRAINER` or an `ADMIN` as a workout's trainer
  (`workout-creation` spec), so a prescribed workout's trainer is not always a `TRAINER`.
- `CreateWorkoutTemplateUseCase` checks a template owner with `User.CanBeTrainer()` (role `TRAINER` and active) and
  refuses with `InvalidWorkoutTemplateTrainerException` (400).
- `WorkoutsController` inherits `AuthorizedController`; `[RequireRole]` is method-only and answers `403` after the
  `401` checks. `WorkoutTemplatesController.CreateWorkout` already returns `CreatedAtAction` to another controller.
- `WorkoutTemplateResponse.From(template)` needs no catalog lookup: its entries carry `exerciseId` only.
- Seed: `PrescribedSuperSeriesWorkoutId` (prescribed by the trainer, superseries 1), `PrescribedStartedWorkoutId`
  (`STARTED`), `SelfCreatedPairWorkoutId` (the user's, no trainer) and `TrainerOwnWorkoutId` (the trainer's own, no
  trainer).

## Goals / Non-Goals

**Goals:**

- One domain factory owns the copy, so the copy rules sit next to `Workout.CreateFromTemplate`.
- The template owner rule is the same as for `POST /workout-templates`.
- No schema, migration, seed or template-backed file change.

**Non-Goals:**

- No change to `WorkoutEditAuthorizer` or to the workout edit rule.
- No shared helper between the two copy factories. Each is a few lines in its own aggregate.

## Decisions

### Layer mapping

| Layer | Change |
| --- | --- |
| Domain | `WorkoutTemplate.CreateFromWorkout(Workout workout)`; `Exceptions/WorkoutWithoutTrainerException`. |
| Application | `UseCases/WorkoutTemplates/CreateWorkoutTemplateFromWorkoutUseCase`; `Services/WorkoutTemplateTrainerValidator` (`EnsureCanOwnTemplates`), extracted from `CreateWorkoutTemplateUseCase`, which now uses it too. |
| Infrastructure | None. `IWorkoutRepository`, `IWorkoutTemplateRepository` and `IUserRepository` already have what is needed. |
| Api | `WorkoutsController.CreateTemplate` (`POST /workouts/{workoutId:guid}/templates`); `ExceptionMapper` entry; `Program.cs` registration. |

Cross-area dependencies (Application only): WorkoutTemplates → Workouts (`IWorkoutRepository`), → Users
(`IUserRepository`, through `WorkoutTemplateTrainerValidator`). In Api, `WorkoutsController` → `CreateWorkoutTemplateFromWorkoutUseCase` and
`WorkoutTemplateResponse`. Domain gains no cross-area reference: both aggregates are in the Workouts area.

### `WorkoutTemplate.CreateFromWorkout`

`static WorkoutTemplate CreateFromWorkout(Workout workout)`:

1. A workout without `TrainerId` → `WorkoutWithoutTrainerException` (domain, 400). The aggregate cannot be created
   without an owner, even if a caller skips the Application checks.
2. `Create(workout.TrainerId, workout.Name, workout.Description)`, then each entry in order copied with
   `WorkoutExercise.Create(exerciseId, effort, sets, perSide, rest)` and, when grouped, `JoinSuperSeries` with the same id.

Superseries ids are kept as they are: they are unique within the workout and the template starts empty. Status,
owner, date and mesocycle placement are not copied, because a template has none.

- *Alternative: an instance method on `Workout` (`workout.ToTemplate()`)*. Rejected: the factory that creates an
  aggregate belongs to that aggregate, as `Workout.CreateFromTemplate` belongs to `Workout`.

### Use case

`CreateWorkoutTemplateFromWorkoutUseCase.Execute(callerId, callerRole, workoutId)`:

1. Load the workout → `WorkoutNotFoundException` (404).
2. `USER` → `UnauthorizedOperationException` (403). The controller's `[RequireRole]` already refuses users; the use
   case refuses them too, as every template use case does.
3. `TRAINER` whose id is not `workout.TrainerId` → `UnauthorizedOperationException` (403). This also covers a workout
   without a trainer, so a trainer never learns why another trainer's workout was refused.
4. When the workout has a trainer, `WorkoutTemplateTrainerValidator.EnsureCanOwnTemplates`: missing or not `CanBeTrainer()` →
   `InvalidWorkoutTemplateTrainerException` (400).
5. `WorkoutTemplate.CreateFromWorkout(workout)`; for an admin on a workout without trainer this raises
   `WorkoutWithoutTrainerException` (400).
6. `IWorkoutTemplateRepository.Save`, then `SaveChangesAsync` once. The workout is never saved.

Returns the `WorkoutTemplate`.

- *Alternative: reuse `WorkoutEditAuthorizer`*. Rejected: it lets the owner of a self-planned workout through and
  applies the lock, neither of which fits. The trainer rule here is one comparison, kept explicit in this use case.
- *Alternative: a `trainerId` in the request for the admin*. Rejected by the brief: the template's trainer is always
  the workout's trainer, so there is nothing for the admin to choose or get wrong.

### Template owner rule in one service

`CreateWorkoutTemplateUseCase` and this use case both decide who owns a template, so the rule (an existing, active
`TRAINER`, otherwise `InvalidWorkoutTemplateTrainerException`) lives in `WorkoutTemplateTrainerValidator`, shaped like
`MesocycleAssignmentValidator.EnsureTrainer`, and both use cases call it. Its behaviour for `POST /workout-templates`
is unchanged.

### Api

`[HttpPost("{workoutId:guid}/templates")]`, `[RequireRole(Role.TRAINER, Role.ADMIN)]`, no body,
`CreatedAtAction(nameof(WorkoutTemplatesController.GetById), "WorkoutTemplates", new { id }, WorkoutTemplateResponse.From(template))`.
`[ProducesResponseType]` for `201` (`WorkoutTemplateResponse`), `400`, `401`, `403`, `404`.

`ExceptionMapper`: `WorkoutWithoutTrainerException` → 400.

### Tests

- Unit, domain: `CreateFromWorkout` cases in `WorkoutTemplateTests`, mirroring the `CreateFromTemplate` cases in
  `WorkoutTests`.
- Unit, application: `test/WeightTracker.Unit/Application/WorkoutTemplates/CreateWorkoutTemplateFromWorkoutUseCaseTests.cs`,
  shaped like `CreateWorkoutFromTemplateUseCaseTests` (strict mocks, `MesocycleTestUsers` helpers, nothing saved on failure).
- Integration: `test/WeightTracker.Integration/WorkoutTemplates/WorkoutTemplateFromWorkoutEndpointsTests.cs` on the
  `WorkoutTemplateEndpointTest` base, carrying its own `401`/`403`/`404` cases since the route is outside the
  `/workout-templates` matrix. Arrangement through the API only:
  - a workout whose trainer is an admin: `POST /workouts` as the admin with `trainerId` = admin;
  - an empty prescribed workout: `POST /workouts` as the trainer;
  - an archived exercise: the trainer creates a custom exercise, adds it to `TrainedWorkoutId`, then archives it;
  - a deactivated trainer: `PUT /users/{id}/deactivate` as the admin.

## Risks / Trade-offs

- [Two copy factories, one per direction, can drift] → Both copy the same `WorkoutExercise` fields; each has unit
  tests asserting every field, so a new field missing from one copy fails its test once the field is asserted.
- [`403` for a trainer on a workout without trainer hides the reason] → Intended: the same answer as for another
  trainer's workout, so nothing is revealed. The admin gets the explicit `400`.
- [Saving the same workout twice creates duplicates] → Accepted; template names are not unique and the trainer can
  delete or rename them.

## Migration Plan

None. No schema or data change. Rollback is removing the route and code.
