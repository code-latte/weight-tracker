## Change

Model mesocycles (training plans from a trainer to an athlete) in Domain, persistence and Infra, and let workouts
optionally point to a mesocycle week

## Objective

Today a trainer can prescribe isolated workouts, but there is no way to describe the plan they belong to: the block
of weeks, what each week is for, and the overall objective. This change introduces the **Mesocycles** domain area:

- **Mesocycle**: a plan from a trainer to an athlete, with a name, an objective, a start date and a duration in weeks.
- **MesocycleWeek**: one week (microcycle) inside the mesocycle, with its training type and objective.

Workouts gain an optional reference to a mesocycle and a week number, so a workout can be placed inside a plan
while workouts outside any plan keep working as today.

This change covers only Domain, migrations and Infra (EF configuration, repositories, mappers, seed). No use case,
endpoint or DTO is added or changed.

## Capabilities

- Build a `Mesocycle` aggregate for a trainer and an athlete, and change its name, objective, start date and duration.
- Define, redefine and remove the weeks of a mesocycle, each with a `TrainingWeekType` and an objective.
- Link a planned `Workout` to a mesocycle week (`MesocycleId` + `MesocycleWeekNumber`), move it to another week or
  mesocycle, and unlink it. Started or finished workouts keep their link.
- Persist and load the `Mesocycle` aggregate through `IMesocycleRepository`, and persist the new workout columns
  through the existing `WorkoutRepository`.
- Keep the schema ready for future read views that join workouts and mesocycles in EF, without any domain coupling.

## Module Classification

Domain areas, not modules (the architecture is layer-first; each area is a slice across the layer projects).

- Mesocycles (domain) -> mutable aggregate root with an inner entity (`MesocycleWeek`) and value objects ->
  `.stpr/template/src/WeightTracker.Domain/Workouts` (`Workout` + `WorkoutExercise`)
- Mesocycles (infra) -> new repository, mapper, EF entities over two tables ->
  `src/WeightTracker.Infra/Repositories/WorkoutRepository.cs`, `src/WeightTracker.Infra/Mappers/WorkoutMapper.cs`
- Mesocycles (application contract only) -> `IMesocycleRepository` in `src/WeightTracker.Application/Interfaces`,
  required by `STPR_INVARIANTS.md` section 4. No use case.
- Workouts (domain + infra) -> existing mutable aggregate, two new optional properties ->
  `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs`, `WorkoutMapper`, `WorkoutRepository`

Mesocycles are not a catalog. They hold business state that changes through use (weeks redefined, duration
changed, dates moved). The fact that no endpoint exposes them yet says nothing about their mutability.

## Canonical Template Mapping

- `Mesocycle` aggregate -> `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs` -> `Create` / `Rehydrate` /
  `IsNew`, private constructor, inner entity list exposed as a copied snapshot, action methods that guard semantics
- `MesocycleWeek` entity -> `src/WeightTracker.Domain/Workouts/WorkoutExercise.cs` -> nearest inner entity loaded and
  saved only through its root
- `MesocycleName`, `MesocycleObjective`, `DurationInWeeks` value objects -> `src/WeightTracker.Domain/Workouts/WorkoutName.cs`,
  `WorkoutDescription.cs` -> `record`, `From`, `MaxLength` constant aligned with the column length
- `WeekNumber` value object -> `.stpr/template/src/WeightTracker.Domain/Common` (`Id`, `Email`) -> shared value object
  used by two domain areas without either depending on the other
- `TrainingWeekType` enum -> `src/WeightTracker.Domain/Workouts/WorkoutStatus.cs` -> nearest persisted enum (int column, starts at 1)
- Repository and mapper -> `src/WeightTracker.Infra/Repositories/WorkoutRepository.cs` + `src/WeightTracker.Infra/Mappers/WorkoutMapper.cs`
  -> multi-table aggregate, `Save` branching on `IsNew`, inner rows replaced on update, mapping inside the repository
