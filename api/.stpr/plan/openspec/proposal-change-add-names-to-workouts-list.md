## Change

Add the athlete and the trainer of each workout to the items of `GET /workouts`

## Objective

The backoffice "Planes asignados" screen lists workouts but `GET /workouts` only returns `UserId` and `TrainerId`, so
the table can only print UUIDs. The front could join with `GET /users` (admin only) or `GET /trainer-athletes`
(trainer only, and only *current* athletes), but a trainer would still see ids for workouts of former athletes, and
every caller would need a second request. The API already reads the workouts and knows who owns and prescribes each
one, so it should return their names directly, the same way `add-trainer-to-users-list` did for `GET /users`.

Front plan that depends on this change:
`front/.stpr/plan/openspec/proposal-change-show-workout-athlete-and-trainer-names.md`.

## Capabilities

- `GET /workouts` returns, on every item, two new properties:
  - `Athlete`: the workout owner's public details. Never `null`.
  - `Trainer`: the prescribing trainer's public details, or `null` when the workout has no `TrainerId`.
- `UserId` and `TrainerId` stay, with the same values, so no existing client breaks.
- Nothing else changes in the endpoint: same route, same per-role scoping, same order, same failures.

## Module Classification

- Workouts (application) -> mutable aggregate use case (read) ->
  `src/WeightTracker.Application/UseCases/Workouts/GetAllWorkoutsUseCase.cs` and its read model
  `WorkoutListDTO.cs`
- Users (application port) -> existing `IUserRepository.GetAllByIds`, no new method ->
  `src/WeightTracker.Application/Interfaces/IUserRepository.cs`
- Workouts (API) -> REST controller, existing action, response shape extended ->
  `src/WeightTracker.Api/Endpoints/Workouts/WorkoutsController.cs`,
  `src/WeightTracker.Api/Endpoints/Workouts/Dtos/WorkoutResponse.cs`
- Workouts (domain), Users (domain), persistence -> no change.

Neither area is a catalog. `Workout` and `User` keep changing through their own endpoints; this change only adds read
data to an existing read path.

## Canonical Template Mapping

