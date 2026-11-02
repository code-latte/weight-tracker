# Tasks

## 1. Minutes value object

- [x] 1.1 Add `TimeInMinutes` (`src/WeightTracker.Domain/Workouts/TimeInMinutes.cs`) following `WorkoutName.cs`: `Value`, `MaxValue = 999.99m`, `From` rejecting `<= 0`, `> MaxValue` and more than 2 decimals (D1). Add `InvalidTimeInMinutesException` in `src/WeightTracker.Domain/Exceptions`. Verify `dotnet build WeightTracker.slnx` passes.
- [x] 1.2 Map `InvalidTimeInMinutesException` to `400` in `src/WeightTracker.Api/Common/ExceptionMapper.cs`. Add a case in `test/WeightTracker.Integration/Common/ExceptionMapperTests.cs` and verify it passes.
- [x] 1.3 Add `test/WeightTracker.Unit/Domain/Workouts/TimeInMinutesTests.cs` covering `0.01`, `1.25`, `1.250` (equal to `1.25`) and `999.99` accepted, and `0`, `-1`, `1.255` and `1000` rejected. Use Arrange/Act/Assert labels and verify the tests pass.

## 2. Minutes set target (domain)

- [x] 2.1 Add `SetTargetTypes.Minutes` and the `SetTarget.Minutes(TimeInMinutes value)` record with `ToString()` `"{value} min."` (D2). Verify the build passes.
- [x] 2.2 Extend `test/WeightTracker.Unit/Domain/Workouts/SetTargetTests.cs` with construction, value equality and `ToString` of `Minutes`. Verify the tests pass.
- [x] 2.3 Review: compare `SetTarget.Minutes` and `TimeInMinutes` against `SetTarget.cs` / `WorkoutName.cs` (naming, XML docs, exception docs). Copy `SetTarget.cs` to `.stpr/template/src/WeightTracker.Domain/Workouts/SetTarget.cs` and verify `cmp` reports no drift.

## 3. Per side and rest on the entry (domain + application)

- [x] 3.1 Add `PerSide` and `Rest` to `WorkoutExercise`, with trailing optional parameters on `Create` / `Rehydrate` and required parameters on `Replace` (D4). Verify the build passes with the seed unchanged.
- [x] 3.2 Add `perSide` and `rest` to `Workout.AddExercise` and `Workout.UpdateExercise` and pass them to the entry. Verify the build passes.
- [x] 3.3 Extend `test/WeightTracker.Unit/Domain/Workouts/WorkoutExerciseTests.cs` and `WorkoutTests.cs`: create with defaults (`false`, `null`), create with values, update replaces both, update without them clears previous values, and a superseries update that fails keeps the previous `perSide` and rest. Verify the tests pass.
- [x] 3.4 Add `bool perSide` and `TimeInMinutes? rest` to `AddWorkoutExerciseUseCase.Execute` and `UpdateWorkoutExerciseUseCase.Execute` and pass them to the aggregate. Update `AddWorkoutExerciseUseCaseTests` and `UpdateWorkoutExerciseUseCaseTests` to assert that the saved workout carries them. Verify the tests pass.
- [x] 3.5 Review: compare `WorkoutExercise` / `Workout` changes against how `Effort` is handled there (validation before mutation, XML docs, `internal Replace`). Copy `Workout.cs`, `WorkoutExercise.cs` and `test/WeightTracker.Unit/Domain/Workouts/WorkoutTests.cs` to their `.stpr/template` counterparts and verify the drift check from `AGENTS.md` prints nothing.

## 4. Persistence

