# Tasks

## 1. Week date in the Mesocycle aggregate

- [x] 1.1 Add `Mesocycle.DateOfWeek(WeekNumber)` per design decision 1 (Monday-based week, Wednesday of week 1, plus `7 × (n − 1)` days, `MesocycleWeekOutOfRangeException` beyond the duration), with XML docs. Verify `dotnet build WeightTracker.slnx` passes with warnings as errors.
- [x] 1.2 Add tests in `test/WeightTracker.Unit/Domain/Mesocycles/MesocycleTests.cs` for every scenario of "Each week of a mesocycle has a date" (start Monday, Wednesday and Sunday, later week, undefined week, beyond the duration, after `Reschedule`), using labelled Arrange/Act/Assert. Verify the unit suite passes.

## 2. Removing a mesocycle week

- [x] 2.1 Add `IWorkoutRepository.ExistsByMesocycleWeek(Id mesocycleId, WeekNumber weekNumber, CancellationToken)` and implement it in `WorkoutRepository` as an `AnyAsync` over every status. Copy the interface change to `.stpr/template/src/.../IWorkoutRepository.cs`. Verify the build passes and the parity check in `AGENTS.md` reports no drift.
- [x] 2.2 Add integration tests for `ExistsByMesocycleWeek` in `test/WeightTracker.Integration/Repositories/WorkoutRepositoryTests.cs`: seeded week 1 is true, week 4 is false, and the same week number in another mesocycle is false. Verify they pass.
- [x] 2.3 Add `MesocycleWeekHasWorkoutsException` in `Application/Exceptions`, map it to `409` in `ExceptionMapper`, and cover it in `ExceptionMapperTests`. Verify that test passes.
- [x] 2.4 Add `RemoveMesocycleWeekUseCase` following `RedefineMesocycleWeekUseCase`, with the check order from design decision 2 (not found, edit rule, week not defined, workouts in the week, then `RemoveWeek` and save). Register it in `Program.cs`. Verify the build passes.
- [x] 2.5 Add unit tests for `RemoveMesocycleWeekUseCase` next to `MesocycleWriteUseCaseTests`, covering trainer success, admin success, user `403`, another trainer `403`, unknown mesocycle, undefined week (no workouts query is made), a week with workouts (nothing is saved). Verify they pass.
- [x] 2.6 Add `DELETE /mesocycles/{id}/weeks/{weekNumber}` to `MesocyclesController`, mirroring `WorkoutsController.RemoveExercise` (`204`, `ProducesResponseType` 204/400/401/403/404/409, `ExceptionMapper.Map` in the catch). Verify the route appears in `/openapi/v1.json`.
- [x] 2.7 Add integration tests in `MesocycleWriteEndpointsTests` for every `mesocycle-management` delta scenario on the new route:
  - `401` without a token and `401` with an invalid token
  - `403` for the athlete and for another trainer
  - `404` for an unknown mesocycle and for undefined week 3
  - `409` for week 1 (published workout) and week 2 (started workout)
  - `204` for week 4, after which `GET` shows weeks 1 and 2 with a duration of 4
  - admin success
  - week 4 defined again after removal

  Verify they pass.
- [x] 2.8 Review: compare `RemoveMesocycleWeekUseCase`, the controller action and their tests against `RedefineMesocycleWeekUseCase` and `WorkoutsController.RemoveExercise` and their tests. Verify there is no structural deviation.

## 3. Derived workout date on POST and PUT /workouts

- [x] 3.1 Add `MissingWorkoutDateException` in `Application/Exceptions`, map it to `400` in `ExceptionMapper`, and cover it in `ExceptionMapperTests`. Verify that test passes.
- [x] 3.2 Add `MesocyclePlacementValidator.EnsureDateOrPlacement` (static) and `ResolveWorkoutDate` per design decision 3. Verify the build passes.
- [x] 3.3 Change `CreateWorkoutUseCase.Execute` to take `DateTime? workoutDate`: call `EnsureDateOrPlacement` after `EnsureComplete`, resolve the date after `EnsureCanPlace`, and pass the resolved date to `Workout.Create`. Update the XML docs. Verify the build passes.
- [x] 3.4 Change `UpdateWorkoutUseCase.Execute` the same way. Resolve the date whenever a placement is sent, even an unchanged one, and keep the existing `EnsureCanPlace` and lock conditions. Update the XML docs. Verify the build passes.
- [x] 3.5 Update `CreateWorkoutUseCaseTests` and `UpdateWorkoutUseCaseTests`:
  - The date is ignored with a placement.
  - Missing date without a placement throws `MissingWorkoutDateException`.
  - Missing date with a placement succeeds.
  - On update, an unchanged placement still recalculates the date.
  - Unlinking uses the sent date.
  - A refused placement leaves the date unchanged.

  Verify they pass.
- [x] 3.6 Remove `[Required]` from `WorkoutDate` in `CreateWorkoutRequest` and `UpdateWorkoutRequest`, rewrite their XML docs (required without a placement, ignored with one), and map `request.WorkoutDate?.ToDateTime(TimeOnly.MinValue)` in `WorkoutsController`. Verify the build passes.
- [x] 3.7 Update the seed per design decision 7: `TrainedWorkout` becomes `2026-09-30` and `PrescribedStartedWorkout` becomes `2026-10-07`. Express them as `SeedData` constants, and update the integration tests that assert the old dates (`WorkoutUpdateEndpointTests`, `WorkoutGetByIdEndpointTests`, `WorkoutMesocyclePlacementEndpointTests`, `WorkoutResponseShapeTests`). Verify those tests pass.
- [x] 3.8 Add integration tests in `WorkoutMesocyclePlacementEndpointTests` for every scenario of "A placed workout's date is the date of its week" and "Rescheduling a mesocycle does not move its workouts". Add the `workout-creation` and `workout-details` delta scenarios to their existing endpoint tests: missing date without a placement is `400`, and a placed workout without a date is `201`/`200`. Verify they pass.
- [x] 3.9 Review: compare the changed workout use cases, validator and tests against their pre-change structure and `.stpr/template` conventions (Arrange/Act/Assert, exceptions mapped explicitly). Verify there is no structural deviation.

## 4. Verification

- [x] 4.1 Run the template parity check from `AGENTS.md` and verify it prints no `DRIFT` lines. The `IWorkoutRepository.cs` copy touched here matches. The only remaining drift, `test/WeightTracker.Integration/Workouts/WorkoutScopingTests.cs`, comes from separate uncommitted work (`GET /workouts?standalone=true`) and is left for that change to sync.
- [x] 4.2 Run the full `dotnet test WeightTracker.slnx` with Docker running and verify every unit and integration test passes.
- [x] 4.3 Run `openspec validate remove-mesocycle-weeks-and-derive-workout-date --strict` and verify the change is valid.

## Workflow follow-up

- Archive the change after review.
