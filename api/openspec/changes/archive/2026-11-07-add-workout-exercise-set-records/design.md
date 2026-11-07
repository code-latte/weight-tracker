# Design

## Context

See proposal.md (Why) and `specs/workout-set-records/spec.md` for the required behavior.

Current state that shapes the approach:

- `Workout` is the aggregate root. `WorkoutExercise` is its child entity and is also reused by `WorkoutTemplate`. Both are rebuilt with `Rehydrate`.
- `WorkoutExercise` holds its prescriptions as `SetPrescription` value records. They have no identity, so `WorkoutMapper` gives every set row a fresh `Id.New()` and `WorkoutRepository.Reconcile` clears and re-adds the set rows on every save.
- `WorkoutRepository.Reconcile` matches `WorkoutExercise` rows by Id, and removed entries are cascaded as orphans.
- The edit rule and the `STARTED`/`FINISHED` lock live in an Application service (`WorkoutEditAuthorizer`). An `ADMIN` bypasses the lock. The aggregate only knows `IsLocked`.
- Decimal value objects follow `TimeInMinutes`: a `record` with `From`, a `MaxValue` aligned with the numeric column, and a 2-decimal check.
- UTC timestamps are `timestamptz` columns named `...Utc` (`CreateTrainerAthletesTable`, `CreateFavoriteExercisesTable`).

## Goals / Non-Goals

**Goals:**

- Put every set record rule in the aggregate, so a future use case only loads the workout, calls `AddSetRecord` and saves.
- Give set records stable identity so later saves never rewrite stored rows.
- Keep `WorkoutTemplate` and `WorkoutTemplateMapper` unchanged.

**Non-Goals:**

- Use cases, authorization of who may record, DTOs and endpoints.
- Editing or deleting set records.

## Decisions

### Layer map

| Layer | Change |
|---|---|
| Domain | `SetRecord` entity, `RepCount` and `Weight` value objects, `WorkoutExercise.SetRecords`, `Workout.AddSetRecord`, guard in `WorkoutExercise.Replace`, four new exceptions |
| Application | none |
| Infrastructure | `SetRecordEntity`, EF configuration, `WorkoutMapper`, `WorkoutRepository` include and reconcile, migration `CreateSetRecordsTable` |
| Api | `ExceptionMapper` entries only |

No cross-aggregate dependencies are added. A set record references only its `WorkoutExercise`, through the aggregate's own tables.

### `SetRecord` is an entity, not a value object

`SetRecord : Entity` with `Id`, `RepCount Reps`, `Weight Weight`, `DateTime RegisteredAtUtc`, a `Create` that uses `Id.New()` and a `Rehydrate`.

Two set records with the same reps, weight and time are still two different sets, so they need identity. Identity also lets the repository insert only new rows instead of clearing and rewriting the athlete's log on every save. The alternative was a value record like `SetPrescription`, rejected because a clear-and-replace on every save would rewrite historical data and lose row identity.

### Where each rule lives

- `Workout.AddSetRecord(Id exerciseId, RepCount reps, Weight weight, DateTime registeredAtUtc)`:
  - checks `Status == STARTED` and throws `WorkoutNotStartedException` otherwise;
  - finds the entry by `ExerciseId` and throws `ExerciseNotInWorkoutException` otherwise;
  - delegates to `WorkoutExercise.AddSetRecord` (`internal`).

  The status belongs to the root, while the entry lookup follows `UpdateExercise`.
- `WorkoutExercise.AddSetRecord` throws `SetRecordNotAllowedForTargetException` unless every prescription is `Reps`, `RepsRange` or `MaxReps`. The entry owns its prescriptions, so it owns this check.
- `WorkoutExercise.Replace` throws the same exception when the entry has set records and any new prescription is time-based. It throws before mutating, so the existing "nothing changes when invalid" contract holds. Reusing one exception keeps a single rule: set records require rep-based targets.
- The rep-based check is one private helper in `WorkoutExercise` (`target is Reps or RepsRange or MaxReps`), used by both methods. Adding a property to `SetTarget` was considered, but nothing outside `WorkoutExercise` needs it.
- `Workout.AddSetRecord` does not check `IsLocked` or the edit rule. Recording is not a content edit (spec requirement), and the `STARTED` check already covers what is needed.

### Value objects

- `RepCount`: an `int` of at least 1.
- `Weight`: a `decimal` in kilograms, from 0 to `MaxValue = 9999.99m`, with at most 2 decimals. `MaxValue` is aligned with `numeric(6,2)`, documented the same way as `TimeInMinutes.MaxValue`.

Each has its own exception (`InvalidRepCountException`, `InvalidWeightException`), mapped to 400 next to `InvalidTimeInMinutesException`.

