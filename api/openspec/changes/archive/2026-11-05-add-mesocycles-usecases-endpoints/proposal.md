# Proposal

## Why

The previous change (`add-mesocycles`) modelled mesocycles, their weeks and the optional placement of a workout in
Domain and persistence, but nothing reaches them over HTTP. Trainers cannot plan a block for an athlete, athletes
cannot read their plan, and workouts cannot be placed in a plan. This change adds the use cases and endpoints, plus
the cross-aggregate checks the previous change explicitly left for Application.

Source brief: `.stpr/plan/openspec/proposal-change-add-mesocycles-usecases-endpoints.md`.

## What Changes

- New `/mesocycles` resource:
  - `GET /mesocycles/{id}`: the mesocycle's athlete, its trainer, or an admin.
  - `GET /mesocycles`: the caller's mesocycles (athlete → where athlete, trainer → where trainer, admin → all).
  - `GET /users/{userId}/mesocycles`: trainer → their mesocycles for that athlete, admin → all of that athlete.
  - `POST /mesocycles`: a trainer for an athlete with an **active** trainer–athlete relationship; an admin for any
    trainer (role `TRAINER`) and athlete (role `USER`).
  - `PUT /mesocycles/{id}`: name, objective, start date and duration; the mesocycle's trainer or an admin.
  - `POST /mesocycles/{id}/weeks`: define a new week; the mesocycle's trainer or an admin.
  - `PUT /mesocycles/{id}/weeks/{weekNumber}`: redefine an existing week; the mesocycle's trainer or an admin.
- Every mesocycle response carries its weeks and the public profiles of its trainer and athlete.
- `POST /workouts` and `PUT /workouts/{id}` accept an optional `mesocycleId` + `mesocycleWeekNumber` pair (both or
  neither). `WorkoutResponse` and `WorkoutDetailResponse` expose the pair.
- Placement checks across aggregates: the mesocycle exists, its athlete is the workout's owner, the week is within its
  duration, the workout's trainer (if any) is the mesocycle's trainer, and the caller is the mesocycle's trainer or an
  admin.
- Domain: `Mesocycle.DefineWeek` (an upsert) is replaced by `AddWeek` (refuses an existing week) and `RedefineWeek`
  (refuses a missing week). **BREAKING** for the aggregate API only; no HTTP contract existed.
- `PUT /workouts/{id}` now replaces the placement: omitting both values unlinks a linked workout. **BREAKING** for
  clients that already send `PUT /workouts/{id}` on a linked workout without the pair. Today only the seeded workouts
  are linked, since placement had no HTTP entry point.
- `IMesocycleRepository` gains `GetAll`, `GetAllByTrainerId`, `GetAllByAthleteId`, `GetAllByTrainerIdAndAthleteId`.

## Capabilities

### New Capabilities

- `mesocycle-retrieval`: reading one mesocycle and listing mesocycles over HTTP, scoped by role and ownership, and the
  response shape (weeks, trainer and athlete profiles).
- `mesocycle-management`: creating and updating mesocycles and defining and redefining their weeks over HTTP, with
  who may do each.

### Modified Capabilities

- `mesocycles`: "Redefining a week keeps its number" is replaced. Defining a new week refuses an existing number, and
  redefining refuses a number that is not defined.
- `workout-mesocycle-placement`: "Existing workout HTTP behaviour is unchanged" is removed. Requirements are added for
  setting, moving and clearing the placement through `POST`/`PUT /workouts`, the checks across aggregates, and
  exposing the pair in workout responses.

## Module Classification And Template Mapping

Domain areas, not modules (layer-first architecture).

