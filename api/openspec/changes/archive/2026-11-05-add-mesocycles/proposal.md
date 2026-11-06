# Proposal

## Why

Trainers can prescribe isolated workouts, but there is no way to describe the plan those workouts belong to: the block
of weeks, what each week is for, and the overall objective. This change introduces mesocycles in the domain and
persistence so the later Application and API changes can build planning on top of a stable model, while workouts
outside any plan keep working as today.

Source brief: `.stpr/plan/openspec/proposal-change-add-mesocycles.md`.

## What Changes

- New **Mesocycles** domain area: aggregate root `Mesocycle` (trainer, athlete, name, objective, start date, duration
  in weeks) with inner entity `MesocycleWeek` (week number, `TrainingWeekType`, objective), value objects
  `MesocycleName`, `MesocycleObjective`, `DurationInWeeks`, and enum `TrainingWeekType` (`ADAPTATION`,
  `ACCUMULATION`, `INTENSIFICATION`, `REALIZATION`, `DELOAD`, `RECOVERY`).
- New shared value object `WeekNumber` in `Domain/Common`, the only type the Workouts and Mesocycles areas share.
- `Workout` gains optional domain properties `MesocycleId` and `MesocycleWeekNumber` (set together or both null),
  with `LinkToMesocycle` (also moves) and `UnlinkFromMesocycle`, refused on `STARTED` / `FINISHED` workouts.
  `Workout.Create` keeps its signature; `Rehydrate` gains two optional trailing parameters.
- New tables `Mesocycles` and `MesocycleWeeks`; two nullable columns, a both-or-neither check constraint, an FK and
  an index on `Workouts`. Three FluentMigrator migrations, each reversible.
- Infra: EF entities and configuration, `MesocycleMapper`, `MesocycleRepository` behind `IMesocycleRepository`
  (`GetById`, `Save`), `WorkoutMapper` and `WorkoutRepository` carrying the new columns, seed and truncate list.
- No use case, endpoint, DTO or OpenAPI change. No existing HTTP behaviour changes.

## Capabilities

### New Capabilities

- `mesocycles`: what a mesocycle is and the invariants it protects (trainer differs from athlete, name, required
  objectives, duration 1–52, sparse weeks within the duration, duration cannot shrink below a defined week) and how
  it is persisted.
- `workout-mesocycle-placement`: how a workout optionally points to a mesocycle week (both-or-neither, link, move,
  unlink, refused once the workout is started or finished), and that the two aggregates stay independent.

### Modified Capabilities

None. Existing workout specs describe HTTP behaviour that this change does not alter: the new workout properties are
not exposed by any endpoint yet.

## Module Classification And Template Mapping

Domain areas, not modules (layer-first architecture).

| Capability / area | Classification | Canonical reference | Why |
| --- | --- | --- | --- |
| `Mesocycle` | Mutable aggregate root with inner entity and value objects | `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs` | `Create` / `Rehydrate` / `IsNew`, inner entity snapshot, guarded action methods |
| `MesocycleWeek` | Entity inside `Mesocycle` | `src/WeightTracker.Domain/Workouts/WorkoutExercise.cs` | Nearest inner entity saved only through its root |
| `MesocycleName`, `MesocycleObjective`, `DurationInWeeks` | Value objects | `src/WeightTracker.Domain/Workouts/WorkoutName.cs` | `record`, `From`, `MaxLength` aligned with the column |
| `WeekNumber` | Shared value object | `.stpr/template/src/WeightTracker.Domain/Common` (`Id`, `Email`) | Shared across areas without coupling them |
| `TrainingWeekType` | Persisted enum | `src/WeightTracker.Domain/Workouts/WorkoutStatus.cs` | Int column starting at 1 |
| Workout placement | Existing mutable aggregate, extended | `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs` | Same aggregate; references the mesocycle by `Id` only |
| Repository / mapper | Repository implementation, aggregate mapper | `src/WeightTracker.Infra/Repositories/WorkoutRepository.cs`, `src/WeightTracker.Infra/Mappers/WorkoutMapper.cs` | Two-table aggregate, `IsNew` branching, children reconciled by id |
| Migrations | FluentMigrator | `202610080931_CreateWorkoutsTable.cs`, `202611031642_CreateFavoriteExercisesTable.cs`, `202610301437_CreateTrainerAthletesTable.cs` (`Execute.Sql`) | Naming, FKs, indexes, raw SQL constraint, reversing `Down()` |
| Seed | Seed | `src/WeightTracker.Infra/Seed/DatabaseSeeder.cs` | Deterministic ids, built through the domain |

