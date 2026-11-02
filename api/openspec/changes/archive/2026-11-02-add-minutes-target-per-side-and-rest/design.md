# Design

## Context

See `proposal.md` for the motivation and `specs/` for the behavior. The current state that shapes the approach:

- `SetTarget` is a closed record hierarchy (`Reps`, `Duration`, `MaxReps`, `RepsRange`) whose values are all `int`. It is persisted in `WorkoutExerciseSets.TargetType`, `TargetValue` and `TargetMaxValue` (both `int` columns).
- `WorkoutExercise` holds `Effort` and the sets. Both are validated in `EnsureValid` and replaced together in `Replace`, which only `Workout.UpdateExercise` calls.
- The request DTO builds domain objects inside the controller `try` (`set.ToDomain()`), so domain exceptions raised there reach `ExceptionMapper`.
- `WorkoutRepository.Save` copies scalar entry fields (`Position`, `Effort`, `SuperSeriesId`) onto tracked rows. Any new entry column must be copied there too, or updates are silently lost.
- `.stpr/template/src` holds byte-identical copies of `Workout.cs`, `WorkoutExercise.cs` and `SetTarget.cs`, and `.stpr/template/test` holds a copy of `WorkoutTests.cs`.

## Goals / Non-Goals

**Goals:**
- One domain type owns the rules for minute values, used by both the target and the rest.
- Existing integer target columns and request fields keep their meaning and type.
- Existing callers of `WorkoutExercise.Create` / `Rehydrate` (seed, tests) keep compiling without changes.

**Non-Goals:**
- No conversion between `Duration` (seconds) and `Minutes`.
- No change to superseries rules. `perSide` and rest do not take part in the set-total check.

## Decisions

### D1. `TimeInMinutes` value object in `Domain/Workouts`

`sealed record TimeInMinutes` with `decimal Value`, `const decimal MaxValue = 999.99m` and `static From(decimal value)`. It rejects values that are `<= 0`, `> MaxValue` or `decimal.Round(value, 2) != value`. The check is on the value, not on the decimal's scale, so `1.250` is accepted as `1.25`. It throws a new `InvalidTimeInMinutesException`, mapped to `400` in `ExceptionMapper`. It follows the `WorkoutName` pattern: private constructor, a column-aligned constant, and a `From` factory.

- *Why not `Minutes`?* `SetTarget.Minutes` is the target record's name. Giving the value object the same name would make every reference inside `SetTarget` ambiguous to read.
- *Why not `Domain/Common`?* Only the Workouts area uses it. The invariant moves value objects to `Common` once two domain areas share them.
- *Why not reuse `InvalidSetPrescriptionException`?* Rest is not a set prescription. One exception for the value object keeps the error source honest for both uses.

### D2. `SetTarget.Minutes(TimeInMinutes value)` and `SetTargetTypes.Minutes = "Minutes"`

This is a new sealed record in the existing hierarchy, with `ToString()` returning `"{value} min."`. The value object already validated the value, so the record adds no checks.

### D3. Separate decimal column and fields instead of widening `TargetValue`

- DB: `WorkoutExerciseSets.TargetMinutes numeric(5,2) NULL`. It is non-null only for `Minutes`. `TargetValue` and `TargetMaxValue` stay `int` and are null for `Minutes`.
- Request: `SetPrescriptionRequest.TargetMinutes` (`decimal?`). `ToDomain` builds `new SetTarget.Minutes(TimeInMinutes.From(TargetMinutes ?? 0))`. As with `Target ?? 0`, a missing value fails domain validation and returns `400`.
- Response: `SetTargetResponse.Minutes` (`decimal?`, omitted when null), alongside `Value` and `MaxValue`.
- *Alternative:* change `TargetValue`, `Target` and `Value` to decimal. Rejected because it lets `Reps` accept `8.5` at the edge and pushes integer checks into every integer target. It also alters an existing column type for all rows.

### D4. `PerSide` and `Rest` live on `WorkoutExercise`, next to `Effort`

