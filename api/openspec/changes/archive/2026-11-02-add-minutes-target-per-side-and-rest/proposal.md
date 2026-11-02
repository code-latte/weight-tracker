# Proposal

## Why

Trainers prescribe exercises that the workout exercise configuration cannot express naturally. Long efforts such as "bike 30 minutes" have to be written as `Duration` in seconds (1800). Unilateral exercises such as "side plank 1x8" really mean 8 per side, and nothing records that. The rest between sets cannot be prescribed at all.

Source brief: `.stpr/plan/openspec/proposal-change-add-minutes-target-per-side-and-rest.md`.

## What Changes

- New set target type `Minutes`, with a decimal value in minutes (`1.25` = 1 min 15 s, `1.5` = 1 min 30 s). It is received in a new request field, `targetMinutes`, and returned in a new response field, `target.minutes`. `Duration` keeps meaning seconds.
- New `perSide` flag on a workout exercise. It is optional on add and update, and defaults to `false`. The stored target stays per side (`1x8` per side stores 8).
- New `restMinutes` on a workout exercise, a decimal in minutes. It is optional, and `null` means no rest is prescribed.
- Minute values must be greater than 0, have at most 2 decimal places, and be no more than 999.99. Anything else is `400 Bad Request`.
- Both workout responses (list and detail) carry `perSide` and `restMinutes` on every entry.
- Existing data: every entry becomes `perSide = false` and `restMinutes = null`. Existing sets are unchanged.
- This is not a breaking change. All new request fields are optional, and the response only gains fields.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `workout-exercises`: set prescription validation gains the `Minutes` type. Add and update accept `perSide` and `restMinutes`, and update replaces them together with effort and sets.
- `workout-retrieval`: workout entries in responses carry `perSide` and `restMinutes`.

## Canonical Template Mapping

| Capability / area | Template reference | Why it is the nearest |
| --- | --- | --- |
| `Minutes` target type | `src/WeightTracker.Domain/Workouts/SetTarget.cs`, `SetTargetTypes.cs` | Closed target hierarchy where `Duration` and `RepsRange` live |
| Minutes value object | `src/WeightTracker.Domain/Workouts/WorkoutName.cs` | Validated `record` value object of the Workouts area with a column-aligned limit |
| `perSide` / `restMinutes` on the entry | `src/WeightTracker.Domain/Workouts/WorkoutExercise.cs` (`Effort`) | Per-entry setting validated in `EnsureValid` and replaced in `Replace` |
| Set column | `src/WeightTracker.Migrations/Migrations/202610212330_AddTargetMaxValueToWorkoutExerciseSets.cs` | Adds a nullable target column to `WorkoutExerciseSets` |
| Entry columns | `src/WeightTracker.Migrations/Migrations/202610131437_AddEffortToWorkoutExercises.cs` | Adds a column to `WorkoutExercises` |
| Mapping | `src/WeightTracker.Infra/Mappers/WorkoutMapper.cs` | Target ↔ columns and entry mapping |
| Request / response | `src/WeightTracker.Api/Endpoints/Workouts/Dtos/AddOrUpdateExerciseRequest.cs`, `WorkoutResponse.cs` | Where sets and effort are received and returned |

Module classification: Workouts is a **mutable aggregate**, and entries are edited only through `Workout`. Nothing here is catalog-like.

Business identifiers: none are added or changed.

No conflicts with `.stpr/template/STPR_INVARIANTS.md` or the template were found, and no deviation from the mapped references is allowed.

## Impact

- **Domain**: `SetTarget`, `SetTargetTypes`, `WorkoutExercise`, `Workout.AddExercise` / `UpdateExercise`, a new minutes value object and its exception.
- **Application**: `AddWorkoutExerciseUseCase` and `UpdateWorkoutExerciseUseCase` signatures.
- **Infra**: `WorkoutExerciseEntity`, `WorkoutExerciseSetEntity`, `WeightTrackerDbContext`, `WorkoutMapper`, `WorkoutRepository` (copying fields on update), and one FluentMigrator migration.
- **API**: `AddOrUpdateExerciseRequest`, `WorkoutResponse` (entries and `SetTargetResponse`), `WorkoutsController`, `ExceptionMapper`.
- **Tests**: unit tests (domain, use cases, mapper) and integration tests (add, update, response shape, repository).
- **Clients**: the backoffice and app can start sending and showing the new fields. Converting decimal minutes to "min + s" is their job.