Mesocycles are mutable business state, not a catalog: weeks are redefined, durations and dates change. Having no
endpoint yet says nothing about mutability.

## Business Identifiers And Mutability

- `Mesocycle.Id`: technical id from `Id.New()`, immutable.
- `MesocycleWeek.Id`: technical row id from `Id.New()`, immutable, internal to the aggregate.
- `(Mesocycle.Id, WeekNumber)`: unique business identifier of a week and what workouts point to. Immutable per week:
  redefining a week changes its type and objective, never its number.
- `Mesocycle.TrainerId`, `Mesocycle.AthleteId`: reference `User` by `Id`, immutable in this change.
- `Workout.MesocycleId`, `Workout.MesocycleWeekNumber`: mutable only through `LinkToMesocycle` /
  `UnlinkFromMesocycle`, and only while the workout is `DRAFT` or `PUBLISHED`.

## Conflicts With Template Or Invariants

None. The request "no cross references between mesocycles and workouts at domain level" agrees with the invariant
"aggregates reference other aggregates by `Id` only": `Workout` holds the mesocycle's `Id` and a `WeekNumber`, no
`Mesocycle` type, and `Mesocycle` holds no workouts. `Workout.cs` and `WorkoutTests.cs` have template copies in
`.stpr/template`, which are updated together so parity holds.

## Acceptance Criteria

- Every invariant in `specs/mesocycles` and `specs/workout-mesocycle-placement` is enforced by the aggregates and
  covered by unit tests with labelled Arrange / Act / Assert.
- Both mappers round-trip, and repository integration tests load what they save, including the workout link.
- Migrations apply and roll back in `MigrationsTests`; the check constraint rejects a half-set workout link.
- The full `dotnet test WeightTracker.slnx` passes with Docker, and template parity reports no drift.

## Out Of Scope

- Use cases, endpoints, DTOs; role checks (trainer → athlete, admin assigns) and the trainer–athlete relationship check.
- Cross-aggregate checks on linking (mesocycle exists, same athlete, week within duration): future Application work.
- Read views joining workouts and mesocycles (only kept possible here).
- An explicit operation to correct the link of a started or finished workout.
- Deleting mesocycles, a mesocycle lifecycle, `TrainingPlan` / macrocycle / `Microcycle` concepts.
- What happens to linked workouts when a week is removed or a duration shrinks.
- Domain events, front changes.

## Impact

- **Domain**: new `Domain/Mesocycles/`, new `Domain/Common/WeekNumber.cs`, `Workout.cs` extended, new domain exceptions.
- **Application**: only the new `IMesocycleRepository` contract in `Application/Interfaces`.
- **Infra**: new entities, EF configuration, mapper and repository; `WorkoutEntity`, `WorkoutMapper`,
  `WorkoutRepository` (update path) extended; seed extended.
- **Migrations**: three new migrations.
- **Api**: `Program.cs` registration and `ExceptionMapper` entries for the new exceptions; no route change.
- **Tests**: new unit tests (domain, mappers), repository integration tests, migration tests, truncate list.
- **Template**: `.stpr/template` copies of `Workout.cs` and `WorkoutTests.cs` updated in step.