| Capability / area | Classification | Canonical reference | Why |
| --- | --- | --- | --- |
| Mesocycle read use cases | Use cases over a mutable aggregate | `.stpr/template/src/WeightTracker.Application/UseCases/Exercises/GetAllExercisesUseCase.cs`, `src/WeightTracker.Application/UseCases/Workouts/GetWorkoutByIdUseCase.cs` | Use case shape; single read with a visibility check |
| `MesocycleVisibility` | Application service | `src/WeightTracker.Application/Services/WorkoutVisibility.cs` | One place for the read scope |
| `MesocycleViewLoader`, `MesocycleView` | Application service + view | `src/WeightTracker.Application/Services/TrainerAthleteViewLoader.cs`, `UseCases/TrainerAthletes/TrainerAthleteView.cs` | Joins trainer and athlete in one lookup |
| Mesocycle write use cases | Use cases over a mutable aggregate | `src/WeightTracker.Application/UseCases/Workouts/CreateWorkoutUseCase.cs`, `UpdateWorkoutUseCase.cs`, `AddWorkoutExerciseUseCase.cs`, `UpdateWorkoutExerciseUseCase.cs` | Role-resolved assignment; load, authorize, act, save; act on an inner entity through its root |
| `MesocycleEditAuthorizer` | Application service | `src/WeightTracker.Application/Services/WorkoutEditAuthorizer.cs` | Single edit rule shared by every write |
| `MesocycleAssignmentValidator` | Application service | `src/WeightTracker.Application/Services/WorkoutAssignmentValidator.cs` | User existence, role and active checks |
| `MesocyclePlacementValidator` | Application service | `src/WeightTracker.Application/Services/WorkoutAssignmentValidator.cs` | Shared by workout create and update |
| `Mesocycle.AddWeek` / `RedefineWeek` | Existing mutable aggregate, action methods | `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs` | Guarded action methods |
| `MesocycleRepository` list queries | Repository | `.stpr/template/src/WeightTracker.Application/Interfaces/IWorkoutRepository.cs`, `src/WeightTracker.Infra/Repositories/WorkoutRepository.cs` | `GetAll` / `GetAllBy<Criteria>` shapes |
| `MesocyclesController`, DTOs | Api resource | `src/WeightTracker.Api/Endpoints/Workouts/WorkoutsController.cs`, `Endpoints/Workouts/Dtos/WorkoutListItemResponse.cs` | `AuthorizedController`, `RequireRole`, `ExceptionMapper`, DTO `From`, `UserProfileResponse` for people |
| `GET /users/{userId}/mesocycles` | Api action on the users resource | `src/WeightTracker.Api/Endpoints/Users/UsersController.cs` | Route lives under the resource that owns the path |
| Workout placement over HTTP | Existing mutable aggregate, use cases and DTOs extended | `CreateWorkoutUseCase`, `UpdateWorkoutUseCase`, `CreateWorkoutRequest`, `UpdateWorkoutRequest`, `WorkoutResponse` | Same endpoints, new optional fields |
| Integration tests | Endpoint tests | `.stpr/template/test/WeightTracker.Integration/Workouts/WorkoutEndpointTest.cs`, `WorkoutScopingTests.cs` | Auth matrix per route, scoped reads asserting exclusions |

Mesocycles are mutable business state, not a catalog.

## Business Identifiers And Mutability

- `Mesocycle.Id`: from `Id.New()`, immutable; path key of `/mesocycles/{id}`.
- `(Mesocycle.Id, WeekNumber)`: business identifier of a week, immutable; path key of
  `/mesocycles/{id}/weeks/{weekNumber}`. Redefining changes type and objective, never the number.
- `MesocycleWeek.Id`: internal row id, never exposed or accepted.
- `Mesocycle.TrainerId`, `Mesocycle.AthleteId`: set on creation, immutable; no endpoint changes them.
- `Workout.MesocycleId`, `Workout.MesocycleWeekNumber`: mutable through `POST`/`PUT /workouts` while the workout is
  `DRAFT` or `PUBLISHED`; unchanged values on a locked workout are accepted.

## Conflicts With Template Or Invariants

- The brief asked for `PUT /mesocycles/{id}/weeks/{weekId}`. The week row id is internal and the business identifier
  is the week number, so the route uses `{weekNumber}`. No invariant is broken; this is a naming decision, recorded.
- Replacing `DefineWeek` changes the accepted `mesocycles` spec. It is declared as a modified capability, not a
  silent change.
- No other conflict. Cross-aggregate placement checks live in Application, as the invariants require for rules
  spanning two aggregates.

## Acceptance Criteria

- Every requirement in the four delta specs is covered by unit tests (use cases, services, aggregate) and integration
  tests (endpoints), with labelled Arrange / Act / Assert.
- Every new protected route covers `401` with no token and `401` with an invalid token, plus `403`/`404`/`400`/`409`
  where they apply. Scoped reads assert what is excluded.
- Every new exception has an explicit `ExceptionMapper` entry; every action declares `[ProducesResponseType]`.
- The full `dotnet test WeightTracker.slnx` passes with Docker, and template parity reports no drift.

## Out Of Scope

- Deleting mesocycles, removing weeks over HTTP, and `GET /mesocycles/{id}/weeks`.
- Reassigning a mesocycle's trainer or athlete.
- Listing the workouts of a mesocycle or week; placement in `WorkoutListItemResponse`.
- Correcting the placement of a started or finished workout.
- What happens to linked workouts when a duration shrinks below their week.
- Front changes.

## Impact

- **Domain**: `Mesocycle` (`AddWeek`, `RedefineWeek`, `DefineWeek` removed); existing exceptions reused.
- **Application**: seven mesocycle use cases, `MesocycleView`, `MesocycleViewLoader`, `MesocycleVisibility`,
  `MesocycleEditAuthorizer`, `MesocycleAssignmentValidator`, `MesocyclePlacementValidator`; `IMesocycleRepository`
  extended; `CreateWorkoutUseCase` and `UpdateWorkoutUseCase` extended; new Application exceptions.
- **Infra**: `MesocycleRepository` list queries.
- **Api**: `MesocyclesController` and DTOs, a new action on `UsersController`, workout request and response DTOs
  extended, `ExceptionMapper` entries, `Program.cs` registrations.
- **Tests**: unit tests for every use case, service and the new aggregate methods; integration tests per route; existing
  `DefineWeek` tests and callers migrated.
- **Template**: `.stpr/template` copies are touched only if `Workout.cs` or its template tests change, which this change does
  not plan to do.
