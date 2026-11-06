# Proposal

## Why

A trainer who prescribes the same session to several athletes, or repeats it across mesocycle weeks, rebuilds every
exercise, set, effort and superseries by hand each time. Workout templates let a trainer define that content once and
create workouts for athletes from it as often as needed.

Source brief: `.stpr/plan/openspec/proposal-change-add-workout-templates.md`.

## What Changes

- New `WorkoutTemplate` aggregate in the Workouts domain area: a trainer's reusable workout content (name, optional
  description, exercises with effort, sets, per side, rest, and superseries). It has no athlete, date, status or
  mesocycle and is never locked. It reuses the `WorkoutExercise` inner entity and the workout value objects.
- New `/workout-templates` resource, every route `[Authorize]`:
  - `GET /workout-templates` (trainer → own, admin → all with optional `trainerId` filter), `GET /workout-templates/{id}`.
  - `POST /workout-templates`, `PUT /workout-templates/{id}` (name, description), `DELETE /workout-templates/{id}`.
  - Content: `POST`/`PUT`/`DELETE /workout-templates/{id}/exercises[/{exerciseId}]`,
    `PUT /workout-templates/{id}/exercises/order`, `POST /workout-templates/{id}/superseries`,
    `DELETE /workout-templates/{id}/superseries/{superSeriesId}`.
  - `POST /workout-templates/{id}/workouts`: creates a `DRAFT` workout for an athlete by **copying** the template's
    content, standalone (with a date) or placed in a mesocycle week. The workout's trainer is always the template's
    trainer.