### Ordering

`WorkoutExercise.SetRecords` returns a snapshot ordered by `RegisteredAtUtc`. LINQ `OrderBy` is stable, so ties keep the order they were added in.

The order is stored in a `Position` column, numbered from 0 in snapshot order, and the mapper reloads rows by `Position`. Ordering rows by `RegisterDatetimeUtc` and then `Id` was rejected: `Guid.CreateVersion7()` is only ordered to the millisecond, so ties created in the same millisecond could come back swapped.

Because a set record registered earlier can be added later, saving can shift the `Position` of stored rows. Reconciliation updates `Position` on surviving rows and never rewrites their other values.

### Rehydrate stays lenient

`WorkoutExercise.Rehydrate` gets an optional `IEnumerable<SetRecord>? setRecords = null` (last parameter), so `WorkoutTemplateMapper` and existing callers compile unchanged. Rehydrate does not re-check the rep-based or `STARTED` rules. Stored rows were valid when written, and an admin can later override the status. Re-checking would make a legal stored state, such as a `FINISHED` workout with set records, fail to load.

`WorkoutExercise.Create`, `Workout.CreateFromTemplate` and `WorkoutTemplate` start with an empty list.

### Persistence

- `SetRecordEntity { Id, WorkoutExerciseId, Position, Reps, Weight, RegisterDatetimeUtc }` and `WorkoutExerciseEntity.SetRecords` navigation.
- EF configuration:
  - table `SetRecords`;
  - `Id` uses `.ValueGeneratedNever()`;
  - `Weight` uses `HasPrecision(6, 2)`;
  - `RegisterDatetimeUtc` uses `HasColumnType("timestamptz")`;
  - `HasMany(SetRecords).WithOne().HasForeignKey(WorkoutExerciseId).OnDelete(Cascade)`, mirroring `Sets`.
- Migration `202611071200_CreateSetRecordsTable`:
  - columns as in the brief plus `Position` (int), with `RegisterDatetimeUtc` as `AsCustom("timestamptz")`;
  - FK `FK_SetRecords_WorkoutExercise` with cascade delete;
  - index on `WorkoutExerciseId`;
  - `Down()` deletes the table.
- `WorkoutMapper.MapToEntity` maps each set record with its own `Id`, not `Id.New()`, and numbers `Position`. `MapToDomain` rehydrates them by `Position`, and an invalid stored value already surfaces as `PersistenceMappingException` through the existing `DomainException` catch.
- `WorkoutRepository`:
  - `WithAggregateTables` adds a second include path, `.Include(WorkoutExercises).ThenInclude(SetRecords)`, and uses `AsSplitQuery()`. `Sets` and `SetRecords` are sibling collections, so one query would return a row per prescription × set record for each exercise, including in the list queries.
  - `Reconcile` matches the set records of each surviving entry by Id. It adds the incoming ones it does not track, removes tracked ones that are no longer present, and updates only `Position` on the ones that survive.
  - Entries that are new or dropped carry their set records through the existing add and cascade paths.

### Invariants document

Add one bullet under "Editing a workout" in `.stpr/template/STPR_INVARIANTS.md`: adding a set record is not a content edit, so neither the edit rule nor the lock applies. It is allowed only while the workout is `STARTED`, and the aggregate enforces that. This is not a `src` copy, so template parity is unaffected.

## Risks / Trade-offs

- **[Risk] Split queries run one round trip per collection, with no snapshot across them.**
  → Accepted: a concurrent write between the queries is unlikely for one athlete's workout, and it avoids loading every set record once per prescription.
- **[Trade-off] Adding a set record registered earlier rewrites the `Position` of later rows.**
  → Accepted: positions are small integers per exercise, and the other values of a stored set record are never rewritten.
- **[Risk] A non-UTC `DateTime` reaches `timestamptz`, which Npgsql rejects on write.**
  → Mitigation: the future use case passes `DateTime.UtcNow`. Npgsql reads `timestamptz` back as `Kind = Utc`, so the round trip is lossless.
- **[Trade-off] An `ADMIN` can still move a workout with set records back to `DRAFT` or `PUBLISHED` and edit it.**
  → Accepted by decision. Only the switch to time-based targets is blocked.
- **[Trade-off] Removing an exercise deletes its set records by cascade.**
  → Accepted. Only an `ADMIN` can remove exercises from a `STARTED` or `FINISHED` workout.

## Migration Plan

- The migration is additive (a new table), so it is safe on existing data. Rollback is `--down` to the previous version, which drops `SetRecords` and its data.
- Deploy order: migrator first, then the API, as today.
