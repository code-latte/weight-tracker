# Tasks

## 1. Value objects

- [x] 1.1 Add `RepCount` (int ≥ 1) and `InvalidRepCountException` in the Domain, following `TimeInMinutes`. Verify with `RepCountTests` in `test/WeightTracker.Unit/Domain/Workouts`: 1 is accepted, 0 and negative values are rejected.
- [x] 1.2 Add `Weight` (decimal kg, 0 to `MaxValue = 9999.99m`, at most 2 decimals, `MaxValue` documented as aligned with `numeric(6,2)`) and `InvalidWeightException`. Verify with `WeightTests`: 0, 22.75 and `MaxValue` are accepted; negative values, values above `MaxValue` and 20.125 are rejected.

## 2. `SetRecord` entity and aggregate rules

- [x] 2.1 Add `SetRecord : Entity` (`Id`, `Reps`, `Weight`, `RegisteredAtUtc`) with `Create` using `Id.New()` and `Rehydrate`. Verify that `Create` sets `IsNew` and a fresh Id, and `Rehydrate` restores the given values, with tests in `WorkoutExerciseTests` or a new `SetRecordTests`.
- [x] 2.2 Add `WorkoutNotStartedException` and `SetRecordNotAllowedForTargetException`, both deriving from `DomainException`. Verify the build compiles with warnings as errors.
- [x] 2.3 Add `SetRecords` to `WorkoutExercise` (private list, snapshot ordered by `RegisteredAtUtc` with a stable sort), an optional last `setRecords` parameter on `Rehydrate`, and an `internal AddSetRecord` that rejects any non-rep-based prescription. Verify with `WorkoutExerciseTests`: an empty list after `Create`, set records restored by `Rehydrate`, and the ordering scenarios from the spec.
- [x] 2.4 Add the guard to `WorkoutExercise.Replace`: when set records exist and any new prescription is `Duration` or `Minutes`, throw `SetRecordNotAllowedForTargetException` before mutating. Verify with `WorkoutTests` through `UpdateExercise`: the switch is refused and the entry is unchanged (effort, sets, per side, rest, set records); a rep-based change keeps the set records; an entry without set records can switch to duration.
- [x] 2.5 Add `Workout.AddSetRecord(exerciseId, reps, weight, registeredAtUtc)`, which checks `STARTED`, looks up the entry and delegates. Verify with `WorkoutTests`:
  - happy path, with other entries left untouched;
  - each of `DRAFT`, `PUBLISHED` and `FINISHED` refused;
  - unknown exercise refused;
  - duration-only and reps+minutes prescriptions refused;
  - a reps/range/max mix accepted;
  - more set records than `TotalSets` accepted;
  - set records surviving `Workout.Rehydrate`;
  - `CreateFromTemplate` starting with no set records.

  Every test uses labelled Arrange / Act / Assert phases.
- [x] 2.6 Review: compare the new domain code and tests against `WorkoutTests` / `WorkoutExercise` / `TimeInMinutes` and `STPR_INVARIANTS.md` (records for value objects, `Id.New()`, input validated before mutation, XML docs with `<exception>` tags). Verify that `dotnet test test/WeightTracker.Unit` passes.

## 3. Exception mapping and invariants

- [x] 3.1 Map `InvalidRepCountException` and `InvalidWeightException` to 400, and `WorkoutNotStartedException` and `SetRecordNotAllowedForTargetException` to 409, in `ExceptionMapper`. Verify with the existing `ExceptionMapper` unit tests, if present, or by build and review against the neighbouring entries.
- [x] 3.2 Add the bullet to `.stpr/template/STPR_INVARIANTS.md` under "Editing a workout": adding a set record is not a content edit, and it is allowed only while the workout is `STARTED`. Verify by reading the section for consistency with the edit and status rules.

## 4. Persistence

- [x] 4.1 Add migration `202611071200_CreateSetRecordsTable`:
  - `Id` uuid PK, `WorkoutExerciseId` uuid, `Position` int, `Reps` int, `Weight` decimal(6,2) and `RegisterDatetimeUtc` timestamptz, all not null;
  - cascading FK `FK_SetRecords_WorkoutExercise` to `WorkoutExercises.Id`;
  - index on `WorkoutExerciseId`;
  - `Down()` deletes the table.

  Verify by running the migrator against a throwaway Postgres container, then with `--down` and up again. Never run `--down` on the development database.
- [x] 4.2 Add `SetRecordEntity`, the `WorkoutExerciseEntity.SetRecords` navigation, the `DbSet` and the EF configuration (table and column names including `Position`, `.ValueGeneratedNever()`, `HasPrecision(6, 2)`, `timestamptz`, cascading `HasMany`, index). Verify the build passes and the configuration matches the migration column by column.
- [x] 4.3 Update `WorkoutMapper`: `MapToEntity` keeps each set record's own Id and numbers `Position` from 0 in snapshot order, and `MapToDomain` rehydrates set records ordered by `Position`. Verify with `WorkoutMapperTests`: a round trip of a workout with set records on two entries keeps Ids, reps, weight, time and order; ties keep the order added; stored positions win over row order; an invalid stored weight or rep count raises `PersistenceMappingException`.
- [x] 4.4 Update `WorkoutRepository`: include `SetRecords` in `WithAggregateTables` with `AsSplitQuery()`, and reconcile set records by Id in `Reconcile` (add untracked incoming ones, remove missing ones, update only `Position` on survivors). Verify with `WorkoutRepositoryTests` against the database: set records of a new aggregate persist; adding to a stored aggregate keeps the earlier rows; an earlier set record added later reorders correctly; a removed exercise leaves no set record rows (`ApiFactory.CountRows`); `GetAllByUserId` restores set records alongside the sets.
- [x] 4.5 Review: compare the migration, entity, EF configuration, mapper and repository against `CreateWorkoutTemplateExerciseSetsTable_202611061202`, `WorkoutExerciseSetEntity` and the existing `WorkoutMapper` / `WorkoutRepository` patterns. Confirm no EF Core migration was generated and `WorkoutTemplateMapper` is unchanged.

## 5. Verification

- [x] 5.1 Run `dotnet build WeightTracker.slnx` with no warnings, then `dotnet test WeightTracker.slnx` with Docker running. Verify that unit and integration suites pass, including the Testcontainers migration of `SetRecords`.
- [x] 5.2 Run the template drift check from `AGENTS.md`. Verify it prints no `DRIFT:` lines.
- [x] 5.3 Walk the `AGENTS.md` quality checklist against the change and note any item that does not apply. Verify every applicable item is ticked.

## Workflow follow-up

- A later change adds the use case and endpoint for recording sets, with the integration round trip and the auth matrix.
- Archive this change once it is reviewed and merged.