- EF configuration -> `src/WeightTracker.Infra/Persistence/WeightTrackerDbContext.cs` -> `.ValueGeneratedNever()`, lengths
  mirroring migrations, cross-aggregate FKs as `HasOne<T>().WithMany()` with no navigation
- Migrations -> `src/WeightTracker.Migrations/Migrations/202610080931_CreateWorkoutsTable.cs`,
  `202610080938_CreateWorkoutExercisesTable.cs` (create), `202610271847_AddNameAndDescriptionToWorkouts.cs` (alter) ->
  naming, FK, index and a reversing `Down()`
- Seed -> `src/WeightTracker.Infra/Seed/DatabaseSeeder.cs` + `SeedData.cs` -> deterministic ids, aggregates built through the domain
- Aggregate unit tests -> `.stpr/template/test/WeightTracker.Unit/Domain/Workouts/WorkoutTests` -> Arrange/Act/Assert,
  invariants, factory and rehydrate
- Mapper unit tests -> existing `test/WeightTracker.Unit/Infra/Mappers` workout mapper tests -> round-trip

## State And Mutability

- New table `Mesocycles`: `Id` (PK), `TrainerId` (FK `Users`), `AthleteId` (FK `Users`), `Name`, `Objective`
  (long text, not null), `StartDate` (date), `DurationInWeeks` (int).
- New table `MesocycleWeeks`: `Id` (PK), `MesocycleId` (FK `Mesocycles`, cascade delete), `WeekNumber` (int),
  `TrainingWeekType` (int), `Objective` (long text, not null). Unique index on `(MesocycleId, WeekNumber)`.
- `Workouts` gains `MesocycleId` (nullable, FK `Mesocycles`, no cascade) and `MesocycleWeekNumber` (nullable int),
  with a check constraint so both are null or both are set, and an index on `(MesocycleId, MesocycleWeekNumber)` for
  future read views. Existing rows stay with both null; no backfill.
- `TrainingWeekType`: `ADAPTATION`, `ACCUMULATION`, `INTENSIFICATION`, `REALIZATION`, `DELOAD`, `RECOVERY`, persisted as
  int starting at 1.
- `Users` is only referenced by FK; it is not modified.
- Three FluentMigrator migrations: create `Mesocycles`, create `MesocycleWeeks`, alter `Workouts`. Each `Down()`
  reverses it.

## Business Rules

### Mesocycle

- `TrainerId` and `AthleteId` are required and must differ (domain error, 409 once mapped).
- `Name` is required, trimmed, with a maximum length (same limit as `WorkoutName` unless the proposal says otherwise).
- `Objective` is required: trimmed, non-empty, at most 4000 characters (`MesocycleObjective`). It is a core part of
  the plan, not optional metadata.
- `StartDate` is a date (no time part), as `Workout.WorkoutDate` is.
- `DurationInWeeks` is an integer between 1 and 52 (`DurationInWeeks` value object).
- Weeks are identified by `WeekNumber` inside the mesocycle: unique, and always within `1..DurationInWeeks`.
- Weeks may be defined sparsely while the plan is being built: a mesocycle can exist with no weeks or only some of
  them defined. The aggregate never invents weeks.
- Every week has a required `TrainingWeekType` and a required `Objective` (`MesocycleObjective`, same rules as the
  mesocycle objective).
- `DefineWeek(weekNumber, trainingWeekType, objective)` adds the week or replaces the definition of an existing one.
  A week number outside `1..DurationInWeeks` is a domain error.
- `RemoveWeek(weekNumber)` removes a defined week; removing one that does not exist is a domain error.
- `ChangeDuration(duration)` refuses a duration shorter than the highest defined week number. It does not drop
  weeks silently.
- `Rename`, `ChangeObjective` and `Reschedule(startDate)` validate their input before mutating state.
- `Weeks` is exposed ordered by `WeekNumber`, as a copied snapshot.

### Workout link

