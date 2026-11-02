## Change

Add a minutes set target, a per-side flag and a rest time to workout exercises

## Objective

Trainers prescribe exercises that the current configuration cannot express naturally:

- Long efforts such as "bike 30 minutes" can only be written as `Duration` in seconds (1800), which is awkward to write and read.
- Unilateral exercises such as "side plank 1x8" mean 8 per side (16 in total), and nothing says so today.
- There is no way to prescribe the rest between sets.

## Capabilities

- Prescribe a set target in minutes, with decimals (`1.25` = 1 min 15 s, `1.5` = 1 min 30 s)
- Mark a workout exercise as per side
- Prescribe the rest time of a workout exercise in minutes, with decimals

## Module Classification

- Workout Domain -> mutable aggregate -> WeightTracker.Domain/Workouts
- Workout Mapper -> mutable aggregate -> WeightTracker.Infra/Mappers
- Workout Persistence (EF configuration + migration) -> mutable aggregate -> WeightTracker.Infra/Persistence, WeightTracker.Migrations/Migrations
- Workout API -> mutable aggregate -> WeightTracker.Api/Endpoints/Workouts

None of these is catalog-like. Workout exercises are edited through the Workout aggregate.

## Canonical Template Mapping

- New target type name -> WeightTracker.Domain/Workouts/SetTargetTypes.cs -> where the set target kinds are named
- `SetTarget.Minutes` -> WeightTracker.Domain/Workouts/SetTarget.cs -> closed hierarchy where `Duration` and `RepsRange` live
- Minutes value object (shared by target and rest) -> WeightTracker.Domain/Workouts/WorkoutName.cs / Effort.cs -> nearest validated domain primitives in the Workouts area
- `PerSide` and `RestMinutes` on the entry -> WeightTracker.Domain/Workouts/WorkoutExercise.cs -> `Effort` is the nearest per-entry setting, validated in `EnsureValid` and replaced in `Replace`
- New column on sets -> WeightTracker.Migrations/Migrations/202610212330_AddTargetMaxValueToWorkoutExerciseSets.cs -> adds a nullable target column to `WorkoutExerciseSets`
- New columns on entries -> WeightTracker.Migrations/Migrations/202610131437_AddEffortToWorkoutExercises.cs -> adds a column to `WorkoutExercises`
- Mapping -> WeightTracker.Infra/Mappers/WorkoutMapper.cs -> `MapTargetToColumns` / `MapTargetToDomain` and entry mapping
- Request DTO -> WeightTracker.Api/Endpoints/Workouts/Dtos/AddOrUpdateExerciseRequest.cs -> where sets and effort are received
- Response DTO -> WeightTracker.Api/Endpoints/Workouts/Dtos/WorkoutResponse.cs -> where entries and sets are returned

## State And Mutability

- `WorkoutExerciseSets` gets `TargetMinutes` (`numeric(5,2)`, nullable). It is null for every target type except `Minutes`. `TargetValue` and `TargetMaxValue` stay integers and stay null for `Minutes`.
- `WorkoutExercises` gets `PerSide` (`boolean`, not null, default `false`) and `RestMinutes` (`numeric(5,2)`, nullable).
- Existing rows: every entry becomes `PerSide = false` and `RestMinutes = null`. No existing set changes.
- `PerSide` and `RestMinutes` belong to the whole workout exercise, not to each set prescription. They are set on add and replaced on update together with effort and sets.
- Request: `SetPrescriptionRequest` gets `TargetMinutes` (decimal, required only for `Minutes`, ignored otherwise). `AddOrUpdateExerciseRequest` gets `PerSide` (optional, defaults to `false`) and `RestMinutes` (optional, null means no rest prescribed).
- Response: sets return `TargetMinutes` for `Minutes`; entries return `PerSide` and `RestMinutes`.

## Business Rules

- Minutes target must be greater than 0.
- Rest, when given, must be greater than 0. Null means no rest is prescribed.
- Minutes values (target and rest) accept at most 2 decimal places and must not exceed 999.99. Values with more decimals are rejected, not rounded.
- `Duration` keeps meaning seconds. `Minutes` is a separate type, and no conversion between them happens.
- `PerSide` does not change the stored target: `1x8` per side stores 8, not 16. It applies to every target type.
- Adding or updating these fields is a workout content edit and follows the existing workout edit rule. No new rule is introduced.
- Invalid values throw `InvalidSetPrescriptionException` (target) or a new domain exception for rest, mapped to `400` in `ExceptionMapper`.

## Roles And Visibility

- No change. Whoever can add or update a workout exercise today can set these fields, and whoever can read the workout sees them.

## Business Identifiers And Mutability

- N/A. No identifier is added or changed.

## Out Of Scope

- Converting decimal minutes to "min + s" for display. That belongs to the frontend.
- Rest per set prescription, rest between superseries rounds, or a superseries-level rest.
- Per-side for a single set prescription instead of the whole exercise.
- Migrating existing `Duration` sets to `Minutes`.
- Recording the rest actually taken by the athlete.

## Notes

- Model minutes as a domain value object (for example `Minutes`) used by both `SetTarget.Minutes` and `WorkoutExercise.RestMinutes`, so the decimal rules live in one place and use cases never validate raw decimals.
- Use `decimal` in C#, mapped to `numeric(5,2)` in Postgres. EF configuration must match the migration (column names and precision).
- JSON uses a dot as the decimal separator (`1.25`). The comma in "1,25" is display only.
- `WorkoutExercise.Create`, `Rehydrate` and `Replace` gain `perSide` and `rest` parameters. Validation happens before any state changes, as with effort and sets.
- `ToString` of the new target follows the existing ones (for example `30 min.`).
- Tests: unit tests for the minutes value object, `SetTarget.Minutes`, `WorkoutExercise` (create/replace with per side and rest, invalid rest leaves state untouched) and mapper round-trip; integration tests for add and update exercise with the new fields and the `400` cases. Existing auth matrices on those routes stay as they are.
- No deviations from the mapped template references are allowed.
