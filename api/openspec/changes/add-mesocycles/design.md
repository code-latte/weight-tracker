# Design

## Context

See `proposal.md` (Why, What Changes) and the two delta specs for the required behaviour.

Current state that shapes the approach:

- `Workout` (`src/WeightTracker.Domain/Workouts/Workout.cs`, copied byte-identical in `.stpr/template`) already
  references `User` by `Id` (`UserId`, `TrainerId`) and exposes `IsLocked` (`STARTED` / `FINISHED`). The lock on
  content edits is enforced in Application by `WorkoutEditAuthorizer` (with an admin bypass) through the
  Application-level `WorkoutLockedException`; the domain itself does not refuse content edits.
- `Workout.Rehydrate` has ~40 callers, almost all in unit tests, plus `WorkoutMapper` and `DatabaseSeeder`.
- `WorkoutRepository.Save` updates by loading the tracked row and copying scalar fields one by one, then reconciling
  children by entity id. A new scalar that is not copied there is silently lost on update.
- `ExceptionMapper` maps any unlisted `DomainException` to `400`; `409` and `404` need explicit entries.
- Repository behaviour is tested in `test/WeightTracker.Integration/Repositories`, migrations in
  `test/WeightTracker.Integration/Migrations/MigrationsTests.cs`.

## Goals / Non-Goals

**Goals:**

- Every rule in the specs lives in an aggregate or value object; nothing in Application.
- `Workout` and `Mesocycle` compile without referencing each other's namespace.
- The schema supports future EF joins between `Workouts` and `Mesocycles` without navigation properties.
- No change in any HTTP response or existing test expectation.

**Non-Goals:**

- No repository query beyond `GetById` / `Save` for mesocycles, and no new `IWorkoutRepository` query.
- No admin bypass for the placement lock (an explicit correction operation is a later change).

## Decisions

### Layer mapping

| Layer | Change |
| --- | --- |
| Domain | `Domain/Mesocycles/`: `Mesocycle`, `MesocycleWeek`, `TrainingWeekType`, `MesocycleName`, `MesocycleObjective`, `DurationInWeeks`. `Domain/Common/WeekNumber.cs`. `Workout` placement. New exceptions in `Domain/Exceptions`. |
| Application | `Interfaces/IMesocycleRepository.cs` only. |
| Infrastructure | `MesocycleEntity`, `MesocycleWeekEntity`, `WorkoutEntity` columns, `WeightTrackerDbContext` configuration, `MesocycleMapper`, `MesocycleRepository`, `WorkoutMapper`, `WorkoutRepository`, seed. Three migrations. |
| Api | `Program.cs` registration of `IMesocycleRepository`; `ExceptionMapper` entries. No controller. |

Cross-area dependencies: `Workouts → Common (Id, WeekNumber)`, `Mesocycles → Common (Id, WeekNumber)`. No edge
between `Workouts` and `Mesocycles` in Domain. In Infra, `WorkoutEntity` has an FK to `MesocycleEntity` declared
without navigation.

### `Mesocycle` aggregate

Mirrors `Workout`: private constructor, `Create(trainerId, athleteId, name, objective, startDate, duration)` generating
`Id.New()` with `IsNew = true` and no weeks, `Rehydrate(id, …, weeks)` with `IsNew = false`.

- `StartDate` is a `DateTime` truncated with `.Date`, stored as a `date` column, like `Workout.WorkoutDate`.
- Actions: `Rename`, `ChangeObjective`, `Reschedule`, `ChangeDuration`, `DefineWeek`, `RemoveWeek`. Each takes value
  objects, so input is validated before it reaches the aggregate; each guards the action, not only the result.
- `Weeks` returns a copy ordered by `WeekNumber`.
- `Rehydrate` re-checks what can be broken by stored data: trainer ≠ athlete, duplicate week numbers, week beyond
  duration. Same approach as `Workout.Rehydrate` re-checking duplicates and superseries.

*Alternative considered:* generating all weeks on `Create`. Rejected: week type and objective are required, so
generated weeks would need invented values, and the confirmed rule is progressive definition with gaps.

### `MesocycleWeek` entity

Extends `Entity`. Its `Id` is a technical row id so the repository can reconcile children by identity, as
`WorkoutExercise` does. `WeekNumber` is set on creation and has no setter. `Redefine(type, objective)` is internal to
the domain assembly so only `Mesocycle.DefineWeek` changes a week; `Rehydrate` is public for the mapper, like
`WorkoutExercise.Rehydrate`.