- [x] 4.1 Add the migration `src/WeightTracker.Migrations/Migrations/<timestamp>_AddMinutesPerSideAndRestToWorkouts.cs` with `PerSide` (boolean, not null, default false), `RestMinutes` and `TargetMinutes` (`AsDecimal(5, 2)`, nullable), and a `Down()` that drops all three (D5). Mirror `202610212330_AddTargetMaxValueToWorkoutExerciseSets.cs`. Verify that running the migrator up then `--down` against the compose database succeeds.
- [x] 4.2 Add `PerSide`, `RestMinutes` and `TargetMinutes` to `WorkoutExerciseEntity` / `WorkoutExerciseSetEntity` and configure them in `WeightTrackerDbContext` with `HasColumnName` and `HasPrecision(5, 2)`. Verify the build passes.
- [x] 4.3 Update `WorkoutMapper`: the target uses a 4-column tuple including `TargetMinutes`, and the entry maps `PerSide` / `RestMinutes` both ways. Extend `test/WeightTracker.Unit/Infra/Mappers/WorkoutMapperTests.cs` with a round-trip of a `Minutes` set and of an entry with `perSide` and rest, and assert that a `Reps` set writes `TargetMinutes = null`. Verify the tests pass.
- [x] 4.4 Copy `PerSide` and `RestMinutes` in the update branch of `WorkoutRepository.Save`. Extend `test/WeightTracker.Integration/Repositories/WorkoutRepositoryTests.cs` with an update that changes both and a reload that asserts them. Verify the test passes.
- [x] 4.5 Review: compare the migration, EF configuration and mapper changes against the mapped references (column names, precision, `Down()` symmetry, mapping only inside the repository).

## 5. API

- [x] 5.1 Update `AddOrUpdateExerciseRequest`: add `TargetMinutes` to `SetPrescriptionRequest`, add `Minutes` to `[AllowedValues]` and the `ToDomain` switch, and add `PerSide` / `RestMinutes` to the request with XML docs (D3, D4). Pass them through `WorkoutsController.AddExercise` / `UpdateExercise` inside the existing `try`. Verify the build passes.
- [x] 5.2 Update `WorkoutResponse.cs`: add `SetTargetResponse.Minutes` (omitted when null), and add `PerSide` and `RestMinutes` to `WorkoutExerciseDetailResponse` and `WorkoutExerciseResponse` (always serialized). Verify the build passes.
- [x] 5.3 Extend `test/WeightTracker.Integration/Workouts/WorkoutExerciseAddEndpointTests.cs` for the add scenarios in `specs/workout-exercises/spec.md`: Minutes target (`30`, `1.25`, `999.99`), Minutes missing or invalid, Minutes ignores the integer targets, other types ignore targetMinutes, per side with reps and duration, per side omitted, rest `1.5` / `0.75` / omitted, and rest `1.333` / `0` / `1000` returning `400`. Authenticate with `AuthenticatedClientFor`. Verify the tests pass.
- [x] 5.4 Extend `test/WeightTracker.Integration/Workouts/WorkoutExerciseUpdateEndpointTests.cs`: change per side and rest, omitted values clearing them, and invalid rest `-1` returning `400` with the entry unchanged. Verify the tests pass.
- [x] 5.5 Extend `test/WeightTracker.Integration/Workouts/WorkoutResponseShapeTests.cs`: entries carry `perSide` and `restMinutes` (including `null`) on both read-by-id and list, and a `Minutes` target has `minutes` with no `value` or `maxValue`. Verify the tests pass.
- [x] 5.6 Review: compare the DTO and controller changes against `AddOrUpdateExerciseRequest.cs` / `WorkoutResponse.cs` conventions (structural validation only, XML docs, `ExceptionMapper.Map` in every catch). The existing `401`/`403`/`404` matrices on the add and update routes stay as they are, because no route is added.

## 6. Verification

- [x] 6.1 With Docker running, run `dotnet test WeightTracker.slnx` and verify that both unit and integration suites pass with no warnings.
- [x] 6.2 Run the template drift check from `AGENTS.md` and verify it prints nothing. Walk the `AGENTS.md` quality checklist and verify each item applies or holds.
- [x] 6.3 Run `openspec validate add-minutes-target-per-side-and-rest --strict` and verify it passes.
