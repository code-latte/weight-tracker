# Design

## Context

See `proposal.md` (Why, What Changes) and the four delta specs for the required behaviour.

Current state that shapes the approach:

- `Mesocycle` exposes `Rename`, `ChangeObjective`, `Reschedule`, `ChangeDuration`, `DefineWeek` (upsert) and
  `RemoveWeek`. `IMesocycleRepository` has only `GetById` and `Save`.
- `Workout.LinkToMesocycle` / `UnlinkFromMesocycle` throw `WorkoutPlacementLockedException` (409) when `IsLocked`,
  whoever the caller is. `Workout.Create` creates standalone workouts.
- `CreateWorkoutUseCase` resolves owner and trainer from the caller's role, then checks them through
  `WorkoutAssignmentValidator`. `UpdateWorkoutUseCase` checks reassignment (admin only), then
  `WorkoutEditAuthorizer`, then applies `Reassign` and `UpdateDetails`.
- `GET /workouts/{id}` answers `403` (`UnauthorizedOperationException`) for a workout outside the caller's scope.
- `User` already exposes `CanBeTrainer()` (role `TRAINER` and active) and `CanBeAthlete()` (role `USER` and active).
- `TrainerAthleteViewLoader` joins relationships with their users in one `GetAllByIds` call; `WorkoutListItemResponse`
  returns people as `UserProfileResponse`.
- `RequireRole` takes several roles. `POST /workouts` returns `WorkoutResponse`, `PUT /workouts/{id}` returns
  `WorkoutDetailResponse`.
- Seed: one trainer (`TrainerId`), one user (`UserId`), one admin, an active and an ended relationship between trainer
  and user, and one mesocycle from trainer to user with weeks 1, 2 and 4; `TrainedWorkout` (`PUBLISHED`) in week 1 and
  `PrescribedStartedWorkout` (`STARTED`) in week 2.

## Goals / Non-Goals

**Goals:**

- Every permission and cross-aggregate rule lives in one Application service, reused by every use case it applies to.
- Every mesocycle endpoint returns the same `MesocycleResponse`.
- Existing workout behaviour without placement is unchanged.

**Non-Goals:**

- No migration and no new table. The only Infra change is new read queries.
- No change to `Workout` in Domain, so the `.stpr/template` copies of `Workout.cs` and `WorkoutTests.cs` stay as they are.

## Decisions

### Layer mapping

| Layer | Change |
| --- | --- |
| Domain | `Mesocycle.AddWeek`, `Mesocycle.RedefineWeek`; `DefineWeek` removed. |
| Application | `UseCases/Mesocycles/`: `GetMesocycleByIdUseCase`, `GetAllMesocyclesUseCase`, `GetUserMesocyclesUseCase`, `CreateMesocycleUseCase`, `UpdateMesocycleUseCase`, `DefineMesocycleWeekUseCase`, `RedefineMesocycleWeekUseCase`, `MesocycleView`. `Services/`: `MesocycleViewLoader`, `MesocycleVisibility`, `MesocycleEditAuthorizer`, `MesocycleAssignmentValidator`, `MesocyclePlacementValidator`. `IMesocycleRepository` list queries. `CreateWorkoutUseCase`, `UpdateWorkoutUseCase` extended. New exceptions. |
| Infrastructure | `MesocycleRepository`: `GetAll`, `GetAllByTrainerId`, `GetAllByAthleteId`, `GetAllByTrainerIdAndAthleteId`. |
| Api | `Endpoints/Mesocycles/MesocyclesController.cs` and `Dtos/`; `GET /users/{userId}/mesocycles` on `UsersController`; workout request and response DTOs; `ExceptionMapper`; `Program.cs`. |

Cross-area dependencies (Application only): Mesocycles → Users (`IUserRepository`), Mesocycles → TrainerAthletes
(`ITrainerAthleteRepository`), Workouts → Mesocycles (`IMesocycleRepository` through `MesocyclePlacementValidator`).
In Api, `UsersController` → `GetUserMesocyclesUseCase`, and `MesocycleResponse` reuses `UserProfileResponse`. Domain
gains no cross-area reference.

### Week actions in the aggregate

`AddWeek(weekNumber, type, objective)` throws `MesocycleWeekOutOfRangeException` (409) above the duration and
`DuplicateMesocycleWeekException` (409) for an existing number. `RedefineWeek(weekNumber, type, objective)` throws
`MesocycleWeekNotFoundException` (404) for a missing number. `DefineWeek` is removed, and its callers (unit tests,
mapper tests, repository tests) move to `AddWeek`/`RedefineWeek`.

*Alternative considered:* keep `DefineWeek` and check existence in the use cases. Rejected: "a new week must not exist"
is a rule about the aggregate's own state, so the aggregate guards it, and an upsert left next to the two guarded
methods would let a future caller bypass them.

### Week path key

`{weekNumber:int}`, the business identifier, not the internal row id. `MesocycleWeek.Id` is never serialized.

### Read side

