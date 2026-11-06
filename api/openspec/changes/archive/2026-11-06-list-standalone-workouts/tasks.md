# Tasks

## 1. Application

- [x] 1.1 Add `standaloneOnly` to `GetAllWorkoutsUseCase.Execute`, filtering `MesocycleId is null` after `VisibleTo`; verify with `GetAllWorkoutsUseCaseTests` (placed workouts left out when set, kept when not, users joined only for returned workouts)

## 2. Api

- [x] 2.1 Bind `[FromQuery] bool standalone` in `WorkoutsController.GetAll` and forward it; verify the OpenAPI document declares `standalone` on `GET /workouts`

## 3. Integration tests

- [x] 3.1 Add `WorkoutScopingTests` cases: admin `?standalone=true` gets exactly the seeded workouts with no mesocycle; a trainer's standalone list is a subset of their full list without placed workouts; verify `dotnet test test/WeightTracker.Integration`

## 4. Review

- [x] 4.1 Compare the use case, action and tests with their mapped references (same scoping path, Arrange / Act / Assert, seed data) and run `dotnet test test/WeightTracker.Unit` and `dotnet test test/WeightTracker.Integration`