- Visibility: a trainer only sees and acts on their own templates (another trainer's template is `404`). An admin sees
  and acts on all, and creates templates only by naming an active trainer, so an admin never owns one. Regular users
  get `403` on every template route.
- New `Workout.CreateFromTemplate` factory. Workouts keep no link to their source template; editing or deleting a
  template never changes existing workouts.
- Three new tables (`WorkoutTemplates`, `WorkoutTemplateExercises`, `WorkoutTemplateExerciseSets`). No change to the
  workout tables or to any existing route.

## Capabilities

### New Capabilities

- `workout-templates`: creating, reading, listing, renaming and deleting workout templates over HTTP, who owns them,
  who may see and change them, and persistence of the aggregate.
- `workout-template-content`: adding, updating, removing and reordering exercises of a template and grouping them into
  superseries, under the same content rules as workouts.
- `workout-from-template`: creating a workout for an athlete from a template, the copy semantics, and the assignment
  and placement rules it shares with `POST /workouts`.

### Modified Capabilities

None. `workout-creation`, `workout-exercises`, `workout-superseries` and `workout-mesocycle-placement` keep their
requirements. The new capabilities reuse their rules by reference rather than changing them.

## Module Classification And Template Mapping

Domain areas, not modules (layer-first architecture).

| Capability / area | Classification | Canonical reference | Why |
| --- | --- | --- | --- |
| `WorkoutTemplate` aggregate | Mutable aggregate root with inner entity and value objects | `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs` | Same content operations, `Create` / `Rehydrate` / `IsNew`, snapshot list |
| Template exercises | Inner entity, reused | `src/WeightTracker.Domain/Workouts/WorkoutExercise.cs` | Holds no parent reference, so both aggregates can own it |
| `Workout.CreateFromTemplate` | Existing mutable aggregate, new factory | `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs` | Sibling of `Create` |
| Template write use cases | Use cases over a mutable aggregate | `src/WeightTracker.Application/UseCases/Workouts/CreateWorkoutUseCase.cs`, `UpdateWorkoutUseCase.cs`, `AddWorkoutExerciseUseCase.cs`, `UpdateWorkoutExerciseUseCase.cs`, `RemoveWorkoutExerciseUseCase.cs`, `ReorderWorkoutExercisesUseCase.cs`, `CreateSuperSeriesUseCase.cs`, `BreakSuperSeriesUseCase.cs` | Load, authorize, act, save |
| Template read use cases | Caller-scoped reads | `src/WeightTracker.Application/UseCases/Exercises` (`GetAllExercisesUseCase`), `UseCases/Workouts/GetWorkoutByIdUseCase.cs` | Result depends on the caller |
| `WorkoutTemplateEditAuthorizer` | Business authorization service | `src/WeightTracker.Application/Services/WorkoutEditAuthorizer.cs` | One rule for every template use case |
| Create workout from template | Use case over two aggregates | `src/WeightTracker.Application/UseCases/Workouts/CreateWorkoutUseCase.cs` | Reuses `WorkoutAssignmentValidator` and `MesocyclePlacementValidator` |
| Repository, mapper, EF, migrations | Repository / aggregate mapper / migration | `src/WeightTracker.Infra/Repositories/WorkoutRepository.cs`, `src/WeightTracker.Infra/Mappers/WorkoutMapper.cs`, `src/WeightTracker.Migrations/Migrations/202610080931_CreateWorkoutsTable.cs` | Multi-table aggregate |
| `WorkoutTemplatesController`, DTOs | REST controller | `src/WeightTracker.Api/Endpoints/Workouts/WorkoutsController.cs` | `AuthorizedController`, `CurrentUser`, `ExceptionMapper` |
| Integration tests | Endpoint auth matrix, scoped read | `.stpr/template/test/WeightTracker.Integration/Auth/AuthEndpointsTests`, `.stpr/template/test/WeightTracker.Integration/Workouts/WorkoutScopingTests` | `401` matrix per route; exclusions asserted |

Templates are mutable business state, not a catalog.

## Business Identifiers And Mutability

- `WorkoutTemplate.Id`: generated with `Id.New()`, immutable; path key of `/workout-templates/{id}`.
- `WorkoutTemplate.TrainerId`: set on creation, immutable in this change; no endpoint changes it.
- Template `WorkoutExercise` entry ids: generated with `Id.New()`, immutable. Copies into a workout get new ids; an id
  is never shared between a template and a workout.
- `(WorkoutTemplate.Id, catalog exercise id)`: identifies a template exercise in content routes, immutable (swapping
  an exercise is a removal plus an addition), as for workouts.
- Template names are not unique. No new unique business identifier.

## Conflicts With Template Or Invariants

- `WorkoutTemplate` duplicates the content operations of `Workout` instead of sharing a base type. Extracting one
  would rewrite `Workout.cs`, a template-backed file. The duplication is accepted and recorded. No invariant is broken.
- A template is a second aggregate that owns `WorkoutExercise`. Each aggregate still owns its own instances and saves
  them only through its own repository, so the inner entity is never shared across roots.
- Another trainer's template answers `404` instead of the `403` used by workout edits, so a trainer cannot learn that
  another trainer's template exists. This is a visibility decision for a private resource and is recorded here.
- No other conflict.

## Acceptance Criteria

- Every requirement in the three delta specs is covered by unit tests (aggregate, use cases, authorizer, mapper) and
  integration tests (endpoints), with labelled Arrange / Act / Assert.
- Every new route covers `401` with no token, `401` with an invalid token, `403` for a regular user, `404` for a
  missing template and for another trainer's template, and `400` where structural errors apply. The list endpoint asserts
  that templates are excluded across trainers.
- A workout created from a template matches the template's content, and later template edits or deletion do not
  change it.
- Every new exception has an explicit `ExceptionMapper` entry, and every action declares `[ProducesResponseType]`.
- The full `dotnet test WeightTracker.slnx` passes with Docker, and template parity reports no drift.

## Out Of Scope

- Reassigning a template to another trainer, sharing templates, or a global admin library.
- Saving an existing workout as a template.
- A `SourceTemplateId` link from a workout to its template.
- Mesocycle templates, and creating several workouts from one template in one call.
- Template versioning, or applying template edits to existing workouts.
- Backoffice and app changes.

## Impact

- **Domain**: `WorkoutTemplate` (new), `Workout.CreateFromTemplate` (new), new domain exceptions if needed.
- **Application**: `IWorkoutTemplateRepository`, `WorkoutTemplateEditAuthorizer`, eleven template use cases plus
  `CreateWorkoutFromTemplateUseCase`, new Application exceptions.
- **Infra**: `WorkoutTemplateRepository`, `WorkoutTemplateMapper`, three EF entities and their configuration, seed;
  `WorkoutMapper` set-target and effort conversions become `internal` so the template mapper reuses them.
- **Migrations**: three create-table migrations with reversing `Down()`.
- **Api**: `WorkoutTemplatesController` and DTOs, `ExceptionMapper` entries, `Program.cs` registrations.
- **Tests**: unit and integration tests above; integration truncate list extended.
- **Template**: `Workout.cs` and `WorkoutTests` change, so their `.stpr/template` copies are updated together.