- `MesocycleView(Mesocycle Mesocycle, User Trainer, User Athlete)` and `MesocycleViewLoader.Load(one | many)`, built
  like `TrainerAthleteViewLoader`: one `GetAllByIds` for every referenced user. A missing user is a data-integrity
  failure (`InvalidOperationException` → 500), as `GetAllWorkoutsUseCase` treats it, since foreign keys guarantee them.
- `MesocycleVisibility.VisibleTo(callerId, role)`: `ADMIN` → `GetAll`, `TRAINER` → `GetAllByTrainerId`, `USER` →
  `GetAllByAthleteId`. `CanSee(mesocycle, callerId, role)` is the single-item rule used by `GetMesocycleByIdUseCase`
  (`ADMIN`, trainer or athlete). Both live in one service so the list and the item cannot drift.
- `GetUserMesocyclesUseCase`: `USER` → `UnauthorizedOperationException` (also enforced by
  `[RequireRole(TRAINER, ADMIN)]`, kept in the use case so the rule is unit-tested). Then `UserNotFoundException` if
  the user does not exist, then `ADMIN` → `GetAllByAthleteId(userId)`, `TRAINER` →
  `GetAllByTrainerIdAndAthleteId(callerId, userId)`.
- Repository lists include weeks and are ordered by `StartDate` descending, then `Id`, for a deterministic order.
- Every use case returns `MesocycleView` (or a list of it); the controller maps it with `MesocycleResponse.From(view)`.

### Write side

- `MesocycleAssignmentValidator` (mirrors `WorkoutAssignmentValidator`):
  - `EnsureAthlete(athleteId)`: `UserNotFoundException` (404) when missing; `InvalidMesocycleAssignmentException` (400)
    when `CanBeAthlete()` is false.
  - `EnsureTrainer(trainerId)`: `InvalidMesocycleAssignmentException` (400) when missing or `CanBeTrainer()` is false.
    That refuses admins as trainers, as decided.
- `CreateMesocycleUseCase`:
  - Resolves from the role first: `USER` → 403; `TRAINER` with a `trainerId` → 400, otherwise the trainer is the caller;
    `ADMIN` without `trainerId` → 400.
  - `ADMIN`: `EnsureAthlete`, then `EnsureTrainer`.
  - `TRAINER`: `EnsureAthlete`, then `ITrainerAthleteRepository.GetActiveByTrainerIdAndUserId`; none → 403.
  - `Mesocycle.Create`, `Save`, `SaveChangesAsync`, return the view.
- `MesocycleEditAuthorizer.EnsureCanEdit(mesocycle, callerId, role, operation)`: `ADMIN`, or `TRAINER` with
  `mesocycle.TrainerId == callerId`; otherwise 403. There's no relationship re-check, as decided.
- `UpdateMesocycleUseCase`, `DefineMesocycleWeekUseCase`, `RedefineMesocycleWeekUseCase`: `GetById` → 404, then
  authorizer, then the aggregate actions, `Save`, `SaveChangesAsync`, return the view. The update calls `Rename`,
  `ChangeObjective`, `Reschedule`, `ChangeDuration`.

### Workout placement

- Controllers convert the pair into `Id?` and `WeekNumber?` (`WeekNumber.From` → 400 below 1) and pass both to the
  use cases. The use cases throw `IncompleteMesocyclePlacementException` (Application, 400) when only one is set. The
  domain `IncompleteWorkoutPlacementException` (409) stays for stored data only.
- `MesocyclePlacementValidator.EnsureCanPlace(workout owner, workout trainer, mesocycleId, weekNumber, callerId,
  role, operation)`:
  1. `IMesocycleRepository.GetById`, or `MesocycleNotFoundException` (404).
  2. `MesocycleEditAuthorizer.EnsureCanEdit` (403). That reuses the edit rule, so "may place" means "may edit the
     mesocycle".
  3. Owner ≠ athlete → `MesocycleAthleteMismatchException` (409).
  4. Workout trainer not null and ≠ mesocycle trainer → `MesocycleTrainerMismatchException` (409).
  5. Week above duration → the existing `MesocycleWeekOutOfRangeException` (409).
- `CreateWorkoutUseCase`: after the existing assignment checks and before `Workout.Create`, run the validator when a
  pair is sent, then call `workout.LinkToMesocycle` before `Save`. A new workout is a `DRAFT`, so the lock never
  applies.
- `UpdateWorkoutUseCase`: keep the existing order (reassignment 403, edit authorizer, assignment checks). Then:
  - The placement is **changed** when the sent pair differs from the current one.
  - The validator runs when the resulting pair is non-null and either the placement changed or the owner or trainer
    changed, so a reassignment that breaks a placement is refused.
  - Only when the placement changed: `LinkToMesocycle` or `UnlinkFromMesocycle`. The domain then refuses it on a
    locked workout (409), admins included. Sending the current pair back on a locked workout touches nothing.
  - Unlinking needs only the workout edit rule, so a pair of nulls skips the validator.
  - Then `Reassign` / `UpdateDetails` as today, then a single `SaveChangesAsync`.
- `WorkoutResponse` and `WorkoutDetailResponse` gain `Guid? MesocycleId` and `int? MesocycleWeekNumber`.
  `WorkoutListItemResponse` does not change.

