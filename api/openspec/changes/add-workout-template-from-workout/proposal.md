# Proposal

## Why

Trainers often build a good session directly as a workout for an athlete and only later want to reuse it. Today the
only way to turn it into a template is to rebuild every exercise, set and superseries by hand. This change adds the
reverse of `POST /workout-templates/{id}/workouts`: saving an existing workout as a new template.

Source brief: `.stpr/plan/openspec/proposal-change-add-workout-template-from-workout.md`.

## What Changes

- New route `POST /workouts/{workoutId}/templates` (`[Authorize]`, trainers and admins only, no request body):
  creates a workout template by **copying** the workout's name, description and exercises in order, with effort, sets,
  per-side flag, rest and superseries grouping. It answers `201` with the template and a `Location` pointing at
  `GET /workout-templates/{id}`.
- The new template's trainer is always the workout's trainer:
  - a trainer may only save workouts they prescribe (`403` otherwise, including workouts without a trainer);
  - an admin may save any workout that has a trainer (`400` for a workout without one), and never owns the template;
  - the workout's trainer must be an active `TRAINER`, so a workout whose trainer is an admin or deactivated is
    refused (`400`).
- The workout is only read, in any status. No link is kept between workout and template.
- New domain factory `WorkoutTemplate.CreateFromWorkout`. No schema change.

## Capabilities

### New Capabilities

- `workout-template-from-workout`: saving an existing workout as a new workout template over HTTP, who may do it, who
  owns the result, and the copy semantics.

### Modified Capabilities

None. `workout-templates` keeps its requirements: its access rules cover routes under `/workout-templates`, while
this route lives under `/workouts` and declares its own access rules. `workout-from-template` is the mirror operation
and is unchanged.

## Module Classification And Template Mapping

Domain areas, not modules (layer-first architecture).

| Capability / area | Classification | Canonical reference | Why |
| --- | --- | --- | --- |
| `WorkoutTemplate.CreateFromWorkout` | Existing mutable aggregate, new factory | `src/WeightTracker.Domain/Workouts/Workout.cs` (`CreateFromTemplate`) | The same copy in the opposite direction |
| `CreateWorkoutTemplateFromWorkoutUseCase` | Use case over two aggregates | `src/WeightTracker.Application/UseCases/WorkoutTemplates/CreateWorkoutFromTemplateUseCase.cs` | Loads one aggregate, checks the caller, creates the other, commits once |
| `WorkoutTemplateTrainerValidator` | Application service, extracted | `src/WeightTracker.Application/Services/MesocycleAssignmentValidator.cs` | One owner rule (`User.CanBeTrainer()`, `InvalidWorkoutTemplateTrainerException`) shared with `CreateWorkoutTemplateUseCase` |
| `WorkoutsController.CreateTemplate` | REST controller action | `src/WeightTracker.Api/Endpoints/Workouts/WorkoutsController.cs`, `src/WeightTracker.Api/Endpoints/WorkoutTemplates/WorkoutTemplatesController.cs` (`CreateWorkout`) | `[RequireRole]`, `ExceptionMapper`, `CreatedAtAction` to another controller |
| Response | DTO, reused | `src/WeightTracker.Api/Endpoints/WorkoutTemplates/Dtos/WorkoutTemplateResponse.cs` | Same template shape as every template route |
| Tests | Aggregate, use case and endpoint tests | `test/WeightTracker.Unit/Domain/Workouts/WorkoutTemplateTests.cs`, `test/WeightTracker.Unit/Application/WorkoutTemplates/CreateWorkoutFromTemplateUseCaseTests.cs`, `.stpr/template/test/WeightTracker.Integration/Auth/AuthEndpointsTests` | Copy cases, role branches, `401` matrix |

Workouts and templates are mutable business state. The workout is only read by this change.

## Business Identifiers And Mutability

- New `WorkoutTemplate.Id`: generated with `Id.New()`, immutable.
- `WorkoutTemplate.TrainerId`: set from `Workout.TrainerId` at creation, immutable, as for every template.
- Copied entry ids: new `Id.New()` values; never shared between a workout and a template.
- `Workout.Id`: path key, only read.
- No new unique business identifier. Saving the same workout twice creates two templates.

## Conflicts With Template Or Invariants

- The use case does not use `WorkoutEditAuthorizer`. That rule lets the owner of a self-planned workout edit it,
  and this operation must refuse that owner. Saving a workout as a template is not an edit of the workout, so the
  workout edit rule does not apply; the trainer check is explicit in the use case and recorded here.
- No other conflict.

## Acceptance Criteria

- Every requirement in the delta spec is covered by unit tests (aggregate factory, use case) and integration tests
  (endpoint), with labelled Arrange / Act / Assert and one Act each.
- The route covers `401` without token, `401` with an invalid token, `403` for a regular user and for another trainer,
  `404` for an unknown workout, and `400` for a workout without a trainer and for a workout trainer who cannot own
  templates.
- The new domain exception has an explicit `ExceptionMapper` entry; the action declares `[ProducesResponseType]`.
- The full `dotnet test WeightTracker.slnx` passes with Docker, and template parity reports no drift.

## Out Of Scope

- Choosing the template's name, description or trainer in the request (rename later with `PUT /workout-templates/{id}`).
- A stored link between workout and template.
- Saving self-planned workouts as templates.
- Updating an existing template from a workout, or saving several workouts in one call.
- Backoffice and app changes.

## Impact

- **Domain**: `WorkoutTemplate.CreateFromWorkout`; new `WorkoutWithoutTrainerException`.
- **Application**: `CreateWorkoutTemplateFromWorkoutUseCase`; `WorkoutTemplateTrainerValidator` extracted from
  `CreateWorkoutTemplateUseCase`, which now uses it.
- **Api**: one action on `WorkoutsController`; `ExceptionMapper` entry; `Program.cs` registration.
- **Infra / Migrations**: none.
- **Tests**: unit tests for the factory and the use case; integration tests for the route.
- **Template**: no template-backed file changes.
