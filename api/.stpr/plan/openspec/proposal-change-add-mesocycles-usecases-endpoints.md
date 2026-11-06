## Change

Add the mesocycle use cases and `/mesocycles` endpoints, and let `POST /workouts` and `PUT /workouts/{id}` place a
workout in a mesocycle week.

## Objective

The previous change (`add-mesocycles`) built the `Mesocycle` aggregate, its weeks, the optional placement of a workout
and the persistence behind them. None of it can be reached over HTTP yet. This change adds the Application use cases
and API endpoints so trainers and admins can create and maintain mesocycles. Athletes, trainers and admins can read the
mesocycles they are entitled to, and workouts can be created in, moved to or taken out of a mesocycle week. It also adds
the checks across aggregates that the previous change left for Application: the mesocycle exists, it is for the same
athlete, and the week is within its duration.

## Capabilities

- Read one mesocycle with its weeks: `GET /mesocycles/{id}`.
- List the caller's mesocycles: `GET /mesocycles`, scoped by role.
- List the mesocycles of one athlete: `GET /users/{userId}/mesocycles`, for trainers and admins.
- Create a mesocycle: `POST /mesocycles`. A trainer creates one for an athlete they have an active relationship with;
  an admin creates one for any trainer and athlete.
- Update a mesocycle's name, objective, start date and duration: `PUT /mesocycles/{id}`.
- Define a new week: `POST /mesocycles/{id}/weeks`.
- Redefine an existing week's training type and objective: `PUT /mesocycles/{id}/weeks/{weekNumber}`.
- Create or update a workout with an optional `mesocycleId` + `mesocycleWeekNumber` pair (both or neither), and expose
  the pair in `WorkoutResponse`.

There is no `GET /mesocycles/{id}/weeks`: weeks come back inside the mesocycle response.

## Module Classification

Domain areas, not modules (the architecture is layer-first).

- Mesocycles (application + api) -> mutable aggregate, extended from domain/infra to use cases, controller and DTOs ->
  `.stpr/template/src/WeightTracker.Application/UseCases/Workouts`, `src/WeightTracker.Api/Endpoints/Workouts/WorkoutsController.cs`
- Mesocycles (infra) -> existing repository, gains list queries -> `src/WeightTracker.Infra/Repositories/WorkoutRepository.cs`
- Workouts (application + api) -> existing mutable aggregate; create and update use cases, request DTOs and
  `WorkoutResponse` change -> `CreateWorkoutUseCase`, `UpdateWorkoutUseCase`, `WorkoutsController`
- TrainerAthletes -> auth/authorization support, read only, used to check a trainer's relationship with an athlete ->
  `ITrainerAthleteRepository.GetActiveByTrainerIdAndUserId`

Mesocycles are not a catalog. Their state changes through use: they are renamed and rescheduled, their duration
changes, and weeks are defined and redefined.

## Canonical Template Mapping

- `GetMesocycleByIdUseCase` -> `GetWorkoutByIdUseCase` -> nearest read of one aggregate with a visibility check
- `GetAllMesocyclesUseCase` + `MesocycleVisibility` -> `GetAllWorkoutsUseCase` + `WorkoutVisibility` -> read scoped by
  role, with the rule kept in one service
- `GetUserMesocyclesUseCase` -> `GetAllWorkoutsUseCase` / `WorkoutVisibility` -> same scoped-read pattern, filtered by athlete
- `CreateMesocycleUseCase` -> `CreateWorkoutUseCase` + `WorkoutAssignmentValidator` -> resolve trainer and athlete from
  the caller's role, then check the users and the relationship
- `UpdateMesocycleUseCase` -> `UpdateWorkoutUseCase` -> load the aggregate, authorize, call its action methods, save
- `DefineMesocycleWeekUseCase`, `RedefineMesocycleWeekUseCase` -> `AddWorkoutExerciseUseCase`,
  `UpdateWorkoutExerciseUseCase` -> act on an inner entity through its root
- `MesocycleEditAuthorizer` -> `WorkoutEditAuthorizer` -> one place for the edit rule
- `MesocyclePlacementValidator` -> `WorkoutAssignmentValidator` -> shared check used by workout create and update
- `MesocyclesController`, DTOs, response mapping -> `WorkoutsController` and `Endpoints/Workouts/Dtos` ->
  `AuthorizedController` base, `RequireRole`, `ExceptionMapper.Map`
- Integration tests -> existing `WorkoutsController` integration tests -> auth matrix per route

## State And Mutability