- `Workout` holds `MesocycleId` (`Id?`) and `MesocycleWeekNumber` (`WeekNumber?`) as real domain properties, persisted
  and round-tripped by the mapper. They are a reference by id only: `Workout` holds no `Mesocycle` reference and does
  not depend on the Mesocycles domain area, and `Mesocycle` holds no collection of workouts. Both stay independent
  aggregate roots.
- `Workout` knows only the week number, never the week's `TrainingWeekType` or objective.
- Both are set together or both are null; the aggregate never holds only one of them. A standalone workout (both
  null) is fully valid.
- `LinkToMesocycle(mesocycleId, weekNumber)` sets both, and also moves an already linked workout to another week or
  mesocycle. `UnlinkFromMesocycle()` clears both.
- Only planned workouts (`DRAFT`, `PUBLISHED`) can be linked, moved or unlinked. On a `STARTED` or `FINISHED`
  workout (`IsLocked`) both methods are refused with a domain error (409), so the planning context and execution
  traceability are preserved. Retroactive corrections, if ever needed, will be an explicit separate operation (out
  of scope).
- `Workout.Create` keeps its current signature, so no application caller changes. `Rehydrate` gains the two values.
- Rules that need both aggregates span aggregates, so they belong in the Application use cases that will link
  workouts, deferred to a later change: the mesocycle exists, it belongs to the same athlete as the workout, and the
  week number is within the mesocycle's `DurationInWeeks`. The DB FK on `MesocycleId` is the only guarantee in this
  change.
- Read operations may later combine both aggregates in queries returning screen-tailored DTOs. That must never add a
  cross-aggregate reference in the domain.

## Roles And Visibility

No endpoint or use case is added, so nothing is enforced in this change. The rules below are recorded so the
Application change implements them, and the domain must not contradict them:

- A mesocycle always goes from a `TRAINER` to a `USER` (athlete). A `USER` cannot create a mesocycle for themselves.
- A `TRAINER` creates mesocycles where they are the trainer.
- An `ADMIN` can create a mesocycle and assign any trainer and athlete.
- Role checks and the trainer–athlete relationship check need other aggregates, so they are Application rules.
  The domain only enforces `TrainerId != AthleteId` and does not depend on the `User` aggregate.

## Business Identifiers And Mutability

- `Mesocycle.Id` is the technical identifier, generated by `Id.New()` in `Create`, immutable.
- `MesocycleWeek.Id` is the technical identifier of the row, generated by `Id.New()`, immutable. It is internal to
  the aggregate and never referenced from outside.
- `(Mesocycle.Id, MesocycleWeek.WeekNumber)` is the unique business identifier of a week and what workouts point to.
  `WeekNumber` is immutable for a given week: redefining a week changes its type and objective, never its number.
- `Mesocycle.TrainerId` and `Mesocycle.AthleteId` reference `User` by `Id` and are immutable in this change.
  Reassigning a mesocycle to another trainer or athlete needs its own change.
- `Workout.MesocycleId` / `Workout.MesocycleWeekNumber` are mutable only through `LinkToMesocycle` and
  `UnlinkFromMesocycle`.

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## Out Of Scope

- Use cases, application services, endpoints, DTOs and OpenAPI changes, for both mesocycles and the workout link.
- Role and ownership checks (Application rules listed above), and checking the trainer–athlete relationship.
- Cross-aggregate validation of a workout link (mesocycle exists, same athlete, week within duration).
- An explicit operation to correct the mesocycle link of a started or finished workout.
- A `TrainingPlan` or macrocycle concept, a `Microcycle` aggregate, or any other abstraction beyond `Mesocycle` and
  `MesocycleWeek`.
- Read views joining workouts and mesocycles. This change only keeps them possible (FK without navigation, index).
- What happens to linked workouts when a week is removed or the duration shortened, and deleting a mesocycle.
- A mesocycle status or lifecycle (draft, active, finished).
- Domain events (dormant per `STPR_INVARIANTS.md`).
- Integration tests over HTTP, since no route is added.
- Any front change.