- `GetAllWorkoutsUseCase` resolving owners and trainers -> the same use case's existing exercise join
  (`_exercises.GetAllByIds` over the scoped workouts' ids, returned as a dictionary in `WorkoutListDTO`) -> the
  nearest reference: same use case, same "join only what the scoped workouts reference" shape, bounded queries
- Users lookup -> `src/WeightTracker.Application/UseCases/Users/GetAllUsersUseCase.cs` after
  `add-trainer-to-users-list` (`IUserRepository.GetAllByIds` for the related users) -> same port, same pairing
- Response DTO -> `src/WeightTracker.Api/Endpoints/Users/Dtos/UserProfileResponse.cs` (`From(User)`) for the nested
  people, as `GET /users` does for `Trainer` -> existing public user DTO
- Tests -> `test/WeightTracker.Unit/Application/Workouts/GetAllWorkoutsUseCaseTests.cs`,
  `test/WeightTracker.Integration/Workouts/WorkoutScopingTests.cs`,
  `test/WeightTracker.Integration/Workouts/WorkoutResponseShapeTests.cs`,
  `test/WeightTracker.Integration/OpenApi/OpenApiDocumentTests.cs`

## State And Mutability

- Nothing changes state. `GET /workouts` still only reads, now from `workouts`, `exercises` and `users`.
- No new tables, columns or indexes, so no migration.

## Business Rules

- `Athlete` is the user whose id is the workout's `UserId`; `Trainer` is the user whose id is its `TrainerId`, or
  `null` when `TrainerId` is `null`.
- Both are returned whatever their activation state (`IsActive`) and whatever the current trainer–athlete
  relationship: a workout prescribed by a trainer to a former athlete still names both.
- The per-role scoping is unchanged and decides visibility: the join only resolves users referenced by the workouts
  the caller may already see, so it never widens what the caller learns.
- The list order and the `401` / `403` behaviour are unchanged.
- The users are read with one query for the whole list (no query per workout), alongside the existing exercise query.

## Roles And Visibility

- `ADMIN`: every workout, each with `Athlete` and `Trainer`.
- `TRAINER`: the workouts they own or prescribe, as today, each with `Athlete` and `Trainer`.
- `USER`: their own published workouts, as today, each with `Athlete` (themselves) and `Trainer` (the trainer who
  prescribed it). An athlete seeing their trainer's name and email is already possible through the trainer–athlete
  endpoints.
- Unauthenticated or invalid token: `401`, unchanged.
- The nested people expose only `Id`, `Email`, `Name`, `Surname` and `Role`. Never the password hash, refresh tokens,
  activation data or trainer relationships.

## Business Identifiers And Mutability

- `Workout.Id`, `Workout.UserId`, `Workout.TrainerId`, `User.Id` and `User.Email` are not affected.

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## Out Of Scope

- `GET /workouts/{id}` (`WorkoutDetailResponse`), `PUT /workouts/{id}` and every other workout endpoint: their
  responses do not gain the people (see Notes for `POST /workouts`).
- Filtering, searching or sorting `GET /workouts` by athlete or trainer; pagination.
- Removing `UserId` / `TrainerId` from the list items.
- Any front change. That is the separate front plan named in Objective, after the OpenAPI snapshot refresh.

## Notes

- **Nested type (decided).** `Athlete` is `UserProfileResponse` and `Trainer` is `UserProfileResponse?`, the existing
  public user DTO already nested by `GET /users`. No new person DTO.
- **Where the DTO changes (decided).** `WorkoutResponse` is also the `201` body of `POST /workouts`, which builds it
  without loading exercises. Add a dedicated list item DTO, `WorkoutListItemResponse` (the `WorkoutResponse` fields plus
  `Athlete` and `Trainer`), used only by `GET /workouts`, so `POST /workouts` stays exactly as it is. This mirrors
  `UserListItemResponse` in `add-trainer-to-users-list`. The front plan must point its list alias at the new schema
  (`WorkoutListItemResponse`) instead of `WorkoutResponse`.
- **Application result.** `WorkoutListDTO` gains `IReadOnlyDictionary<Id, User> Users`, filled with one
  `IUserRepository.GetAllByIds` call over the distinct `UserId` and non-null `TrainerId` of the scoped workouts. The
  controller maps each item from the workout, the exercises and the users dictionaries, without querying.
- **Unresolved user.** Users are deactivated, never deleted, so every `UserId` and `TrainerId` resolves. If one does
  not, the use case throws an internal error instead of returning a half-filled item; confirm at proposal time that no
  hard-delete path exists.
- `[ProducesResponseType]` on `GET /workouts` changes to `IReadOnlyList<WorkoutListItemResponse>`; `401` and `403`
  stay.
- Tests to add or update:
  - Unit `GetAllWorkoutsUseCase`: users are read once with exactly the referenced owner and trainer ids (deduplicated,
    no `null`); a workout without trainer adds no trainer id; an empty list reads no users (or an empty id set);
    scoping per role is unchanged; an unknown role still throws `UnauthorizedOperationException` before any read.
  - Integration `GET /workouts`:
    - as the seeded `ADMIN`, a workout created with a trainer returns `athlete` and `trainer` with exactly `id`,
      `email`, `name`, `surname`, `role`, matching `userId` / `trainerId`;
    - a workout without trainer returns `trainer: null`;
    - as a `TRAINER`, a workout for an athlete whose relationship was ended still returns that athlete;
    - a deactivated athlete is still returned;
    - existing scoping, `401` and `403` tests stay green.
  - Response shape test: the list item has `athlete` and `trainer` and no sensitive field; the `POST /workouts`
    response is unchanged.
  - OpenAPI document test: the `GET /workouts` item schema is `WorkoutListItemResponse` with a required `athlete` and a
    nullable `trainer`, both referencing the user profile schema; `POST /workouts` still references `WorkoutResponse`.
- OpenAPI snapshot changes (the `GET /workouts` item schema); the front refreshes it in its own plan.
- No deviations from the mapped template references are allowed.
