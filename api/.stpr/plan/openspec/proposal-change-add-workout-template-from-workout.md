## Change

Let a trainer, or an admin on the trainer's behalf, save an existing workout as a new workout template

## Objective

Trainers often build a good session directly as a workout for an athlete and only later realise they want to reuse
it. Today the only way to get it into a template is to rebuild every exercise, set and superseries by hand in a new
template. This change adds the reverse of `POST /workout-templates/{id}/workouts`: a new template is created by
**copying** the content of an existing workout.

The new template belongs to the workout's trainer, whoever makes the request. A trainer can only save workouts they
prescribe, and an admin can only save workouts that have a trainer, because the admin never owns a template.

The copy is independent. Later changes to the workout or the template never affect each other, and neither keeps a
link to the other.

## Capabilities

- Create a workout template from an existing workout with `POST /workouts/{workoutId}/templates`: the workout's name,
  description and exercises in order, each with effort, sets, per-side flag, rest and superseries grouping.
- The template's trainer is always the workout's trainer, for both a trainer and an admin request.
- Return the new template (`201`) with a `Location` header pointing at `GET /workout-templates/{id}`.

## Module Classification

Domain areas, not modules (the architecture is layer-first; each area is a slice across the layer projects).

- Workouts (domain) -> existing mutable aggregate `WorkoutTemplate`, one new factory that copies a workout ->
  `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs` (`Workout.CreateFromTemplate`, the mirror operation)
- Workout templates (application) -> one new use case over two aggregates, reading a workout and creating a template ->
  `src/WeightTracker.Application/UseCases/WorkoutTemplates/CreateWorkoutFromTemplateUseCase.cs`
- Workouts (api) -> one new action on the existing protected controller ->
  `src/WeightTracker.Api/Endpoints/Workouts/WorkoutsController.cs`

Workouts and templates are both mutable business state. Neither is a catalog. The workout is only read by this change.

## Canonical Template Mapping

- `WorkoutTemplate.CreateFromWorkout` -> `src/WeightTracker.Domain/Workouts/Workout.cs` (`CreateFromTemplate`) -> the
  same copy in the opposite direction: new entry ids, superseries ids kept as they are
- `CreateWorkoutTemplateFromWorkoutUseCase` -> `src/WeightTracker.Application/UseCases/WorkoutTemplates/CreateWorkoutFromTemplateUseCase.cs`
  -> loads one aggregate, checks the caller, creates the other, saves once
- Trainer check -> `src/WeightTracker.Application/UseCases/WorkoutTemplates/CreateWorkoutTemplateUseCase.cs` -> the
  template's trainer must be an active `TRAINER` (`User.CanBeTrainer()`), refused with `InvalidWorkoutTemplateTrainerException`
- Controller action -> `src/WeightTracker.Api/Endpoints/Workouts/WorkoutsController.cs` -> `[RequireRole]`,
  `CurrentUser`, `ExceptionMapper.Map`, `[ProducesResponseType]`, `CreatedAtAction` to another controller as
  `WorkoutTemplatesController.CreateWorkout` does
- Response -> `src/WeightTracker.Api/Endpoints/WorkoutTemplates/Dtos/WorkoutTemplateResponse.cs`, reused as is
- Tests -> `test/WeightTracker.Unit/Domain/Workouts/WorkoutTemplateTests.cs`, mirroring the `CreateFromTemplate` cases in
  `.stpr/template/test/WeightTracker.Unit/Domain/Workouts/WorkoutTests`,
  `test/WeightTracker.Unit/Application/WorkoutTemplates/CreateWorkoutFromTemplateUseCaseTests.cs`,
  `test/WeightTracker.Integration/WorkoutTemplates/WorkoutTemplateAuthEndpointsTests.cs` and
  `WorkoutFromTemplateEndpointsTests.cs`

## State And Mutability

- A new row in `WorkoutTemplates`, with its `WorkoutTemplateExercises` and `WorkoutTemplateExerciseSets`. No schema
  change and no migration.
- The source workout is read, never saved. Its status, owner, date and mesocycle placement are not copied and not changed.
- No link is stored between the workout and the template.

## Business Rules

- The caller must be a `TRAINER` or an `ADMIN`. A regular user is refused (`403`), including the workout's owner.
- An unknown workout is `404`.
- **Trainer caller:** the workout's `TrainerId` must be the caller. A workout prescribed by another trainer, or one
  without a trainer, is refused with `403`, as the workout edit rule refuses it.
- **Admin caller:** the workout must have a trainer, and that trainer owns the template. The admin never owns it.
  A workout without a trainer is refused with `400`.