*Alternative considered:* validating both-or-neither in the request DTO (`IValidatableObject`). Rejected: an
Application exception keeps the rule unit-tested next to the use cases and gives a consistent `ProblemDetails`. It's
still a 400, so it stays a request-shape error.

*Alternative considered:* a separate `PUT /workouts/{id}/placement`. Rejected: the brief asks to extend `POST` and
`PUT /workouts`.

### Exceptions

| Exception | Layer | Status |
| --- | --- | --- |
| `MesocycleNotFoundException` | Application | 404 |
| `InvalidMesocycleAssignmentException` | Application | 400 |
| `IncompleteMesocyclePlacementException` | Application | 400 |
| `MesocycleAthleteMismatchException` | Application | 409 |
| `MesocycleTrainerMismatchException` | Application | 409 |

They're reused: `UnauthorizedOperationException` (403), `UserNotFoundException` (404), and the existing mesocycle domain
exceptions. Each new one gets an explicit `ExceptionMapper` entry.

### API

- `MesocyclesController : AuthorizedController`, `[Route("mesocycles")]`.
  - `GET {id:guid}` and `GET` take any authenticated role.
  - Writes use `[RequireRole(Role.TRAINER, Role.ADMIN)]`, and the use cases still enforce the same rule.
  - `POST` returns `201` with `Location` at `GetById`. `POST {id}/weeks` returns `201` with the same `Location`.
  - `PUT {id}` and `PUT {id}/weeks/{weekNumber:int}` return `200`.
- `UsersController`: `GET {userId:guid}/mesocycles` with `[RequireRole(Role.TRAINER, Role.ADMIN)]`.
- DTOs:
  - `CreateMesocycleRequest` (`TrainerId?`, `[Required] AthleteId`, `Name`, `Objective`, `StartDate` as `DateOnly`,
    `DurationInWeeks`).
  - `UpdateMesocycleRequest` (`Name`, `Objective`, `StartDate`, `DurationInWeeks`, all required).
  - `DefineMesocycleWeekRequest` (`WeekNumber`, `TrainingWeekType`, `Objective`).
  - `RedefineMesocycleWeekRequest` (`TrainingWeekType`, `Objective`).
  - `MesocycleResponse(Id, TrainerId, AthleteId, Name, Objective, StartDate, DurationInWeeks, Weeks, Trainer, Athlete)`,
    with `Trainer` and `Athlete` as `UserProfileResponse`.
  - `MesocycleWeekResponse(WeekNumber, TrainingWeekType, Objective)`.
  - The training type is bound with `JsonStringEnumConverter<TrainingWeekType>`, like `ChangeWorkoutStatusRequest`
    binds `WorkoutStatus`. An unknown name is rejected by model binding with `400`.
- Every action declares `[ProducesResponseType]` for its DTO and `ProblemDetails` failures.

### Tests

- **Unit, Application:** one test class per use case and per new service under
  `test/WeightTracker.Unit/Application/Mesocycles`, covering every role branch, the order of checks (404 before 403,
  403 before 409) and that nothing is saved on failure. The workout use case tests gain the placement cases, including
  unchanged placement on a locked workout and reassignment that breaks a placement.
- **Unit, Domain:** `MesocycleTests` replaces the `DefineWeek` cases with `AddWeek` and `RedefineWeek` cases.
- **Integration:** under `test/WeightTracker.Integration/Mesocycles`, one file per route family, shaped like
  `WorkoutEndpointTest` and `WorkoutScopingTests`. The per-route `401` matrix, plus `403`, `404`, `400` and `409`.
  - Exclusion fixtures are built **through the API**: the admin creates a second trainer and a second athlete with
    `POST /users`, then a mesocycle with `POST /mesocycles` (no relationship needed). The shared seed stays unchanged.
  - Placement cases go in `test/WeightTracker.Integration/Workouts/WorkoutMesocyclePlacementEndpointTests.cs`.
  - `WorkoutResponseShapeTests` gains the two fields.
- **Verification:** the full `dotnet test WeightTracker.slnx` with Docker, plus the template parity check.

## Risks / Trade-offs

- **Existing clients unlink by accident.** `PUT /workouts/{id}` now clears an omitted placement, so a client that
  doesn't know the new fields would unlink a placed workout. Mitigation: the fields are returned by every workout read,
  and only seed data is placed today.
- **Placement isn't re-validated when a mesocycle changes.** Shrinking a mesocycle's duration below a linked workout's
  week is still accepted, so the workout can end up placed outside its mesocycle. This is out of scope and recorded in
  the proposal.
- **Shared seed.** Tests that change the seeded mesocycle could affect each other. Mitigation: every integration test
  starts from the truncate-and-reseed baseline.
- **Duplicate role check.** `[RequireRole]` and the use cases both check roles. That's deliberate: the attribute gives
  the early `403`, and the use case keeps the rule testable without HTTP.

## Migration Plan

There's no schema change, so deploying is just the code. Rolling back is reverting the change: placement values
written through the API stay valid for the previous version, which never reads them over HTTP.