- Creating, updating and defining weeks change the `Mesocycle` aggregate.
- Creating or updating a workout can set, move or clear its mesocycle placement.
- The `GET` routes only read. Nothing is deleted in this change: no mesocycle deletion and no week removal through the API.

## Business Rules

- **Create:**
  - A trainer creates mesocycles with themselves as trainer, for an athlete they have an **active** `TrainerAthlete`
    relationship with. Without one they get `403`.
  - An admin sends both `trainerId` and `athleteId`. The trainer must exist and be able to train (to confirm whether an
    admin can be the trainer, as `CanTrain` allows for workouts); the athlete must exist and be a regular user. No
    relationship is required.
  - A deactivated trainer or athlete is refused, using the same rule as workout assignment.
  - Trainer = athlete is refused with `409` by the existing domain rule.
- **Update (`PUT /mesocycles/{id}`):** replaces name, objective, start date and duration. `trainerId` and `athleteId`
  are immutable and not in the body. Shrinking below a defined week is a `409` (`MesocycleDurationTooShortException`).
- **Weeks:** a week is identified by `(mesocycleId, weekNumber)`.
  - `POST` defines a week that does not exist yet. If it already exists, `409`.
  - `PUT` redefines an existing week (type and objective, never its number). A missing week is `404`.
  - A week beyond the duration is `409`.
- **Workout placement (POST and PUT `/workouts`):**
  - `mesocycleId` and `mesocycleWeekNumber` are both sent or both omitted. A half-set pair is `400`.
  - The mesocycle must exist, otherwise `404`.
  - The mesocycle's athlete must be the workout's owner, otherwise `409`.
  - The week number must be within the mesocycle's duration, otherwise `409`. The week does not need to be defined,
    since weeks are sparse.
  - The caller must be allowed to edit the mesocycle: its trainer or an admin. A regular user cannot place their own
    workout in a mesocycle (`403`).
  - When the workout has a trainer, it must be the mesocycle's trainer (`409`).
  - On `PUT`, the body replaces the placement: omitting both values unlinks the workout. The domain is called only when
    the placement actually changes, so an unchanged pair on a `STARTED`/`FINISHED` workout does not fail, and a real
    change keeps the domain's `409` (`WorkoutPlacementLockedException`), whoever the caller is.
  - On `POST`, the workout is created and then linked in the same unit of work.

## Roles And Visibility

| Route | USER | TRAINER | ADMIN |
| --- | --- | --- | --- |
| `GET /mesocycles/{id}` | if athlete | if trainer of it | all |
| `GET /mesocycles` | where athlete | where trainer | all |
| `GET /users/{userId}/mesocycles` | `403` | where trainer = caller and athlete = `userId` | all of that athlete |
| `POST /mesocycles` | `403` | own, active relationship | any |
| `PUT /mesocycles/{id}` | `403` | own | all |
| `POST /mesocycles/{id}/weeks` | `403` | own | all |
| `PUT /mesocycles/{id}/weeks/{weekNumber}` | `403` | own | all |

- A mesocycle outside the caller's scope on `GET /mesocycles/{id}` gets the same response `GET /workouts/{id}` gives
  for an invisible workout.
- Trainer ownership is `Mesocycle.TrainerId == caller`. It is not re-checked against an active relationship after
  creation, so a trainer keeps reading and editing their mesocycles after the relationship ends.
- On `GET /users/{userId}/mesocycles`, an unknown `userId` returns `404`. A trainer with no mesocycles for that athlete
  gets an empty list.

## Business Identifiers And Mutability

- `Mesocycle.Id` is generated with `Id.New()` and is immutable.
- `(Mesocycle.Id, WeekNumber)` is the business identifier of a week and the path key of
  `PUT /mesocycles/{id}/weeks/{weekNumber}`. It is immutable.
- `MesocycleWeek.Id` stays internal and is never exposed or accepted.
- `Mesocycle.TrainerId` and `Mesocycle.AthleteId` are immutable. There is no reassignment endpoint.
- `Workout.MesocycleId` and `Workout.MesocycleWeekNumber` are mutable through `POST`/`PUT /workouts` only while the
  workout is `DRAFT`/`PUBLISHED`.

## Out Of Scope

- Deleting mesocycles, `DELETE /mesocycles/{id}/weeks/{weekNumber}` (`RemoveWeek` stays unexposed) and
  `GET /mesocycles/{id}/weeks`.
