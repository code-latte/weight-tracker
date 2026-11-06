# Design

## Context

See `proposal.md` for the reasons behind this change. The current state that shapes the approach:

- `WorkoutResponse` / `WorkoutDetailResponse` already project `MesocycleId?.Value` and `MesocycleWeekNumber?.Value`.
  `WorkoutListItemResponse` does not.
- The API has no global `JsonIgnoreCondition.WhenWritingNull`, so nullable record parameters serialize as `null`.
- `GetAllWorkoutsUseCase` gets the visible workouts from `WorkoutVisibility.VisibleTo`. It then joins the referenced
  exercises and users inline, checks that every user exists, and returns a `WorkoutListDTO`.
- `GetWorkoutByIdUseCase` keeps its own inline per-workout visibility predicate:
  - an admin sees any workout;
  - a trainer sees workouts they own or prescribe;
  - a user sees workouts they own;
  - nobody but an admin sees a workout `IsHiddenFrom` them.

  `WorkoutVisibility` has no per-workout check. `MesocycleVisibility` has both `VisibleTo` and `CanSee`.
- `IWorkoutRepository` has no query by mesocycle, only `ExistsByMesocycleWeek`.

## Goals / Non-Goals

**Goals:** add both reads without a third copy of the workout visibility rule or of the list joins.

**Non-Goals:**
- No change to how `GET /workouts` scopes or filters.
- No domain change.
- No persistence schema change.

## Decisions

1. **Per-workout visibility in `WorkoutVisibility.CanSee(workout, callerId, callerRole)`.** The predicate moves from
   `GetWorkoutByIdUseCase` into the service, which then uses it. This mirrors `MesocycleVisibility.CanSee`.
   `VisibleTo` keeps its repository queries.
   *Alternative:* filter `VisibleTo` by `MesocycleId`, as `standalone` does. That loads every visible workout for an
   admin to return one mesocycle's, so it was rejected. *Alternative:* copy the predicate into the new use case. That
   duplicates the rule, so it was rejected.
2. **`IWorkoutRepository.GetAllByMesocycleId(Id, CancellationToken)`** uses the existing `Query(...)` helper of
   `WorkoutRepository`, so the aggregate tables are loaded and mapping stays inside the repository.
3. **Shared list loader `WorkoutListLoader` (Application service).** It takes a list of workouts and returns a
   `WorkoutListDTO`, with the exercise join, the user join and the missing-user `InvalidOperationException` taken
   unchanged from `GetAllWorkoutsUseCase`. It follows `MesocycleViewLoader`, and both list use cases use it.
4. **`GetMesocycleWorkoutsUseCase.Execute(callerId, callerRole, mesocycleId, ct)`** runs these steps in order:
   1. `IMesocycleRepository.GetById`, or `MesocycleNotFoundException`.
   2. `MesocycleVisibility.CanSee`, or `UnauthorizedOperationException("GET /mesocycles/{id}/workouts")`.
   3. `GetAllByMesocycleId`.
   4. Filter with `WorkoutVisibility.CanSee`.
   5. Order by `MesocycleWeekNumber`, then `WorkoutDate`.
   6. `WorkoutListLoader`.

   Checking the mesocycle first means a caller who cannot see the mesocycle gets `403`. They never get an empty list
   that would confirm it exists. Both exceptions already have `ExceptionMapper` entries, so no new exception is needed.
5. **Route on `MesocyclesController`:** `[HttpGet("{id:guid}/workouts")]`. It inherits authorization from
   `AuthorizedController` and maps the result with `WorkoutListItemResponse.From`, the same way `WorkoutsController.GetAll`
   does. `ProducesResponseType` declares `200` (list of `WorkoutListItemResponse`), `401`, `403` and `404`.
6. **`WorkoutListItemResponse`** gains `Guid? MesocycleId, int? MesocycleWeekNumber` as its last positional parameters,
   with the XML docs of `WorkoutResponse`. There is no `[JsonIgnore]`.

7. **By id names its people under a new schema.** `GetWorkoutByIdUseCase` resolves the owner and trainer with
   `IUserRepository.GetAllByIds` and returns `WorkoutView(Workout, User Athlete, User? Trainer)`, following
   `MesocycleView`. A missing user throws `InvalidOperationException`, as `WorkoutListLoader` does. The new
   `WorkoutByIdResponse` is the `WorkoutDetailResponse` fields plus `Athlete` and `Trainer`
   (`UserProfileResponse`), just as `WorkoutListItemResponse` extends `WorkoutResponse`.
   *Alternative:* add the people to `WorkoutDetailResponse`. Its three write routes would then each need a user
   lookup, which goes beyond the request, so it was rejected.

**Layers:**
- Api: DTO and action.
- Application: use case, loader, and the visibility method.
- Infra: one repository query.
- Domain: untouched.

The use case depends on Mesocycles (`IMesocycleRepository`, `MesocycleVisibility`) and Workouts (`IWorkoutRepository`,
`WorkoutVisibility`, `WorkoutListLoader`).

## Risks / Trade-offs

- [Moving the by-id predicate could change `GET /workouts/{id}` behavior] → Move it unchanged. The existing
  `GetWorkoutByIdUseCaseTests` and `WorkoutGetByIdEndpointTests` must pass untouched, and `WorkoutVisibilityTests`
  gains `CanSee` cases.
- [The trainer of a mesocycle does not see a placed workout that has no trainer] → This is intended, and it matches
  `GET /workouts`. The spec states that the route never widens workout visibility.
- [Adding positional parameters to `WorkoutListItemResponse` breaks callers] → It is constructed only through `From`.
- [Template drift] → If any touched file has a copy under `.stpr/template`, update it in the same change and run the
  parity check.

## Migration Plan

There is no migration. The response change is additive. Rollback reverts the code.