- Domain: `bool PerSide { get; private set; }` and `TimeInMinutes? Rest { get; private set; }`.
  - `Create(exerciseId, effort, sets, bool perSide = false, TimeInMinutes? rest = null)` and `Rehydrate(..., superSeriesId = null, bool perSide = false, TimeInMinutes? rest = null)` use trailing optional parameters, so the existing seed and tests compile unchanged.
  - `Replace(effort, sets, perSide, rest)` makes all four parameters required. Update is a full replacement, so omitted values must clear previous ones.
  - `Workout.AddExercise` and `Workout.UpdateExercise` gain the same two parameters and pass them through.
- Validation: `TimeInMinutes` is valid by construction and `bool` has no invalid state, so `EnsureValid` gains no checks. Atomicity holds because the value object is built in the controller before any aggregate method runs.
- Application: `AddWorkoutExerciseUseCase.Execute` and `UpdateWorkoutExerciseUseCase.Execute` gain `bool perSide` and `TimeInMinutes? rest` after `sets`. Authorization (`WorkoutEditAuthorizer.EnsureCanEdit`) is unchanged.
- API: `AddOrUpdateExerciseRequest.PerSide` (`bool?`, `?? false`) and `RestMinutes` (`decimal?`, mapped with `TimeInMinutes.From` when not null). Both are built inside the existing controller `try`. `WorkoutExerciseDetailResponse` and `WorkoutExerciseResponse` gain `bool PerSide` and `decimal? RestMinutes`, which are always serialized, `null` included, as the spec requires.

### D5. Persistence

- One FluentMigrator migration, `AddMinutesPerSideAndRestToWorkouts_<timestamp>`:
  - `WorkoutExercises.PerSide` is `AsBoolean().NotNullable().WithDefaultValue(false)`, so existing rows backfill to `false`.
  - `WorkoutExercises.RestMinutes` is `AsDecimal(5, 2).Nullable()`.
  - `WorkoutExerciseSets.TargetMinutes` is `AsDecimal(5, 2).Nullable()`.
  - `Down()` deletes all three columns.
- EF: the entity properties are `bool PerSide`, `decimal? RestMinutes` and `decimal? TargetMinutes`, configured with `HasColumnName` and `HasPrecision(5, 2)` for the decimals. `numeric(5,2)` caps at 999.99, which matches `TimeInMinutes.MaxValue`.
- Mapper: `MapTargetToColumns` returns `(TargetType, TargetValue, TargetMaxValue, TargetMinutes)`, and `MapTargetToDomain` takes the extra column. Entry mapping writes and reads `PerSide` and `RestMinutes`. `Rehydrate` uses `TimeInMinutes.From` when `RestMinutes` is not null.
- Repository: the update branch in `WorkoutRepository.Save` copies `PerSide` and `RestMinutes`. Sets are already replaced wholesale, so `TargetMinutes` needs nothing extra.

### D6. Template copies

`Workout.cs`, `WorkoutExercise.cs`, `SetTarget.cs` and `WorkoutTests.cs` change, so their `.stpr/template` copies are updated to stay byte-identical. The drift check in `AGENTS.md` verifies this. `TimeInMinutes.cs` is not added to the template, because it is not a canonical reference.

## Risks / Trade-offs

- [Leftover values when a set changes type, for example `TargetMinutes` left on a set that became `Reps`] → Sets are deleted and reinserted on every update, and `MapTargetToColumns` writes `null` for columns the type does not use.
- [A forgotten field copy in `WorkoutRepository.Save` silently drops `perSide` or rest on update] → An integration test updates `perSide` and rest, reads the workout back, and asserts the stored values.
- [JSON decimals from a client in a comma locale] → The contract is a JSON number with a dot. Formatting for display is the client's job (see proposal).
- [Trailing optional parameters on `Create` and `Rehydrate` hide new arguments from callers] → This is accepted to keep the seed and existing tests unchanged. `Replace` and the use cases make them required, so the edit path cannot forget them.

## Migration Plan

1. Deploy the migration. It is additive, and existing rows get `PerSide = false` and nulls.
2. Deploy the API. Old clients keep working because all new request fields are optional.
3. Rollback: `Down()` drops the three columns, which loses any per-side, rest or minutes data written meanwhile. Any `Minutes` sets left in the database cannot be read by the old API, so delete or convert them before rolling back.