## Notes

- Domain: `src/WeightTracker.Domain/Mesocycles/` (`Mesocycle`, `MesocycleWeek`, `TrainingWeekType`, `MesocycleName`,
  `MesocycleObjective`, `DurationInWeeks`) and `src/WeightTracker.Domain/Common/WeekNumber.cs`.
- `WeekNumber` lives in `Common` on purpose: it is the only type both areas share, so `Workouts` never imports
  `Mesocycles` and the "no cross references at domain level" requirement holds. A `WeekNumber` is `>= 1`; the upper
  bound depends on a mesocycle and is checked by the aggregate that knows it.
- Confirmed: the two workout values are domain properties (not DB-only), referencing the mesocycle by `Id` only, as
  the invariant "aggregates reference other aggregates by `Id` only" requires.
- Enum members follow the existing UPPERCASE convention (`WorkoutStatus.DRAFT`): `ADAPTATION`, not `Adaptation`.
- The mesocycle link reuses the workout's existing `IsLocked`; no new status logic. Who may link a workout (edit rule
  vs. another rule) is decided by the future use case change.
- `Mesocycle.Objective` and `MesocycleWeek.Objective` share `MesocycleObjective` (required, max 4000).
- `StartDate` is not forced to be a Monday.
- New domain exceptions (expected, names final at proposal time), each needing an explicit `ExceptionMapper` entry
  even with no route yet: `InvalidMesocycleNameException`, `InvalidMesocycleObjectiveException`,
  `InvalidDurationInWeeksException`, `InvalidWeekNumberException` (400), `MesocycleTrainerIsAthleteException`,
  `MesocycleWeekOutOfRangeException`, `MesocycleWeekNotFoundException`, `MesocycleDurationTooShortException`,
  `LockedWorkoutMesocycleLinkException` (409),
  `IncompleteMesocycleLinkException` if `Rehydrate` receives only one of the two workout values.
- `IMesocycleRepository` in `Application/Interfaces` with `GetById` and `Save` only. Further queries (by athlete, by
  trainer) come with the use cases that need them. Register `MesocycleRepository` in `Program.cs`.
- EF: `MesocycleEntity` and `MesocycleWeekEntity`; `WorkoutEntity` gains the two columns.
  `HasOne<UserEntity>().WithMany()` for trainer and athlete, `HasOne<MesocycleEntity>().WithMany()` from
  `WorkoutEntity`, no navigation crossing aggregate roots, so future read views can join with LINQ `join`.
- `WorkoutMapper` maps the two new columns both ways; `WorkoutRepository` needs no new query.
- Add `Mesocycles` and `MesocycleWeeks` to the integration test truncate list (before `Users`, after `Workouts`).
- Seed: one mesocycle from a seeded trainer to a seeded athlete with some weeks defined (leaving at least one gap),
  and one seeded workout linked to one of its weeks, so the future Application change starts from a useful baseline.
- Tests to add:
  - Unit, domain: `Mesocycle` create/rehydrate, every value object boundary, trainer equals athlete, define/redefine/
    remove weeks, week out of range, duplicate numbers on rehydrate, shortening below a defined week, ordering of
    `Weeks`, snapshot not mutable from outside. `Workout` link, move between weeks and mesocycles, unlink, refused on `STARTED` and `FINISHED`, and rehydrate
    with and without a link. Blank and too long objectives on mesocycle and week.
    `WeekNumber` boundaries.
  - Unit, infra: `MesocycleMapper` round-trip (with no weeks, sparse weeks, all weeks); `WorkoutMapper` round-trip
    with and without a link.
  - Full `dotnet test WeightTracker.slnx` with Docker, so the migrations, check constraint and seed run against Postgres.
- `.stpr/template/src` parity: `Workout.cs` and `WorkoutTests` are template copies, so the template files must be
  updated together with `src` and `test` to stay byte-identical.
- No deviations from the mapped template references are allowed.
