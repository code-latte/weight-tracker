# Tasks

## 1. Domain: shared week number

- [x] 1.1 Add `InvalidWeekNumberException` in `src/WeightTracker.Domain/Exceptions` and `WeekNumber` record in `src/WeightTracker.Domain/Common` (`From(int)`, `>= 1`), mirroring `WorkoutName`; verify `dotnet build WeightTracker.slnx` succeeds
- [x] 1.2 Add `test/WeightTracker.Unit/Domain/Common/WeekNumberTests.cs` (labelled Arrange/Act/Assert): 1 accepted, 0 and negatives refused, value equality; verify the tests pass

## 2. Domain: Mesocycles value objects and enum

- [x] 2.1 Add `TrainingWeekType` (`ADAPTATION = 1` … `RECOVERY = 6`) in `src/WeightTracker.Domain/Mesocycles`, mirroring `WorkoutStatus`; verify the build succeeds
- [x] 2.2 Add `InvalidMesocycleNameException`, `InvalidMesocycleObjectiveException`, `InvalidDurationInWeeksException`, `InvalidTrainingWeekTypeException` and the records `MesocycleName` (256), `MesocycleObjective` (required, 4000) and `DurationInWeeks` (1–52) with `MaxLength` / bound constants; verify the build succeeds
- [x] 2.3 Add unit tests under `test/WeightTracker.Unit/Domain/Mesocycles` for each value object: trimming, blank, exact limit, limit + 1, bounds 0/1/52/53; verify the tests pass

## 3. Domain: Mesocycle aggregate

- [x] 3.1 Add `MesocycleTrainerIsAthleteException`, `MesocycleWeekOutOfRangeException`, `DuplicateMesocycleWeekException`, `MesocycleDurationTooShortException` and `MesocycleWeekNotFoundException` in `Domain/Exceptions`; verify the build succeeds
- [x] 3.2 Add entity `MesocycleWeek` (extends `Entity`; immutable `WeekNumber`; internal `Create` and `Redefine`; public `Rehydrate(id, weekNumber, type, objective)` refusing undefined types), mirroring `WorkoutExercise`; verify the build succeeds
- [x] 3.3 Add aggregate `Mesocycle` per design (`Create` with no weeks, `Rehydrate` re-checking trainer ≠ athlete, duplicate weeks and weeks beyond duration; `Rename`, `ChangeObjective`, `Reschedule`, `ChangeDuration`, `DefineWeek`, `RemoveWeek`; ordered `Weeks` snapshot; no `AddDomainEvent`), mirroring `Workout`; verify the build succeeds
- [x] 3.4 Add `test/WeightTracker.Unit/Domain/Mesocycles/MesocycleTests.cs` replicating `WorkoutTests`, covering every scenario in `specs/mesocycles/spec.md` that is not about storage (create is new with no weeks, start date drops time, sparse and out-of-order definitions, redefine keeps one week and its id, week beyond duration, remove and remove undefined, shrink allowed/refused, rehydrate is not new and refuses trainer = athlete / duplicates / week beyond duration, snapshot not mutable, refused actions leave state unchanged); verify the tests pass
- [x] 3.5 Review `Domain/Mesocycles` against `.stpr/template/src/WeightTracker.Domain/Workouts` and the aggregate lifecycle and DDD sections of `STPR_INVARIANTS.md`; confirm no reference to `Domain/Workouts` and record any deviation or confirm none

## 4. Domain: workout placement

- [x] 4.1 Add `WorkoutPlacementLockedException` and `IncompleteWorkoutPlacementException` in `Domain/Exceptions`; verify the build succeeds
- [x] 4.2 Extend `Workout` with `MesocycleId` / `MesocycleWeekNumber`, `LinkToMesocycle`, `UnlinkFromMesocycle` (both refused when `IsLocked`) and two optional trailing `Rehydrate` parameters (refusing a half-set pair); `Create` unchanged; copy the file to `.stpr/template/src/WeightTracker.Domain/Workouts/Workout.cs`; verify the build succeeds with no existing caller changed
- [x] 4.3 Add placement tests to `test/WeightTracker.Unit/Domain/Workouts/WorkoutTests.cs` and copy it to `.stpr/template/test/...`: new workout standalone; link draft; move within and across mesocycles on published; unlink; link/move/unlink refused on `STARTED` and `FINISHED` with placement unchanged; rehydrate with link, without link and half-set refused; verify the tests pass
- [x] 4.4 Review placement against `specs/workout-mesocycle-placement/spec.md` and the template `Workout`; confirm `Domain/Workouts` does not reference `Domain/Mesocycles`, and run the template parity loop from `AGENTS.md` with no `DRIFT` output