*Alternative considered:* `MesocycleWeek` as a value object keyed by number. Rejected: the brief and invariants treat
one table per entity with `Id.New()` keys, and an entity id keeps repository updates in place instead of
delete-and-insert on every redefinition.

### Value objects

- `MesocycleName`: required, trimmed, `MaxLength = 256` (same as `WorkoutName`).
- `MesocycleObjective`: required, trimmed, `MaxLength = 4000`; used by both the mesocycle and its weeks. `From`
  returns a non-null value or throws (unlike `WorkoutDescription`, which returns `null` for blank).
- `DurationInWeeks`: `Min = 1`, `Max = 52`.
- `WeekNumber` (`Domain/Common`): `>= 1`. The upper bound depends on a mesocycle and is checked there.
- `TrainingWeekType`: `ADAPTATION = 1` … `RECOVERY = 6`; undefined values refused on define and rehydrate, like
  `WorkoutStatus` with `EnsureDefined`.

### Workout placement

- Two properties, `Id? MesocycleId` and `WeekNumber? MesocycleWeekNumber`, as requested.
- `LinkToMesocycle(Id mesocycleId, WeekNumber weekNumber)` and `UnlinkFromMesocycle()` throw
  `WorkoutPlacementLockedException` (domain, `409`) when `IsLocked`.
- `Rehydrate` gains two optional trailing parameters (`Id? mesocycleId = null, WeekNumber? mesocycleWeekNumber = null`)
  and throws `IncompleteWorkoutPlacementException` (`409`) when only one is given. Optional parameters keep the ~40
  existing callers compiling unchanged; the style already exists in `Workout.AddExercise`.
- `Create` is unchanged; new workouts are standalone.

*Alternative considered:* a `MesocycleWeekReference(Id, WeekNumber)` value object making "both or neither" structural.
Rejected because the confirmed model exposes `MesocycleId` and `MesocycleWeekNumber` as properties and it adds an
abstraction the user asked to avoid. The aggregate keeps the pair consistent in its two methods and in `Rehydrate`.

*Alternative considered:* enforcing the lock in Application, like content edits. Rejected: the confirmed rule has no
caller-dependent bypass ("whatever the caller"), so it is an aggregate invariant. A domain exception is needed because
`WorkoutLockedException` is an Application exception.

### Exceptions

All in `Domain/Exceptions`, extending `DomainException`, each with an explicit `ExceptionMapper` entry:

| Status | Exceptions |
| --- | --- |
| 400 | `InvalidMesocycleNameException`, `InvalidMesocycleObjectiveException`, `InvalidDurationInWeeksException`, `InvalidWeekNumberException`, `InvalidTrainingWeekTypeException` |
| 404 | `MesocycleWeekNotFoundException` |
| 409 | `MesocycleTrainerIsAthleteException`, `MesocycleWeekOutOfRangeException`, `DuplicateMesocycleWeekException`, `MesocycleDurationTooShortException`, `WorkoutPlacementLockedException`, `IncompleteWorkoutPlacementException` |

### Persistence

Three migrations, timestamps on 2026-11-05 at non-round minutes, in this order:

1. `CreateMesocyclesTable`: `Id` PK, `TrainerId` and `AthleteId` (FK `Users`, `Rule.None`), `Name` (256),
   `Objective` (4000, not null), `StartDate` (`date`), `DurationInWeeks` (int). Indexes on `TrainerId` and
   `AthleteId` for the future per-trainer and per-athlete reads.
2. `CreateMesocycleWeeksTable`: `Id` PK, `MesocycleId` (FK `Mesocycles`, cascade), `WeekNumber`, `TrainingWeekType`,
   `Objective` (4000, not null); unique `UQ_MesocycleWeeks_MesocycleId_WeekNumber`.
3. `AddMesocycleToWorkouts`: nullable `MesocycleId` (FK `Mesocycles`, `Rule.None`) and `MesocycleWeekNumber`; check
   constraint `CK_Workouts_MesocycleLink` via `Execute.Sql`
   (`("MesocycleId" IS NULL) = ("MesocycleWeekNumber" IS NULL)`); index
   `IX_Workouts_MesocycleId_MesocycleWeekNumber`. `Down()` drops constraint, index, FK, then columns.

No DB constraint ties `MesocycleWeekNumber` to a defined week: weeks are sparse and removable, and a composite FK
would contradict both. The week range is a future Application check.

