# Proposal

## Why

The backoffice "Planes asignados" screen lists workouts, but `GET /workouts` returns only `userId` and `trainerId`, so the table can show nothing but UUIDs. Joining on the front needs a second request that is role-restricted (`GET /users` is admin only, `GET /trainer-athletes` covers only current athletes), and a trainer would still see ids for former athletes. The API already knows who owns and prescribes each listed workout, so it should return them, as `add-trainer-to-users-list` did for `GET /users`.

Source brief: `.stpr/plan/openspec/proposal-change-add-names-to-workouts-list.md`. Dependent front plan: `front/.stpr/plan/openspec/proposal-change-show-workout-athlete-and-trainer-names.md`.

## What Changes

- Every item of `GET /workouts` gains:
  - `athlete`: the public profile (`id`, `email`, `name`, `surname`, `role`) of the workout owner. Never `null`.
  - `trainer`: the public profile of the prescribing trainer, or `null` when the workout has no trainer.
- Both people are returned whatever their activation state and whatever the current trainer–athlete relationship.
- `userId` and `trainerId` stay, with the same values.
- `GET /workouts` items get their own response type (`WorkoutListItemResponse`). `POST /workouts` keeps returning `WorkoutResponse`, unchanged.
- Route, per-role scoping, draft hiding, order and `401` / `403` behaviour are unchanged.
- Users are read with one query for the whole list, never one per workout.
- The OpenAPI document changes for the `GET /workouts` item schema. Additive for clients; not breaking.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `workout-retrieval`: a new requirement makes each listed workout carry its athlete and trainer, defines how they are resolved, and limits what is exposed about them.

## Canonical Template Mapping

| Area | Classification | Change | Template reference |
| --- | --- | --- | --- |
| Workouts (application) | mutable aggregate, read use case | `GetAllWorkoutsUseCase` also resolves the referenced users; `WorkoutListDTO` gains `Users` | the same use case's exercise join (`IExerciseRepository.GetAllByIds` over the scoped workouts, returned as a dictionary) |
| Users (application port) | mutable aggregate, existing port | reuse `IUserRepository.GetAllByIds`, no new method | `UseCases/Users/GetAllUsersUseCase.cs` (same port, same pairing) |
| Workouts (API) | REST controller, existing action | new `WorkoutListItemResponse`, `[ProducesResponseType]` updated | `Endpoints/Users/Dtos/UserListItemResponse.cs` and `UserProfileResponse.From(User)` |
| Workouts / Users (domain), persistence | — | no change | — |

Neither area is a catalog: `Workout` and `User` keep changing through their own endpoints. This change only adds read data to an existing read path. No conflict with `.stpr/template/STPR_INVARIANTS.md` or the template was found, and no deviation from the mapped references is allowed.

Business identifiers: `Workout.Id`, `Workout.UserId`, `Workout.TrainerId`, `User.Id` and `User.Email` are unaffected.

## Impact

- **Application**: `GetAllWorkoutsUseCase` (new `IUserRepository` dependency), `WorkoutListDTO`.
- **API**: new `Endpoints/Workouts/Dtos/WorkoutListItemResponse.cs`; `WorkoutsController.GetAll` mapping and `[ProducesResponseType]`.
- **Domain / Infra / Migrations**: no change. `IUserRepository` is already registered; DI resolves the new constructor dependency.
- **Tests**: unit `GetAllWorkoutsUseCaseTests`; integration `WorkoutScopingTests`, `WorkoutResponseShapeTests`, `OpenApiDocumentTests`.
- **Clients**: the front refreshes its OpenAPI snapshot and points its list type at `WorkoutListItemResponse` in its own plan.
