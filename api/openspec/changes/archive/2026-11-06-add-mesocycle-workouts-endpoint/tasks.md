# Tasks

## 1. Workout list placement

- [x] 1.1 Add `MesocycleId` and `MesocycleWeekNumber` to `WorkoutListItemResponse` (same projection and XML docs as `WorkoutResponse`); verify `dotnet build WeightTracker.slnx`
- [x] 1.2 Add `WorkoutScopingTests` (or `WorkoutResponseShapeTests`) cases: a seeded placed workout returns its mesocycle id and week number; a standalone workout returns both properties present and `null`, also with `standalone=true`; verify with `dotnet test test/WeightTracker.Integration`
- [x] 1.3 Add an `OpenApiDocumentTests` case asserting `WorkoutListItemResponse` declares nullable `mesocycleId` (uuid) and nullable integer `mesocycleWeekNumber`; verify it passes
- [x] 1.4 Review: compare `WorkoutListItemResponse` with `WorkoutResponse` and the new tests with their neighbours (Arrange / Act / Assert, seed data)

## 2. Shared workout visibility and list loading

- [x] 2.1 Add `WorkoutVisibility.CanSee(workout, callerId, callerRole)` with the predicate moved unchanged from `GetWorkoutByIdUseCase`, and make that use case call it; verify new `WorkoutVisibilityTests` cases (admin, trainer owner/prescriber/unrelated, user owner/other, hidden prescribed draft) and unchanged `GetWorkoutByIdUseCaseTests` pass
- [x] 2.2 Extract the exercise and user joins of `GetAllWorkoutsUseCase` into `WorkoutListLoader` (Application service, following `MesocycleViewLoader`), register it in `Program.cs`, and use it from `GetAllWorkoutsUseCase`; verify `WorkoutListLoaderTests` (joins only referenced exercises and users, throws on a missing user) and unchanged `GetAllWorkoutsUseCaseTests` pass
- [x] 2.3 Review: compare `CanSee` with `MesocycleVisibility.CanSee` and the loader with `MesocycleViewLoader`

## 3. Repository

- [x] 3.1 Add `IWorkoutRepository.GetAllByMesocycleId` and implement it in `WorkoutRepository` with the existing `Query(...)` helper; verify an integration repository test (or the endpoint tests in 4.4) returns only that mesocycle's workouts with exercises loaded

## 4. Mesocycle workouts read

- [x] 4.1 Add `GetMesocycleWorkoutsUseCase` (mesocycle lookup → `MesocycleNotFoundException`, `MesocycleVisibility.CanSee` → `UnauthorizedOperationException`, `GetAllByMesocycleId`, filter with `WorkoutVisibility.CanSee`, order by week then date, `WorkoutListLoader`) and register it in `Program.cs`
- [x] 4.2 Add `GetMesocycleWorkoutsUseCaseTests` under `test/WeightTracker.Unit/Application/Mesocycles`: not found, not visible (other trainer, other user), athlete without the prescribed draft, trainer with drafts, admin sees all, empty list, ordering; verify `dotnet test test/WeightTracker.Unit`
- [x] 4.3 Add `GET /mesocycles/{id:guid}/workouts` to `MesocyclesController` with `ProducesResponseType` for `200`/`401`/`403`/`404`, catching into `ExceptionMapper.Map`, mapping with `WorkoutListItemResponse.From`; verify the route appears in `/openapi/v1.json`
- [x] 4.4 Add `MesocycleReadEndpointsTests` cases for the route: `401` no token, `401` invalid token, `404` unknown id, `403` other trainer and other user, `200` for athlete, trainer and admin; assert workouts of more than one week are included and standalone and other-mesocycle workouts are excluded, and the athlete does not see a prescribed draft (seed an extra workout or mesocycle only if the baseline cannot prove it); verify `dotnet test test/WeightTracker.Integration`
- [x] 4.5 Review: compare the use case with `GetMesocycleByIdUseCase` + `GetAllWorkoutsUseCase`, the action with `MesocyclesController.GetById` / `WorkoutsController.GetAll`, and the tests with `MesocycleReadEndpointsTests` / `GetMesocycleByIdUseCaseTests`

## 5. Verification

- [x] 5.1 Run the `.stpr/template` parity check from `AGENTS.md` and update any template copy touched by this change; verify no `DRIFT` lines
- [x] 5.2 Run the full `dotnet test WeightTracker.slnx` with Docker running; verify it passes
- [x] 5.3 Run `openspec validate add-mesocycle-workouts-endpoint`; verify it passes

## 6. Workout read by id names its athlete and trainer

- [x] 6.1 Add `WorkoutView` and make `GetWorkoutByIdUseCase` resolve the owner and trainer through `IUserRepository` (throwing `InvalidOperationException` for a missing user); register nothing new; verify `GetWorkoutByIdUseCaseTests` pass with the visibility cases unchanged, plus new cases for the resolved people, a null trainer and a missing user
- [x] 6.2 Add `WorkoutByIdResponse` (`WorkoutDetailResponse` fields + `Athlete` / `Trainer`) and return it from `WorkoutsController.GetById`, keeping `WorkoutDetailResponse` on the write routes; verify `dotnet build WeightTracker.slnx`
- [x] 6.3 Add `WorkoutGetByIdEndpointTests` cases (prescribed workout names both people with exactly the public profile properties; self-planned has `trainer` `null`; `PUT /workouts/{id}` body has neither) and an `OpenApiDocumentTests` case (by-id references `WorkoutByIdResponse` with required `athlete` and nullable `trainer`; `PUT` keeps `WorkoutDetailResponse`); verify `dotnet test test/WeightTracker.Integration`
- [x] 6.4 Review: compare `WorkoutByIdResponse` with `WorkoutListItemResponse`, `WorkoutView` with `MesocycleView`, and the tests with their neighbours
- [x] 6.5 Rerun the `.stpr/template` parity check, the full `dotnet test WeightTracker.slnx` with Docker and `openspec validate add-mesocycle-workouts-endpoint`; verify no drift and both pass