- Reassigning a mesocycle's trainer or athlete.
- Listing the workouts of a mesocycle or a week, and placement in `WorkoutListItemResponse`.
- Correcting the placement of a started or finished workout.
- What happens to linked workouts when a duration shrinks (the domain refuses shrinking below a defined week, but not
  below a linked workout's week).
- Front changes.

## Notes

- **Repository:** `IMesocycleRepository` gains `GetAll`, `GetAllByTrainerId`, `GetAllByAthleteId` and
  `GetAllByTrainerIdAndAthleteId`, each including weeks and ordered by `StartDate` descending.
- **Domain gap to resolve:** `Mesocycle.DefineWeek` is an upsert today, so it cannot tell `POST` (new only) from `PUT`
  (existing only). The proposal adds two guarded action methods in the aggregate, `AddWeek` (throws
  `DuplicateMesocycleWeekException`, `409`) and `RedefineWeek` (throws `MesocycleWeekNotFoundException`, `404`), and
  decides whether `DefineWeek` stays.
- **Path key:** `{weekNumber}` instead of `{weekId}`, because the week number is the business identifier and the
  week's row id is internal.
- **`MesocycleResponse`:** `id`, `trainerId`, `athleteId`, `name`, `objective`, `startDate` (`yyyy-MM-dd`),
  `durationInWeeks`, and `weeks[]` with `weekNumber`, `trainingWeekType` (UPPER_CASE string) and `objective`. Whether
  trainer and athlete names are included (pattern of `add-names-to-workouts-list` and `TrainerAthleteViewLoader`) is to
  be confirmed.
- **Requests:**
  - `CreateMesocycleRequest`: `trainerId` (admin only), `athleteId`, `name`, `objective`, `startDate`, `durationInWeeks`.
  - `UpdateMesocycleRequest`: `name`, `objective`, `startDate`, `durationInWeeks`.
  - Week requests: `weekNumber` (`POST` only), `trainingWeekType`, `objective`.
  - `CreateWorkoutRequest` and `UpdateWorkoutRequest` gain optional `mesocycleId` and `mesocycleWeekNumber`.
  - Only structural validation in the DTOs; the both-or-neither check lives in the use case or a request validator.
- **Spec impact:** the `workout-mesocycle-placement` requirement "Existing workout HTTP behaviour is unchanged" is
  modified. `WorkoutResponse` gains `mesocycleId` and `mesocycleWeekNumber` (null for standalone workouts).
- **New Application exceptions with explicit `ExceptionMapper` entries:**
  - `MesocycleNotFoundException` (`404`)
  - `MesocycleAthleteMismatchException` (`409`)
  - a workout-trainer-mismatch exception (`409`)
  - `IncompleteMesocyclePlacementException` (`400`), if the both-or-neither check is not purely structural
  - `InvalidMesocycleAssignmentException` (`400`/`409`, mirroring `InvalidWorkoutAssignmentException`)
- **Registration:** new use cases, `MesocycleVisibility`, `MesocycleEditAuthorizer` and `MesocyclePlacementValidator`
  are registered in `Program.cs`.
- **Seed:** the existing seeded mesocycle (`TrainerId` -> `UserId`, weeks 1, 2 and 4, linked `TrainedWorkout` and
  `PrescribedStartedWorkout`) is the baseline. Add a second trainer's mesocycle if needed to assert what scoped reads
  exclude.
- **Tests:**
  - Unit: every use case (role branches, relationship check, visibility, placement checks, unchanged placement on a
    locked workout) and the new aggregate methods.
  - Integration, per route: `401` with no token, `401` with an invalid token, and `403`/`404`/`400`/`409` where they
    apply. Scoped reads assert what is excluded: another trainer's mesocycle, another athlete's.
  - Workouts: placement cases on `POST` and `PUT`, including a half-set pair, an unknown mesocycle, another athlete's
    mesocycle, an out-of-range week, a regular user placing a workout, and moving a started workout.
  - Full `dotnet test WeightTracker.slnx` with Docker.
- **Template parity:** if `Workout.cs`, `CreateWorkoutUseCase` or their tests are template copies, update them in
  `.stpr/template` at the same time.
- No deviations from the mapped template references are allowed.

## Open Decisions

Defaults used unless changed before implementation:

1. `{weekNumber}` instead of `{weekId}` as the week path key.
2. New `AddWeek` / `RedefineWeek` aggregate methods, since `DefineWeek` is an upsert.
3. Only the mesocycle's trainer or an admin can place a workout in it; a regular user cannot.
4. A workout with a trainer must have the mesocycle's trainer to be placed in it.
5. No active-relationship check after creation.
6. An omitted pair on `PUT /workouts/{id}` unlinks the workout.
7. An admin cannot be a mesocycle's trainer: only users with the `TRAINER` role (confirmed).
8. `MesocycleResponse` includes the trainer and athlete public profiles (confirmed).