- The workout's trainer must be able to own a template: an existing, active user with the `TRAINER` role. A workout
  whose trainer is an admin (an admin can assign an admin as a workout's trainer) or a deactivated trainer is refused
  with `400`. This is the same rule as `POST /workout-templates`.
- The request carries no body. The template takes the workout's trainer, name and description.
- The workout may be in any status (`DRAFT`, `PUBLISHED`, `STARTED`, `FINISHED`). Copying only reads its content, so
  the lock does not apply.
- Every exercise is copied as it is, including one archived after it was added to the workout. Custom exercises stay
  valid in the template, because a prescribed workout only holds custom exercises of its trainer.
- A workout with no exercises produces an empty template.
- Copied entries get new identifiers (`Id.New()`). Superseries ids are kept, since they are unique within the workout
  and the template starts empty.

## Roles And Visibility

- **Trainer:** may save only the workouts they prescribe. The template is theirs.
- **Admin:** may save any workout that has a trainer who can own templates. The template belongs to that trainer.
- **Regular user:** no access (`403`).
- Route: `POST /workouts/{workoutId}/templates`, `[Authorize]`, `[RequireRole(TRAINER, ADMIN)]`, no body,
  `201` with `WorkoutTemplateResponse`.

## Business Identifiers And Mutability

- The new `WorkoutTemplate.Id` is generated with `Id.New()` and immutable.
- `WorkoutTemplate.TrainerId` is set from `Workout.TrainerId` at creation and immutable, as for every template.
- Copied entry ids are new. An entry id is never shared between a workout and a template.
- No new unique business identifier. Template names are not unique, so saving the same workout twice creates two
  templates.

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## Out Of Scope

- Choosing the template's name, description or trainer in the request. The template can be renamed afterwards with
  `PUT /workout-templates/{id}`.
- Keeping a link between the workout and the template (`SourceWorkoutId` or `SourceTemplateId`).
- Saving a self-planned workout (no trainer) as a template, for anyone.
- Overwriting or updating an existing template from a workout.
- Saving several workouts, or a whole mesocycle, as templates in one call.
- Backoffice and app changes.

## Notes

- Domain: `WorkoutTemplate.CreateFromWorkout(Workout workout)` in `src/WeightTracker.Domain/Workouts/WorkoutTemplate.cs`.
  It takes `workout.TrainerId`, and throws a domain exception when the workout has none, so the aggregate cannot be
  created without an owner even if a caller skips the Application check. Both aggregates are in the Workouts area, so
  no domain area references another.
- The workout's trainer check (role and active) lives in Application, because it needs the user.
- Exceptions (names final at proposal time), each with an explicit `ExceptionMapper` entry:
  - Reuse `WorkoutNotFoundException` (404), `UnauthorizedOperationException` (403) and
    `InvalidWorkoutTemplateTrainerException` (400).
  - Add a domain exception for a workout without a trainer (400), for example `WorkoutWithoutTrainerException`.
- Check order in the use case: load the workout (404), then refuse a regular user (403), then a trainer who is not the
  workout's trainer (403), then a workout without a trainer (400), then a trainer who cannot own templates (400). A
  trainer is never told why another trainer's workout was refused.
- The use case does not go through `WorkoutEditAuthorizer`. That rule lets the owner of a self-planned workout edit it,
  and this operation does not, so the trainer check stays explicit in this use case. Saving is not an edit of the
  workout, so the `AGENTS.md` checklist item on the workout edit rule does not apply.
- Register the use case in `Program.cs`. Repositories already exist: `IWorkoutRepository`, `IWorkoutTemplateRepository`,
  `IUserRepository`.
- The action lives on `WorkoutsController` because the path belongs to the workouts resource, and returns the template
  DTO from `Endpoints/WorkoutTemplates/Dtos`.
- Tests to add:
  - Unit, domain (`WorkoutTemplateTests`): `CreateFromWorkout` copies name, description and every exercise in order with effort, sets, per side,
    rest and superseries; gives new entry ids; marks the template new; takes the workout's trainer; refuses a workout
    without a trainer; works for an empty workout and for a started one; later changes to the workout do not reach
    the template.
  - Unit, application: trainer of the workout, other trainer, trainer on a self-planned workout, admin, admin on a
    workout without trainer, workout trainer is an admin, deactivated workout trainer, regular user, unknown workout.
    Assert nothing is saved on failure, and that the workout is never saved.
  - Integration: `401` without token and with an invalid token, `403` for the workout's owner (a regular user), `403`
    for another trainer, `404` for an unknown workout, `400` for an admin on a self-planned workout and for a
    deactivated workout trainer. Success for the trainer and the admin, checking `201`, `Location`, `trainerId`, and
    content copied in order with superseries. The created template is listed by `GET /workout-templates` for the
    workout's trainer and not for another trainer. Removing an exercise from the workout afterwards leaves the template
    unchanged.
  - Add `POST /workouts/{workoutId}/templates` to the auth matrix of `WorkoutTemplateAuthEndpointsTests`, or to the
    workout auth matrix, whichever the proposal picks.
  - Full `dotnet test WeightTracker.slnx` with Docker.
- `.stpr/template` parity: no template-backed file changes. `CreateFromWorkout` and its tests live in
  `WorkoutTemplate.cs` and `WorkoutTemplateTests.cs`, which are not template copies.
- No deviations from the mapped template references are allowed.