## 5. Persistence: migrations

- [x] 5.1 Add `202611051037_CreateMesocyclesTable.cs` per design (FKs to `Users` with `Rule.None`, trainer and athlete indexes, reversing `Down()`), mirroring `CreateFavoriteExercisesTable`; verify the build succeeds
- [x] 5.2 Add `202611051043_CreateMesocycleWeeksTable.cs` (FK cascade to `Mesocycles`, unique `UQ_MesocycleWeeks_MesocycleId_WeekNumber`, reversing `Down()`); verify the build succeeds
- [x] 5.3 Add `202611051049_AddMesocycleToWorkouts.cs` (nullable columns, FK `Rule.None`, `CK_Workouts_MesocycleLink` via `Execute.Sql`, index; `Down()` drops constraint, index, FK, columns), mirroring `CreateTrainerAthletesTable` for raw SQL; verify the build succeeds
- [x] 5.4 Extend `test/WeightTracker.Integration/Migrations/MigrationsTests.cs` if it enumerates tables or columns, and add a case where a direct SQL insert/update of a half-set placement on `Workouts` is rejected; verify `MigrationsTests` passes up and down with Docker running

## 6. Persistence: EF, mappers and repositories

- [x] 6.1 Add `MesocycleEntity` and `MesocycleWeekEntity` in `Infra/Entities`, the two columns on `WorkoutEntity`, and their configuration in `WeightTrackerDbContext` per design (names, lengths from the value object constants, `.ValueGeneratedNever()`, unique week index, no cross-aggregate navigation); verify the build succeeds and no EF Core migration is generated
- [x] 6.2 Add `MesocycleMapper` (`MapToEntity` / `MapToDomain` via `Rehydrate`, domain failures wrapped as in `WorkoutMapper`) and round-trip cases in `test/WeightTracker.Unit/Infra/Mappers/AggregateMapperTests.cs` (no weeks, sparse weeks, all weeks, every `TrainingWeekType`); verify the tests pass
- [x] 6.3 Map placement both ways in `WorkoutMapper` and add round-trip cases to `test/WeightTracker.Unit/Infra/Mappers/WorkoutMapperTests.cs` (linked and standalone); verify the tests pass
- [x] 6.4 Add `IMesocycleRepository` (`GetById`, `Save`) in `Application/Interfaces` and `MesocycleRepository` in `Infra/Repositories` mirroring `WorkoutRepository` (`IsNew` branching, scalar copy, weeks reconciled by id); register it in `Program.cs`; copy placement onto the tracked row in `WorkoutRepository.Save`; verify the build succeeds
- [x] 6.5 Add `"MesocycleWeeks"` and `"Mesocycles"` to the `ApiFactory` truncate list, then add `test/WeightTracker.Integration/Repositories/MesocycleRepositoryTests.cs` (save/load round trip, update with redefine + remove keeps the id, remove-then-define same week in one save, unknown id returns `null`, unknown athlete fails) and placement cases in `WorkoutRepositoryTests` (insert linked, update standalone → linked, update name keeps link, unknown mesocycle fails, saving a mesocycle leaves linked workouts unchanged); verify the tests pass
- [x] 6.6 Review persistence against `WorkoutRepository`, `WorkoutMapper` and the persistence section of `STPR_INVARIANTS.md`; confirm EF configuration matches the three migrations and record any deviation or confirm none

## 7. Api: exception mapping

- [x] 7.1 Add explicit `ExceptionMapper` entries per the design table (400 / 404 / 409) and extend `test/WeightTracker.Integration/Common/ExceptionMapperTests.cs` with each new exception; verify the tests pass

## 8. Seed

- [x] 8.1 Add deterministic ids to `SeedData` and build the seeded mesocycle (trainer → user, 4 weeks, weeks 1, 2 and 4) through `Mesocycle.Rehydrate` in `DatabaseSeeder`, saved before the workouts; link `TrainedWorkout` to week 1 and `PrescribedStartedWorkout` to week 2 through the new `Rehydrate` parameters; verify the integration suite still passes and that a `PUT /workouts/{id}` on the seeded `TrainedWorkout` keeps its placement (repository or seeded-state assertion)

## 9. Final verification

- [x] 9.1 Run `dotnet build WeightTracker.slnx` (warnings as errors) and the full `dotnet test WeightTracker.slnx` with Docker running; verify everything passes
- [x] 9.2 Run the template parity loop from `AGENTS.md`; verify no `DRIFT` lines
- [x] 9.3 Walk the `AGENTS.md` quality checklist and `.stpr/review/CHECKLIST_REVISION.md` for this change; verify every applicable item holds and record any exception in the change