EF configuration mirrors the migrations exactly (names, lengths, `.ValueGeneratedNever()`), with
`HasOne<UserEntity>().WithMany()` for trainer and athlete, `HasMany(Weeks).WithOne()` cascade inside the aggregate,
the unique index declared with `HasIndex(...).IsUnique()` so EF orders a delete before an insert of the same week
number, and `HasOne<MesocycleEntity>().WithMany()` on `WorkoutEntity.MesocycleId` with `Restrict`. A future read view
joins with LINQ `join` on these columns.

### Repositories and mappers

- `IMesocycleRepository`: `GetById(Id, CancellationToken)`, `Save(Mesocycle, CancellationToken)`.
- `MesocycleRepository` mirrors `WorkoutRepository`: `AsNoTracking` + `Include(Weeks)` for reads; `Save` adds when
  `IsNew`, otherwise loads tracked, copies every scalar and reconciles weeks by id (update existing, add new, remove
  dropped). Mapping stays inside the repository.
- `MesocycleMapper`: `MapToEntity` / `MapToDomain` through `Mesocycle.Rehydrate` and `MesocycleWeek.Rehydrate`,
  wrapping domain failures the same way `WorkoutMapper` does.
- `WorkoutMapper` maps the two columns both ways. `WorkoutRepository.Save` copies both onto the tracked row in its
  update branch.

### Seed

In `SeedData` / `DatabaseSeeder`, built through `Rehydrate` with deterministic ids: one 4-week mesocycle from
`TrainerId` to `UserId` with weeks 1 (`ADAPTATION`), 2 (`ACCUMULATION`) and 4 (`DELOAD`), leaving week 3 undefined.
`TrainedWorkout` (`PUBLISHED`) is linked to week 1 and `PrescribedStartedWorkout` (`STARTED`) to week 2, so the future
Application change has a movable and a locked placement in the baseline. The mesocycle is added before the workouts
that reference it. `ApiFactory` truncates `MesocycleWeeks` and `Mesocycles`.

### Tests

- Unit, domain: `MesocycleTests` and value object tests under `test/WeightTracker.Unit/Domain/Mesocycles`,
  `WeekNumberTests` under `Domain/Common`, placement cases added to `WorkoutTests` (and its template copy). Shape
  from `WorkoutTests`.
- Unit, infra: `MesocycleMapper` round-trips in `AggregateMapperTests`; placement round-trips in `WorkoutMapperTests`.
- Integration: `MesocycleRepositoryTests` and placement cases in `WorkoutRepositoryTests`, `MigrationsTests` up/down,
  and a direct SQL insert proving the check constraint. No HTTP tests: no route is added, and the existing
  `PUT /workouts/{id}` tests plus a placement-preservation case cover "HTTP unchanged".

## Risks / Trade-offs

- [`WorkoutRepository.Save` forgets a column on update] → spec scenario "Update keeps a new placement" backed by a
  repository integration test.
- [Removing and re-adding the same week number in one save hits the unique index] → unique index declared in the EF
  model; repository test covers remove-then-define in one save.
- [Admin `Reassign` can move a linked workout to another athlete, leaving a mismatched placement] → accepted for now;
  the athlete match is an Application rule for the future linking use cases, which must also decide what `Reassign`
  does to placement.
- [Removing a week or shrinking the duration can orphan linked workouts' week numbers] → out of scope by decision;
  the brief defers it.
- [Optional `Rehydrate` parameters let a caller forget to pass placement] → only `WorkoutMapper` and the seeder pass
  real data; the mapper round-trip test catches a missing pass-through.
- [Template drift on `Workout.cs` / `WorkoutTests.cs`] → update both copies in the same tasks and run the parity check.

## Migration Plan

Forward: the three migrations run in order on startup in Development or through `WeightTracker.Migrations`. Existing
workouts get null placement; no backfill. Rollback: `--down` past them reverses in reverse order (placement columns,
then weeks, then mesocycles); any placement data is lost, which is acceptable since nothing writes it yet outside seed.

## Implementation Notes

- `ApiFactory.RemoveUser` (template-backed, copied to `.stpr/template` in step) also clears mesocycle placements of
  other users' workouts and deletes the user's mesocycles, because the seeded mesocycle's foreign keys to `Users`
  would otherwise block removing the seeded trainer or user. Not listed in the tasks; required by the seed.
- The unique week index is a FluentMigrator `UniqueConstraint` (as in `CreateWorkoutExercisesTable`), mirrored in EF
  as `HasIndex(...).IsUnique()`.
- No other deviation from the mapped references. The `AGENTS.md` quality checklist and
  `.stpr/review/CHECKLIST_REVISION.md` hold for this change; endpoint-related items do not apply (no route added).
