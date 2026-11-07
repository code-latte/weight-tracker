# Tasks

## 1. Domain: copy a workout into a template

- [x] 1.1 Add `WorkoutWithoutTrainerException` in `Domain/Exceptions` and `WorkoutTemplate.CreateFromWorkout(Workout)` per design (workout's trainer, name, description, entries in order with new ids, superseries ids kept); verify `dotnet build WeightTracker.slnx` passes
- [x] 1.2 Add `CreateFromWorkout` cases to `WorkoutTemplateTests`: every field copied in order, new entry ids, marked new, trainer from the workout, workout without trainer refused, empty workout, started workout, later workout changes do not reach the template; verify the unit tests pass
- [x] 1.3 Review `CreateFromWorkout` and its tests against `Workout.CreateFromTemplate` and its `WorkoutTests` cases; verify no deviation is left and no template-backed file changed

## 2. Application: use case

- [x] 2.1 Implement `CreateWorkoutTemplateFromWorkoutUseCase` in `UseCases/WorkoutTemplates` with the check order from design (404, user 403, other trainer 403, workout trainer not `CanBeTrainer` 400, no trainer 400, save template once, workout never saved); verify the build passes
- [x] 2.2 Add `CreateWorkoutTemplateFromWorkoutUseCaseTests`: trainer of the workout, other trainer, trainer on their own workout without trainer, admin, admin on a workout without trainer, workout trainer is an admin, deactivated workout trainer, regular user, unknown workout; assert nothing saved on failure and the workout never saved; verify the unit tests pass
- [x] 2.3 Extract the template owner rule into `Services/WorkoutTemplateTrainerValidator.EnsureCanOwnTemplates`, use it from `CreateWorkoutTemplateUseCase` and `CreateWorkoutTemplateFromWorkoutUseCase`, register it in `Program.cs`, and add `WorkoutTemplateTrainerValidatorTests` (active trainer, admin, user, deactivated, unknown); verify the unit tests pass
- [x] 2.4 Review the use case and its tests against `CreateWorkoutFromTemplateUseCase` and `CreateWorkoutTemplateUseCase`; verify no deviation is left

## 3. Api: route

- [x] 3.1 Add `CreateTemplate` to `WorkoutsController` (`POST /workouts/{workoutId:guid}/templates`, `[RequireRole(TRAINER, ADMIN)]`, `ExceptionMapper.Map`, `[ProducesResponseType]`, `CreatedAtAction` to `WorkoutTemplatesController.GetById`, `WorkoutTemplateResponse`), map `WorkoutWithoutTrainerException` to 400, register the use case in `Program.cs`; verify the build passes and `ExceptionMapperTests` covers the new entry
- [x] 3.2 Add `WorkoutTemplateFromWorkoutEndpointsTests` on `WorkoutTemplateEndpointTest`: `401` without token and with an invalid token, `403` for the owning user and for another trainer and for the trainer's own workout without trainer, `404` unknown workout, `400` admin on a self-planned workout, admin-trainer workout and deactivated trainer; success for trainer and admin with `201`, `Location`, `trainerId` and copied content with superseries; started workout saved and unchanged; empty workout; archived exercise copied; listed for the trainer and not for another trainer; workout edited afterwards leaves the template intact; saved twice gives two templates; verify they pass with Docker
- [x] 3.3 Review the action and tests against `WorkoutsController`, `WorkoutTemplatesController.CreateWorkout`, `.stpr/template/test/WeightTracker.Integration/Auth/AuthEndpointsTests` and `WorkoutFromTemplateEndpointsTests`; verify no deviation is left and every test has labelled Arrange / Act / Assert with one Act

## 4. Verification

- [x] 4.1 Run `dotnet build WeightTracker.slnx` and the full `dotnet test WeightTracker.slnx` with Docker running; verify both pass with no warnings
- [x] 4.2 Run the `.stpr/template` parity loop and the `AGENTS.md` quality checklist; verify no `DRIFT` and every item holds
- [x] 4.3 Check that `proposal.md`, `design.md` and `tasks.md` still describe the implemented names and decisions, and run `openspec validate add-workout-template-from-workout --strict`; verify it passes

## Workflow follow-up

- Archive the change after review.
